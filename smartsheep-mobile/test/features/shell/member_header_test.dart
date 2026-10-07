import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/core/widgets/app_header.dart';
import 'package:smartsheep_mobile/features/account/account_screen.dart';
import 'package:smartsheep_mobile/features/auth/auth_controller.dart';
import 'package:smartsheep_mobile/features/favorites/favorite_menu.dart';
import 'package:smartsheep_mobile/features/favorites/favorites_controller.dart';
import 'package:smartsheep_mobile/features/favorites/favorites_screen.dart';
import 'package:smartsheep_mobile/features/notifications/notifications_screen.dart';
import 'package:smartsheep_mobile/features/shell/header_identity.dart';
import 'package:smartsheep_mobile/features/shell/member_header.dart';
import 'package:smartsheep_mobile/features/sources/sources_data.dart';
import 'package:smartsheep_mobile/features/sources/sources_screen.dart';

class _SignedInAuth extends AuthController {
  @override
  Future<AuthSession?> build() async => const AuthSession(
    token: 'test-token',
    username: 'FARM.OPERATOR',
    profile: {'FullName': 'Farm Operator', 'Email': 'farm@example.test'},
  );
}

class _FavoritesRepository extends Fake implements FavoritesRepository {
  String? email;
  List<String>? ids;

  @override
  Future<void> save({required String email, required List<String> ids}) async {
    this.email = email;
    this.ids = ids;
  }
}

Future<void> _pump(
  WidgetTester tester,
  Widget child, {
  double width = 393,
  _FavoritesRepository? favorites,
}) async {
  tester.view.physicalSize = Size(width, 852);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        authControllerProvider.overrideWith(_SignedInAuth.new),
        assignedFarmsProvider.overrideWith((_) async => []),
        headerProfileImageProvider.overrideWith((_) async => null),
        notificationsProvider.overrideWith(
          (_) async => [
            {'Id': '1', 'Title': 'Unread alert', 'IsRead': false},
            {'Id': '2', 'Title': 'Read alert', 'IsRead': true},
          ],
        ),
        sourcesProvider.overrideWith((_) async => const SourcesData()),
        if (favorites != null) ...[
          favoritesRepositoryProvider.overrideWithValue(favorites),
          favoriteMenuCatalogProvider.overrideWith(
            (_) async => const FavoriteMenuCatalog(
              available: [
                MobileFavoriteMenu(
                  id: 'home',
                  name: 'Home',
                  controller: 'Home',
                  sequenceNumber: '1',
                  type: FavoriteMenuType.home,
                ),
              ],
              selected: [],
            ),
          ),
        ],
      ],
      child: MaterialApp(theme: AppTheme.light, home: child),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets(
    'member header presents session identity and actual unread count',
    (tester) async {
      await _pump(
        tester,
        const Scaffold(
          body: MemberHeader(
            title: 'Sources',
            subtitle: 'Source videos & availability',
          ),
        ),
      );
      final header = tester.widget<AppHeader>(find.byType(AppHeader));
      expect(header.profileName, 'Farm Operator');
      expect(header.profileImage, isNull);
      expect(header.onProfile, isNotNull);
      expect(header.title, 'Sources');
      expect(
        tester
            .widget<NotificationAction>(find.byType(NotificationAction))
            .count,
        1,
      );
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'notification button opens notifications and back returns to source page',
    (tester) async {
      await _pump(tester, const SourcesScreen());
      await tester.tap(find.byType(NotificationAction));
      await tester.pumpAndSettle();
      expect(find.byType(NotificationsScreen), findsOneWidget);
      expect(find.text('Unread alert'), findsOneWidget);
      expect(find.byType(NotificationAction), findsNothing);
      await tester.pageBack();
      await tester.pumpAndSettle();
      expect(find.byType(NotificationsScreen), findsNothing);
      expect(find.text('Barns grouped by farm'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'profile route has a back button and does not recursively offer itself',
    (tester) async {
      await _pump(tester, const SourcesScreen());
      tester.widget<AppHeader>(find.byType(AppHeader)).onProfile!();
      await tester.pumpAndSettle();
      expect(find.byType(AccountScreen), findsOneWidget);
      expect(
        tester.widget<AccountScreen>(find.byType(AccountScreen)).showBackButton,
        isTrue,
      );
      expect(
        tester.widget<AppHeader>(find.byType(AppHeader)).onProfile,
        isNull,
      );
      expect(find.text('Farm Operator'), findsOneWidget);
      await tester.pageBack();
      await tester.pumpAndSettle();
      expect(find.byType(AccountScreen), findsNothing);
      expect(find.text('Barns grouped by farm'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('source header has no dead search or filter buttons', (
    tester,
  ) async {
    await _pump(tester, const SourcesScreen(), width: 320);
    final header = find.byType(MemberHeader);
    expect(
      find.descendant(of: header, matching: find.byIcon(Icons.search)),
      findsNothing,
    );
    expect(
      find.descendant(
        of: header,
        matching: find.byIcon(Icons.filter_alt_outlined),
      ),
      findsNothing,
    );
    expect(find.byKey(const ValueKey('sources-filter-all')), findsOneWidget);
    expect(find.byKey(const ValueKey('sources-filter-online')), findsOneWidget);
    expect(
      find.byKey(const ValueKey('sources-filter-offline')),
      findsOneWidget,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'Favorites preserves Save action and saves selected menu before returning',
    (tester) async {
      final repository = _FavoritesRepository();
      await _pump(
        tester,
        Builder(
          builder: (context) => Scaffold(
            body: Center(
              child: FilledButton(
                onPressed: () => Navigator.of(context).push(
                  MaterialPageRoute<void>(
                    builder: (_) => const FavoritesScreen(),
                  ),
                ),
                child: const Text('Open favorites'),
              ),
            ),
          ),
        ),
        width: 320,
        favorites: repository,
      );
      await tester.tap(find.text('Open favorites'));
      await tester.pumpAndSettle();
      expect(find.text('Save'), findsOneWidget);
      await tester.tap(find.text('Home'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Save'));
      await tester.pumpAndSettle();
      expect(repository.email, 'farm@example.test');
      expect(repository.ids, ['home']);
      expect(find.byType(FavoritesScreen), findsNothing);
      expect(find.text('Open favorites'), findsOneWidget);
      expect(find.text('Favorites updated.'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );
}
