// This is a basic Flutter widget test.
//
// To perform an interaction with a widget in your test, use the WidgetTester
// utility in the flutter_test package. For example, you can send tap and scroll
// gestures. You can also use WidgetTester to find child widgets in the widget
// tree, read text, and verify that the values of widget properties are correct.

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:smartsheep_mobile/app.dart';
import 'package:smartsheep_mobile/features/auth/auth_controller.dart';
import 'package:smartsheep_mobile/features/auth/login_screen.dart';

class _TestAuthController extends AuthController {
  @override
  Future<AuthSession?> build() async => null;
}

void main() {
  testWidgets('shows the SmartSheep login screen', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authControllerProvider.overrideWith(_TestAuthController.new),
        ],
        child: const SmartSheepApp(),
      ),
    );
    await tester.pump();
    expect(find.text('Welcome back'), findsOneWidget);
    expect(find.text('Sign In'), findsOneWidget);
    expect(find.text('Forgot password?'), findsNothing);
  });

  testWidgets('username accepts text input and moves focus to password', (
    tester,
  ) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authControllerProvider.overrideWith(_TestAuthController.new),
        ],
        child: const SmartSheepApp(),
      ),
    );
    await tester.pump();

    final usernameFinder = find.byKey(const ValueKey('login-username'));
    final passwordFinder = find.byKey(const ValueKey('login-password'));

    await tester.tap(usernameFinder);
    await tester.enterText(usernameFinder, 'operator');
    expect(find.text('operator'), findsOneWidget);

    await tester.testTextInput.receiveAction(TextInputAction.next);
    await tester.pump();

    final passwordEditable = tester.widget<EditableText>(
      find.descendant(of: passwordFinder, matching: find.byType(EditableText)),
    );
    expect(passwordEditable.focusNode.hasFocus, isTrue);
  });

  testWidgets('login layout does not overflow when the keyboard is visible', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(393, 852);
    tester.view.devicePixelRatio = 1;
    tester.view.viewInsets = const FakeViewPadding(bottom: 300);
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    addTearDown(tester.view.resetViewInsets);

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authControllerProvider.overrideWith(_TestAuthController.new),
        ],
        child: const SmartSheepApp(),
      ),
    );
    await tester.pump();

    expect(tester.takeException(), isNull);
    expect(find.byKey(const ValueKey('login-password')), findsOneWidget);
  });

  test('shows a concise message for incomplete login configuration', () {
    final request = RequestOptions(path: '/api/MobileApp/Login');
    final error = DioException(
      requestOptions: request,
      response: Response<dynamic>(
        requestOptions: request,
        statusCode: 400,
        data: {
          'errors': {
            'ClientSecret': ['The ClientSecret field is required.'],
          },
        },
      ),
    );

    final message = friendlyLoginError(error);

    expect(message, 'The application login configuration is incomplete.');
    expect(message, isNot(contains('DioException')));
  });

  test('shows the API message for rejected user credentials', () {
    final request = RequestOptions(path: '/api/MobileApp/Login');
    final error = DioException(
      requestOptions: request,
      response: Response<dynamic>(
        requestOptions: request,
        statusCode: 401,
        data: {'Message': 'The username or password is incorrect.'},
      ),
    );

    final message = friendlyLoginError(error);

    expect(message, 'The username or password is incorrect.');
    expect(message, isNot(contains('DioException')));
  });
}
