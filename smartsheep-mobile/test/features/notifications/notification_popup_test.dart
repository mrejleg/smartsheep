import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/notifications/notification_popup.dart';

void main() {
  testWidgets('popup appears above detail and filter without closing either', (
    tester,
  ) async {
    final key = GlobalKey<NavigatorState>();
    await tester.pumpWidget(
      MaterialApp(
        navigatorKey: key,
        theme: AppTheme.light,
        home: const Scaffold(body: Text('Home')),
      ),
    );
    key.currentState!.push(
      MaterialPageRoute<void>(
        builder: (_) => const Scaffold(body: Text('Detail page')),
      ),
    );
    await tester.pumpAndSettle();
    showModalBottomSheet<void>(
      context: key.currentContext!,
      builder: (_) => const SizedBox(height: 200, child: Text('Filter sheet')),
    );
    await tester.pumpAndSettle();
    NotificationPopupPresenter(key).receive(
      const RemoteMessage(
        notification: RemoteNotification(title: 'Global notification'),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Global notification'), findsOneWidget);
    await tester.tap(find.text('Got it'));
    await tester.pumpAndSettle();
    expect(find.text('Filter sheet'), findsOneWidget);
    key.currentState!.pop();
    await tester.pumpAndSettle();
    expect(find.text('Detail page'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('popup uses app theme, message and local date, and dismisses', (
    tester,
  ) async {
    final key = GlobalKey<NavigatorState>();
    await tester.pumpWidget(
      MaterialApp(
        navigatorKey: key,
        theme: AppTheme.light,
        home: const Scaffold(),
      ),
    );
    final presenter = NotificationPopupPresenter(key);
    presenter.receive(
      RemoteMessage(
        messageId: 'test',
        sentTime: DateTime(2026, 9, 14, 19, 30),
        data: const {'type': 'reminder-test'},
        notification: const RemoteNotification(
          title: 'Low Suckling Activity',
          body: 'Please check the barn.',
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Reminder'), findsOneWidget);
    expect(find.text('Low Suckling Activity'), findsOneWidget);
    expect(find.text('Please check the barn.'), findsOneWidget);
    expect(find.text('14 Sep 2026 19:30'), findsOneWidget);
    final button = tester.widget<FilledButton>(find.byType(FilledButton));
    expect(button.style!.backgroundColor!.resolve({}), AppColors.reminder);
    await tester.tap(find.text('Got it'));
    await tester.pumpAndSettle();
    expect(find.byType(NotificationPopup), findsNothing);
  });

  testWidgets(
    'queues messages without stacking and ignores duplicates and empty push',
    (tester) async {
      final key = GlobalKey<NavigatorState>();
      await tester.pumpWidget(
        MaterialApp(
          navigatorKey: key,
          theme: AppTheme.light,
          home: const Scaffold(),
        ),
      );
      final presenter = NotificationPopupPresenter(key);
      const first = RemoteMessage(
        messageId: '1',
        notification: RemoteNotification(title: 'First'),
      );
      presenter.receive(const RemoteMessage(messageId: 'empty'));
      presenter.receive(first);
      presenter.receive(first);
      presenter.receive(
        const RemoteMessage(
          messageId: '2',
          data: {'title': 'Second', 'body': 'Message from data'},
        ),
      );
      await tester.pumpAndSettle();
      expect(find.byType(NotificationPopup), findsOneWidget);
      expect(find.text('First'), findsOneWidget);
      expect(find.text('Second'), findsNothing);
      await tester.tap(find.byTooltip('Close notification'));
      await tester.pumpAndSettle();
      expect(find.text('Second'), findsOneWidget);
      expect(find.text('Message from data'), findsOneWidget);
      await tester.tap(find.text('Got it'));
      await tester.pumpAndSettle();
      expect(find.byType(NotificationPopup), findsNothing);
    },
  );

  testWidgets('long text on small screen with large font remains scrollable', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(360, 640);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final key = GlobalKey<NavigatorState>();
    await tester.pumpWidget(
      MaterialApp(
        navigatorKey: key,
        theme: AppTheme.light,
        builder: (context, child) => MediaQuery(
          data: MediaQuery.of(
            context,
          ).copyWith(textScaler: const TextScaler.linear(1.8)),
          child: child!,
        ),
        home: const Scaffold(),
      ),
    );
    NotificationPopupPresenter(key).receive(
      RemoteMessage(
        notification: RemoteNotification(
          title: 'An important message with a long title',
          body: 'Check the barn and livestock. ' * 80,
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(find.byType(SingleChildScrollView), findsOneWidget);
    await tester.tap(find.text('Got it'));
    await tester.pumpAndSettle();
    expect(find.byType(NotificationPopup), findsNothing);
  });

  testWidgets('popup renders body_html while the tray gets plain text', (
    tester,
  ) async {
    final key = GlobalKey<NavigatorState>();
    await tester.pumpWidget(
      MaterialApp(
        navigatorKey: key,
        theme: AppTheme.light,
        home: const Scaffold(),
      ),
    );
    NotificationPopupPresenter(key).receive(
      RemoteMessage(
        messageId: 'html',
        sentTime: DateTime(2026, 9, 18, 0, 35),
        data: const {
          'type': 'reminder',
          'body_html': '<p><strong>Frekuensi Pendekatan: </strong>12 kali</p>',
        },
        // What the API now puts in the system notification.
        notification: const RemoteNotification(
          title: 'Low Suckling Activity',
          body: 'Frekuensi Pendekatan: 12 kali',
        ),
      ),
    );
    await tester.pumpAndSettle();

    final body = tester
        .widgetList<RichText>(find.byType(RichText))
        .firstWhere((w) => w.text.toPlainText().contains('Frekuensi'));
    expect(body.text.toPlainText(), 'Frekuensi Pendekatan: 12 kali');

    // The markup, not the plain fallback, was used: the label is bold.
    var bold = false;
    body.text.visitChildren((span) {
      if (span is TextSpan &&
          span.text?.contains('Frekuensi') == true &&
          span.style?.fontWeight == FontWeight.w700) {
        bold = true;
      }
      return true;
    });
    expect(bold, isTrue);
  });
}
