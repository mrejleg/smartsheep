import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/network/api_client.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/auth/auth_controller.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_data.dart';
import 'package:smartsheep_mobile/features/favorites/favorite_menu.dart';
import 'package:smartsheep_mobile/features/favorites/favorites_controller.dart';
import 'package:smartsheep_mobile/features/home/home_screen.dart';
import 'package:smartsheep_mobile/features/news/news_data.dart';
import 'package:smartsheep_mobile/features/shell/header_identity.dart';
import 'package:smartsheep_mobile/features/statistics/statistics_screen.dart';

void main() {
  for (final controller in [
    'SucklingActivity',
    'SucklingActivityReport',
    'SucklingStatistic',
    'SucklingStatisticReport',
  ]) {
    testWidgets('$controller opens real report endpoint from Home', (
      tester,
    ) async {
      final api = _Api();
      await _pump(tester, api, controller: controller);
      await tester.tap(find.text('My report'));
      await tester.pumpAndSettle();
      final statistics = controller.contains('Statistic');
      expect(
        api.calls.single.path,
        '/api/SmartSheepReport/${statistics ? 'SucklingStatistics' : 'SucklingActivities'}',
      );
      expect(api.calls.single.query['pageSize'], 5);
      expect(api.calls.single.query['to'], endsWith('T23:59:59.9999999'));
      expect(
        find.text(statistics ? 'Code: STAT-001' : 'Barn: Real barn'),
        findsOneWidget,
      );
      expect(find.text('86%'), findsNothing);
      expect(find.text('Successful jobs'), findsNothing);
      expect(tester.takeException(), isNull);
    });
  }

  test(
    'query matches web dates and farm/barn exactly, including end of day',
    () async {
      final api = _Api();
      final container = ProviderContainer(
        overrides: [apiClientProvider.overrideWithValue(api)],
      );
      addTearDown(container.dispose);
      await container.read(
        sucklingReportProvider((
          view: SucklingReportView.activities,
          filter: DashboardFilter(
            startDate: DateTime(2025, 2, 1),
            endDate: DateTime(2025, 2, 2),
            farmId: 'farm',
            barnId: 'barn',
          ),
          page: 3,
        )).future,
      );
      expect(api.calls.single.query, {
        'from': '2025-02-01T00:00:00',
        'to': '2025-02-02T23:59:59.9999999',
        'farmId': 'farm',
        'barnId': 'barn',
        'page': 3,
        'pageSize': 5,
      });
    },
  );

  testWidgets('empty, failed, and refreshed results have distinct states', (
    tester,
  ) async {
    final api = _Api()..rows = [];
    await _pump(tester, api);
    expect(find.text('No data'), findsOneWidget);
    expect(find.text('Page 1'), findsNothing);
    api.fail = true;
    await tester
        .widget<RefreshIndicator>(find.byType(RefreshIndicator))
        .onRefresh();
    await tester.pumpAndSettle();
    expect(find.text('No data'), findsNothing);
    expect(
      find.text(
        'Unable to load report. Please check your connection or access.',
      ),
      findsOneWidget,
    );
    api.fail = false;
    api.rows = [_row];
    await tester.tap(find.text('Retry'));
    await tester.pumpAndSettle();
    expect(find.text('Code: STAT-001'), findsOneWidget);
  });

  testWidgets('5 rows per page with pagination beside Report Data', (
    tester,
  ) async {
    final api = _Api()
      ..rows = List.generate(6, (i) => {..._row, 'Code': 'ROW-$i'});
    await _pump(tester, api);
    expect(find.text('Code: ROW-5'), findsNothing);
    expect(find.byTooltip('Next').hitTestable(), findsOneWidget);
    expect(find.byTooltip('Previous').hitTestable(), findsOneWidget);
    expect(find.text('Page 1'), findsOneWidget);
    final pager = find.byKey(const ValueKey('suckling-report-pagination'));
    expect(
      tester.getCenter(pager).dy,
      closeTo(tester.getCenter(find.text('Report Data')).dy, 1),
    );
    expect(
      tester.getTopLeft(pager).dx,
      greaterThan(tester.getTopRight(find.text('Report Data')).dx),
    );
    expect(
      tester.widget<Scaffold>(find.byType(Scaffold).first).bottomNavigationBar,
      isNull,
    );
    await tester.tap(find.byTooltip('Next'));
    await tester.pumpAndSettle();
    expect(api.calls.last.query['page'], 2);
    expect(find.text('Code: PAGE-2'), findsOneWidget);
    expect(find.text('Page 2'), findsOneWidget);
    expect(
      tester
          .widget<IconButton>(
            find.byWidgetPredicate(
              (widget) => widget is IconButton && widget.tooltip == 'Next',
            ),
          )
          .onPressed,
      isNull,
    );
    await tester.tap(find.byTooltip('Previous'));
    await tester.pumpAndSettle();
    expect(api.calls.last.query['page'], 1);
    expect(find.text('Code: ROW-0'), findsOneWidget);
  });

  testWidgets(
    'filter uses bottom sheet, cancels draft, applies and resets page',
    (tester) async {
      final api = _Api();
      await _pump(tester, api);
      await tester.tap(find.byKey(const ValueKey('suckling-report-filter')));
      await tester.pumpAndSettle();
      expect(find.text('Report Filters'), findsOneWidget);
      await tester.tap(find.byTooltip('Close filters'));
      await tester.pumpAndSettle();
      expect(api.calls, hasLength(1));
      await tester.tap(find.byKey(const ValueKey('suckling-report-filter')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Apply Filters'));
      await tester.pumpAndSettle();
      expect(find.text('Report Filters'), findsNothing);
      expect(find.text('Code: STAT-001'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'statistics fields support camelCase nulls and 320px large text',
    (tester) async {
      final api = _Api()
        ..rows = [
          {
            for (final entry in _row.entries)
              '${entry.key[0].toLowerCase()}${entry.key.substring(1)}':
                  entry.value,
          },
        ];
      await _pump(tester, api, narrow: true);
      await tester.scrollUntilVisible(
        find.text('Hour Number'),
        200,
        scrollable: find.byType(Scrollable).last,
      );
      expect(find.text('01 Feb 2025 09:30'), findsOneWidget);
      expect(find.text('Yes'), findsOneWidget);
      expect(find.text('42'), findsOneWidget);
    },
  );
}

const _row = <String, dynamic>{
  'Code': 'STAT-001',
  'Barn': {'Name': 'Real barn'},
  'PeriodDescription': 'Morning',
  'TotalFrequency': 42,
  'TotalDurationSeconds': 120,
  'RequiresReminder': true,
  'ActivityStart': '2025-02-01T09:30:00',
  'ActivityEnd': null,
  'ActivityFrom': '2025-02-01T09:30:00',
  'ActivityTo': null,
  'HourNumber': 9,
};

class _Api extends Fake implements ApiClient {
  final calls = <({String path, Map<String, dynamic> query})>[];
  List<Map<String, dynamic>> rows = [_row];
  bool fail = false;
  @override
  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async {
    calls.add((path: path, query: query!));
    if (fail) throw StateError('Offline');
    return query['page'] == 2
        ? [
            {..._row, 'Code': 'PAGE-2'},
          ]
        : rows;
  }
}

class _Auth extends AuthController {
  @override
  Future<AuthSession?> build() async =>
      const AuthSession(token: 'test', username: 'operator');
}

Future<void> _pump(
  WidgetTester tester,
  _Api api, {
  String? controller,
  bool narrow = false,
}) async {
  tester.view.physicalSize = Size(narrow ? 320 : 430, 1000);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  final menu = MobileFavoriteMenu(
    id: 'report',
    name: 'My report',
    controller: controller ?? 'SucklingStatistic',
    sequenceNumber: '1',
    type: FavoriteMenuType.other,
  );
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        apiClientProvider.overrideWithValue(api),
        authControllerProvider.overrideWith(_Auth.new),
        headerProfileImageProvider.overrideWith((_) async => null),
        unreadNotificationCountProvider.overrideWithValue(0),
        dashboardOptionsProvider.overrideWith(
          (_) async => const DashboardFilterOptions(),
        ),
        favoriteMenuCatalogProvider.overrideWith(
          (_) async => FavoriteMenuCatalog(available: [menu], selected: [menu]),
        ),
        newsItemsProvider.overrideWith((_) async => []),
      ],
      child: MaterialApp(
        theme: AppTheme.light,
        builder: (context, child) => MediaQuery(
          data: MediaQuery.of(
            context,
          ).copyWith(textScaler: TextScaler.linear(narrow ? 2 : 1)),
          child: child!,
        ),
        home: controller == null
            ? const StatisticsScreen()
            : const HomeScreen(),
      ),
    ),
  );
  await tester.pumpAndSettle();
}
