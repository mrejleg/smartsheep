import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/network/api_client.dart';
import 'package:smartsheep_mobile/core/network/secure_store.dart';
import 'package:smartsheep_mobile/features/favorites/favorite_menu.dart';
import 'package:smartsheep_mobile/features/favorites/favorites_controller.dart';

void main() {
  test(
    'grouping never restores non-mobile parents or children or favorites',
    () {
      final catalog = FavoriteMenuCatalog.fromData(
        accessMenuResponse: {
          'MenuParents': [
            _menu('mobile-parent', 'Mobile parent', ''),
            {..._menu('web-parent', 'Web parent', ''), 'IsMobile': false},
          ],
          'MenuChilds': [
            {
              ..._menu('mobile-child', 'Mobile child', 'News'),
              'FkParentId': 'web-parent',
            },
            {
              ..._menu('web-child', 'Web child', 'News'),
              'FkParentId': 'mobile-parent',
              'IsMobile': false,
            },
          ],
        },
        favoriteResponse: [
          _menu('web-parent', 'Stale parent', ''),
          _menu('web-child', 'Stale child', 'News'),
          _menu('mobile-child', 'Mobile child', 'News'),
        ],
      );
      expect(catalog.groupedMenus.map((row) => row.menu.id).toSet(), {
        'mobile-parent',
        'mobile-child',
      });
      expect(catalog.groupedMenus.every((row) => row.depth == 0), isTrue);
      expect(catalog.selected.map((menu) => menu.id), ['mobile-child']);
    },
  );

  test(
    'groups children by database parent IDs, not adjacent sequence names',
    () {
      final catalog = FavoriteMenuCatalog.fromData(
        accessMenuResponse: {
          'MenuParents': [
            {..._menu('logs', 'Logs', ''), 'SequenceNumber': 'a'},
            {..._menu('other', 'Other', 'News'), 'SequenceNumber': 'b'},
          ],
          'MenuChilds': [
            {
              ..._menu('child', 'Reminder Logs', 'ReminderLog'),
              'FkParentId': 'logs',
              'SequenceNumber': 'z',
            },
          ],
        },
        favoriteResponse: [],
      );
      expect(catalog.groupedMenus.map((entry) => entry.menu.id), [
        'logs',
        'child',
        'other',
      ]);
      expect(catalog.groupedMenus.map((entry) => entry.depth), [0, 1, 0]);
      expect(
        catalog.available.firstWhere((item) => item.id == 'child').parentId,
        'logs',
      );
    },
  );

  test(
    'orphans and cyclic parents render once without inventing parent records',
    () {
      final catalog = FavoriteMenuCatalog.fromData(
        accessMenuResponse: {
          'MenuChilds': [
            {..._menu('a', 'A', 'News'), 'fkParentId': 'b'},
            {..._menu('b', 'B', 'News'), 'FkParentId': 'a'},
            {..._menu('orphan', 'Orphan', 'News'), 'FkParentId': 'missing'},
          ],
        },
        favoriteResponse: [],
      );
      expect(catalog.groupedMenus.map((entry) => entry.menu.id).toSet(), {
        'a',
        'b',
        'orphan',
      });
      expect(catalog.groupedMenus, hasLength(3));
    },
  );
  test('reads database Icon and IconActive with either API key casing', () {
    final pascal = MobileFavoriteMenu.fromJson({
      ..._menu('dashboard-id', 'Dashboard', 'Dashboard'),
      'Icon': '  mdi mdi-view-dashboard-outline  ',
      'IconActive': ' mdi mdi-view-dashboard ',
    });
    final camel = MobileFavoriteMenu.fromJson({
      'id': 'news-id',
      'name': 'News',
      'controller': 'News',
      'sequenceNumber': 'b.2',
      'isMobile': true,
      'isActive': true,
      'isSection': false,
      'icon': 'ri-newspaper-line',
      'iconActive': 'ri-newspaper-fill',
    });

    expect(pascal?.iconName, 'mdi mdi-view-dashboard-outline');
    expect(pascal?.iconActiveName, 'mdi mdi-view-dashboard');
    expect(camel?.iconName, 'ri-newspaper-line');
    expect(camel?.iconActiveName, 'ri-newspaper-fill');
  });

  test('missing, empty, and non-string database icon fields remain null', () {
    for (final icon in [null, '', '   ', 42, false]) {
      final menu = MobileFavoriteMenu.fromJson({
        ..._menu('menu-id', 'Menu', 'CustomController'),
        'Icon': icon,
        'IconActive': icon,
      });

      expect(menu, isNotNull);
      expect(menu!.iconName, isNull);
      expect(menu.iconActiveName, isNull);
    }
  });

  test(
    'requires a boolean true IsMobile instead of accepting missing flags',
    () {
      for (final flag in [false, null, 'true', 'false', 1, 0]) {
        expect(
          MobileFavoriteMenu.fromJson({
            ..._menu('menu-id', 'Menu', 'News'),
            'IsMobile': flag,
          }),
          isNull,
        );
        expect(
          MobileFavoriteMenu.fromJson({
            'id': 'menu-id',
            'name': 'Menu',
            'controller': 'News',
            'isMobile': flag,
          }),
          isNull,
        );
      }
      final missingFlag = _menu('menu-id', 'Menu', 'News')..remove('IsMobile');
      expect(MobileFavoriteMenu.fromJson(missingFlag), isNull);
      expect(
        MobileFavoriteMenu.fromJson({
          ..._menu('menu-id', 'Menu', 'News'),
          'IsMobile': null,
          'isMobile': true,
        }),
        isNull,
      );
    },
  );

  test('mobile sections, inactive records, and missing IDs are excluded', () {
    for (final fields in [
      {'IsSection': true},
      {'IsActive': false},
      {'Id': ''},
    ]) {
      expect(
        MobileFavoriteMenu.fromJson({
          ..._menu('menu-id', 'Menu', 'News'),
          ...fields,
        }),
        isNull,
      );
    }
  });

  test(
    'favorite payload IDs resolve to current canonical access menu objects',
    () {
      final catalog = FavoriteMenuCatalog.fromData(
        accessMenuResponse: {
          'MenuParents': [
            {
              ..._menu('same-id', 'Renamed News', 'News'),
              'Icon': 'ri-newspaper-line',
              'IconActive': 'ri-newspaper-fill',
              'SequenceNumber': 'current-order',
            },
          ],
        },
        favoriteResponse: [
          {
            'Id': 'same-id',
            'Name': 'Old Dashboard',
            'Controller': 'Dashboard',
            'SequenceNumber': 'stale-order',
            'Icon': 'old-icon',
            'IconActive': 'old-active-icon',
            'IsMobile': false,
            'IsActive': false,
          },
        ],
      );

      expect(catalog.selected.single, same(catalog.available.single));
      final selected = catalog.selected.single;
      expect(selected.label, 'Renamed News');
      expect(selected.controller, 'News');
      expect(selected.type, FavoriteMenuType.news);
      expect(selected.sequenceNumber, 'current-order');
      expect(selected.iconName, 'ri-newspaper-line');
      expect(selected.iconActiveName, 'ri-newspaper-fill');
    },
  );

  test(
    'a stale mobile favorite cannot re-enable a currently disallowed menu',
    () {
      final catalog = FavoriteMenuCatalog.fromData(
        accessMenuResponse: {
          'MenuParents': [
            {..._menu('web-only', 'Web Menu', 'News'), 'IsMobile': false},
            {..._menu('inactive', 'Inactive', 'News'), 'IsActive': false},
            {..._menu('section', 'Section', 'News'), 'IsSection': true},
            {'Id': 'unspecified', 'Name': 'No flag', 'Controller': 'News'},
            _menu('allowed', 'Allowed Menu', 'News'),
          ],
        },
        favoriteResponse: [
          for (final id in [
            'web-only',
            'inactive',
            'section',
            'unspecified',
            'absent',
            'allowed',
          ])
            _menu(id, 'Stale mobile name', 'Dashboard'),
        ],
      );

      expect(catalog.available.map((menu) => menu.id), ['allowed']);
      expect(catalog.selected.map((menu) => menu.id), ['allowed']);
      expect(catalog.selected.single.label, 'Allowed Menu');
    },
  );

  test(
    'de-duplicates menu IDs and preserves the first four selected IDs in response order',
    () {
      final menus = [
        for (var index = 1; index <= 6; index++)
          {
            ..._menu('menu-$index', 'Menu $index', 'News'),
            'SequenceNumber': '$index',
          },
      ];
      final catalog = FavoriteMenuCatalog.fromData(
        accessMenuResponse: {
          'MenuParents': menus,
          'MenuChilds': [menus.first, menus[1]],
        },
        favoriteResponse: [
          for (final id in [
            'menu-6',
            'menu-6',
            'missing',
            'menu-4',
            'menu-2',
            'menu-5',
            'menu-1',
          ])
            {'id': id},
        ],
      );

      expect(catalog.available, hasLength(6));
      expect(catalog.available.map((menu) => menu.id), [
        'menu-1',
        'menu-2',
        'menu-3',
        'menu-4',
        'menu-5',
        'menu-6',
      ]);
      expect(catalog.selected.map((menu) => menu.id), [
        'menu-6',
        'menu-4',
        'menu-2',
        'menu-5',
      ]);
      for (final menu in catalog.selected) {
        expect(
          menu,
          same(catalog.available.firstWhere((item) => item.id == menu.id)),
        );
      }
    },
  );

  test('a database icon update is honored without changing the controller', () {
    FavoriteMenuCatalog catalogWith(String icon) =>
        FavoriteMenuCatalog.fromData(
          accessMenuResponse: {
            'MenuParents': [
              {..._menu('news-id', 'News', 'News'), 'Icon': icon},
            ],
          },
          favoriteResponse: [
            {'Id': 'news-id', 'Icon': 'stale-icon'},
          ],
        );

    final before = catalogWith('ri-newspaper-line').selected.single;
    final after = catalogWith('ri-article-line').selected.single;

    expect(before.type, after.type);
    expect(before.controller, after.controller);
    expect(before.iconName, 'ri-newspaper-line');
    expect(after.iconName, 'ri-article-line');
  });

  test('builds a mobile catalog from PascalCase API data', () {
    final catalog = FavoriteMenuCatalog.fromData(
      accessMenuResponse: {
        'MenuParents': [
          {
            'Id': 'dashboard-id',
            'Name': 'Dashboard',
            'Controller': 'Dashboard',
            'SequenceNumber': 'b.1',
            'IsSection': false,
            'IsActive': true,
            'IsMobile': true,
          },
          {
            'Id': 'section-id',
            'Name': 'FEATURES',
            'Controller': null,
            'SequenceNumber': 'b.0',
            'IsSection': true,
            'IsActive': true,
            'IsMobile': true,
          },
        ],
        'MenuChilds': [
          {
            'Id': 'source-id',
            'Name': 'Source Videos',
            'Controller': 'SourceVideo',
            'SequenceNumber': 'c.1.13',
            'IsSection': false,
            'IsActive': true,
            'IsMobile': true,
          },
          {
            'Id': 'unsupported-id',
            'Name': 'App Clients',
            'Controller': 'AppClient',
            'SequenceNumber': 'c.3.1',
            'IsSection': false,
            'IsActive': true,
            'IsMobile': true,
          },
        ],
      },
      favoriteResponse: [
        {
          'Id': 'source-id',
          'Name': 'Source Videos',
          'Controller': 'SourceVideo',
          'SequenceNumber': 'c.1.13',
          'IsSection': false,
          'IsActive': true,
          'IsMobile': true,
        },
      ],
    );

    expect(catalog.available.map((menu) => menu.label), [
      'Dashboard',
      'Source Videos',
      'App Clients',
    ]);
    expect(catalog.selected.single.id, 'source-id');
    expect(catalog.selected.single.type, FavoriteMenuType.liveStreams);
  });

  test('keeps each actionable menu returned by AccessMenu', () {
    final catalog = FavoriteMenuCatalog.fromData(
      accessMenuResponse: {
        'menuParents': const [],
        'menuChilds': [
          {
            'id': 'template-id',
            'name': 'Reminder Templates',
            'controller': 'ReminderTemplate',
            'sequenceNumber': 'c.2.11',
            'isSection': false,
            'isActive': true,
            'isMobile': true,
          },
          {
            'id': 'report-id',
            'name': 'Reminder Report',
            'controller': 'ReminderReport',
            'sequenceNumber': 'b.12.3',
            'isSection': false,
            'isActive': true,
            'isMobile': true,
          },
        ],
      },
      favoriteResponse: [
        {
          'id': 'report-id',
          'name': 'Reminder Report',
          'controller': 'ReminderReport',
          'sequenceNumber': 'b.12.3',
          'isSection': false,
          'isActive': true,
          'isMobile': true,
        },
      ],
    );

    expect(catalog.available, hasLength(2));
    expect(catalog.available.map((menu) => menu.id), [
      'report-id',
      'template-id',
    ]);
    expect(catalog.selected.single.id, 'report-id');
  });

  test('ignores inactive and unsupported favorites', () {
    final catalog = FavoriteMenuCatalog.fromData(
      accessMenuResponse: const {},
      favoriteResponse: [
        {
          'Id': 'inactive-id',
          'Name': 'News',
          'Controller': 'News',
          'IsActive': false,
        },
        {
          'Id': 'unsupported-id',
          'Name': 'App Role',
          'Controller': 'AppRole',
          'IsActive': true,
          'IsMobile': true,
        },
      ],
    );

    expect(catalog.available, isEmpty);
    expect(catalog.selected, isEmpty);
  });

  test('limits favorites returned to Home to four menus', () {
    final menus =
        [
              ('source-id', 'Source Videos', 'SourceVideo'),
              (
                'streamer-id',
                'Source Video Status Logs',
                'SourceVideoStatusLog',
              ),
              ('reminder-id', 'Reminder Templates', 'ReminderTemplate'),
              (
                'report-id',
                'Suckling Activity Report',
                'SucklingActivityReport',
              ),
              ('statistics-id', 'Suckling Statistics', 'SucklingStatistic'),
            ]
            .map(
              (menu) => {
                'Id': menu.$1,
                'Name': menu.$2,
                'Controller': menu.$3,
                'IsSection': false,
                'IsActive': true,
                'IsMobile': true,
              },
            )
            .toList();

    final catalog = FavoriteMenuCatalog.fromData(
      accessMenuResponse: {'MenuParents': const [], 'MenuChilds': menus},
      favoriteResponse: menus,
    );

    expect(catalog.available, hasLength(5));
    expect(catalog.selected, hasLength(maxFavoriteMenus));
  });

  test('loads the available list from the AccessMenu API', () async {
    final client = _RecordingApiClient();
    final repository = FavoritesRepository(client);

    await repository.fetchAccessMenu('SuperAdmin');

    expect(client.lastGetPath, '/api/MobileApp/AccessMenu');
    expect(client.lastQuery, {'username': 'SuperAdmin'});
  });

  test('saves the complete favorite selection through the API', () async {
    final client = _RecordingApiClient();
    final repository = FavoritesRepository(client);

    await repository.save(email: 'user@example.com', ids: ['one', 'two']);

    expect(client.lastPostPath, '/api/MobileApp/MobilePostFavoriteMenu');
    expect(client.lastPostData, {
      'Email': 'user@example.com',
      'FkAppMenuIds': ['one', 'two'],
    });
  });
}

Map<String, dynamic> _menu(String id, String name, String controller) => {
  'Id': id,
  'Name': name,
  'Controller': controller,
  'IsMobile': true,
  'IsActive': true,
  'IsSection': false,
};

class _RecordingApiClient extends ApiClient {
  _RecordingApiClient() : super(const SecureStore());

  String? lastGetPath;
  Map<String, dynamic>? lastQuery;
  String? lastPostPath;
  Object? lastPostData;

  @override
  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async {
    lastGetPath = path;
    lastQuery = query;
    return const {};
  }

  @override
  Future<dynamic> post(String path, {Object? data}) async {
    lastPostPath = path;
    lastPostData = data;
    return true;
  }
}
