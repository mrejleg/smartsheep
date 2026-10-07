import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/intl.dart';
import 'package:smartsheep_mobile/core/network/api_client.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_charts.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_data.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_filter_sheet.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_map.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_overview.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_screen.dart';

class _MemoryTiles extends TileProvider {
  final _image = MemoryImage(TileProvider.transparentImage);

  @override
  ImageProvider getImage(TileCoordinates coordinates, TileLayer options) =>
      _image;
}

class _DashboardApi extends Fake implements ApiClient {
  _DashboardApi({this.load});

  final Future<Object?> Function(Map<String, dynamic> query)? load;
  final queries = <Map<String, dynamic>>[];

  @override
  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async {
    if (path == '/api/Farm/All') {
      return [
        {'Id': 'farm-a', 'Name': 'Alpha Farm', 'IsActive': true},
        {'Id': 'farm-b', 'Name': 'Beta Farm', 'IsActive': true},
      ];
    }
    if (path == '/api/Barn/All') {
      return [
        {
          'Id': 'barn-a',
          'Name': 'Alpha Barn',
          'FkFarmId': 'farm-a',
          'IsActive': true,
        },
        {
          'Id': 'barn-b',
          'Name': 'Beta Barn',
          'FkFarmId': 'farm-b',
          'IsActive': true,
        },
      ];
    }
    if (path != '/api/SmartSheepReport/Dashboard') {
      throw StateError('Unexpected endpoint: $path');
    }
    final parameters = Map<String, dynamic>.from(query ?? {});
    queries.add(parameters);
    if (load != null) return load!(parameters);
    return _response(filtered: parameters['farmId'] == 'farm-b');
  }
}

Map<String, Object?> _response({bool filtered = false}) => {
  'TotalFarms': filtered ? 1 : 2,
  'TotalBarns': filtered ? 3 : 7,
  'TotalSheep': filtered ? 1200 : 2500,
  'TodayActivities': filtered ? 16 : 40,
  'TodayFrequency': filtered ? 32 : 90,
  'TodayDurationSeconds': filtered ? 3661 : 86399,
  'HourlyTrend': [
    {
      'Hour': 7,
      'Frequency': filtered ? 8 : 4,
      'DurationSeconds': filtered ? 180 : 60,
    },
  ],
  'BarnComparison': [
    {
      'BarnId': filtered ? 'barn-b' : 'barn-a',
      'BarnName': filtered ? 'Beta Barn' : 'Alpha Barn',
      'Frequency': filtered ? 32 : 90,
    },
  ],
  'InactiveSourceVideos': [
    {
      'Id': filtered ? 'source-b' : 'source-a',
      'Code': filtered ? 'CAM-BETA' : 'CAM-ALPHA',
      'BarnName': filtered ? 'Beta Barn' : 'Alpha Barn',
      'LastStatus': 'Offline',
      'LastCheckedAt': '2026-09-14T08:09:00',
    },
  ],
  'FarmMaps': [
    {
      'Id': filtered ? 'farm-b' : 'farm-a',
      'Name': filtered ? 'Beta Farm' : 'Alpha Farm',
      'Latitude': '-2.5489',
      'Longitude': '117.0940',
      'Location': 'Kalimantan',
      'TotalBarns': filtered ? 3 : 7,
      'TotalSheep': filtered ? 1200 : 2500,
      'TotalSourceVideos': 4,
      'OnlineSourceVideos': 3,
    },
  ],
};

DashboardFilter _initial() => DashboardFilter(
  startDate: DateTime(2026, 9, 7),
  endDate: DateTime(2026, 9, 14),
);

ProviderContainer _container(_DashboardApi api) {
  final container = ProviderContainer(
    overrides: [apiClientProvider.overrideWithValue(api)],
  );
  container.read(dashboardFilterProvider.notifier).setFilter(_initial());
  addTearDown(container.dispose);
  return container;
}

Future<void> _size(WidgetTester tester, Size size) async {
  await tester.binding.setSurfaceSize(size);
  addTearDown(() => tester.binding.setSurfaceSize(null));
}

Future<void> _pumpScreen(
  WidgetTester tester,
  ProviderContainer container, {
  bool settle = true,
  Size size = const Size(393, 2400),
}) async {
  await _size(tester, size);
  await tester.pumpWidget(
    UncontrolledProviderScope(
      container: container,
      child: MaterialApp(
        theme: AppTheme.light,
        home: DashboardScreen(mapTileProvider: _MemoryTiles()),
      ),
    ),
  );
  if (settle) {
    await tester.pumpAndSettle();
  } else {
    await tester.pump();
  }
}

Future<void> _pumpContent(
  WidgetTester tester,
  Widget child, {
  double width = 393,
}) async {
  await _size(tester, Size(width, 852));
  await tester.pumpWidget(
    MaterialApp(
      theme: AppTheme.light,
      home: Scaffold(
        body: ListView(padding: const EdgeInsets.all(14), children: [child]),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  test(
    'duration matches Web TimeSpan hh:mm:ss including the 24-hour component',
    () {
      expect(dashboardDuration(0), '00:00:00');
      expect(dashboardDuration(59), '00:00:59');
      expect(dashboardDuration(60), '00:01:00');
      expect(dashboardDuration(3661), '01:01:01');
      expect(dashboardDuration(86399), '23:59:59');
      expect(dashboardDuration(86400), '00:00:00');
      expect(dashboardDuration(90061), '01:01:01');
    },
  );

  testWidgets(
    'overview presents exactly the six Web totals with US formatting',
    (tester) async {
      await _pumpContent(
        tester,
        DashboardOverview(data: DashboardData.fromJson(_response())),
      );
      for (final label in [
        'Livestock Overview',
        "Today's Activity",
        'Total Farms',
        'Total Barns',
        'Total Sheep',
        'Activities',
        'Frequency',
        'Duration',
      ]) {
        expect(find.text(label), findsOneWidget);
      }
      for (final value in ['2', '7', '2,500', '40', '90', '23:59:59']) {
        expect(find.text(value), findsOneWidget);
      }
      expect(find.text('System Health'), findsNothing);
      expect(find.text('Streamers Online'), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('zero totals remain zero instead of restoring demo metrics', (
    tester,
  ) async {
    await _pumpContent(tester, const DashboardOverview(data: DashboardData()));
    expect(find.text('0'), findsNWidgets(5));
    expect(find.text('00:00:00'), findsOneWidget);
    expect(find.text('12'), findsNothing);
    expect(find.text('36'), findsNothing);
    expect(find.text('92%'), findsNothing);
  });

  testWidgets(
    'inactive sources retain code, barn, status and checked timestamp',
    (tester) async {
      await _pumpContent(
        tester,
        DashboardInactiveSources(
          items: [
            DashboardInactiveSourceVideo(
              id: 'a',
              code: 'CAM-001',
              barnName: 'North Barn',
              lastStatus: 'Offline',
              lastCheckedAt: DateTime(2026, 9, 14, 8, 9),
            ),
            const DashboardInactiveSourceVideo(
              id: 'b',
              code: 'CAM-002',
              barnName: 'South Barn',
              lastStatus: 'Unknown',
            ),
          ],
        ),
        width: 320,
      );
      expect(find.text('Inactive Source Videos'), findsOneWidget);
      expect(find.text('2 Sources'), findsOneWidget);
      expect(find.text('CAM-001'), findsOneWidget);
      expect(find.text('North Barn · Offline'), findsOneWidget);
      expect(find.text('2026-09-14 08:09'), findsOneWidget);
      expect(find.text('CAM-002'), findsOneWidget);
      expect(find.text('South Barn · Unknown'), findsOneWidget);
      expect(find.text('Never Checked'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('empty inactive sources use the exact Web online message', (
    tester,
  ) async {
    await _pumpContent(tester, const DashboardInactiveSources(items: []));
    expect(find.text('0 Sources'), findsOneWidget);
    expect(find.text('All source videos are online.'), findsOneWidget);
    expect(find.text('Barn B Camera'), findsNothing);
    expect(find.text('Health Check Reminder'), findsNothing);
  });

  test(
    'farm coordinates reject invalid input and online requires every source',
    () {
      for (final coordinates in [
        ('NaN', '117'),
        ('-2', 'Infinity'),
        ('91', '117'),
        ('-2', '181'),
        ('', ''),
      ]) {
        expect(
          dashboardFarmCoordinates(
            DashboardFarmMap(
              id: 'bad',
              latitude: coordinates.$1,
              longitude: coordinates.$2,
            ),
          ),
          isNull,
        );
      }
      expect(
        dashboardFarmCoordinates(
          const DashboardFarmMap(
            id: 'valid',
            latitude: '-2.5489',
            longitude: '117.0940',
          ),
        )?.latitude,
        -2.5489,
      );
      expect(
        dashboardFarmIsOnline(const DashboardFarmMap(id: 'none')),
        isFalse,
      );
      expect(
        dashboardFarmIsOnline(
          const DashboardFarmMap(
            id: 'part',
            totalSourceVideos: 4,
            onlineSourceVideos: 3,
          ),
        ),
        isFalse,
      );
      expect(
        dashboardFarmIsOnline(
          const DashboardFarmMap(
            id: 'all',
            totalSourceVideos: 4,
            onlineSourceVideos: 4,
          ),
        ),
        isTrue,
      );
    },
  );

  testWidgets(
    'map only plots valid API coordinates and exposes exact farm popup data',
    (tester) async {
      await _pumpContent(
        tester,
        DashboardFarmMapCard(
          tileProvider: _MemoryTiles(),
          items: [
            const DashboardFarmMap(
              id: 'valid',
              name: 'API Farm',
              location: 'Kalimantan',
              latitude: '-2.5489',
              longitude: '117.0940',
              totalBarns: 7,
              totalSheep: 2500,
              totalSourceVideos: 4,
              onlineSourceVideos: 3,
            ),
            const DashboardFarmMap(
              id: 'invalid',
              latitude: 'bad',
              longitude: '117',
            ),
          ],
        ),
        width: 320,
      );
      expect(
        tester.widget<MarkerLayer>(find.byType(MarkerLayer)).markers,
        hasLength(1),
      );
      expect(
        find.byKey(const ValueKey('dashboard-farm-invalid')),
        findsNothing,
      );
      await tester.tap(find.byKey(const ValueKey('dashboard-farm-valid')));
      await tester.pumpAndSettle();
      expect(
        find.byKey(const ValueKey('dashboard-farm-popup')),
        findsOneWidget,
      );
      for (final text in [
        'API Farm',
        'Kalimantan',
        'Barns',
        'Sheep',
        'Online',
        '7',
        '2,500',
        '3/4',
      ]) {
        expect(find.text(text), findsOneWidget);
      }
      await tester.tap(find.byTooltip('Close farm details'));
      await tester.pumpAndSettle();
      expect(find.byKey(const ValueKey('dashboard-farm-popup')), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'loading and API failure never display demo metrics or fake charts',
    (tester) async {
      final pending = Completer<Object?>();
      final api = _DashboardApi(load: (_) => pending.future);
      await _pumpScreen(
        tester,
        _container(api),
        settle: false,
        size: const Size(320, 568),
      );
      expect(find.text('Loading dashboard…'), findsOneWidget);
      expect(find.byType(DashboardOverview), findsNothing);
      expect(find.byType(DashboardHourlyChart), findsNothing);
      expect(find.byType(DashboardFarmMapCard), findsNothing);
      pending.completeError(StateError('API unavailable'));
      await tester.pumpAndSettle();
      expect(find.text('Unable to load dashboard.'), findsOneWidget);
      expect(find.text('Check your connection and try again.'), findsOneWidget);
      expect(find.text('Try again'), findsOneWidget);
      expect(find.text('System Health'), findsNothing);
      expect(find.text('No data'), findsNothing);
      expect(find.byType(DashboardOverview), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('screen consumes real provider fields and default Web query', (
    tester,
  ) async {
    final api = _DashboardApi();
    final container = _container(api);
    await _pumpScreen(tester, container);
    expect(api.queries.single, {
      'startDate': '2026-09-07',
      'endDate': '2026-09-14',
    });
    expect(
      tester
          .widget<Text>(find.byKey(const ValueKey('dashboard-filter-title')))
          .data,
      'Filter',
    );
    expect(find.text('Farm: All  Barn: All'), findsOneWidget);
    expect(find.text('Period: 07 Sep 2026 - 14 Sep 2026'), findsOneWidget);
    expect(find.text('2,500'), findsOneWidget);
    expect(find.text('23:59:59'), findsOneWidget);
    expect(
      tester
          .widget<DashboardHourlyChart>(find.byType(DashboardHourlyChart))
          .items
          .single
          .frequency,
      4,
    );
    expect(
      tester
          .widget<DashboardBarnChart>(find.byType(DashboardBarnChart))
          .items
          .single
          .frequency,
      90,
    );
    expect(
      tester
          .widget<DashboardFarmMapCard>(find.byType(DashboardFarmMapCard))
          .items
          .single
          .id,
      'farm-a',
    );
    expect(find.text('CAM-ALPHA'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'bottom-sheet apply sends farm barn dates and updates every dashboard section',
    (tester) async {
      final api = _DashboardApi();
      final container = _container(api);
      await _pumpScreen(tester, container);
      await tester.tap(find.byKey(const ValueKey('dashboard-filter-button')));
      await tester.pumpAndSettle();
      expect(find.byType(DashboardFilterSheet), findsOneWidget);
      await tester.tap(find.byKey(const ValueKey('dashboard-filter-farm')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Beta Farm'));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const ValueKey('dashboard-filter-barn')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Beta Barn'));
      await tester.pumpAndSettle();
      expect(api.queries, hasLength(1));
      expect(container.read(dashboardFilterProvider).farmId, isNull);
      await tester.tap(
        find.byKey(const ValueKey('dashboard-filter-start-date')),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.text('8'));
      await tester.tap(find.text('OK'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Apply Filters'));
      await tester.pumpAndSettle();
      expect(find.byType(DashboardFilterSheet), findsNothing);
      expect(api.queries.last, {
        'farmId': 'farm-b',
        'barnId': 'barn-b',
        'startDate': '2026-09-08',
        'endDate': '2026-09-14',
      });
      expect(find.text('Farm: Beta Farm  Barn: Beta Barn'), findsOneWidget);
      expect(find.text('Period: 08 Sep 2026 - 14 Sep 2026'), findsOneWidget);
      expect(find.text('1,200'), findsOneWidget);
      expect(find.text('01:01:01'), findsOneWidget);
      expect(find.text('2,500'), findsNothing);
      expect(
        tester
            .widget<DashboardOverview>(find.byType(DashboardOverview))
            .data
            .todayActivities,
        16,
      );
      expect(
        tester
            .widget<DashboardHourlyChart>(find.byType(DashboardHourlyChart))
            .items
            .single
            .frequency,
        8,
      );
      expect(
        tester
            .widget<DashboardHourlyChart>(find.byType(DashboardHourlyChart))
            .items
            .single
            .durationSeconds,
        180,
      );
      expect(
        tester
            .widget<DashboardBarnChart>(find.byType(DashboardBarnChart))
            .items
            .single
            .barnId,
        'barn-b',
      );
      expect(
        tester
            .widget<DashboardBarnChart>(find.byType(DashboardBarnChart))
            .items
            .single
            .frequency,
        32,
      );
      expect(
        tester
            .widget<DashboardFarmMapCard>(find.byType(DashboardFarmMapCard))
            .items
            .single
            .id,
        'farm-b',
      );
      expect(
        find.byKey(const ValueKey('dashboard-farm-farm-b')),
        findsOneWidget,
      );
      expect(find.byKey(const ValueKey('dashboard-farm-farm-a')), findsNothing);
      expect(find.text('CAM-BETA'), findsOneWidget);
      expect(find.text('CAM-ALPHA'), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('pull refresh reloads data while preserving selected filters', (
    tester,
  ) async {
    final api = _DashboardApi();
    final container = _container(api);
    final applied = _initial().copyWith(farmId: 'farm-b', barnId: 'barn-b');
    container.read(dashboardFilterProvider.notifier).setFilter(applied);
    await _pumpScreen(tester, container);
    expect(find.text('CAM-BETA'), findsOneWidget);
    expect(find.byKey(const ValueKey('dashboard-refresh')), findsNothing);
    await tester
        .widget<RefreshIndicator>(find.byType(RefreshIndicator))
        .onRefresh();
    await tester.pumpAndSettle();
    expect(container.read(dashboardFilterProvider), applied);
    expect(api.queries.last, applied.toQuery());
    expect(find.text('Farm: All  Barn: All'), findsNothing);
    expect(
      find.text(
        'Period: ${DateFormat('dd MMM yyyy', 'en_US').format(applied.startDate)} - ${DateFormat('dd MMM yyyy', 'en_US').format(applied.endDate)}',
      ),
      findsOneWidget,
    );
    expect(find.text('CAM-ALPHA'), findsNothing);
    expect(find.text('CAM-BETA'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
