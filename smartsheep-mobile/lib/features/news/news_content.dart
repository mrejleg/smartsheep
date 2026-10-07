import 'package:flutter/material.dart';
import 'package:html/dom.dart' as dom;
import 'package:html/parser.dart' as html;

import '../../core/theme/app_theme.dart';
import 'news_source_info.dart';
import 'news_thumbnail.dart';

const _hiddenTags = {
  'script',
  'style',
  'iframe',
  'object',
  'embed',
  'form',
  'input',
  'button',
  'select',
  'textarea',
  'link',
  'meta',
  'head',
  'noscript',
  'svg',
};

const _blockTags = {
  'address',
  'article',
  'aside',
  'blockquote',
  'div',
  'figcaption',
  'figure',
  'footer',
  'h1',
  'h2',
  'h3',
  'h4',
  'h5',
  'h6',
  'header',
  'li',
  'main',
  'ol',
  'p',
  'pre',
  'section',
  'table',
  'tr',
  'ul',
};

dom.DocumentFragment _parseContent(String value) {
  final fragment = html.parseFragment(value);
  for (final element in fragment.querySelectorAll(_hiddenTags.join(','))) {
    element.remove();
  }
  return fragment;
}

/// Text-only news previews, with entities decoded and block boundaries retained
/// as spaces. Non-content HTML such as scripts is never displayed.
String newsPlainText(String value) {
  final result = StringBuffer();

  void visit(dom.Node node) {
    if (node is dom.Text) {
      result.write(node.data);
      return;
    }
    final tag = node is dom.Element ? node.localName : null;
    if (tag == 'br' || _blockTags.contains(tag)) result.write(' ');
    for (final child in node.nodes) {
      visit(child);
    }
    if (_blockTags.contains(tag)) result.write(' ');
  }

  visit(_parseContent(value));
  return result.toString().replaceAll(RegExp(r'\s+'), ' ').trim();
}

/// Moves the recognized source footer out of the detail article without
/// changing the stored HTML or removing ordinary editorial paragraphs.
({String content, String? category}) newsArticle(String value) {
  final fragment = _parseContent(value);
  String? category;
  for (final paragraph in fragment.querySelectorAll('p')) {
    final fields = _sourceFields(paragraph);
    if (fields == null) continue;
    for (final field in fields) {
      if (field.label == 'Kategori' && field.value != '—') {
        category ??= field.value;
      }
    }
    if (paragraph.previousElementSibling?.localName == 'hr') {
      paragraph.previousElementSibling!.remove();
    }
    paragraph.remove();
  }
  return (content: fragment.outerHtml, category: category);
}

/// A deliberately native, non-interactive HTML article reader. It renders the
/// editorial text and images without executing scripts or embedding web views.
class NewsContent extends StatelessWidget {
  const NewsContent({super.key, required this.content, this.summary = ''});

  final String content;
  final String summary;

  @override
  Widget build(BuildContext context) {
    const bodyStyle = TextStyle(
      color: AppColors.ink,
      fontSize: 16,
      height: 1.7,
    );
    final blocks = _contentBlocks(_parseContent(content).nodes, bodyStyle);
    if (blocks.isEmpty) {
      final preview = newsPlainText(summary);
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (preview.isNotEmpty) ...[
            Text(preview, style: bodyStyle),
            const SizedBox(height: 16),
          ],
          const Text(
            'Full article content is not available yet.',
            style: TextStyle(color: AppColors.muted, height: 1.5),
          ),
        ],
      );
    }
    return SelectionArea(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: blocks,
      ),
    );
  }
}

List<Widget> _contentBlocks(Iterable<dom.Node> nodes, TextStyle style) {
  if (nodes.every((node) => node is dom.Text)) {
    final text = nodes.cast<dom.Text>().map((node) => node.data).join().trim();
    return text.isEmpty
        ? []
        : [
            Padding(
              padding: const EdgeInsets.only(bottom: 14),
              child: Text(text, style: style),
            ),
          ];
  }
  final blocks = <Widget>[];
  var spans = <InlineSpan>[];

  void flush() {
    if (spans.isNotEmpty &&
        TextSpan(children: spans).toPlainText().trim().isNotEmpty) {
      blocks.add(
        Padding(
          padding: const EdgeInsets.only(bottom: 14),
          child: Text.rich(TextSpan(children: spans), style: style),
        ),
      );
    }
    spans = [];
  }

  void visit(dom.Node node, TextStyle currentStyle) {
    if (node is dom.Text) {
      spans.add(
        TextSpan(
          text: node.data.replaceAll(RegExp(r'[\t\r\n ]+'), ' '),
          style: currentStyle,
        ),
      );
      return;
    }
    if (node is! dom.Element) return;
    final tag = node.localName;
    if (_hiddenTags.contains(tag)) return;
    final sourceFields = _sourceFields(node);
    if (sourceFields != null) {
      flush();
      // The API separates this footer with <hr>; the card supplies its own
      // boundary and spacing instead of leaving an extra loose divider.
      if (blocks.isNotEmpty && blocks.last is Divider) blocks.removeLast();
      blocks.add(
        Padding(
          padding: const EdgeInsets.only(top: 10, bottom: 14),
          child: NewsSourceInfo(fields: sourceFields),
        ),
      );
      return;
    }
    if (tag == 'br') {
      spans.add(const TextSpan(text: '\n'));
      return;
    }
    if (tag == 'img') {
      final path = _imagePath(node.attributes['src']);
      if (path == null) return;
      flush();
      blocks.add(
        Padding(
          padding: const EdgeInsets.only(bottom: 16),
          child: AspectRatio(
            aspectRatio: 16 / 9,
            child: NewsThumbnail(
              imagePath: path,
              width: double.infinity,
              height: double.infinity,
              fit: BoxFit.contain,
              semanticLabel: node.attributes['alt'],
            ),
          ),
        ),
      );
      return;
    }
    if (tag == 'hr') {
      flush();
      blocks.add(const Divider(height: 28));
      return;
    }
    if (tag == 'ul' || tag == 'ol') {
      flush();
      var number = int.tryParse(node.attributes['start'] ?? '') ?? 1;
      for (final child in node.children.where(
        (child) => child.localName == 'li',
      )) {
        final itemBlocks = _contentBlocks(child.nodes, currentStyle);
        if (itemBlocks.isEmpty) continue;
        blocks.add(
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              SizedBox(
                width: 28,
                child: Text(tag == 'ol' ? '${number++}.' : '•', style: style),
              ),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: itemBlocks,
                ),
              ),
            ],
          ),
        );
      }
      return;
    }
    final block = _blockTags.contains(tag);
    if (block) flush();
    var nextStyle = currentStyle;
    if (tag == 'b' || tag == 'strong') {
      nextStyle = nextStyle.copyWith(fontWeight: FontWeight.w700);
    } else if (tag == 'i' || tag == 'em') {
      nextStyle = nextStyle.copyWith(fontStyle: FontStyle.italic);
    } else if (tag == 'u') {
      nextStyle = nextStyle.copyWith(decoration: TextDecoration.underline);
    } else if (tag == 's' || tag == 'del') {
      nextStyle = nextStyle.copyWith(decoration: TextDecoration.lineThrough);
    } else if (tag == 'blockquote') {
      nextStyle = nextStyle.copyWith(
        color: AppColors.muted,
        fontStyle: FontStyle.italic,
      );
    } else if (tag != null && RegExp(r'^h[1-6]$').hasMatch(tag)) {
      nextStyle = nextStyle.copyWith(
        fontSize: tag == 'h1' ? 24 : (tag == 'h2' ? 21 : 18),
        fontWeight: FontWeight.w700,
        height: 1.4,
      );
    }
    for (final child in node.nodes) {
      visit(child, nextStyle);
    }
    if (tag == 'td' || tag == 'th') spans.add(const TextSpan(text: '  '));
    if (block) flush();
  }

  for (final node in nodes) {
    visit(node, style);
  }
  flush();
  return blocks;
}

List<NewsSourceField>? _sourceFields(dom.Element element) {
  if (element.localName != 'p') return null;
  const labels = {
    'judul sumber': 'Judul sumber',
    'kategori': 'Kategori',
    'tanggal terbit': 'Tanggal terbit',
    'sumber': 'Sumber',
  };
  final lines = <List<dom.Node>>[[]];
  for (final node in element.nodes) {
    if (node is dom.Element && node.localName == 'br') {
      lines.add([]);
    } else {
      lines.last.add(node);
    }
  }

  final fields = <NewsSourceField>[];
  final seen = <String>{};
  for (final line in lines) {
    final nodes = line
        .skipWhile((node) => node is dom.Text && node.data.trim().isEmpty)
        .toList();
    if (nodes.isEmpty) continue;
    final first = nodes.first;
    if (first is! dom.Element ||
        (first.localName != 'strong' && first.localName != 'b')) {
      return null;
    }
    final label =
        labels[first.text
            .trim()
            .replaceFirst(RegExp(r':\s*$'), '')
            .toLowerCase()];
    if (label == null || !seen.add(label)) return null;
    final value = nodes
        .skip(1)
        .map((node) => node.text ?? '')
        .join()
        .replaceAll(RegExp(r'\s+'), ' ')
        .trim();
    fields.add(
      NewsSourceField(label: label, value: value.isEmpty ? '—' : value),
    );
  }
  // Ordinary article paragraphs with a bold opening remain article content.
  return fields.length >= 2 ? fields : null;
}

String? _imagePath(String? source) {
  final value = source?.trim() ?? '';
  if (value.isEmpty) return null;
  final uri = Uri.tryParse(value);
  if (uri == null ||
      (uri.hasScheme && uri.scheme != 'http' && uri.scheme != 'https')) {
    return null;
  }
  return value.startsWith('//') ? 'https:$value' : value;
}
