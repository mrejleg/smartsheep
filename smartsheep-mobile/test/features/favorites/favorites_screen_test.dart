import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/network/api_client.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/core/widgets/database_menu_icon.dart';
import 'package:smartsheep_mobile/features/auth/auth_controller.dart';
import 'package:smartsheep_mobile/features/favorites/favorite_menu.dart';
import 'package:smartsheep_mobile/features/favorites/favorites_controller.dart';
import 'package:smartsheep_mobile/features/favorites/favorites_screen.dart';
import 'package:smartsheep_mobile/features/home/home_screen.dart';
import 'package:smartsheep_mobile/features/news/news_data.dart';
import 'package:smartsheep_mobile/features/shell/header_identity.dart';
import 'package:smartsheep_mobile/features/shell/member_header.dart';

void main() {
  testWidgets(
    'opening Favorites reloads IsMobile changes instead of cached menus',
    (tester) async {
      final api = _MenuApi();
      await _pumpApiScreen(tester, api, const HomeScreen());
      expect(find.text('Mobile Home'), findsOneWidget);
      api.access.first['IsMobile'] = false;
      await tester.tap(find.text('Edit'));
      await tester.pumpAndSettle();
      expect(find.byType(FavoritesScreen), findsOneWidget);
      expect(find.text('Mobile Home'), findsNothing);
      expect(find.text('Mobile Unknown Icon'), findsOneWidget);
      expect(find.text('0 of 4 selected'), findsOneWidget);
      expect(
        api.paths.where((path) => path == '/api/MobileApp/AccessMenu'),
        hasLength(2),
      );
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'Save stays in page footer and child menus are indented under their parent',
    (tester) async {
      final api = _MenuApi();
      api.access[1]['FkParentId'] = api.access[0]['Id'];
      await _pumpApiScreen(tester, api, const FavoritesScreen());
      final header = find.byType(MemberHeader);
      final save = find.byKey(const ValueKey('favorites-save'));
      expect(
        find.descendant(of: header, matching: find.text('Save')),
        findsNothing,
      );
      expect(save, findsOneWidget);
      expect(
        tester.getRect(save).top,
        greaterThan(tester.getRect(header).bottom),
      );
      final parent = find.widgetWithText(CheckboxListTile, 'Mobile Home');
      final child = find.widgetWithText(
        CheckboxListTile,
        'Mobile Unknown Icon',
      );
      expect(
        tester.getRect(child).left,
        greaterThan(tester.getRect(parent).left),
      );
      expect(
        tester.getRect(child).top,
        greaterThan(tester.getRect(parent).bottom),
      );
      expect(tester.takeException(), isNull);
    },
  );
  testWidgets('allows no more than four favorite menus', (tester) async {
    tester.view.physicalSize = const Size(430, 1400);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final menus = List.generate(
      5,
      (index) => MobileFavoriteMenu(
        id: 'menu-$index',
        name: 'Menu ${index + 1}',
        controller: 'Controller$index',
        sequenceNumber: 'a.$index',
        type: FavoriteMenuType.other,
      ),
    );

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authControllerProvider.overrideWith(_TestAuth.new),
          headerProfileImageProvider.overrideWith((_) async => null),
          unreadNotificationCountProvider.overrideWithValue(0),
          favoriteMenuCatalogProvider.overrideWith(
            (ref) async =>
                FavoriteMenuCatalog(available: menus, selected: const []),
          ),
        ],
        child: const MaterialApp(home: FavoritesScreen()),
      ),
    );
    await tester.pumpAndSettle();

    for (var index = 1; index <= 4; index++) {
      await tester.tap(find.text('Menu $index'));
      await tester.pump();
    }
    expect(find.text('4 of 4 selected'), findsOneWidget);

    await tester.tap(find.text('Menu 5'));
    await tester.pump();

    expect(find.text('4 of 4 selected'), findsOneWidget);
    expect(
      find.text('You can select up to four favorite menus.'),
      findsOneWidget,
    );
  });

  testWidgets(
    'Favorites only exposes IsMobile true menus from current AccessMenu data',
    (tester) async {
      final api = _MenuApi();
      await _pumpApiScreen(tester, api, const FavoritesScreen());
      expect(find.text('Mobile Home'), findsOneWidget);
      expect(find.text('Mobile Unknown Icon'), findsOneWidget);
      expect(find.text('Web Only'), findsNothing);
      expect(find.text('Missing Mobile Flag'), findsNothing);
      expect(find.byType(CheckboxListTile), findsNWidgets(2));
      expect(find.text('1 of 4 selected'), findsOneWidget);
      expect(
        api.paths,
        containsAll([
          '/api/MobileApp/AccessMenu',
          '/api/MobileApp/MenuFavorite',
        ]),
      );
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'Favorites uses current IconActive for selected rows and Icon when unchecked',
    (tester) async {
      final api = _MenuApi();
      await _pumpApiScreen(tester, api, const FavoritesScreen());
      DatabaseMenuIcon homeIcon() => tester.widget<DatabaseMenuIcon>(
        find.descendant(
          of: find.widgetWithText(CheckboxListTile, 'Mobile Home'),
          matching: find.byType(DatabaseMenuIcon),
        ),
      );
      expect(homeIcon().active, isTrue);
      expect(homeIcon().resolvedIcon, _activeSvg);
      expect(homeIcon().resolvedIcon, isNot(_staleSvg));

      await tester.tap(find.text('Mobile Home'));
      await tester.pumpAndSettle();
      expect(homeIcon().active, isFalse);
      expect(homeIcon().resolvedIcon, _regularSvg);
      await tester.tap(find.text('Mobile Home'));
      await tester.pumpAndSettle();
      expect(homeIcon().active, isTrue);
      expect(homeIcon().resolvedIcon, _activeSvg);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'unknown database icon does not substitute an invented controller icon',
    (tester) async {
      await _pumpApiScreen(tester, _MenuApi(), const FavoritesScreen());
      final row = find.widgetWithText(CheckboxListTile, 'Mobile Unknown Icon');
      final icon = tester.widget<DatabaseMenuIcon>(
        find.descendant(of: row, matching: find.byType(DatabaseMenuIcon)),
      );
      expect(icon.resolvedIcon, isNull);
      expect(
        find.descendant(of: row, matching: find.byIcon(Icons.circle_outlined)),
        findsOneWidget,
      );
      expect(
        find.descendant(
          of: row,
          matching: find.byIcon(Icons.dashboard_outlined),
        ),
        findsNothing,
      );
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'changing database icons for the same controller updates Favorites after refresh',
    (tester) async {
      final api = _MenuApi();
      await _pumpApiScreen(tester, api, const FavoritesScreen());
      api.access.first['IconActive'] = _changedSvg;
      await tester
          .widget<RefreshIndicator>(find.byType(RefreshIndicator))
          .onRefresh();
      await tester.pumpAndSettle();
      final icon = tester.widget<DatabaseMenuIcon>(
        find.descendant(
          of: find.widgetWithText(CheckboxListTile, 'Mobile Home'),
          matching: find.byType(DatabaseMenuIcon),
        ),
      );
      expect(api.access.first['Controller'], 'Home');
      expect(icon.resolvedIcon, _changedSvg);
      expect(icon.resolvedIcon, isNot(_staleSvg));
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'Home favorites render current active database SVG and exclude nonmobile saved menus',
    (tester) async {
      final api = _MenuApi();
      final selectedTabs = <int>[];
      await _pumpApiScreen(
        tester,
        api,
        HomeScreen(onSelectTab: selectedTabs.add),
      );
      expect(find.text('Mobile Home'), findsOneWidget);
      expect(find.text('Web Only'), findsNothing);
      expect(find.text('Missing Mobile Flag'), findsNothing);
      final icon = tester.widget<DatabaseMenuIcon>(
        find.byType(DatabaseMenuIcon),
      );
      expect(icon.active, isTrue);
      expect(icon.resolvedIcon, _activeSvg);
      expect(icon.resolvedIcon, isNot(_staleSvg));
      await tester.tap(find.text('Mobile Home'));
      await tester.pumpAndSettle();
      expect(selectedTabs, [0]);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'Home refresh picks up changed icons without controller-specific mapping',
    (tester) async {
      final api = _MenuApi();
      await _pumpApiScreen(tester, api, const HomeScreen());
      api.access.first['IconActive'] = _changedSvg;
      await tester
          .widget<RefreshIndicator>(find.byType(RefreshIndicator))
          .onRefresh();
      await tester.pumpAndSettle();
      expect(api.access.first['Controller'], 'Home');
      expect(
        tester
            .widget<DatabaseMenuIcon>(find.byType(DatabaseMenuIcon))
            .resolvedIcon,
        _changedSvg,
      );
      expect(tester.takeException(), isNull);
    },
  );
}

const _regularSvg =
    '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M3 3h18v18H3z"/></svg>';
const _activeSvg =
    '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><circle cx="12" cy="12" r="9"/></svg>';
const _changedSvg =
    '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M2 22 12 2 22 22z"/></svg>';
const _staleSvg =
    '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M1 12h22"/></svg>';

class _TestAuth extends AuthController {
  @override
  Future<AuthSession?> build() async =>
      const AuthSession(token: 'test-token', username: 'operator');
}

class _MenuApi extends Fake implements ApiClient {
  final paths = <String>[];
  final access = <Map<String, Object?>>[
    {
      'Id': 'mobile-home',
      'Name': 'Mobile Home',
      'Controller': 'Home',
      'SequenceNumber': '1',
      'IsActive': true,
      'IsMobile': true,
      'Icon': _regularSvg,
      'IconActive': _activeSvg,
    },
    {
      'Id': 'mobile-unknown',
      'Name': 'Mobile Unknown Icon',
      'Controller': 'Dashboard',
      'SequenceNumber': '2',
      'IsActive': true,
      'IsMobile': true,
      'Icon': 'unknown-icon',
    },
    {
      'Id': 'web-only',
      'Name': 'Web Only',
      'Controller': 'News',
      'SequenceNumber': '3',
      'IsActive': true,
      'IsMobile': false,
      'Icon': _regularSvg,
    },
    {
      'Id': 'missing-flag',
      'Name': 'Missing Mobile Flag',
      'Controller': 'Farm',
      'SequenceNumber': '4',
      'IsActive': true,
      'Icon': _regularSvg,
    },
  ];

  @override
  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async {
    paths.add(path);
    return switch (path) {
      '/api/MobileApp/AccessMenu' => {'MenuParents': access, 'MenuChilds': []},
      '/api/MobileApp/MenuFavorite' => [
        {
          'Id': 'mobile-home',
          'Name': 'Stale mobile title',
          'Controller': 'Home',
          'IsMobile': true,
          'Icon': _staleSvg,
          'IconActive': _staleSvg,
        },
        {
          'Id': 'web-only',
          'Name': 'Web Only',
          'Controller': 'News',
          'IsMobile': true,
        },
        {
          'Id': 'missing-flag',
          'Name': 'Missing Mobile Flag',
          'Controller': 'Farm',
          'IsMobile': true,
        },
      ],
      _ => throw StateError('Unexpected API request: $path'),
    };
  }
}

Future<void> _pumpApiScreen(
  WidgetTester tester,
  _MenuApi api,
  Widget screen,
) async {
  tester.view.physicalSize = const Size(393, 1400);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        apiClientProvider.overrideWithValue(api),
        authControllerProvider.overrideWith(_TestAuth.new),
        headerProfileImageProvider.overrideWith((_) async => null),
        unreadNotificationCountProvider.overrideWithValue(0),
        newsItemsProvider.overrideWith((_) async => []),
      ],
      child: MaterialApp(theme: AppTheme.light, home: screen),
    ),
  );
  await tester.pumpAndSettle();
}
