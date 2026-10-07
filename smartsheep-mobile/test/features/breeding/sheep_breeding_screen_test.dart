import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/auth/auth_controller.dart';
import 'package:smartsheep_mobile/features/breeding/sheep_breeding_screen.dart';
import 'package:smartsheep_mobile/features/shell/header_identity.dart';

void main() {
  testWidgets('breeding menu opens a themed page without fabricated records', (
    tester,
  ) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authControllerProvider.overrideWith(_Auth.new),
          unreadNotificationCountProvider.overrideWithValue(0),
        ],
        child: MaterialApp(
          theme: AppTheme.light,
          home: const SheepBreedingScreen(),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Sheep Breeding'), findsNWidgets(2));
    expect(
      find.text('Breeding management has not been configured yet.'),
      findsOneWidget,
    );
    expect(find.text('Save'), findsNothing);
    expect(tester.takeException(), isNull);
  });
}

class _Auth extends AuthController {
  @override
  Future<AuthSession?> build() async =>
      const AuthSession(token: 'test', username: 'operator');
}
