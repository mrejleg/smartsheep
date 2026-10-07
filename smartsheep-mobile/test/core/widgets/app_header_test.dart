import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/core/widgets/app_header.dart';

void main() {
  testWidgets('header uses dark gradient and screen context without a logo', (
    tester,
  ) async {
    await _pump(
      tester,
      const AppHeader(title: 'Dashboard', subtitle: 'Operational overview'),
    );

    expect(find.text('Dashboard'), findsOneWidget);
    expect(find.text('Operational overview'), findsOneWidget);
    expect(_brandImage, findsNothing);
    final gradients = find.byWidgetPredicate((widget) {
      final decoration = widget is Container ? widget.decoration : null;
      return decoration is BoxDecoration && decoration.gradient != null;
    });
    expect(gradients, findsWidgets);
    final decorations = tester
        .widgetList<Container>(gradients)
        .map((widget) => widget.decoration! as BoxDecoration);
    expect(
      decorations.any(
        (decoration) =>
            decoration.gradient!.colors.length > 1 &&
            decoration.gradient!.colors.any(
              (color) => color.computeLuminance() < .15,
            ),
      ),
      isTrue,
    );
    expect(tester.takeException(), isNull);
  });

  testWidgets('notification then profile actions stay right and both work', (
    tester,
  ) async {
    var notificationTaps = 0;
    var profileTaps = 0;
    await _pump(
      tester,
      AppHeader(
        title: 'Dashboard',
        profileName: 'Nadia Putri',
        onProfile: () => profileTaps++,
        actions: [
          NotificationAction(count: 3, onPressed: () => notificationTaps++),
        ],
      ),
    );
    final notification = find.byType(NotificationAction);
    final profile = find.byTooltip('Open profile');
    expect(profile, findsOneWidget);
    expect(
      tester.getRect(notification).left,
      greaterThan(
        tester.getRect(find.byKey(const ValueKey('app-header-title'))).right,
      ),
    );
    expect(
      tester.getRect(profile).left,
      greaterThanOrEqualTo(tester.getRect(notification).right),
    );
    await tester.tap(notification);
    await tester.tap(profile);
    expect(notificationTaps, 1);
    expect(profileTaps, 1);
    expect(tester.takeException(), isNull);
  });

  testWidgets('notification badge uses supplied count and hides at zero', (
    tester,
  ) async {
    await _pump(
      tester,
      AppHeader(
        title: 'Home',
        actions: [NotificationAction(onPressed: () {})],
      ),
    );
    var badges = tester.widgetList<Badge>(find.byType(Badge));
    expect(badges.where((badge) => badge.isLabelVisible), isEmpty);

    await _pump(
      tester,
      AppHeader(
        title: 'Home',
        actions: [NotificationAction(count: 7, onPressed: () {})],
      ),
    );
    badges = tester.widgetList<Badge>(find.byType(Badge));
    expect(badges.where((badge) => badge.isLabelVisible), hasLength(1));
    expect(find.text('7'), findsOneWidget);
    expect(find.text('2'), findsNothing);

    await _pump(
      tester,
      AppHeader(
        title: 'Home',
        actions: [NotificationAction(count: 120, onPressed: () {})],
      ),
    );
    expect(find.text('99+'), findsOneWidget);
    expect(find.byTooltip('Notifications (120 unread)'), findsOneWidget);
  });

  testWidgets('profile uses supplied image or initials without a photo', (
    tester,
  ) async {
    await _pump(
      tester,
      const AppHeader(title: 'Account', profileName: 'Nadia Putri'),
    );
    expect(find.text('NP'), findsOneWidget);
    await _pump(
      tester,
      AppHeader(
        title: 'Account',
        profileName: 'Nadia Putri',
        profileImage: MemoryImage(
          base64Decode(
            'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=',
          ),
        ),
      ),
    );
    expect(
      find.descendant(
        of: find.byType(HeaderProfileAvatar),
        matching: find.byType(Image),
      ),
      findsOneWidget,
    );
    expect(find.text('NP'), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('optional Back returns to the previous route', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Scaffold(
          body: Builder(
            builder: (context) => TextButton(
              onPressed: () => Navigator.of(context).push(
                MaterialPageRoute<void>(
                  builder: (_) => const Scaffold(
                    body: AppHeader(title: 'News Detail', showBackButton: true),
                  ),
                ),
              ),
              child: const Text('Open detail'),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.text('Open detail'));
    await tester.pumpAndSettle();
    expect(find.text('News Detail'), findsOneWidget);
    await tester.tap(find.byTooltip('Back'));
    await tester.pumpAndSettle();
    expect(find.text('News Detail'), findsNothing);
    expect(find.text('Open detail'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'expanded Home header shows greeting name and role with more room',
    (tester) async {
      await _pump(tester, const AppHeader(title: 'Home'));
      final compactHeight = tester.getSize(find.byType(AppHeader)).height;
      await _pump(
        tester,
        const AppHeader(
          title: 'Nadia Putri',
          greeting: 'Good afternoon,',
          subtitle: 'Farm Manager',
          expanded: true,
        ),
      );

      expect(find.text('Good afternoon,'), findsOneWidget);
      expect(find.text('Nadia Putri'), findsOneWidget);
      expect(find.text('Farm Manager'), findsOneWidget);
      expect(
        tester.getSize(find.byType(AppHeader)).height,
        greaterThan(compactHeight),
      );
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('status bar inset is included only when requested', (
    tester,
  ) async {
    await _pump(tester, const AppHeader(title: 'Sources'), topInset: 44);
    final withInset = tester.getSize(find.byType(AppHeader)).height;
    final titleWithInset = tester.getTopLeft(find.text('Sources')).dy;
    await _pump(
      tester,
      const AppHeader(title: 'Sources', includeTopInset: false),
      topInset: 44,
    );
    expect(
      withInset - tester.getSize(find.byType(AppHeader)).height,
      closeTo(44, .1),
    );
    expect(
      titleWithInset - tester.getTopLeft(find.text('Sources')).dy,
      closeTo(44, .1),
    );
  });

  testWidgets('optional footer appears below header context', (tester) async {
    await _pump(
      tester,
      const AppHeader(
        title: 'Sources',
        subtitle: 'Live streams',
        footer: Text('All · Online · Offline'),
      ),
    );
    expect(find.text('All · Online · Offline'), findsOneWidget);
    expect(
      tester.getRect(find.text('All · Online · Offline')).top,
      greaterThanOrEqualTo(tester.getRect(find.text('Live streams')).bottom),
    );
    expect(tester.takeException(), isNull);
  });

  for (final expanded in [false, true]) {
    testWidgets(
      '320px large text header stays within width, expanded=$expanded',
      (tester) async {
        await _pump(
          tester,
          AppHeader(
            title: 'SmartSheep Farm Management Operational Dashboard',
            subtitle: 'Administrator for farms and livestock in West Java',
            greeting: expanded ? 'Good afternoon,' : null,
            expanded: expanded,
            profileName: 'Nadia Putri Pramudita',
            onProfile: () {},
            actions: [
              IconButton(
                tooltip: 'Refresh dashboard',
                onPressed: () {},
                icon: const Icon(Icons.refresh),
              ),
              NotificationAction(count: 12, onPressed: () {}),
            ],
          ),
          size: const Size(320, 568),
          textScale: 2,
          topInset: 44,
        );
        expect(tester.takeException(), isNull);
        final header = tester.getRect(find.byType(AppHeader));
        expect(header.width, 320);
        for (final finder in [
          find.byKey(const ValueKey('app-header-title')),
          find.byTooltip('Open profile'),
          find.byTooltip('Refresh dashboard'),
          find.byType(NotificationAction),
        ]) {
          expect(finder, findsOneWidget);
          final bounds = tester.getRect(finder);
          expect(bounds.left, greaterThanOrEqualTo(0));
          expect(bounds.right, lessThanOrEqualTo(320));
          expect(bounds.bottom, lessThanOrEqualTo(header.bottom));
        }
      },
    );
  }
}

Finder get _brandImage => find.byWidgetPredicate(
  (widget) =>
      widget is Image &&
      widget.image is AssetImage &&
      (widget.image as AssetImage).assetName ==
          'assets/images/smartsheep-mark.png',
);

Future<void> _pump(
  WidgetTester tester,
  Widget header, {
  Size size = const Size(393, 852),
  double textScale = 1,
  double topInset = 0,
}) async {
  await tester.binding.setSurfaceSize(size);
  addTearDown(() => tester.binding.setSurfaceSize(null));
  await tester.pumpWidget(
    MaterialApp(
      theme: AppTheme.light,
      home: MediaQuery(
        data: MediaQueryData(
          size: size,
          textScaler: TextScaler.linear(textScale),
          padding: EdgeInsets.only(top: topInset),
        ),
        child: Scaffold(
          body: Column(
            children: [
              header,
              const Expanded(child: SizedBox.shrink()),
            ],
          ),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}
