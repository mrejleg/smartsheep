import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/network/api_client.dart';
import 'package:smartsheep_mobile/core/network/secure_store.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/auth/auth_controller.dart';
import 'package:smartsheep_mobile/features/favorites/favorite_menu.dart';
import 'package:smartsheep_mobile/features/favorites/favorites_controller.dart';
import 'package:smartsheep_mobile/features/home/home_screen.dart';
import 'package:smartsheep_mobile/features/news/news_data.dart';
import 'package:smartsheep_mobile/features/reminders/reminders_screen.dart';
import 'package:smartsheep_mobile/features/shell/header_identity.dart';
import 'package:smartsheep_mobile/features/shell/member_header.dart';

void main() {
  for (final view in ReminderView.values) {
    testWidgets(
      '$view opens correct database endpoint from Home reminder menu',
      (tester) async {
        final api = _Api();
        await _pump(tester, api, view, home: true);
        await tester.tap(find.text('My reminder'));
        await tester.pumpAndSettle();
        expect(find.byType(RemindersScreen), findsOneWidget);
        expect(api.paths.single, switch (view) {
          ReminderView.templates => '/api/ReminderTemplate/All',
          ReminderView.logs => '/api/ReminderLog/All',
          ReminderView.report => '/api/SmartSheepReport/Reminders',
        });
        expect(
          find.text('Database reminder message, not a notification.'),
          findsOneWidget,
        );
        expect(find.text('Low activity'), findsOneWidget);
        if (view != ReminderView.templates) {
          expect(find.text('Status: Failed'), findsOneWidget);
          expect(find.text('Scheduled: 01 Feb 2025 09:30'), findsOneWidget);
          expect(find.text('Delivery error from DB'), findsOneWidget);
        }
        expect(
          find.descendant(
            of: find.byType(MemberHeader).last,
            matching: find.byKey(const ValueKey('reminders-refresh')),
          ),
          findsNothing,
        );
        expect(tester.takeException(), isNull);
      },
    );
  }

  testWidgets(
    'empty, failed and refreshed data never display sample reminders',
    (tester) async {
      final api = _Api()..rows = [];
      await _pump(tester, api, ReminderView.templates);
      expect(find.text('No reminder data available.'), findsOneWidget);
      api.fail = true;
      expect(find.byKey(const ValueKey('reminders-refresh')), findsNothing);
      await tester
          .widget<RefreshIndicator>(find.byType(RefreshIndicator))
          .onRefresh();
      await tester.pumpAndSettle();
      expect(
        find.text(
          'Unable to load reminders. Please check your connection or access.',
        ),
        findsOneWidget,
      );
      expect(find.text('No reminder data available.'), findsNothing);
      api.fail = false;
      api.rows = [
        {..._template, 'ReminderText': 'Updated database message'},
      ];
      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();
      expect(find.text('Updated database message'), findsOneWidget);
    },
  );

  testWidgets('inactive templates are excluded and text fits at 320px / 2x', (
    tester,
  ) async {
    final api = _Api()
      ..rows = [
        _template,
        {
          ..._template,
          'Id': 'inactive',
          'Name': 'Inactive template',
          'IsActive': false,
        },
      ];
    await _pump(tester, api, ReminderView.templates, large: true);
    expect(find.text('Inactive template'), findsNothing);
    expect(
      find.text('Database reminder message, not a notification.'),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'report uses server paging and the extra row only as next-page signal',
    (tester) async {
      final api = _Api()
        ..rows = [
          for (var i = 0; i < 21; i++) {..._log, 'Id': '$i'},
        ];
      await _pump(tester, api, ReminderView.report);
      expect(api.query, {'page': 1, 'pageSize': 20});
      await tester.scrollUntilVisible(
        find.byTooltip('Next page'),
        500,
        scrollable: find.byType(Scrollable).first,
        maxScrolls: 60,
      );
      expect(find.byKey(const ValueKey('reminder-20')), findsNothing);
      await tester.tap(find.byTooltip('Next page'));
      await tester.pumpAndSettle();
      expect(api.query, {'page': 2, 'pageSize': 20});
      expect(tester.takeException(), isNull);
    },
  );
}

const _template = {
  'Id': 'template',
  'Name': 'Low activity',
  'Code': 'LOW',
  'ReminderText': 'Database reminder message, not a notification.',
  'IsActive': true,
};
const _log = {
  'Id': 'log',
  'ReminderTemplate': _template,
  'Username': 'TEST',
  'Status': 'Failed',
  'ScheduledAt': '2025-02-01T09:30:00',
  'ErrorMessage': 'Delivery error from DB',
  'IsActive': true,
};

Future<void> _pump(
  WidgetTester tester,
  _Api api,
  ReminderView view, {
  bool home = false,
  bool large = false,
}) async {
  tester.view.physicalSize = Size(large ? 320 : 393, 852);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  final menu = MobileFavoriteMenu(
    id: 'reminder',
    name: 'My reminder',
    controller: switch (view) {
      ReminderView.templates => 'ReminderTemplate',
      ReminderView.logs => 'ReminderLog',
      ReminderView.report => 'ReminderReport',
    },
    sequenceNumber: 'a',
    type: FavoriteMenuType.reminders,
  );
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        apiClientProvider.overrideWithValue(api),
        authControllerProvider.overrideWith(_Auth.new),
        headerProfileImageProvider.overrideWith((_) async => null),
        unreadNotificationCountProvider.overrideWithValue(0),
        newsItemsProvider.overrideWith((_) async => []),
        favoriteMenuCatalogProvider.overrideWith(
          (_) async => FavoriteMenuCatalog(available: [menu], selected: [menu]),
        ),
      ],
      child: MaterialApp(
        theme: AppTheme.light,
        builder: (context, child) => MediaQuery(
          data: MediaQuery.of(
            context,
          ).copyWith(textScaler: TextScaler.linear(large ? 2 : 1)),
          child: child!,
        ),
        home: home ? const HomeScreen() : RemindersScreen(view: view),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

class _Auth extends AuthController {
  @override
  Future<AuthSession?> build() async =>
      const AuthSession(token: 'test', username: 'TEST');
}

class _Api extends ApiClient {
  _Api() : super(const SecureStore());
  final paths = <String>[];
  Map<String, dynamic>? query;
  List<Map<String, dynamic>>? rows;
  bool fail = false;
  @override
  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async {
    paths.add(path);
    this.query = query;
    if (fail) throw StateError('Unavailable');
    return rows ?? (path == '/api/ReminderTemplate/All' ? [_template] : [_log]);
  }
}
