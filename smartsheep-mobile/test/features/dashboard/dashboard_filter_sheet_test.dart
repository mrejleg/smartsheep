import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_data.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_filter_sheet.dart';

const _options = DashboardFilterOptions(
  farms: [
    DashboardFarmOption(id: 'farm-a', name: 'Alpha Farm'),
    DashboardFarmOption(id: 'farm-b', name: 'Beta Farm'),
  ],
  barns: [
    DashboardBarnOption(id: 'barn-a', name: 'Alpha Barn', farmId: 'farm-a'),
    DashboardBarnOption(id: 'barn-b', name: 'Beta Barn', farmId: 'farm-b'),
  ],
);

DashboardFilter _initial({String? farmId, String? barnId}) => DashboardFilter(
  startDate: DateTime(2026, 9, 7),
  endDate: DateTime(2026, 9, 14),
  farmId: farmId,
  barnId: barnId,
);

void main() {
  testWidgets('opens a bottom-attached sheet with all four web filter fields', (
    tester,
  ) async {
    _phoneSize(tester);
    await _open(tester);

    expect(find.text('Farm'), findsOneWidget);
    expect(find.text('Barn'), findsOneWidget);
    expect(find.text('Start Date'), findsOneWidget);
    expect(find.text('End Date'), findsOneWidget);
    expect(find.text('07 Sep 2026'), findsOneWidget);
    expect(find.text('14 Sep 2026'), findsOneWidget);
    expect(find.text('Apply Filters'), findsOneWidget);
    final headerIcon = find.byKey(
      const ValueKey('dashboard-filter-sheet-icon'),
    );
    final iconTile = tester.widget<Container>(headerIcon);
    final iconDecoration = iconTile.decoration! as BoxDecoration;
    expect(iconDecoration.borderRadius, BorderRadius.circular(8));
    expect(iconDecoration.gradient?.colors, [
      AppColors.teal,
      AppColors.tealDark,
    ]);
    final icon = tester.widget<Icon>(
      find.descendant(
        of: headerIcon,
        matching: find.byIcon(Icons.tune_rounded),
      ),
    );
    expect(icon.color, Colors.white);
    expect(icon.size, 18);
    final reset = find.byKey(const ValueKey('dashboard-filter-reset'));
    expect(reset, findsOneWidget);
    expect(
      find.descendant(of: reset, matching: find.text('Reset')),
      findsOneWidget,
    );
    final resetIcon = find.descendant(
      of: reset,
      matching: find.byIcon(Icons.restart_alt_rounded),
    );
    expect(resetIcon, findsOneWidget);
    expect(tester.widget<Icon>(resetIcon).size, 19);
    final sheet = tester.widget<BottomSheet>(find.byType(BottomSheet));
    final shape = sheet.shape! as RoundedRectangleBorder;
    expect(
      shape.borderRadius,
      const BorderRadius.vertical(top: Radius.circular(24)),
    );
    expect(tester.getRect(find.byType(BottomSheet)).bottom, 852);
    expect(tester.takeException(), isNull);
  });

  testWidgets('searches farms and barns and applies only the final draft', (
    tester,
  ) async {
    _phoneSize(tester);
    DashboardFilter? result;
    var completed = false;
    await _open(
      tester,
      onResult: (value) {
        completed = true;
        result = value;
      },
    );

    await tester.tap(find.byKey(const ValueKey('dashboard-filter-farm')));
    await tester.pumpAndSettle();
    await tester.enterText(
      find.byKey(const ValueKey('dashboard-filter-farm-search')),
      'beta',
    );
    await tester.pumpAndSettle();
    expect(find.text('Alpha Farm'), findsNothing);
    await tester.tap(find.text('Beta Farm'));
    await tester.pumpAndSettle();
    expect(completed, isFalse);

    await tester.tap(find.byKey(const ValueKey('dashboard-filter-barn')));
    await tester.pumpAndSettle();
    expect(find.text('Alpha Barn'), findsNothing);
    expect(find.text('Beta Barn'), findsOneWidget);
    await tester.enterText(
      find.byKey(const ValueKey('dashboard-filter-barn-search')),
      'beta',
    );
    await tester.pumpAndSettle();
    await tester.tap(find.text('Beta Barn'));
    await tester.pumpAndSettle();
    expect(completed, isFalse);

    await tester.tap(find.text('Apply Filters'));
    await tester.pumpAndSettle();
    expect(result?.farmId, 'farm-b');
    expect(result?.barnId, 'barn-b');
    expect(result?.startDate, DateTime(2026, 9, 7));
    expect(completed, isTrue);
  });

  testWidgets('changing farm clears an incompatible barn', (tester) async {
    _phoneSize(tester);
    DashboardFilter? result;
    await _open(
      tester,
      initial: _initial(farmId: 'farm-a', barnId: 'barn-a'),
      onResult: (value) => result = value,
    );

    await tester.tap(find.byKey(const ValueKey('dashboard-filter-farm')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Beta Farm'));
    await tester.pumpAndSettle();
    expect(find.text('All Barns'), findsOneWidget);
    await tester.tap(find.text('Apply Filters'));
    await tester.pumpAndSettle();
    expect(result?.farmId, 'farm-b');
    expect(result?.barnId, isNull);
  });

  testWidgets('all farms permits selecting any barn independently', (
    tester,
  ) async {
    _phoneSize(tester);
    DashboardFilter? result;
    await _open(tester, onResult: (value) => result = value);

    await tester.tap(find.byKey(const ValueKey('dashboard-filter-barn')));
    await tester.pumpAndSettle();
    expect(find.text('Alpha Barn'), findsOneWidget);
    expect(find.text('Beta Barn'), findsOneWidget);
    await tester.tap(find.text('Beta Barn'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Apply Filters'));
    await tester.pumpAndSettle();
    expect(result?.farmId, isNull);
    expect(result?.barnId, 'barn-b');
  });

  testWidgets('Reset edits the draft and close discards it', (tester) async {
    _phoneSize(tester);
    DashboardFilter? result = _initial();
    var completed = false;
    await _open(
      tester,
      initial: _initial(farmId: 'farm-a', barnId: 'barn-a'),
      onResult: (value) {
        completed = true;
        result = value;
      },
    );

    await tester.tap(find.byKey(const ValueKey('dashboard-filter-reset')));
    await tester.pumpAndSettle();
    expect(find.text('All Farms'), findsOneWidget);
    expect(find.text('All Barns'), findsOneWidget);
    expect(completed, isFalse);
    await tester.tap(find.byTooltip('Close filters'));
    await tester.pumpAndSettle();
    expect(completed, isTrue);
    expect(result, isNull);
  });

  testWidgets('Reset applies the default seven-day range when confirmed', (
    tester,
  ) async {
    _phoneSize(tester);
    DashboardFilter? result;
    await _open(
      tester,
      initial: _initial(farmId: 'farm-a', barnId: 'barn-a'),
      onResult: (value) => result = value,
    );
    final defaults = DashboardFilter.defaults();

    await tester.tap(find.byKey(const ValueKey('dashboard-filter-reset')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Apply Filters'));
    await tester.pumpAndSettle();
    expect(result, defaults);
  });

  testWidgets('rejects an end date before the start date', (tester) async {
    _phoneSize(tester);
    var completed = false;
    await _open(
      tester,
      initial: DashboardFilter(
        startDate: DateTime(2026, 9, 14),
        endDate: DateTime(2026, 9, 7),
      ),
      onResult: (_) => completed = true,
    );
    await tester.tap(find.text('Apply Filters'));
    await tester.pumpAndSettle();
    expect(
      find.text('End Date must be on or after Start Date.'),
      findsOneWidget,
    );
    expect(completed, isFalse);
    expect(find.byType(DashboardFilterSheet), findsOneWidget);
  });

  testWidgets('date picker updates draft without closing filters', (
    tester,
  ) async {
    _phoneSize(tester);
    DashboardFilter? result;
    await _open(tester, onResult: (value) => result = value);
    await tester.tap(find.byKey(const ValueKey('dashboard-filter-start-date')));
    await tester.pumpAndSettle();
    expect(find.byType(DatePickerDialog), findsOneWidget);
    await tester.tap(find.text('8'));
    await tester.tap(find.text('OK'));
    await tester.pumpAndSettle();
    expect(find.text('08 Sep 2026'), findsOneWidget);
    expect(result, isNull);
    await tester.tap(find.text('Apply Filters'));
    await tester.pumpAndSettle();
    expect(result?.startDate, DateTime(2026, 9, 8));
  });

  testWidgets('shows option loading and retries an option failure', (
    tester,
  ) async {
    _phoneSize(tester);
    final pending = Completer<DashboardFilterOptions>();
    var attempts = 0;
    await _open(
      tester,
      optionsLoader: () {
        attempts++;
        if (attempts == 1) return pending.future;
        return Future.value(_options);
      },
      settle: false,
    );
    expect(find.text('Loading farms and barns…'), findsOneWidget);
    pending.completeError(StateError('Unavailable'));
    await tester.pumpAndSettle();
    expect(find.text('Unable to load farms and barns.'), findsOneWidget);
    await tester.tap(find.text('Retry'));
    await tester.pumpAndSettle();
    expect(attempts, 2);
    expect(find.text('Unable to load farms and barns.'), findsNothing);
    await tester.tap(find.byKey(const ValueKey('dashboard-filter-farm')));
    await tester.pumpAndSettle();
    expect(find.text('Alpha Farm'), findsOneWidget);
  });

  testWidgets('small screen and open search keyboard do not overflow', (
    tester,
  ) async {
    _phoneSize(tester, size: const Size(320, 568));
    await _open(tester);
    await tester.tap(find.byKey(const ValueKey('dashboard-filter-farm')));
    await tester.pumpAndSettle();
    await tester.tap(
      find.byKey(const ValueKey('dashboard-filter-farm-search')),
    );
    tester.view.viewInsets = const FakeViewPadding(bottom: 240);
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull);
    expect(
      tester.getRect(find.widgetWithText(FilledButton, 'Apply Filters')).bottom,
      lessThanOrEqualTo(328),
    );
    await tester.enterText(
      find.byKey(const ValueKey('dashboard-filter-farm-search')),
      'No such farm',
    );
    await tester.pumpAndSettle();
    expect(find.text('No matching results'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('dates use a two-digit day and English abbreviated month', (
    tester,
  ) async {
    _phoneSize(tester);
    DashboardFilter? result;
    await _open(
      tester,
      initial: DashboardFilter(
        startDate: DateTime(2025, 2, 1),
        endDate: DateTime(2025, 2, 3),
      ),
      onResult: (value) => result = value,
    );

    expect(find.text('01 Feb 2025'), findsOneWidget);
    expect(find.text('03 Feb 2025'), findsOneWidget);
    expect(find.text('2025-02-01'), findsNothing);
    await tester.tap(find.text('Apply Filters'));
    await tester.pumpAndSettle();
    expect(result?.toQuery(), {
      'startDate': '2025-02-01',
      'endDate': '2025-02-03',
    });
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'reset icon and label fit at 320px and 2× text and still reset the draft',
    (tester) async {
      _phoneSize(tester, size: const Size(320, 568));
      DashboardFilter? result;
      await _open(
        tester,
        initial: _initial(farmId: 'farm-a', barnId: 'barn-a'),
        onResult: (value) => result = value,
        textScale: 2,
      );
      final reset = find.byKey(const ValueKey('dashboard-filter-reset'));
      final icon = find.descendant(
        of: reset,
        matching: find.byIcon(Icons.restart_alt_rounded),
      );
      final label = find.descendant(of: reset, matching: find.text('Reset'));
      expect(icon, findsOneWidget);
      expect(label, findsOneWidget);
      final labelParagraph = tester.renderObject<RenderParagraph>(label);
      expect(
        labelParagraph.getBoxesForSelection(
          const TextSelection(baseOffset: 0, extentOffset: 5),
        ),
        hasLength(1),
        reason:
            'Reset must remain a single unbroken label at large text sizes.',
      );
      expect(
        tester.getRect(icon).left,
        greaterThanOrEqualTo(tester.getRect(reset).left),
      );
      expect(
        tester.getRect(label).right,
        lessThanOrEqualTo(tester.getRect(reset).right),
      );
      expect(tester.getRect(reset).bottom, lessThanOrEqualTo(568));
      expect(tester.takeException(), isNull);

      final defaults = DashboardFilter.defaults();
      await tester.tap(reset);
      await tester.pumpAndSettle();
      expect(find.text('All Farms'), findsOneWidget);
      expect(find.text('All Barns'), findsOneWidget);
      expect(result, isNull);
      await tester.tap(find.text('Apply Filters'));
      await tester.pumpAndSettle();
      expect(result, defaults);
      expect(tester.takeException(), isNull);
    },
  );
}

void _phoneSize(WidgetTester tester, {Size size = const Size(393, 852)}) {
  tester.view.physicalSize = size;
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  addTearDown(tester.view.resetViewInsets);
}

Future<void> _open(
  WidgetTester tester, {
  DashboardFilter? initial,
  ValueChanged<DashboardFilter?>? onResult,
  Future<DashboardFilterOptions> Function()? optionsLoader,
  bool settle = true,
  double textScale = 1,
}) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        dashboardOptionsProvider.overrideWith(
          (_) => optionsLoader?.call() ?? Future.value(_options),
        ),
      ],
      child: MaterialApp(
        theme: AppTheme.light,
        builder: (context, child) => MediaQuery(
          data: MediaQuery.of(
            context,
          ).copyWith(textScaler: TextScaler.linear(textScale)),
          child: child!,
        ),
        home: Scaffold(
          body: Builder(
            builder: (context) => TextButton(
              onPressed: () async {
                final value = await showDashboardFilterSheet(
                  context,
                  initial: initial ?? _initial(),
                );
                onResult?.call(value);
              },
              child: const Text('Open filters'),
            ),
          ),
        ),
      ),
    ),
  );
  await tester.tap(find.text('Open filters'));
  if (settle) {
    await tester.pumpAndSettle();
  } else {
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));
  }
}
