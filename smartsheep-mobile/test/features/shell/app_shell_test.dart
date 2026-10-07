import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/account/account_screen.dart';
import 'package:smartsheep_mobile/features/auth/auth_controller.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_data.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_screen.dart';
import 'package:smartsheep_mobile/features/favorites/favorite_menu.dart';
import 'package:smartsheep_mobile/features/favorites/favorites_controller.dart';
import 'package:smartsheep_mobile/features/home/home_screen.dart';
import 'package:smartsheep_mobile/features/news/news_data.dart';
import 'package:smartsheep_mobile/features/notifications/notifications_screen.dart';
import 'package:smartsheep_mobile/features/shell/app_shell.dart';
import 'package:smartsheep_mobile/features/shell/header_identity.dart';
import 'package:smartsheep_mobile/features/sources/sources_data.dart';
import 'package:smartsheep_mobile/features/sources/sources_screen.dart';

void main() {
  for (final mobile in [true, false, null]) {
    testWidgets('four fixed tabs ignore IsMobile=$mobile', (tester) async {
      await _pump(
        tester,
        load: () => _catalog([
          _menu('home-id', 'Home', mobile: mobile),
          _menu('dashboard-id', 'Dashboard', mobile: mobile),
          _menu('source-id', 'SourceVideo', mobile: mobile),
        ]),
      );
      _expectFourTabs(tester);
      expect(find.byType(HomeScreen), findsOneWidget);
      expect(find.byType(DashboardScreen, skipOffstage: false), findsNothing);
      expect(tester.takeException(), isNull);
    });
  }

  testWidgets('empty database menus still allow all four routes', (
    tester,
  ) async {
    await _pump(tester, load: () => const FavoriteMenuCatalog.empty());
    _expectFourTabs(tester);
    final screens = [HomeScreen, DashboardScreen, SourcesScreen, AccountScreen];
    final names = ['home', 'dashboard', 'sources', 'account'];
    for (var index = 0; index < names.length; index++) {
      await tester.tap(_tab(names[index]));
      await tester.pumpAndSettle();
      expect(find.byType(screens[index]), findsOneWidget);
      expect(
        tester.widget<NavigationBar>(find.byType(NavigationBar)).selectedIndex,
        index,
      );
      _expectFourTabs(tester);
      expect(tester.takeException(), isNull);
    }
    await tester.tap(_tab('home'));
    await tester.pumpAndSettle();
    expect(find.byType(HomeScreen), findsOneWidget);
    expect(find.byType(SourcesScreen, skipOffstage: false), findsOneWidget);
  });

  testWidgets('bottom icons stay static regardless of database icon data', (
    tester,
  ) async {
    await _pump(
      tester,
      load: () => _catalog([
        _menu('home-id', 'Home', icon: '<svg/>', activeIcon: '<svg/>'),
      ]),
    );
    final icons = [
      Icons.home_outlined,
      Icons.pie_chart_outline,
      Icons.videocam_outlined,
      Icons.person_outline,
    ];
    final activeIcons = [
      Icons.home,
      Icons.pie_chart,
      Icons.videocam,
      Icons.person,
    ];
    final destinations = tester
        .widget<NavigationBar>(find.byType(NavigationBar))
        .destinations;
    for (var index = 0; index < destinations.length; index++) {
      final destination = destinations[index] as NavigationDestination;
      expect((destination.icon as Icon).icon, icons[index]);
      expect((destination.selectedIcon as Icon).icon, activeIcons[index]);
      expect((destination.selectedIcon as Icon).color, Colors.white);
    }
  });

  testWidgets('Home callbacks use fixed route IDs and ignore invalid indexes', (
    tester,
  ) async {
    await _pump(tester, load: () => const FavoriteMenuCatalog.empty());
    final select = tester
        .widget<HomeScreen>(find.byType(HomeScreen))
        .onSelectTab!;
    final screens = [HomeScreen, DashboardScreen, SourcesScreen, AccountScreen];
    for (var index = 1; index < screens.length; index++) {
      select(index);
      await tester.pumpAndSettle();
      expect(find.byType(screens[index]), findsOneWidget);
    }
    select(-1);
    select(4);
    await tester.pumpAndSettle();
    expect(find.byType(AccountScreen), findsOneWidget);
    select(0);
    await tester.pumpAndSettle();
    expect(find.byType(HomeScreen), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('loading favorite menus does not block navigation', (
    tester,
  ) async {
    final pending = Completer<FavoriteMenuCatalog>();
    await _pump(tester, load: () => pending.future, settle: false);
    _expectFourTabs(tester);
    expect(find.byType(HomeScreen), findsOneWidget);
    await tester.tap(_tab('sources'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));
    expect(find.byType(SourcesScreen), findsOneWidget);
    pending.complete(const FavoriteMenuCatalog.empty());
    await tester.pumpAndSettle();
    _expectFourTabs(tester);
    expect(find.byType(SourcesScreen), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('failed favorite menus leave all tabs usable', (tester) async {
    await _pump(
      tester,
      load: () => throw StateError('Menu service unavailable'),
    );
    _expectFourTabs(tester);
    expect(find.byType(HomeScreen), findsOneWidget);
    await tester.tap(_tab('account'));
    await tester.pumpAndSettle();
    expect(find.byType(AccountScreen), findsOneWidget);
    await tester.tap(_tab('dashboard'));
    await tester.pumpAndSettle();
    expect(find.byType(DashboardScreen), findsOneWidget);
    _expectFourTabs(tester);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'menu refresh, flag changes and failure preserve the active tab',
    (tester) async {
      var catalog = _catalog([_menu('source-id', 'SourceVideo')]);
      var fails = false;
      final pending = Completer<FavoriteMenuCatalog>();
      var refreshing = false;
      final container = await _pump(
        tester,
        load: () {
          if (fails) throw StateError('Menu service unavailable');
          return refreshing ? pending.future : catalog;
        },
      );
      await tester.tap(_tab('sources'));
      await tester.pumpAndSettle();
      refreshing = true;
      container.invalidate(favoriteMenuCatalogProvider);
      await tester.pump();
      _expectFourTabs(tester);
      expect(find.byType(SourcesScreen), findsOneWidget);
      catalog = _catalog([_menu('source-id', 'SourceVideo', mobile: false)]);
      pending.complete(catalog);
      await tester.pumpAndSettle();
      _expectFourTabs(tester);
      expect(find.byType(SourcesScreen), findsOneWidget);
      fails = true;
      container.invalidate(favoriteMenuCatalogProvider);
      await tester.pumpAndSettle();
      _expectFourTabs(tester);
      expect(find.byType(SourcesScreen), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('four tabs fit at 320px with 2x text', (tester) async {
    await _pump(
      tester,
      load: () => const FavoriteMenuCatalog.empty(),
      width: 320,
      textScale: 2,
    );
    _expectFourTabs(tester);
    await tester.tap(_tab('account'));
    await tester.pumpAndSettle();
    expect(find.byType(AccountScreen), findsOneWidget);
    final bar = tester.getRect(find.byType(NavigationBar));
    for (final name in ['home', 'dashboard', 'sources', 'account']) {
      final rect = tester.getRect(_tab(name));
      expect(rect.left, greaterThanOrEqualTo(bar.left));
      expect(rect.right, lessThanOrEqualTo(bar.right));
    }
    expect(tester.takeException(), isNull);
  });
}

void _expectFourTabs(WidgetTester tester) {
  final bar = tester.widget<NavigationBar>(find.byType(NavigationBar));
  expect(
    bar.destinations.map((item) => (item as NavigationDestination).label),
    ['Home', 'Dashboard', 'Sources', 'Account'],
  );
  for (final name in ['home', 'dashboard', 'sources', 'account']) {
    expect(_tab(name), findsOneWidget);
  }
}

Finder _tab(String name) => find.byKey(ValueKey('shell-tab-$name'));

Future<ProviderContainer> _pump(
  WidgetTester tester, {
  required FutureOr<FavoriteMenuCatalog> Function() load,
  bool settle = true,
  double width = 393,
  double textScale = 1,
}) async {
  tester.view.physicalSize = Size(width, 852);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  final container = ProviderContainer(
    retry: (_, _) => null,
    overrides: [
      authControllerProvider.overrideWith(_SignedInAuth.new),
      assignedFarmsProvider.overrideWith((_) async => []),
      favoriteMenuCatalogProvider.overrideWith((_) async => load()),
      headerProfileImageProvider.overrideWith((_) async => null),
      notificationsProvider.overrideWith((_) async => const []),
      newsItemsProvider.overrideWith((_) async => const []),
      sourcesProvider.overrideWith((_) async => const SourcesData()),
      // This suite verifies routing, not the dashboard's map/network rendering.
      dashboardProvider.overrideWith(
        (_) async => throw StateError('Test dashboard data unavailable'),
      ),
      dashboardOptionsProvider.overrideWith(
        (_) async => const DashboardFilterOptions(),
      ),
    ],
  );
  addTearDown(container.dispose);
  await tester.pumpWidget(
    UncontrolledProviderScope(
      container: container,
      child: MaterialApp(
        theme: AppTheme.light,
        builder: (context, child) => MediaQuery(
          data: MediaQuery.of(
            context,
          ).copyWith(textScaler: TextScaler.linear(textScale)),
          child: child!,
        ),
        home: const AppShell(),
      ),
    ),
  );
  if (settle) {
    await tester.pumpAndSettle();
  } else {
    await tester.pump();
  }
  return container;
}

FavoriteMenuCatalog _catalog(List<Map<String, dynamic>> menus) =>
    FavoriteMenuCatalog.fromData(
      accessMenuResponse: {'MenuParents': menus, 'MenuChilds': const []},
      favoriteResponse: const [],
    );

Map<String, dynamic> _menu(
  String id,
  String controller, {
  Object? mobile = true,
  String? icon,
  String? activeIcon,
}) => {
  'Id': id,
  'Name': controller,
  'Controller': controller,
  'SequenceNumber': id,
  'IsMobile': mobile,
  'IsActive': true,
  'IsSection': false,
  'Icon': icon,
  'IconActive': activeIcon,
};

class _SignedInAuth extends AuthController {
  @override
  Future<AuthSession?> build() async => const AuthSession(
    token: 'test-token',
    username: 'TEST.USER',
    profile: {'FullName': 'Test User'},
  );
}
