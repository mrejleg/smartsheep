import 'dart:ui' show PointerDeviceKind;

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/app.dart';

void main() {
  testWidgets('supports scrolling a long page with a mouse drag', (
    tester,
  ) async {
    final controller = ScrollController();
    addTearDown(controller.dispose);

    await tester.pumpWidget(
      MaterialApp(
        scrollBehavior: const SmartSheepScrollBehavior(),
        theme: ThemeData(platform: TargetPlatform.iOS),
        home: Scaffold(
          body: ListView(
            controller: controller,
            children: List.generate(
              30,
              (index) => SizedBox(height: 80, child: Text('Item $index')),
            ),
          ),
        ),
      ),
    );

    await tester.drag(
      find.byType(ListView),
      const Offset(0, -300),
      kind: PointerDeviceKind.mouse,
    );
    await tester.pumpAndSettle();

    expect(controller.offset, greaterThan(0));
    expect(
      const SmartSheepScrollBehavior().dragDevices,
      containsAll(const [PointerDeviceKind.mouse, PointerDeviceKind.trackpad]),
    );

    final physics = const SmartSheepScrollBehavior().getScrollPhysics(
      tester.element(find.byType(ListView)),
    );
    expect(physics, isA<BouncingScrollPhysics>());
    expect(
      (physics as BouncingScrollPhysics).decelerationRate,
      ScrollDecelerationRate.normal,
    );
  });

  testWidgets('two-finger trackpad pan scrolls without a mouse click', (
    tester,
  ) async {
    final controller = ScrollController();
    addTearDown(controller.dispose);

    await tester.pumpWidget(
      MaterialApp(
        scrollBehavior: const SmartSheepScrollBehavior(),
        theme: ThemeData(platform: TargetPlatform.iOS),
        home: Scaffold(
          body: ListView.builder(
            controller: controller,
            itemCount: 30,
            itemBuilder: (context, index) => ListTile(
              key: ValueKey('trackpad-tile-$index'),
              onTap: () {},
              title: Text('Item $index'),
            ),
          ),
        ),
      ),
    );

    await tester.trackpadFling(
      find.byKey(const ValueKey('trackpad-tile-5')),
      const Offset(0, -360),
      1200,
    );
    await tester.pumpAndSettle();

    expect(controller.offset, greaterThan(0));
  });

  testWidgets('mouse drag scrolls when it starts on an interactive list tile', (
    tester,
  ) async {
    final controller = ScrollController();
    addTearDown(controller.dispose);

    await tester.pumpWidget(
      MaterialApp(
        scrollBehavior: const SmartSheepScrollBehavior(),
        home: Scaffold(
          body: ListView.builder(
            controller: controller,
            itemCount: 30,
            itemBuilder: (context, index) => ListTile(
              key: ValueKey('tile-$index'),
              onTap: () {},
              title: Text('Item $index'),
            ),
          ),
        ),
      ),
    );

    await tester.drag(
      find.byKey(const ValueKey('tile-5')),
      const Offset(0, -300),
      kind: PointerDeviceKind.mouse,
    );
    await tester.pumpAndSettle();

    expect(controller.offset, greaterThan(0));
  });

  testWidgets(
    'Android overscroll does not stretch content (Material 3 default)',
    (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          scrollBehavior: const SmartSheepScrollBehavior(),
          theme: ThemeData(
            platform: TargetPlatform.android,
            useMaterial3: true,
          ),
          home: Scaffold(
            body: ListView(
              children: List.generate(
                30,
                (index) => SizedBox(height: 80, child: Text('Item $index')),
              ),
            ),
          ),
        ),
      );

      // Without the override, Material 3 on Android wraps every scroll view
      // in a stretch indicator that distorts text and buttons at the edges.
      expect(find.byType(StretchingOverscrollIndicator), findsNothing);
      expect(find.byType(GlowingOverscrollIndicator), findsNothing);

      // Pull past the top edge, as a user does when the list is at rest.
      final gesture = await tester.startGesture(
        tester.getCenter(find.byType(ListView)),
      );
      await gesture.moveBy(const Offset(0, 250));
      await tester.pump(const Duration(milliseconds: 50));

      // The first row keeps its real size and position mid-gesture: nothing
      // is being scaled or pulled.
      final firstRow = find.text('Item 0');
      expect(tester.getTopLeft(firstRow).dy, 0);
      expect(
        tester
            .getSize(
              find
                  .ancestor(of: firstRow, matching: find.byType(SizedBox))
                  .first,
            )
            .height,
        80,
      );
      // The stretch effect works by scaling the list through a Transform;
      // none may sit between the list and its rows.
      expect(
        find
            .ancestor(of: firstRow, matching: find.byType(Transform))
            .evaluate()
            .where(
              (element) => find
                  .descendant(
                    of: find.byType(ListView),
                    matching: find.byWidget(element.widget),
                  )
                  .evaluate()
                  .isNotEmpty,
            ),
        isEmpty,
      );

      await gesture.up();
      await tester.pumpAndSettle();
    },
  );
}
