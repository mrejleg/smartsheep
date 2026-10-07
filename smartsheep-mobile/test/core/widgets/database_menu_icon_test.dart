import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/widgets/database_menu_icon.dart';

const _regular =
    '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M3 3h18v18H3z"/></svg>';
const _active =
    '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><circle cx="12" cy="12" r="9"/></svg>';

void main() {
  testWidgets('renders the actual database SVG for regular and active states', (
    tester,
  ) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: DatabaseMenuIcon(icon: _regular, activeIcon: _active),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(
      tester
          .widget<DatabaseMenuIcon>(find.byType(DatabaseMenuIcon))
          .resolvedIcon,
      _regular,
    );
    expect(find.byType(SvgPicture), findsOneWidget);
    expect(
      find.byKey(const ValueKey('database-menu-icon-svg')),
      findsOneWidget,
    );
    expect(
      find.byKey(const ValueKey('database-menu-icon-fallback')),
      findsNothing,
    );

    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: DatabaseMenuIcon(
            icon: _regular,
            activeIcon: _active,
            active: true,
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(
      tester
          .widget<DatabaseMenuIcon>(find.byType(DatabaseMenuIcon))
          .resolvedIcon,
      _active,
    );
    expect(find.byType(SvgPicture), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('missing active SVG falls back to the database regular SVG', (
    tester,
  ) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: DatabaseMenuIcon(icon: _regular, active: true)),
      ),
    );
    await tester.pumpAndSettle();
    expect(
      tester
          .widget<DatabaseMenuIcon>(find.byType(DatabaseMenuIcon))
          .resolvedIcon,
      _regular,
    );
    expect(find.byType(SvgPicture), findsOneWidget);
    expect(
      find.byKey(const ValueKey('database-menu-icon-fallback')),
      findsNothing,
    );
  });

  testWidgets(
    'missing unknown or malformed database icons use one neutral fallback',
    (tester) async {
      for (final icon in <String?>[
        null,
        '',
        '  ',
        'home',
        'fa fa-home',
        '<div>not svg</div>',
        '<svg><path></svg>',
      ]) {
        await tester.pumpWidget(
          MaterialApp(
            home: Scaffold(
              body: DatabaseMenuIcon(icon: icon, size: 30, color: Colors.teal),
            ),
          ),
        );
        await tester.pumpAndSettle();
        expect(
          tester
              .widget<DatabaseMenuIcon>(find.byType(DatabaseMenuIcon))
              .resolvedIcon,
          isNull,
        );
        expect(find.byType(SvgPicture), findsNothing);
        expect(
          find.byKey(const ValueKey('database-menu-icon-fallback')),
          findsOneWidget,
        );
        final fallback = tester.widget<Icon>(
          find.byIcon(Icons.circle_outlined),
        );
        expect(fallback.color, Colors.teal);
        expect(fallback.size, 30);
        expect(find.byIcon(Icons.home_outlined), findsNothing);
        expect(find.byIcon(Icons.dashboard_outlined), findsNothing);
        expect(tester.takeException(), isNull);
      }
    },
  );

  testWidgets('rejects unsafe SVG payloads instead of loading remote resources', (
    tester,
  ) async {
    for (final icon in [
      '<svg xmlns="http://www.w3.org/2000/svg"><script>alert(1)</script></svg>',
      '<svg xmlns="http://www.w3.org/2000/svg"><image href="https://example.invalid/private.png"/></svg>',
      '<svg xmlns="http://www.w3.org/2000/svg"><use href="https://example.invalid/icons.svg#home"/></svg>',
      '<svg xmlns="http://www.w3.org/2000/svg" onload="alert(1)"><path d="M0 0h1v1z"/></svg>',
    ]) {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(body: DatabaseMenuIcon(icon: icon)),
        ),
      );
      await tester.pumpAndSettle();
      expect(
        tester
            .widget<DatabaseMenuIcon>(find.byType(DatabaseMenuIcon))
            .resolvedIcon,
        isNull,
      );
      expect(find.byType(SvgPicture), findsNothing);
      expect(
        find.byKey(const ValueKey('database-menu-icon-fallback')),
        findsOneWidget,
      );
      expect(tester.takeException(), isNull);
    }
  });
}
