import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/account/account_screen.dart';
import 'package:smartsheep_mobile/features/account/assigned_farms_screen.dart';
import 'package:smartsheep_mobile/features/auth/auth_controller.dart';
import 'package:smartsheep_mobile/features/auth/login_screen.dart';
import 'package:smartsheep_mobile/features/shell/header_identity.dart';

final _navigator = GlobalKey<NavigatorState>();

void main() {
  testWidgets('account shows only the farm count until the row is tapped', (
    tester,
  ) async {
    await _pump(
      tester,
      farms: [
        {'Id': 'a', 'Code': 'FRM-A', 'Name': 'Farm Alpha'},
        {'Id': 'b', 'Code': 'FRM-B', 'Name': 'Farm Beta'},
      ],
    );
    expect(find.text('Assigned Farms (2)'), findsOneWidget);
    // The rows live on their own page now, like Notifications.
    expect(find.text('Farm Alpha'), findsNothing);

    await tester.tap(find.byKey(const ValueKey('account-assigned-farms')));
    await tester.pumpAndSettle();

    expect(find.byType(AssignedFarmsScreen), findsOneWidget);
    expect(find.text('Farm Alpha'), findsOneWidget);
    expect(find.text('FRM-A'), findsOneWidget);
    expect(find.text('Farm Beta'), findsOneWidget);
    expect(find.text('FRM-B'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('farms page shows empty assignments without fake farms', (
    tester,
  ) async {
    await _pump(tester);
    expect(find.text('Assigned Farms (0)'), findsOneWidget);

    await tester.tap(find.byKey(const ValueKey('account-assigned-farms')));
    await tester.pumpAndSettle();

    expect(
      find.text('No farms assigned. Contact your administrator.'),
      findsOneWidget,
    );
  });

  testWidgets('cancel and dismiss keep the signed-in session', (tester) async {
    final auth = await _pump(tester);
    await _open(tester);
    expect(auth.calls, 0);
    await tester.tap(find.text('Cancel'));
    await tester.pumpAndSettle();
    expect(find.byType(AccountScreen), findsOneWidget);
    await _open(tester);
    await tester.tapAt(const Offset(5, 5));
    await tester.pumpAndSettle();
    expect(find.byType(AlertDialog), findsNothing);
    expect(auth.calls, 0);
  });

  for (final nested in [false, true]) {
    testWidgets(
      'confirmed sign out shows Login and clears routes (nested=$nested)',
      (tester) async {
        final auth = await _pump(tester);
        if (nested) {
          _navigator.currentState!.push(
            MaterialPageRoute<void>(
              builder: (_) => const AccountScreen(showBackButton: true),
            ),
          );
          await tester.pumpAndSettle();
        }
        await _open(tester);
        await tester.tap(find.byKey(const ValueKey('confirm-sign-out')));
        await tester.pumpAndSettle();
        expect(auth.calls, 1);
        expect(find.byType(LoginScreen), findsOneWidget);
        expect(find.byType(AccountScreen, skipOffstage: false), findsNothing);
        expect(_navigator.currentState!.canPop(), isFalse);
        expect(tester.takeException(), isNull);
      },
    );
  }

  testWidgets(
    'pending sign out is disabled and a failed logout remains retryable',
    (tester) async {
      final auth = await _pump(tester);
      auth.pending = Completer<void>();
      await _open(tester);
      await tester.tap(find.byKey(const ValueKey('confirm-sign-out')));
      await tester.pumpAndSettle();
      expect(
        tester
            .widget<OutlinedButton>(
              find.byKey(const ValueKey('account-sign-out')),
            )
            .onPressed,
        isNull,
      );
      auth.pending!.completeError(StateError('Storage unavailable'));
      await tester.pumpAndSettle();
      expect(find.byType(AccountScreen), findsOneWidget);
      expect(
        find.text('Unable to sign out. Please try again.'),
        findsOneWidget,
      );
      expect(
        tester
            .widget<OutlinedButton>(
              find.byKey(const ValueKey('account-sign-out')),
            )
            .onPressed,
        isNotNull,
      );
    },
  );

  testWidgets('repeated pulls at the top do not refetch on every drag', (
    tester,
  ) async {
    var fetches = 0;
    tester.view.physicalSize = const Size(430, 1000);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authControllerProvider.overrideWith(_Auth.new),
          assignedFarmsProvider.overrideWith((ref) async {
            fetches++;
            return [
              {'Id': 'a', 'Code': 'FRM-A', 'Name': 'Farm Alpha'},
            ];
          }),
          unreadNotificationCountProvider.overrideWithValue(0),
        ],
        child: const MaterialApp(home: AssignedFarmsScreen()),
      ),
    );
    await tester.pumpAndSettle();
    expect(fetches, 1);

    // Three pulls in a row, as happens when dragging up a long list.
    for (var i = 0; i < 3; i++) {
      await tester.fling(find.byType(ListView), const Offset(0, 300), 1000);
      await tester.pumpAndSettle();
    }

    // One refetch, not one per drag.
    expect(fetches, 2);
  });
}

Future<void> _open(WidgetTester tester) async {
  await tester.ensureVisible(find.byKey(const ValueKey('account-sign-out')));
  await tester.tap(find.byKey(const ValueKey('account-sign-out')));
  await tester.pumpAndSettle();
  expect(find.text('Sign out?'), findsOneWidget);
}

Future<_Auth> _pump(
  WidgetTester tester, {
  List<Map<String, dynamic>> farms = const [],
}) async {
  tester.view.physicalSize = const Size(430, 1000);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  final auth = _Auth();
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        authControllerProvider.overrideWith(() => auth),
        assignedFarmsProvider.overrideWith((ref) async => farms),
        unreadNotificationCountProvider.overrideWithValue(0),
      ],
      child: Consumer(
        builder: (_, ref, _) {
          final session = ref.watch(authControllerProvider).value;
          return MaterialApp(
            navigatorKey: _navigator,
            theme: AppTheme.light,
            home: session == null ? const LoginScreen() : const AccountScreen(),
          );
        },
      ),
    ),
  );
  await tester.pumpAndSettle();
  return auth;
}

class _Auth extends AuthController {
  int calls = 0;
  Completer<void>? pending;
  @override
  Future<AuthSession?> build() async =>
      const AuthSession(token: 'test', username: 'operator');
  @override
  Future<void> logout() async {
    calls++;
    if (pending != null) await pending!.future;
    state = const AsyncData(null);
  }
}
