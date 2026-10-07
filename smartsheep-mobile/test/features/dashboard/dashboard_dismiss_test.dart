import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_charts.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_data.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_filter_sheet.dart';

const _options = DashboardFilterOptions(
  farms: [DashboardFarmOption(id: 'farm-a', name: 'Alpha Farm')],
  barns: [
    DashboardBarnOption(id: 'barn-a', name: 'Alpha Barn', farmId: 'farm-a'),
  ],
);

/// Two hours of data so a tap can land on a specific point.
final _hourly = [
  const DashboardHourlyTrend(hour: 6, frequency: 12, durationSeconds: 600),
  const DashboardHourlyTrend(hour: 7, frequency: 20, durationSeconds: 900),
];

final _barns = [
  const DashboardBarnComparison(
    barnId: 'barn-a',
    barnName: 'Alpha Barn',
    frequency: 30,
    durationSeconds: 600,
  ),
  const DashboardBarnComparison(
    barnId: 'barn-b',
    barnName: 'Beta Barn',
    frequency: 18,
    durationSeconds: 300,
  ),
];

void main() {
  group('filter sheet close button', () {
    testWidgets('renders a black close icon, not the themed tint', (
      tester,
    ) async {
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            dashboardOptionsProvider.overrideWith(
              (_) => Future.value(_options),
            ),
          ],
          child: MaterialApp(
            theme: AppTheme.light,
            home: Scaffold(
              body: DashboardFilterSheet(
                initial: DashboardFilter(
                  startDate: DateTime(2026, 9, 7),
                  endDate: DateTime(2026, 9, 14),
                ),
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      final iconFinder = find.descendant(
        of: find.byTooltip('Close filters'),
        matching: find.byType(Icon),
      );
      final icon = tester.widget<Icon>(iconFinder);
      expect(icon.icon, Icons.close_rounded);
      expect(icon.size, 24);

      // Icons paint through a RichText glyph, so this is the colour actually
      // put on screen rather than what the widget merely asked for.
      final painted = tester.widget<RichText>(
        find.descendant(of: iconFinder, matching: find.byType(RichText)),
      );
      expect((painted.text as TextSpan).style?.color, Colors.black);
    });

    testWidgets('close button has a full 48dp touch target', (tester) async {
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            dashboardOptionsProvider.overrideWith(
              (_) => Future.value(_options),
            ),
          ],
          child: MaterialApp(
            theme: AppTheme.light,
            home: Scaffold(
              body: DashboardFilterSheet(
                initial: DashboardFilter(
                  startDate: DateTime(2026, 9, 7),
                  endDate: DateTime(2026, 9, 14),
                ),
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      final size = tester.getSize(find.byTooltip('Close filters'));
      expect(size.width, greaterThanOrEqualTo(48));
      expect(size.height, greaterThanOrEqualTo(48));
    });
  });

  group('chart tooltip dismissal', () {
    testWidgets('hourly chart tooltip closes on a tap outside the plot', (
      tester,
    ) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          home: Scaffold(
            body: Column(
              children: [
                DashboardHourlyChart(items: _hourly),
                const SizedBox(height: 40, child: Text('outside area')),
              ],
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      // Tapping the plot opens the tooltip.
      await tester.tap(find.byKey(const ValueKey('dashboard-hourly-plot')));
      await tester.pumpAndSettle();
      expect(find.text('ACTIVITY TIME'), findsOneWidget);

      // Tapping anywhere off the plot closes it, with no close button needed.
      await tester.tap(find.text('outside area'));
      await tester.pumpAndSettle();
      expect(find.text('ACTIVITY TIME'), findsNothing);
    });

    testWidgets('barn chart tooltip closes on a tap outside the plot', (
      tester,
    ) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.light,
          home: Scaffold(
            body: Column(
              children: [
                DashboardBarnChart(items: _barns),
                const SizedBox(height: 40, child: Text('outside area')),
              ],
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const ValueKey('dashboard-barn-plot')));
      await tester.pumpAndSettle();
      expect(find.text('BARN'), findsOneWidget);

      await tester.tap(find.text('outside area'));
      await tester.pumpAndSettle();
      expect(find.text('BARN'), findsNothing);
    });
  });
}
