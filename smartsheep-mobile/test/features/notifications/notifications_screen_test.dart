import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/network/api_client.dart';
import 'package:smartsheep_mobile/core/network/secure_store.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/auth/auth_controller.dart';
import 'package:smartsheep_mobile/features/notifications/notifications_screen.dart';
import 'package:smartsheep_mobile/features/shell/header_identity.dart';

void main() {
  testWidgets(
    'shows API creation date, time, content and explicit read status',
    (tester) async {
      final api = _Api();
      final container = await _pump(tester, api);
      expect(find.text('01 Feb 2025 09:30'), findsOneWidget);
      expect(find.text('Source went offline'), findsOneWidget);
      expect(find.text('Unread'), findsOneWidget);
      expect(find.text('Read'), findsOneWidget);
      expect(container.read(unreadNotificationCountProvider), 1);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('confirmed read persists in list and removes only unread count', (
    tester,
  ) async {
    final api = _Api();
    final container = await _pump(tester, api);
    await tester.tap(find.byKey(const ValueKey('notification-first')));
    await tester.pumpAndSettle();
    expect(api.reads, 1);
    expect(find.byKey(const ValueKey('notification-first')), findsOneWidget);
    expect(find.text('Read'), findsNWidgets(2));
    expect(find.text('Unread'), findsNothing);
    expect(container.read(unreadNotificationCountProvider), 0);
    expect(find.text('01 Feb 2025 09:30'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey('notification-first')));
    await tester.pumpAndSettle();
    expect(api.reads, 1);
  });

  testWidgets('read failure leaves status and count unchanged', (tester) async {
    final api = _Api()..failRead = true;
    final container = await _pump(tester, api);
    await tester.tap(find.byKey(const ValueKey('notification-first')));
    await tester.pumpAndSettle();
    expect(
      find.text('Unable to mark notification as read. Please try again.'),
      findsOneWidget,
    );
    expect(find.text('Unread'), findsOneWidget);
    expect(container.read(unreadNotificationCountProvider), 1);
  });

  testWidgets('duplicate taps do not submit repeated read requests', (
    tester,
  ) async {
    final api = _Api()..pendingRead = Completer<void>();
    await _pump(tester, api);
    await tester.tap(find.byKey(const ValueKey('notification-first')));
    await tester.pump();
    await tester.tap(find.byKey(const ValueKey('notification-first')));
    await tester.pump();
    expect(api.reads, 1);
    expect(find.text('Marking as read…'), findsOneWidget);
    api.pendingRead!.complete();
    await tester.pumpAndSettle();
    expect(find.text('Read'), findsNWidgets(2));
  });

  testWidgets('empty and error states never invent notifications', (
    tester,
  ) async {
    final api = _Api()..items.clear();
    final container = await _pump(tester, api);
    expect(find.text('No notifications yet.'), findsOneWidget);
    expect(container.read(unreadNotificationCountProvider), 0);
    api.failList = true;
    container.invalidate(notificationsProvider);
    await tester.pumpAndSettle();
    expect(find.text('Unable to load notifications.'), findsOneWidget);
    expect(find.text('No notifications yet.'), findsNothing);
    api.failList = false;
    await tester.tap(find.text('Retry'));
    await tester.pumpAndSettle();
    expect(find.text('No notifications yet.'), findsOneWidget);
  });

  testWidgets('camel case dates, unknown dates and large text remain usable', (
    tester,
  ) async {
    final api = _Api();
    api.items[0] = {
      'id': 'first',
      'title': 'Notification',
      'content': 'Message',
      'isRead': false,
      'dateCreated': '2025-02-01T09:30:00',
    };
    api.items[1].remove('DateCreated');
    await _pump(tester, api, width: 320, scale: 2);
    expect(find.text('01 Feb 2025 09:30'), findsOneWidget);
    await tester.scrollUntilVisible(
      find.text('Date unavailable'),
      180,
      scrollable: find.byType(Scrollable).first,
    );
    expect(find.text('Date unavailable'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}

Future<ProviderContainer> _pump(
  WidgetTester tester,
  _Api api, {
  double width = 393,
  double scale = 1,
}) async {
  tester.view.physicalSize = Size(width, 852);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  final container = ProviderContainer(
    retry: (_, _) => null,
    overrides: [
      authControllerProvider.overrideWith(_Auth.new),
      apiClientProvider.overrideWithValue(api),
      headerProfileImageProvider.overrideWith((_) async => null),
    ],
  );
  addTearDown(container.dispose);
  await container.read(authControllerProvider.future);
  await tester.pumpWidget(
    UncontrolledProviderScope(
      container: container,
      child: MaterialApp(
        theme: AppTheme.light,
        builder: (context, child) => MediaQuery(
          data: MediaQuery.of(
            context,
          ).copyWith(textScaler: TextScaler.linear(scale)),
          child: child!,
        ),
        home: const NotificationsScreen(),
      ),
    ),
  );
  await tester.pumpAndSettle();
  return container;
}

class _Auth extends AuthController {
  @override
  Future<AuthSession?> build() async =>
      const AuthSession(token: 'test', username: 'TEST');
}

class _Api extends ApiClient {
  _Api() : super(const SecureStore());
  int reads = 0;
  bool failRead = false;
  bool failList = false;
  Completer<void>? pendingRead;
  final items = <Map<String, dynamic>>[
    {
      'Id': 'first',
      'Title': 'Barn alert',
      'Content': 'Source went offline',
      'DateCreated': '2025-02-01T09:30:00',
      'DateModified': '2026-09-14T12:00:00',
      'IsRead': false,
    },
    {
      'Id': 'second',
      'Title': 'Read alert',
      'DateCreated': '2025-02-01T08:00:00',
      'IsRead': true,
    },
  ];
  @override
  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async {
    if (path == '/api/Auth/SystemNotification/TEST') {
      if (failList) throw StateError('Offline');
      return items.map((item) => Map<String, dynamic>.of(item)).toList();
    }
    if (path == '/api/Auth/ReadNotification/first') {
      reads++;
      if (failRead) throw StateError('Offline');
      await pendingRead?.future;
      items.first['IsRead'] = true;
      return Map<String, dynamic>.of(items.first);
    }
    throw StateError('Unexpected path: $path');
  }
}
