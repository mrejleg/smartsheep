import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/text/html_text.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/notifications/notification_popup.dart';

/// The reminder template the server sends for a low-suckling alert.
const _reminderHtml =
    '<p><strong>Frekuensi Pendekatan: </strong>12 <strong>kali</strong></p>'
    '<p><strong>Jumlah Aktivitas Menyusu: </strong>8 <strong>kali</strong></p>'
    '<p><strong>Durasi Aktivitas Menyusu: </strong>45 <strong>menit</strong></p>'
    '<p><strong>Jumlah Pendekatan Gagal: </strong>4 <strong>kali</strong></p>'
    '<p><strong>Jeda Waktu Terpanjang: </strong>2 <strong>menit</strong></p>'
    '<p><br></p>'
    '<p><strong>Silahkan lakukan pengecekan kandang B-07</strong></p>';

/// Reads back every character the popup actually painted for the body.
String _renderedBody(WidgetTester tester) {
  final richTexts = tester.widgetList<RichText>(find.byType(RichText));
  for (final richText in richTexts) {
    final text = richText.text.toPlainText();
    if (text.contains('Frekuensi Pendekatan')) return text;
  }
  fail('The reminder body was never rendered.');
}

Future<void> _pumpPopup(WidgetTester tester, String body) async {
  await tester.pumpWidget(
    MaterialApp(
      theme: AppTheme.light,
      home: Scaffold(
        body: NotificationPopup(
          title: 'Low Suckling Activity',
          body: body,
          receivedAt: DateTime(2026, 9, 17, 18, 40),
          isReminder: true,
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('reminder body renders HTML instead of printing its tags', (
    tester,
  ) async {
    await _pumpPopup(tester, _reminderHtml);
    final rendered = _renderedBody(tester);

    // The defect the user reported: raw markup leaking into the popup.
    expect(rendered, isNot(contains('<')));
    expect(rendered, isNot(contains('>')));
    expect(rendered, isNot(contains('strong')));
    expect(rendered, isNot(contains('<br>')));

    // The content itself survives the conversion.
    expect(rendered, contains('Frekuensi Pendekatan:'));
    expect(rendered, contains('Jumlah Aktivitas Menyusu:'));
    expect(rendered, contains('Durasi Aktivitas Menyusu:'));
    expect(rendered, contains('Jumlah Pendekatan Gagal:'));
    expect(rendered, contains('Jeda Waktu Terpanjang:'));
    expect(rendered, contains('Silahkan lakukan pengecekan kandang B-07'));
    expect(rendered, contains('12'));
  });

  testWidgets('each <p> becomes its own line', (tester) async {
    await _pumpPopup(tester, _reminderHtml);
    final lines = _renderedBody(tester)
        .split('\n')
        .map((line) => line.trim())
        .where((line) => line.isNotEmpty)
        .toList();

    expect(lines.length, 6);
    expect(lines.first, 'Frekuensi Pendekatan: 12 kali');
    expect(lines.last, 'Silahkan lakukan pengecekan kandang B-07');
    // No line ends in a stray space left over from a closing tag.
    for (final line in lines) {
      expect(line, isNot(endsWith(' ')));
    }
  });

  testWidgets('<strong> renders as bold rather than as text', (tester) async {
    await _pumpPopup(tester, _reminderHtml);
    final richText = tester
        .widgetList<RichText>(find.byType(RichText))
        .firstWhere(
          (widget) => widget.text.toPlainText().contains('Frekuensi'),
        );

    final bold = <String>[];
    richText.text.visitChildren((span) {
      if (span is TextSpan && span.text != null) {
        if (span.style?.fontWeight == FontWeight.w700) bold.add(span.text!);
      }
      return true;
    });

    expect(bold, isNotEmpty, reason: '<strong> should produce bold spans');
    expect(bold.join(' '), contains('Frekuensi Pendekatan:'));
  });

  testWidgets('a plain-text body still renders unchanged', (tester) async {
    await _pumpPopup(tester, 'Frekuensi Pendekatan turun drastis hari ini.');
    expect(
      _renderedBody(tester),
      'Frekuensi Pendekatan turun drastis hari ini.',
    );
  });

  _listRenderingTests();
}

/// The notification list shows the same server HTML, so it must strip markup
/// too — otherwise the tags simply reappear one screen over.
void _listRenderingTests() {
  testWidgets('list rows render HTML content without showing tags', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Scaffold(
          body: Text.rich(
            htmlToSpan(_reminderHtml),
            maxLines: 3,
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    final rendered = tester
        .widget<RichText>(find.byType(RichText))
        .text
        .toPlainText();
    expect(rendered, isNot(contains('<')));
    expect(rendered, contains('Frekuensi Pendekatan:'));
  });
}
