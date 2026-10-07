import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/news/news_content.dart';
import 'package:smartsheep_mobile/features/news/news_detail_screen.dart';
import 'package:smartsheep_mobile/features/news/news_item.dart';
import 'package:smartsheep_mobile/features/news/news_thumbnail.dart';

void main() {
  test(
    'extracts category and removes source metadata without changing article paragraphs',
    () {
      final article = newsArticle(
        '<p>Article body.</p><hr>'
        '<p><strong>Kategori:</strong> Berita<br>'
        '<strong>Sumber:</strong> example.org</p>',
      );
      expect(article.category, 'Berita');
      expect(article.content, '<p>Article body.</p>');
      final ordinary = newsArticle(
        '<p><strong>Kategori:</strong> Ordinary paragraph.</p>',
      );
      expect(ordinary.category, isNull);
      expect(ordinary.content, contains('Ordinary paragraph.'));
    },
  );

  testWidgets(
    'category replaces NEWS beside the date and the information footer is absent',
    (tester) async {
      _setPhoneSize(tester);
      await tester.pumpWidget(
        ProviderScope(
          child: MaterialApp(
            theme: AppTheme.light,
            home: NewsDetailScreen(
              item: _news(
                content:
                    '<p>Article body.</p><hr>'
                    '<p><strong>Judul sumber:</strong> Original title<br>'
                    '<strong>Kategori:</strong> Berita<br>'
                    '<strong>Tanggal terbit:</strong> 14 September 2026 17:39<br>'
                    '<strong>Sumber:</strong> example.org</p>',
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      final category = find.byKey(const ValueKey('news-detail-category'));
      final title = find.text('Healthy sheep & goats');
      final date = find.text('14 Sep 2026 17:39');
      expect(find.text('Berita'), findsOneWidget);
      expect(find.text('NEWS'), findsNothing);
      expect(tester.getCenter(category).dy, tester.getCenter(date).dy);
      expect(
        tester.getTopLeft(category).dy,
        lessThan(tester.getTopLeft(title).dy),
      );
      expect(
        tester.getTopLeft(category).dx,
        lessThan(tester.getTopLeft(date).dx),
      );
      expect(find.text('Informasi berita'), findsNothing);
      expect(find.byKey(const ValueKey('news-source-info')), findsNothing);
      expect(
        find.textContaining('example.org', findRichText: true),
        findsNothing,
      );
      expect(find.text('Article body.', findRichText: true), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  test('preview text decodes entities and separates adjacent HTML blocks', () {
    expect(
      newsPlainText(
        '<p>Sheep &amp; goats</p><p>Healthy <b>flock</b><br>today.</p>'
        '<script>doNotShow()</script><style>.hidden {color:red}</style>',
      ),
      'Sheep & goats Healthy flock today.',
    );
  });

  testWidgets('shows full article, publication date, and lead image', (
    tester,
  ) async {
    _setPhoneSize(tester);
    final requestedImages = <String>[];
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          newsThumbnailBytesProvider.overrideWith((ref, path) async {
            requestedImages.add(path);
            return base64Decode(_png);
          }),
        ],
        child: MaterialApp(
          theme: AppTheme.light,
          home: NewsDetailScreen(
            item: _news(
              content:
                  '<p>Full article &amp; information.</p>'
                  '<p>Second paragraph with <strong>healthy sheep</strong>.</p>',
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('News Detail'), findsOneWidget);
    expect(find.text('Healthy sheep & goats'), findsOneWidget);
    expect(find.text('14 Sep 2026 17:39'), findsOneWidget);
    expect(
      find.text('Full article & information.', findRichText: true),
      findsOneWidget,
    );
    expect(
      find.text('Second paragraph with healthy sheep.', findRichText: true),
      findsOneWidget,
    );
    expect(find.text('Preview only'), findsNothing);
    expect(requestedImages, ['/uploads/image/news-1.jpg']);
    expect(
      find.descendant(
        of: find.byKey(const ValueKey('news-detail-image')),
        matching: find.byType(Image),
      ),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'renders formatted text, lists, and inline images as native widgets',
    (tester) async {
      _setPhoneSize(tester);
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            newsThumbnailBytesProvider.overrideWith((ref, path) async {
              return base64Decode(_png);
            }),
          ],
          child: MaterialApp(
            home: Scaffold(
              body: SingleChildScrollView(
                child: NewsContent(
                  content:
                      '<h2>Article heading</h2>'
                      '<p>A <strong>bold</strong> and <em>italic</em> paragraph.</p>'
                      '<ul><li>First point</li><li>Second point</li></ul>'
                      '<ol start="3"><li>Third point</li></ol>'
                      '<p>Before image<img src="/uploads/body.jpg" alt="A sheep">'
                      'After image</p>'
                      '<script>doNotShow()</script>'
                      '<iframe src="https://example.org">Hidden frame</iframe>'
                      '<img src="javascript:invalid()">',
                ),
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Article heading', findRichText: true), findsOneWidget);
      expect(find.text('First point', findRichText: true), findsOneWidget);
      expect(find.text('Second point', findRichText: true), findsOneWidget);
      expect(find.text('3.'), findsOneWidget);
      expect(find.text('Before image', findRichText: true), findsOneWidget);
      expect(find.text('After image', findRichText: true), findsOneWidget);
      expect(find.byType(NewsThumbnail), findsOneWidget);
      final image = tester.widget<NewsThumbnail>(find.byType(NewsThumbnail));
      expect(image.imagePath, '/uploads/body.jpg');
      expect(image.semanticLabel, 'A sheep');
      expect(
        find.textContaining('doNotShow', findRichText: true),
        findsNothing,
      );
      expect(
        find.textContaining('Hidden frame', findRichText: true),
        findsNothing,
      );

      final paragraph = tester.widget<Text>(
        find.byWidgetPredicate(
          (widget) =>
              widget is Text &&
              widget.textSpan?.toPlainText() == 'A bold and italic paragraph.',
        ),
      );
      final spans = (paragraph.textSpan! as TextSpan).children!
          .cast<TextSpan>();
      expect(
        spans.firstWhere((span) => span.text == 'bold').style?.fontWeight,
        FontWeight.w700,
      );
      expect(
        spans.firstWhere((span) => span.text == 'italic').style?.fontStyle,
        FontStyle.italic,
      );
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'empty article falls back to summary and explains missing content',
    (tester) async {
      _setPhoneSize(tester);
      await tester.pumpWidget(
        ProviderScope(
          child: MaterialApp(
            home: NewsDetailScreen(
              item: _news(content: '<p><br></p>', imagePath: null),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Preview only'), findsOneWidget);
      expect(
        find.text('Full article content is not available yet.'),
        findsOneWidget,
      );
      expect(find.byIcon(Icons.newspaper_outlined), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('failed image keeps article readable and displays placeholder', (
    tester,
  ) async {
    _setPhoneSize(tester);
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          newsThumbnailBytesProvider.overrideWith(
            (ref, path) async => throw StateError('Image unavailable'),
          ),
        ],
        child: MaterialApp(
          home: NewsDetailScreen(
            item: _news(content: '<p>Article remains.</p>'),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Article remains.', findRichText: true), findsOneWidget);
    expect(find.byIcon(Icons.newspaper_outlined), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('long article scrolls to its final paragraph', (tester) async {
    _setPhoneSize(tester, height: 850);
    final content = List.generate(
      30,
      (index) => '<p>Paragraph $index about sheep health and farm care.</p>',
    ).join();
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp(
          home: NewsDetailScreen(
            item: _news(content: content, imagePath: null),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
    final lastParagraph = find.text(
      'Paragraph 29 about sheep health and farm care.',
      findRichText: true,
    );
    expect(lastParagraph.hitTestable(), findsNothing);

    await tester.scrollUntilVisible(lastParagraph, 500);
    await tester.pumpAndSettle();

    expect(lastParagraph.hitTestable(), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('plain-text articles retain paragraph breaks', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: NewsContent(content: 'First paragraph.\n\nSecond &amp; final.'),
        ),
      ),
    );
    expect(find.text('First paragraph.\n\nSecond & final.'), findsOneWidget);
  });
}

void _setPhoneSize(WidgetTester tester, {double height = 1400}) {
  tester.view.physicalSize = Size(430, height);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
}

NewsItem _news({
  required String content,
  String? imagePath = '/uploads/image/news-1.jpg',
}) => NewsItem(
  id: 'news-1',
  title: 'Healthy sheep &amp; goats',
  shortContent: 'Preview only',
  content: content,
  imageThumbnail: imagePath,
  startDate: DateTime(2026, 9, 14, 17, 39),
  dateCreated: DateTime(2026, 9, 13),
  createdBy: 'admin',
);

const _png =
    'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=';
