import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/network/api_client.dart';
import 'package:smartsheep_mobile/core/network/secure_store.dart';
import 'package:smartsheep_mobile/features/sources/sources_data.dart';

void main() {
  group('SourcesData', () {
    test('joins source videos into actual barn and farm master records', () {
      final data = _sample();

      expect(data.totalFarms, 2);
      expect(data.totalBarns, 3);
      expect(data.totalSources, 3);
      expect(data.onlineCount, 2);
      expect(data.offlineCount, 1);
      expect(data.farms.map((farm) => farm.id), ['farm-a', 'farm-z']);
      final farm = data.farms.first;
      expect(farm.name, 'Alpha Farm');
      expect(farm.code, 'FRM-A');
      expect(farm.totalSources, 2);
      expect(farm.onlineCount, 1);
      expect(farm.offlineCount, 1);
      final barn = farm.barns.first;
      expect(barn.id, 'barn-a');
      expect(barn.name, 'Barn A');
      expect(barn.code, 'BRN-A');
      expect(barn.sources.map((source) => source.code), ['CAM-A', 'CAM-B']);
      expect(barn.sources.first.sourceVideoUrl, 'rtsp://example.test/camera-a');
    });

    test('supports camel-case responses and fields', () {
      final data = SourcesData.fromJson(
        sources: [
          {
            'id': 'source-a',
            'code': 'CAM-A',
            'fkBarnId': 'barn-a',
            'isOnline': true,
            'isActive': true,
            'sourceVideoUrl': 'rtsp://example.test/camera-a',
          },
        ],
        barns: [
          {
            'id': 'barn-a',
            'code': 'BRN-A',
            'name': 'Barn A',
            'fkFarmId': 'farm-a',
            'isActive': true,
          },
        ],
        farms: [
          {'id': 'farm-a', 'code': 'FRM-A', 'name': 'Farm A', 'isActive': true},
        ],
      );

      expect(data.farms.single.name, 'Farm A');
      expect(data.farms.single.barns.single.name, 'Barn A');
      expect(data.farms.single.barns.single.sources.single.code, 'CAM-A');
      expect(data.onlineCount, 1);
      expect(data.offlineCount, 0);
    });

    test(
      'current master IDs and names override stale nested relationships',
      () {
        final data = SourcesData.fromJson(
          sources: [
            {
              ..._source('source-a', 'CAM-A', 'barn-a', true),
              'Barn': {
                'Id': 'stale-barn',
                'Name': 'Old barn name',
                'FkFarmId': 'old-farm',
                'IsActive': false,
                'Farm': {'Id': 'old-farm', 'Name': 'Old Farm'},
              },
            },
          ],
          barns: [
            {
              ..._barn('barn-a', 'Renamed Barn', 'farm-new'),
              'Farm': {'Id': 'old-farm', 'Name': 'Old Farm', 'IsActive': false},
            },
          ],
          farms: [_farm('farm-new', 'Renamed Farm')],
        );

        expect(data.totalSources, 1);
        expect(data.farms.single.id, 'farm-new');
        expect(data.farms.single.name, 'Renamed Farm');
        expect(data.farms.single.barns.single.id, 'barn-a');
        expect(data.farms.single.barns.single.name, 'Renamed Barn');
      },
    );

    test(
      'farms and barns with identical display names never merge by name',
      () {
        final data = SourcesData.fromJson(
          sources: [
            _source('source-c', 'CAM-C', 'barn-c', true),
            _source('source-b', 'CAM-B', 'barn-b', false),
            _source('source-a', 'CAM-A', 'barn-a', true),
          ],
          barns: [
            _barn('barn-c', 'Same Barn', 'farm-b'),
            _barn('barn-b', 'Same Barn', 'farm-a'),
            _barn('barn-a', 'Same Barn', 'farm-a'),
          ],
          farms: [_farm('farm-b', 'Same Farm'), _farm('farm-a', 'Same Farm')],
        );

        expect(data.totalFarms, 2);
        expect(data.totalBarns, 3);
        expect(data.farms.map((farm) => farm.id), ['farm-a', 'farm-b']);
        expect(data.farms.first.barns.map((barn) => barn.id), [
          'barn-a',
          'barn-b',
        ]);
        expect(data.farms.last.barns.single.sources.single.id, 'source-c');
      },
    );

    test('one barn retains each configured source and mixed real statuses', () {
      final barn = _sample().farms.first.barns.first;

      expect(barn.hasSources, isTrue);
      expect(barn.totalSources, 2);
      expect(barn.onlineCount, 1);
      expect(barn.offlineCount, 1);
      expect(barn.sources.map((source) => source.id), ['source-a', 'source-b']);
    });

    test('explicitly inactive sources, barns, and farms are excluded', () {
      final data = SourcesData.fromJson(
        sources: [
          _source('keep', 'KEEP', 'active-barn', true),
          _source('source-off', 'HIDDEN', 'active-barn', true, active: false),
          _source('barn-off', 'HIDDEN', 'inactive-barn', true),
          _source('farm-off', 'HIDDEN', 'barn-on-inactive-farm', true),
        ],
        barns: [
          _barn('active-barn', 'Active Barn', 'active-farm'),
          _barn('inactive-barn', 'Inactive Barn', 'active-farm', active: false),
          _barn('barn-on-inactive-farm', 'Hidden Barn', 'inactive-farm'),
        ],
        farms: [
          _farm('active-farm', 'Active Farm'),
          _farm('inactive-farm', 'Inactive Farm', active: false),
        ],
      );

      expect(data.totalFarms, 1);
      expect(data.totalBarns, 1);
      expect(data.totalSources, 1);
      expect(data.farms.single.barns.single.sources.single.id, 'keep');
      expect(data.offlineCount, 0);
    });

    test(
      'orphaned relationships are explicit without inventing farm or barn records',
      () {
        final data = SourcesData.fromJson(
          sources: [
            _source('source-no-barn', 'CAM-NONE', null, false),
            {
              ..._source(
                'source-unknown-barn',
                'CAM-UNKNOWN',
                'missing-barn',
                true,
              ),
              'Barn': {'Id': 'missing-barn', 'Name': 'Stale barn name'},
            },
            _source('source-missing-farm', 'CAM-REAL', 'real-barn', true),
          ],
          barns: [_barn('real-barn', 'Real Barn', 'missing-farm')],
          farms: [],
        );

        expect(data.totalFarms, 0);
        expect(data.totalBarns, 1);
        expect(data.totalSources, 3);
        final farm = data.farms.single;
        expect(farm.id, isNull);
        expect(farm.isUnassigned, isTrue);
        expect(farm.name, 'Unassigned Farm');
        expect(farm.barns.first.id, 'real-barn');
        expect(farm.barns.first.name, 'Real Barn');
        final unassigned = farm.barns.last;
        expect(unassigned.id, isNull);
        expect(unassigned.isUnassigned, isTrue);
        expect(unassigned.name, 'Unassigned Barn');
        expect(unassigned.sources.map((source) => source.id), [
          'source-no-barn',
          'source-unknown-barn',
        ]);
      },
    );

    test(
      'only boolean true is Online; missing flags do not fabricate availability',
      () {
        final data = SourcesData.fromJson(
          sources: [
            {'Id': 'unknown'},
            {'Id': 'offline', 'IsOnline': false},
            {'Id': 'string', 'IsOnline': 'true'},
            {'Id': 'number', 'IsOnline': 1},
            {'Id': 'online', 'IsOnline': true},
          ],
          barns: [],
          farms: [],
        );

        expect(data.onlineCount, 1);
        expect(data.offlineCount, 4);
        expect(data.totalSources, 5);
        expect(data.farms.single.barns.single.sources.first.code, isEmpty);
        expect(
          data.farms.single.barns.single.sources.first.sourceVideoUrl,
          isNull,
        );
      },
    );

    test('empty API lists remain truly empty with all counts zero', () {
      final data = SourcesData.fromJson(sources: [], barns: [], farms: []);

      expect(data.farms, isEmpty);
      expect(data.totalFarms, 0);
      expect(data.totalBarns, 0);
      expect(data.totalSources, 0);
      expect(data.onlineCount, 0);
      expect(data.offlineCount, 0);
      expect(data.filtered(SourceAvailability.online).farms, isEmpty);
      expect(data.filtered(SourceAvailability.offline).farms, isEmpty);
    });

    test(
      'active barns without sources remain visible in All and are not Offline',
      () {
        final data = SourcesData.fromJson(
          sources: [],
          barns: [_barn('barn-a', 'Empty Barn', 'farm-a')],
          farms: [_farm('farm-a', 'Real Farm')],
        );

        expect(data.totalBarns, 1);
        expect(data.farms.single.barns.single.name, 'Empty Barn');
        expect(data.farms.single.barns.single.hasSources, isFalse);
        expect(data.totalSources, 0);
        expect(data.offlineCount, 0);
        expect(data.filtered(SourceAvailability.all), same(data));
        expect(data.filtered(SourceAvailability.online).farms, isEmpty);
        expect(data.filtered(SourceAvailability.offline).farms, isEmpty);
      },
    );

    test(
      'availability filters return matching sources and remove empty groups',
      () {
        final data = _sample();
        final online = data.filtered(SourceAvailability.online);
        final offline = data.filtered(SourceAvailability.offline);

        expect(online.totalSources, 2);
        expect(online.onlineCount, 2);
        expect(online.offlineCount, 0);
        expect(online.totalFarms, 2);
        expect(online.totalBarns, 2);
        expect(offline.totalSources, 1);
        expect(offline.onlineCount, 0);
        expect(offline.offlineCount, 1);
        expect(offline.totalFarms, 1);
        expect(offline.totalBarns, 1);
        expect(offline.farms.single.barns.single.sources.single.id, 'source-b');
        expect(data.totalSources, 3);
        expect(data.totalBarns, 3);
        expect(data.farms.first.barns, hasLength(2));
      },
    );

    test(
      'malformed and duplicate responses fail instead of becoming fake empty results',
      () {
        for (final response in <Object?>[
          null,
          'invalid',
          {},
          [null],
          [
            {'Code': 'missing identity'},
          ],
          [
            {'Id': 42},
          ],
          [
            {'Id': 'same'},
            {'Id': 'same'},
          ],
        ]) {
          expect(
            () => SourcesData.fromJson(sources: response, barns: [], farms: []),
            throwsFormatException,
          );
          expect(
            () => SourcesData.fromJson(sources: [], barns: response, farms: []),
            throwsFormatException,
          );
          expect(
            () => SourcesData.fromJson(sources: [], barns: [], farms: response),
            throwsFormatException,
          );
        }
      },
    );
  });

  group('sourcesProvider', () {
    test(
      'loads the three authorized APIs concurrently and joins their responses',
      () async {
        final pending = <String, Completer<Object?>>{
          '/api/SourceVideo/All': Completer(),
          '/api/Barn/All': Completer(),
          '/api/Farm/All': Completer(),
        };
        final paths = <String>[];
        final container = ProviderContainer(
          overrides: [
            apiClientProvider.overrideWithValue(
              _ApiClient((path, query) {
                paths.add(path);
                expect(query, isNull);
                return pending[path]!.future;
              }),
            ),
          ],
        );
        addTearDown(container.dispose);
        final future = container.read(sourcesProvider.future);
        await Future<void>.delayed(Duration.zero);

        expect(paths, pending.keys.toList());
        pending['/api/Farm/All']!.complete([_farm('farm-a', 'Farm A')]);
        pending['/api/Barn/All']!.complete([
          _barn('barn-a', 'Barn A', 'farm-a'),
        ]);
        pending['/api/SourceVideo/All']!.complete([
          _source('source-a', 'CAM-A', 'barn-a', true),
        ]);
        final data = await future;

        expect(data.totalSources, 1);
        expect(data.farms.single.name, 'Farm A');
        expect(data.farms.single.barns.single.name, 'Barn A');
        expect(data.onlineCount, 1);
      },
    );

    test(
      'API failure rejects the load instead of displaying partial or dummy data',
      () async {
        final container = ProviderContainer(
          retry: (_, _) => null,
          overrides: [
            apiClientProvider.overrideWithValue(
              _ApiClient((path, _) {
                if (path == '/api/Barn/All') {
                  throw StateError('Barns unavailable');
                }
                return [];
              }),
            ),
          ],
        );
        addTearDown(container.dispose);

        await expectLater(
          container.read(sourcesProvider.future),
          throwsStateError,
        );
        expect(container.read(sourcesProvider).hasError, isTrue);
      },
    );

    test(
      'refresh gets the latest API status and master display name',
      () async {
        var online = true;
        var farmName = 'Farm A';
        final container = ProviderContainer(
          overrides: [
            apiClientProvider.overrideWithValue(
              _ApiClient(
                (path, _) => switch (path) {
                  '/api/SourceVideo/All' => [
                    _source('source-a', 'CAM-A', 'barn-a', online),
                  ],
                  '/api/Barn/All' => [_barn('barn-a', 'Barn A', 'farm-a')],
                  '/api/Farm/All' => [_farm('farm-a', farmName)],
                  _ => throw StateError('Unexpected endpoint'),
                },
              ),
            ),
          ],
        );
        addTearDown(container.dispose);
        expect((await container.read(sourcesProvider.future)).onlineCount, 1);

        online = false;
        farmName = 'Renamed Farm';
        final refreshed = await container.refresh(sourcesProvider.future);

        expect(refreshed.onlineCount, 0);
        expect(refreshed.offlineCount, 1);
        expect(refreshed.farms.single.name, 'Renamed Farm');
      },
    );
  });
}

SourcesData _sample() => SourcesData.fromJson(
  sources: [
    _source('source-z', 'CAM-Z', 'barn-z', true),
    _source('source-b', 'CAM-B', 'barn-a', false),
    {
      ..._source('source-a', 'CAM-A', 'barn-a', true),
      'SourceVideoUrl': 'rtsp://example.test/camera-a',
    },
  ],
  barns: [
    _barn('barn-z', 'Barn Z', 'farm-z'),
    _barn('barn-empty', 'Empty Barn', 'farm-a'),
    {..._barn('barn-a', 'Barn A', 'farm-a'), 'Code': 'BRN-A'},
  ],
  farms: [
    _farm('farm-z', 'Zulu Farm'),
    {..._farm('farm-a', 'Alpha Farm'), 'Code': 'FRM-A'},
  ],
);

Map<String, dynamic> _source(
  String id,
  String code,
  String? barnId,
  bool online, {
  bool active = true,
}) => {
  'Id': id,
  'Code': code,
  'FkBarnId': barnId,
  'IsOnline': online,
  'IsActive': active,
};

Map<String, dynamic> _barn(
  String id,
  String name,
  String? farmId, {
  bool active = true,
}) => {'Id': id, 'Name': name, 'FkFarmId': farmId, 'IsActive': active};

Map<String, dynamic> _farm(String id, String name, {bool active = true}) => {
  'Id': id,
  'Name': name,
  'IsActive': active,
};

class _ApiClient extends ApiClient {
  _ApiClient(this.respond) : super(const SecureStore());

  final FutureOr<Object?> Function(String, Map<String, dynamic>?) respond;

  @override
  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async =>
      respond(path, query);
}
