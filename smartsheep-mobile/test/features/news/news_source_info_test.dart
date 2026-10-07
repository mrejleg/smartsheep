import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/news/news_content.dart';
import 'package:smartsheep_mobile/features/news/news_source_info.dart';

const _title =
    'Suara Embikan Domba Bongkar Penyamaran Maling di Cikakak Sukabumi';
const _labels = ['Judul sumber', 'Kategori', 'Tanggal terbit', 'Sumber'];
const _values = [_title, 'Berita', '03 June 2026 13.30', 'detikcom'];
const _article =
    '<p>Isi artikel tetap terbaca dengan rapi.</p>'
    '<hr>'
    '<p><strong>Judul sumber:</strong> $_title<br/>'
    '<strong>Kategori:</strong> Berita<br/>'
    '<strong>Tanggal terbit:</strong> 03 June 2026 13.30<br/>'
    '<strong>Sumber:</strong> '
    '<a href="https://www.detik.com/">detikcom</a></p>';

void main() {
  testWidgets('extracts the HTML source footer into four distinct fields', (
    tester,
  ) async {
    await _pumpArticle(tester);

    expect(find.byType(NewsSourceInfo), findsOneWidget);
    final info = tester.widget<NewsSourceInfo>(find.byType(NewsSourceInfo));
    expect(info.fields.map((field) => field.label), _labels);
    expect(info.fields.map((field) => field.value), _values);
    expect(find.byType(Divider), findsNothing);
    for (final label in _labels) {
      expect(_text(label), findsOneWidget);
    }
    for (final value in _values) {
      expect(_text(value), findsOneWidget);
    }
    expect(
      find.text('Isi artikel tetap terbaca dengan rapi.', findRichText: true),
      findsOneWidget,
    );
    expect(
      find.textContaining('Judul sumber:', findRichText: true),
      findsNothing,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('aligns labels, colons and wrapped values at phone width', (
    tester,
  ) async {
    await _pumpArticle(tester);

    final labelLeft = tester.getTopLeft(_text(_labels.first)).dx;
    final valueLeft = tester.getTopLeft(_text(_values.first)).dx;
    expect(valueLeft, greaterThan(labelLeft));
    for (var index = 0; index < _labels.length; index++) {
      final labelRect = tester.getRect(_text(_labels[index]));
      final valueRect = tester.getRect(_text(_values[index]));
      expect(labelRect.left, closeTo(labelLeft, 0.1));
      expect(valueRect.left, closeTo(valueLeft, 0.1));
      expect(valueRect.top, closeTo(labelRect.top, 0.1));
      expect(labelRect.right, lessThanOrEqualTo(valueRect.left));
      if (index > 0) {
        final previousValue = tester.getRect(_text(_values[index - 1]));
        expect(valueRect.top, greaterThan(previousValue.bottom));
      }
    }

    final colons = _text(':');
    expect(colons, findsNWidgets(4));
    final colonLeft = tester.getTopLeft(colons.first).dx;
    for (var index = 0; index < 4; index++) {
      expect(tester.getTopLeft(colons.at(index)).dx, closeTo(colonLeft, 0.1));
    }
    expect(colonLeft, greaterThan(labelLeft));
    expect(colonLeft, lessThan(valueLeft));
    expect(tester.takeException(), isNull);
  });

  for (final scenario in [
    (width: 320.0, textScale: 1.0),
    (width: 393.0, textScale: 2.0),
  ]) {
    testWidgets('stacks metadata without overflow at width ${scenario.width} '
        'and text scale ${scenario.textScale}', (tester) async {
      await _pumpArticle(
        tester,
        width: scenario.width,
        textScale: scenario.textScale,
      );

      expect(find.byType(NewsSourceInfo), findsOneWidget);
      for (var index = 0; index < _labels.length; index++) {
        final labelRect = tester.getRect(_text(_labels[index]));
        final valueRect = tester.getRect(_text(_values[index]));
        expect(valueRect.left, closeTo(labelRect.left, 0.1));
        expect(valueRect.top, greaterThanOrEqualTo(labelRect.bottom));
        expect(valueRect.left, greaterThanOrEqualTo(0));
        expect(valueRect.right, lessThanOrEqualTo(scenario.width));
        if (index > 0) {
          final previousValue = tester.getRect(_text(_values[index - 1]));
          expect(labelRect.top, greaterThan(previousValue.bottom));
        }
      }
      expect(tester.takeException(), isNull);
    });
  }

  testWidgets('ordinary bold paragraphs remain article content', (
    tester,
  ) async {
    await _pumpArticle(
      tester,
      content:
          '<p><strong>Perawatan rutin:</strong> Periksa kondisi kandang.</p>'
          '<p><strong>Catatan penting:</strong> Sediakan air bersih.</p>',
    );

    expect(find.byType(NewsSourceInfo), findsNothing);
    expect(
      find.text(
        'Perawatan rutin: Periksa kondisi kandang.',
        findRichText: true,
      ),
      findsOneWidget,
    );
    expect(
      find.text('Catatan penting: Sediakan air bersih.', findRichText: true),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
  });
  testWidgets('a single matching bold label remains an ordinary paragraph', (
    tester,
  ) async {
    await _pumpArticle(
      tester,
      content: '<p><strong>Kategori:</strong> Panduan perawatan ternak.</p>',
    );

    expect(find.byType(NewsSourceInfo), findsNothing);
    expect(
      find.text('Kategori: Panduan perawatan ternak.', findRichText: true),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('allows whitespace and newlines between footer field labels', (
    tester,
  ) async {
    await _pumpArticle(
      tester,
      content:
          '<p>Isi artikel.</p>\n<hr/>\n<p>\n  '
          '<strong>Judul sumber:</strong> $_title<br/>\n\t'
          '<strong>Kategori:</strong> Berita<br/>\n  '
          '<strong>Tanggal terbit:</strong> 03 June 2026 13.30<br/>\n'
          '<strong>Sumber:</strong> '
          '<a href="https://www.detik.com/">detikcom</a>\n</p>',
    );

    expect(find.byType(NewsSourceInfo), findsOneWidget);
    final info = tester.widget<NewsSourceInfo>(find.byType(NewsSourceInfo));
    expect(info.fields.map((field) => field.label), _labels);
    expect(info.fields.map((field) => field.value), _values);
    expect(find.byType(Divider), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('preserves prose in a paragraph that mixes labels and content', (
    tester,
  ) async {
    await _pumpArticle(
      tester,
      content:
          '<p><strong>Kategori:</strong> Berita<br>'
          '<strong>Sumber:</strong> detikcom<br>'
          'Keterangan tambahan yang harus tetap terlihat.</p>',
    );

    expect(find.byType(NewsSourceInfo), findsNothing);
    expect(
      find.textContaining(
        'Keterangan tambahan yang harus tetap terlihat.',
        findRichText: true,
      ),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
  });
}

Finder _text(String value) =>
    find.byWidgetPredicate((widget) => widget is Text && widget.data == value);

Future<void> _pumpArticle(
  WidgetTester tester, {
  String content = _article,
  double width = 393,
  double textScale = 1,
}) async {
  tester.view.physicalSize = Size(width, 852);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  await tester.pumpWidget(
    MaterialApp(
      theme: AppTheme.light,
      builder: (context, child) => MediaQuery(
        data: MediaQuery.of(
          context,
        ).copyWith(textScaler: TextScaler.linear(textScale)),
        child: child!,
      ),
      home: Scaffold(
        body: SingleChildScrollView(
          padding: const EdgeInsets.all(20),
          child: NewsContent(content: content),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}
