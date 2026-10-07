import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/network/api_client.dart';
import 'package:smartsheep_mobile/core/network/secure_store.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_data.dart';

void main() {
  group('DashboardFilter', () {
    test('defaults match the web inclusive seven-day offset through today', () {
      final filter = DashboardFilter.defaults(
        now: DateTime(2026, 9, 14, 15, 20),
      );

      expect(filter.startDate, DateTime(2026, 9, 7));
      expect(filter.endDate, DateTime(2026, 9, 14));
      expect(filter.farmId, isNull);
      expect(filter.barnId, isNull);
      expect(filter.toQuery(), {
        'startDate': '2026-09-07',
        'endDate': '2026-09-14',
      });
    });

    test(
      'date boundaries normalize and nullable selections can be cleared',
      () {
        final filter = DashboardFilter(
          startDate: DateTime(2026, 8, 1, 11),
          endDate: DateTime(2026, 8, 31, 23, 59),
          farmId: 'farm-a',
          barnId: 'barn-a',
        );

        expect(filter.toQuery(), {
          'startDate': '2026-08-01',
          'endDate': '2026-08-31',
          'farmId': 'farm-a',
          'barnId': 'barn-a',
        });
        expect(filter.copyWith(), filter);
        expect(filter.copyWith().hashCode, filter.hashCode);
        expect(filter.copyWith(barnId: null).farmId, 'farm-a');
        expect(filter.copyWith(barnId: null).barnId, isNull);
        expect(filter.copyWith(farmId: null, barnId: null).toQuery(), {
          'startDate': '2026-08-01',
          'endDate': '2026-08-31',
        });
      },
    );

    test('invalid ranges are rejected before calling the API', () {
      final filter = DashboardFilter(
        startDate: DateTime(2026, 9, 15),
        endDate: DateTime(2026, 9, 14),
      );
      final container = ProviderContainer();
      addTearDown(container.dispose);
      final notifier = container.read(dashboardFilterProvider.notifier);
      final previous = container.read(dashboardFilterProvider);

      expect(filter.isValid, isFalse);
      expect(filter.toQuery, throwsArgumentError);
      expect(() => notifier.setFilter(filter), throwsArgumentError);
      expect(container.read(dashboardFilterProvider), previous);
    });

    test('reset clears selections and restores the web date range', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);
      final notifier = container.read(dashboardFilterProvider.notifier);
      notifier.setFilter(
        DashboardFilter(
          startDate: DateTime(2025),
          endDate: DateTime(2025, 12, 31),
          farmId: 'farm-a',
          barnId: 'barn-a',
        ),
      );

      notifier.reset(now: DateTime(2026, 1, 3));

      expect(container.read(dashboardFilterProvider).toQuery(), {
        'startDate': '2025-12-27',
        'endDate': '2026-01-03',
      });
    });
  });

  group('DashboardData', () {
    test('parses all current API dashboard DTO fields without substitutes', () {
      final data = DashboardData.fromJson(_dashboardJson);

      expect(data.date, DateTime(2026, 9, 7));
      expect(data.totalFarms, 2);
      expect(data.totalBarns, 3);
      expect(data.totalSheep, 7);
      expect(data.todayActivities, 11);
      expect(data.todayFrequency, 17);
      expect(data.todayDurationSeconds, 127);
      expect(data.statisticsRequiringReminder, 4);
      final hourly = data.hourlyTrend.single;
      expect(hourly.hour, 9);
      expect(hourly.frequency, 17);
      expect(hourly.durationSeconds, 127);
      final barn = data.barnComparison.single;
      expect(barn.barnId, 'barn-a');
      expect(barn.barnCode, 'BRN-A');
      expect(barn.barnName, 'Barn A');
      expect(barn.frequency, 17);
      expect(barn.durationSeconds, 127);
      expect(data.reminders.single.status, 'Pending');
      expect(data.reminders.single.total, 4);
      final source = data.inactiveSourceVideos.single;
      expect(source.id, 'source-a');
      expect(source.code, 'CAM-A');
      expect(source.barnName, 'Barn A');
      expect(source.sourceVideoUrl, 'rtsp://camera-a/live');
      expect(source.lastStatus, 'Offline');
      expect(source.lastCheckedAt, DateTime(2026, 9, 14, 9, 5));
      final inactiveBarn = data.inactiveBarns.single;
      expect(inactiveBarn.id, 'barn-a');
      expect(inactiveBarn.code, 'BRN-A');
      expect(inactiveBarn.name, 'Barn A');
      expect(inactiveBarn.farmName, 'Farm A');
      expect(inactiveBarn.totalSourceVideos, 1);
      expect(inactiveBarn.onlineSourceVideos, 0);
      final farm = data.farmMaps.single;
      expect(farm.id, 'farm-a');
      expect(farm.code, 'FRM-A');
      expect(farm.name, 'Farm A');
      expect(farm.location, 'Bandung');
      expect(farm.address, 'Farm road 12');
      expect(farm.latitude, '-6.9175');
      expect(farm.longitude, '107.6191');
      expect(farm.totalBarns, 3);
      expect(farm.totalSheep, 7);
      expect(farm.totalSourceVideos, 2);
      expect(farm.onlineSourceVideos, 1);
    });

    test('missing values remain zero or empty rather than demo data', () {
      final data = DashboardData.fromJson(<String, dynamic>{});

      expect(data.date, isNull);
      expect(data.totalFarms, 0);
      expect(data.totalBarns, 0);
      expect(data.totalSheep, 0);
      expect(data.todayActivities, 0);
      expect(data.todayFrequency, 0);
      expect(data.todayDurationSeconds, 0);
      expect(data.statisticsRequiringReminder, 0);
      expect(data.hourlyTrend, isEmpty);
      expect(data.barnComparison, isEmpty);
      expect(data.reminders, isEmpty);
      expect(data.inactiveSourceVideos, isEmpty);
      expect(data.inactiveBarns, isEmpty);
      expect(data.farmMaps, isEmpty);
    });

    test('camel-case JSON is accepted with nullable DTO fields intact', () {
      final data = DashboardData.fromJson({
        'totalFarms': 1,
        'hourlyTrend': [
          {'hour': 0, 'frequency': 3, 'durationSeconds': 30},
        ],
        'inactiveSourceVideos': [
          {'id': 'camera-a', 'lastCheckedAt': null},
        ],
        'farmMaps': [
          {'id': 'farm-a', 'latitude': null, 'longitude': null},
        ],
      });

      expect(data.totalFarms, 1);
      expect(data.hourlyTrend.single.frequency, 3);
      expect(data.inactiveSourceVideos.single.lastCheckedAt, isNull);
      expect(data.farmMaps.single.latitude, isNull);
      expect(data.farmMaps.single.longitude, isNull);
    });

    test('malformed payloads fail instead of masquerading as empty data', () {
      for (final payload in <Object?>[
        null,
        [],
        'not dashboard data',
        {'TotalFarms': 'not a number'},
        {'TotalBarns': 1.5},
        {'HourlyTrend': 'not a list'},
        {
          'HourlyTrend': [null],
        },
        {'Date': 'not a date'},
        {
          'InactiveSourceVideos': [
            {'Code': 'missing id'},
          ],
        },
      ]) {
        expect(
          () => DashboardData.fromJson(payload),
          throwsFormatException,
          reason: '$payload must not silently become an empty dashboard',
        );
      }
    });
  });

  group('DashboardFilterOptions', () {
    test(
      'matches active-only alphabetical options and barn ownership on web',
      () {
        final options = DashboardFilterOptions.fromJson(
          farms: [
            {'Id': 'farm-z', 'Name': 'Zebra Farm', 'IsActive': true},
            {'Id': 'farm-old', 'Name': 'Archived', 'IsActive': false},
            {'Id': 'farm-a', 'Name': 'Alpha Farm', 'IsActive': true},
          ],
          barns: [
            {
              'Id': 'barn-b',
              'Name': 'Barn B',
              'IsActive': true,
              'FkFarmId': 'farm-z',
            },
            {'Id': 'barn-old', 'Name': 'Archived', 'IsActive': false},
            {
              'Id': 'barn-a',
              'Name': 'Barn A',
              'IsActive': true,
              'FkFarmId': 'farm-a',
            },
            {
              'Id': 'barn-none',
              'Name': 'Unassigned barn',
              'IsActive': true,
              'FkFarmId': null,
            },
          ],
        );

        expect(options.farms.map((farm) => farm.id), ['farm-a', 'farm-z']);
        expect(options.barns.map((barn) => barn.id), [
          'barn-a',
          'barn-b',
          'barn-none',
        ]);
        expect(options.barns.map((barn) => barn.farmId), [
          'farm-a',
          'farm-z',
          null,
        ]);
      },
    );

    test('empty lists are valid but malformed lookup responses are errors', () {
      final options = DashboardFilterOptions.fromJson(farms: [], barns: []);
      expect(options.farms, isEmpty);
      expect(options.barns, isEmpty);

      for (final response in <Object?>[
        null,
        {},
        [null],
        [
          {'Id': 'farm-a', 'Name': 'Farm A'},
        ],
        [
          {'Name': 'Farm A', 'IsActive': true},
        ],
        [
          {'Id': 'farm-a', 'IsActive': true},
        ],
      ]) {
        expect(
          () => DashboardFilterOptions.fromJson(farms: response, barns: []),
          throwsFormatException,
        );
      }
    });
  });

  group('dashboard providers', () {
    test('fetches the dashboard endpoint with every applied filter', () async {
      final requests = <(String, Map<String, dynamic>?)>[];
      final client = _ApiClient((path, query) {
        requests.add((path, query));
        return _dashboardJson;
      });
      final container = ProviderContainer(
        overrides: [apiClientProvider.overrideWithValue(client)],
      );
      addTearDown(container.dispose);
      final subscription = container.listen(dashboardProvider, (_, _) {});
      addTearDown(subscription.close);
      final notifier = container.read(dashboardFilterProvider.notifier);
      final filter = DashboardFilter(
        startDate: DateTime(2026, 9, 1),
        endDate: DateTime(2026, 9, 14),
        farmId: 'farm-a',
        barnId: 'barn-a',
      );
      notifier.setFilter(filter);

      final data = await container.read(dashboardProvider.future);

      expect(data.todayFrequency, 17);
      expect(requests.last.$1, '/api/SmartSheepReport/Dashboard');
      expect(requests.last.$2, filter.toQuery());
      final before = requests.length;
      notifier.setFilter(filter.copyWith(farmId: 'farm-z', barnId: null));
      await container.read(dashboardProvider.future);

      expect(requests.length, before + 1);
      expect(requests.last.$2, {
        'startDate': '2026-09-01',
        'endDate': '2026-09-14',
        'farmId': 'farm-z',
      });
    });

    test(
      'lookup provider uses the existing authorized Farm and Barn APIs',
      () async {
        final paths = <String>[];
        final client = _ApiClient((path, query) {
          paths.add(path);
          expect(query, isNull);
          return switch (path) {
            '/api/Farm/All' => [
              {'Id': 'farm-a', 'Name': 'Farm A', 'IsActive': true},
            ],
            '/api/Barn/All' => [
              {
                'Id': 'barn-a',
                'Name': 'Barn A',
                'FkFarmId': 'farm-a',
                'IsActive': true,
              },
            ],
            _ => throw StateError('Unexpected endpoint $path'),
          };
        });
        final container = ProviderContainer(
          retry: (_, _) => null,
          overrides: [apiClientProvider.overrideWithValue(client)],
        );
        addTearDown(container.dispose);

        final options = await container.read(dashboardOptionsProvider.future);

        expect(paths, ['/api/Farm/All', '/api/Barn/All']);
        expect(options.farms.single.name, 'Farm A');
        expect(options.barns.single.farmId, 'farm-a');
      },
    );

    test(
      'API failures and malformed responses propagate to error UI',
      () async {
        for (final client in [
          _ApiClient((_, _) => throw StateError('Request failed')),
          _ApiClient((_, _) => 'invalid dashboard response'),
        ]) {
          final container = ProviderContainer(
            retry: (_, _) => null,
            overrides: [apiClientProvider.overrideWithValue(client)],
          );
          addTearDown(container.dispose);

          await expectLater(
            container.read(dashboardProvider.future),
            throwsA(anyOf(isA<StateError>(), isA<FormatException>())),
          );
          expect(container.read(dashboardProvider).hasError, isTrue);
        }
      },
    );
  });
}

class _ApiClient extends ApiClient {
  _ApiClient(this.respond) : super(const SecureStore());

  final FutureOr<Object?> Function(String, Map<String, dynamic>?) respond;

  @override
  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async =>
      respond(path, query);
}

const _dashboardJson = {
  'Date': '2026-09-07T00:00:00',
  'TotalFarms': 2,
  'TotalBarns': 3,
  'TotalSheep': 7,
  'TodayActivities': 11,
  'TodayFrequency': 17,
  'TodayDurationSeconds': 127,
  'StatisticsRequiringReminder': 4,
  'HourlyTrend': [
    {'Hour': 9, 'Frequency': 17, 'DurationSeconds': 127},
  ],
  'BarnComparison': [
    {
      'BarnId': 'barn-a',
      'BarnCode': 'BRN-A',
      'BarnName': 'Barn A',
      'Frequency': 17,
      'DurationSeconds': 127,
    },
  ],
  'Reminders': [
    {'Status': 'Pending', 'Total': 4},
  ],
  'InactiveSourceVideos': [
    {
      'Id': 'source-a',
      'Code': 'CAM-A',
      'BarnName': 'Barn A',
      'SourceVideoUrl': 'rtsp://camera-a/live',
      'LastStatus': 'Offline',
      'LastCheckedAt': '2026-09-14T09:05:00',
    },
  ],
  'InactiveBarns': [
    {
      'Id': 'barn-a',
      'Code': 'BRN-A',
      'Name': 'Barn A',
      'FarmName': 'Farm A',
      'TotalSourceVideos': 1,
      'OnlineSourceVideos': 0,
    },
  ],
  'FarmMaps': [
    {
      'Id': 'farm-a',
      'Code': 'FRM-A',
      'Name': 'Farm A',
      'Location': 'Bandung',
      'Address': 'Farm road 12',
      'Latitude': '-6.9175',
      'Longitude': '107.6191',
      'TotalBarns': 3,
      'TotalSheep': 7,
      'TotalSourceVideos': 2,
      'OnlineSourceVideos': 1,
    },
  ],
};
