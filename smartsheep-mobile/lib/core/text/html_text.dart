import 'package:flutter/material.dart';
import 'package:html/dom.dart' as dom;
import 'package:html/parser.dart' as html_parser;

/// Converts the small HTML fragments the server sends for notification and
/// reminder bodies (`<p>`, `<strong>`, `<em>`, `<br>`) into styled spans.
///
/// Tags become styling instead of literal text, so a reminder never shows its
/// own markup. Anything unrecognised degrades to its text content, and a body
/// with no markup at all passes through untouched.
InlineSpan htmlToSpan(String body) {
  final source = body.trim();
  if (source.isEmpty) return const TextSpan();
  // Plain-text bodies (no markup) render as-is.
  if (!source.contains('<')) return TextSpan(text: source);

  final children = <InlineSpan>[];
  final buffer = StringBuffer();

  void flush(TextStyle? style) {
    if (buffer.isEmpty) return;
    children.add(TextSpan(text: buffer.toString(), style: style));
    buffer.clear();
  }

  // Collapses runs of whitespace the way an HTML renderer would, so the
  // pretty-printed markup from the server does not leak stray indentation.
  void writeText(String text) {
    final collapsed = text.replaceAll(RegExp(r'\s+'), ' ');
    if (collapsed.isEmpty) return;
    final atLineStart = buffer.isEmpty || buffer.toString().endsWith('\n');
    if (collapsed == ' ' && atLineStart) return;
    buffer.write(atLineStart ? collapsed.trimLeft() : collapsed);
  }

  void breakLine({bool paragraph = false}) {
    final text = buffer.toString();
    if (text.isEmpty && children.isEmpty) return;
    if (text.endsWith('\n\n')) return;
    if (paragraph && text.endsWith('\n')) return;
    buffer.write('\n');
  }

  void visit(dom.Node node, TextStyle? style) {
    if (node is dom.Text) {
      writeText(node.text);
      return;
    }
    if (node is! dom.Element) return;

    final tag = node.localName?.toLowerCase();
    var next = style;
    switch (tag) {
      case 'br':
        flush(style);
        buffer.write('\n');
        return;
      case 'strong':
      case 'b':
        next = (style ?? const TextStyle()).copyWith(
          fontWeight: FontWeight.w700,
        );
      case 'em':
      case 'i':
        next = (style ?? const TextStyle()).copyWith(
          fontStyle: FontStyle.italic,
        );
      case 'u':
        next = (style ?? const TextStyle()).copyWith(
          decoration: TextDecoration.underline,
        );
      case 'p':
      case 'div':
      case 'li':
      case 'ul':
      case 'ol':
      case 'h1':
      case 'h2':
      case 'h3':
        flush(style);
        breakLine(paragraph: true);
      default:
        break;
    }

    if (next != style) flush(style);
    for (final child in node.nodes) {
      visit(child, next);
    }
    if (next != style) flush(next);

    if (tag == 'p' ||
        tag == 'div' ||
        tag == 'li' ||
        tag == 'h1' ||
        tag == 'h2' ||
        tag == 'h3') {
      flush(style);
      breakLine(paragraph: true);
    }
  }

  for (final node in html_parser.parseFragment(source).nodes) {
    visit(node, null);
  }
  flush(null);

  if (children.isEmpty) return TextSpan(text: source);

  // Drop trailing blank lines produced by the final closing block tag.
  final last = children.last;
  if (last is TextSpan && last.text != null) {
    final trimmed = last.text!.replaceFirst(RegExp(r'\s+$'), '');
    if (trimmed.isEmpty) {
      children.removeLast();
    } else {
      children[children.length - 1] = TextSpan(
        text: trimmed,
        style: last.style,
      );
    }
  }

  return TextSpan(children: children);
}
