import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_charts.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_data.dart';

const _hours = [
  DashboardHourlyTrend(hour: 0, frequency: 3, durationSeconds: 29),
  DashboardHourlyTrend(hour: 1, frequency: 8, durationSeconds: 30),
  DashboardHourlyTrend(hour: 2, frequency: 5, durationSeconds: 150),
];

List<DashboardBarnComparison> _barns(int count) => [
  for (var i = 0; i < count; i++)
    DashboardBarnComparison(
      barnId: '$i',
      barnName: 'Barn ${i + 1}',
      frequency: i + 1,
      durationSeconds: 300,
    ),
];

Future<void> _pump(
  WidgetTester tester,
  Widget child, {
  double width = 393,
  double textScale = 1,
}) async {
  await tester.binding.setSurfaceSize(Size(width, 852));
  addTearDown(() => tester.binding.setSurfaceSize(null));
  await tester.pumpWidget(
    MaterialApp(
      theme: AppTheme.light,
      home: MediaQuery(
        data: MediaQueryData(
          size: Size(width, 852),
          textScaler: TextScaler.linear(textScale),
        ),
        child: Scaffold(
          body: ListView(padding: const EdgeInsets.all(14), children: [child]),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets(
    'hourly plots the Web frequency and rounded duration series only',
    (tester) async {
      await _pump(tester, const DashboardHourlyChart(items: _hours));
      expect(find.text('Suckling Activity Trend (Hourly)'), findsOneWidget);
      expect(find.text('Frequency'), findsOneWidget);
      expect(find.text('Duration (min)'), findsOneWidget);
      final data = tester.widget<LineChart>(find.byType(LineChart)).data;
      expect(data.lineBarsData, hasLength(2));
      expect(data.lineBarsData[0].spots.map((spot) => spot.y), [3, 8, 5]);
      expect(data.lineBarsData[1].spots.map((spot) => spot.y), [0, 1, 3]);
      expect(data.lineBarsData[0].color, const Color(0xFF009F97));
      expect(data.lineBarsData[1].color, const Color(0xFFE5B629));
      expect(data.lineBarsData[0].belowBarData.show, isTrue);
      expect(data.lineBarsData[1].belowBarData.show, isFalse);
      expect(data.titlesData.rightTitles.sideTitles.showTitles, isFalse);
      expect(data.lineTouchData.enabled, isFalse);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('empty and all-zero hours use the exact Web empty message', (
    tester,
  ) async {
    for (final items in <List<DashboardHourlyTrend>>[
      [],
      [const DashboardHourlyTrend(hour: 8)],
    ]) {
      await _pump(tester, DashboardHourlyChart(items: items));
      expect(find.text('No data'), findsOneWidget);
      expect(
        find.text('Hourly activity data is not available'),
        findsOneWidget,
      );
      expect(find.byType(LineChart), findsNothing);
      expect(find.text('Frequency'), findsNothing);
    }
  });

  testWidgets(
    'positive raw seconds keep the chart even when minutes round to zero',
    (tester) async {
      await _pump(
        tester,
        const DashboardHourlyChart(
          items: [DashboardHourlyTrend(hour: 7, durationSeconds: 1)],
        ),
      );
      expect(find.byType(LineChart), findsOneWidget);
      expect(find.text('No data'), findsNothing);
      expect(find.text('07:00'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('hourly tap displays time, frequency and minute tooltip', (
    tester,
  ) async {
    await _pump(tester, const DashboardHourlyChart(items: _hours));
    final rect = tester.getRect(
      find.byKey(const ValueKey('dashboard-hourly-plot')),
    );
    await tester.tapAt(Offset(rect.right - 8, rect.top + 90));
    await tester.pumpAndSettle();
    expect(find.text('ACTIVITY TIME'), findsOneWidget);
    expect(find.text('Duration (min)'), findsNWidgets(2));
    expect(find.text('02:00'), findsNWidgets(2));
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'barns paginate five items, use frequency, and reset for new filters',
    (tester) async {
      final items = _barns(7);
      await _pump(tester, DashboardBarnChart(items: items));
      BarChartData data() =>
          tester.widget<BarChart>(find.byType(BarChart)).data;
      expect(find.text('1/2'), findsOneWidget);
      expect(data().barGroups.map((group) => group.barRods.single.toY), [
        1,
        2,
        3,
        4,
        5,
      ]);
      expect(find.text('Barn 5'), findsOneWidget);
      expect(find.text('Barn 6'), findsNothing);
      expect(
        tester
            .widget<IconButton>(
              find.widgetWithIcon(IconButton, Icons.chevron_left),
            )
            .onPressed,
        isNull,
      );
      await tester.tap(find.byTooltip('Next barn page'));
      await tester.pumpAndSettle();
      expect(find.text('2/2'), findsOneWidget);
      expect(data().barGroups.map((group) => group.barRods.single.toY), [6, 7]);
      expect(find.text('Barn 6'), findsOneWidget);
      expect(
        tester
            .widget<IconButton>(
              find.widgetWithIcon(IconButton, Icons.chevron_right),
            )
            .onPressed,
        isNull,
      );
      await _pump(tester, DashboardBarnChart(items: _barns(2)));
      expect(data().barGroups, hasLength(2));
      expect(find.byKey(const ValueKey('dashboard-barn-page')), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('barns with no frequency use Web no-data even with duration', (
    tester,
  ) async {
    await _pump(
      tester,
      const DashboardBarnChart(
        items: [
          DashboardBarnComparison(barnName: 'Barn A', durationSeconds: 200),
        ],
      ),
    );
    expect(find.text('No data'), findsOneWidget);
    expect(find.text('Barn activity data is not available'), findsOneWidget);
    expect(find.byType(BarChart), findsNothing);
    expect(find.byKey(const ValueKey('dashboard-barn-page')), findsNothing);
  });

  testWidgets(
    'a zero-only barn page remains visible if other pages have activity',
    (tester) async {
      await _pump(
        tester,
        DashboardBarnChart(
          items: [
            ..._barns(5),
            const DashboardBarnComparison(barnCode: 'B06'),
          ],
        ),
      );
      await tester.tap(find.byTooltip('Next barn page'));
      await tester.pumpAndSettle();
      expect(find.byType(BarChart), findsOneWidget);
      expect(find.text('B06'), findsOneWidget);
      expect(find.text('2/2'), findsOneWidget);
      expect(find.text('No data'), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );

  for (final width in [320.0, 393.0]) {
    testWidgets(
      'charts fit ${width.toInt()}px and swipes over plots scroll the page',
      (tester) async {
        final controller = ScrollController();
        addTearDown(controller.dispose);
        await tester.binding.setSurfaceSize(Size(width, 600));
        addTearDown(() => tester.binding.setSurfaceSize(null));
        await tester.pumpWidget(
          MaterialApp(
            theme: AppTheme.light,
            home: Scaffold(
              body: ListView(
                controller: controller,
                padding: const EdgeInsets.all(14),
                children: [
                  const DashboardHourlyChart(items: _hours),
                  const SizedBox(height: 12),
                  DashboardBarnChart(items: _barns(7)),
                  const SizedBox(height: 500),
                ],
              ),
            ),
          ),
        );
        await tester.pumpAndSettle();
        expect(tester.takeException(), isNull);
        await tester.drag(
          find.byKey(const ValueKey('dashboard-hourly-plot')),
          const Offset(0, -200),
        );
        await tester.pumpAndSettle();
        expect(controller.offset, greaterThan(100));
        expect(find.text('ACTIVITY TIME'), findsNothing);
        final before = controller.offset;
        await tester.drag(
          find.byKey(const ValueKey('dashboard-barn-plot')),
          const Offset(0, -180),
        );
        await tester.pumpAndSettle();
        expect(controller.offset, greaterThan(before + 80));
        expect(tester.takeException(), isNull);
      },
    );
  }

  testWidgets('long barn labels and tooltip stay inside a narrow phone', (
    tester,
  ) async {
    await _pump(
      tester,
      DashboardBarnChart(
        items: [
          const DashboardBarnComparison(
            barnName:
                'A very long barn name across multiple words at the main livestock farm',
            frequency: 12000000,
          ),
          ..._barns(4),
        ],
      ),
      width: 320,
    );
    final rect = tester.getRect(
      find.byKey(const ValueKey('dashboard-barn-plot')),
    );
    await tester.tapAt(Offset(rect.left + 62, rect.top + 90));
    await tester.pumpAndSettle();
    expect(find.text('BARN'), findsOneWidget);
    expect(find.text('12,000,000'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
