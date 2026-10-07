import 'dart:ui' show PointerDeviceKind;

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/theme/app_theme.dart';
import 'features/auth/auth_controller.dart';
import 'features/auth/login_screen.dart';
import 'features/shell/app_shell.dart';

final rootScaffoldMessengerKey = GlobalKey<ScaffoldMessengerState>();
final rootNavigatorKey = GlobalKey<NavigatorState>();

class SmartSheepScrollBehavior extends MaterialScrollBehavior {
  const SmartSheepScrollBehavior();

  @override
  Set<PointerDeviceKind> get dragDevices => {
    ...super.dragDevices,
    PointerDeviceKind.mouse,
    PointerDeviceKind.trackpad,
  };

  @override
  ScrollPhysics getScrollPhysics(BuildContext context) {
    final platform = getPlatform(context);
    if (platform == TargetPlatform.iOS || platform == TargetPlatform.macOS) {
      return const BouncingScrollPhysics(
        decelerationRate: ScrollDecelerationRate.normal,
        parent: AlwaysScrollableScrollPhysics(),
      );
    }

    return const ClampingScrollPhysics(parent: AlwaysScrollableScrollPhysics());
  }

  // Material 3 on Android stretches the whole scroll view when it hits an
  // edge, which visibly distorts text and buttons. Clamping physics already
  // stops the scroll there, so no overscroll effect is drawn at all.
  // Pull-to-refresh is unaffected: RefreshIndicator listens independently.
  @override
  Widget buildOverscrollIndicator(
    BuildContext context,
    Widget child,
    ScrollableDetails details,
  ) => child;
}

class SmartSheepApp extends ConsumerStatefulWidget {
  const SmartSheepApp({super.key});

  @override
  ConsumerState<SmartSheepApp> createState() => _SmartSheepAppState();
}

class _SmartSheepAppState extends ConsumerState<SmartSheepApp> {
  late final AppLifecycleListener _lifecycleListener;

  @override
  void initState() {
    super.initState();
    _lifecycleListener = AppLifecycleListener(
      onResume: () => ref
          .read(authControllerProvider.notifier)
          .synchronizeDeviceRegistration(),
    );
  }

  @override
  void dispose() {
    _lifecycleListener.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final auth = ref.watch(authControllerProvider);
    return MaterialApp(
      title: 'SmartSheep',
      debugShowCheckedModeBanner: false,
      scaffoldMessengerKey: rootScaffoldMessengerKey,
      navigatorKey: rootNavigatorKey,
      scrollBehavior: const SmartSheepScrollBehavior(),
      theme: AppTheme.light,
      home: auth.when(
        loading: () => const _SplashScreen(),
        error: (_, _) => const LoginScreen(),
        data: (session) =>
            session == null ? const LoginScreen() : const AppShell(),
      ),
    );
  }
}

class _SplashScreen extends StatelessWidget {
  const _SplashScreen();

  @override
  Widget build(BuildContext context) => const Scaffold(
    backgroundColor: AppColors.deepGreen,
    body: Center(child: CircularProgressIndicator(color: Colors.white)),
  );
}
