import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_data.dart';
import 'package:smartsheep_mobile/features/dashboard/dashboard_screen.dart';

void main() {
  for (final size in [const Size(320, 568), const Size(393, 852)]) {
    testWidgets('Filter card remains readable at ${size.width}px and 2× text', (
      tester,
    ) async {
      await _pump(tester, size: size, textScale: 2);

      final title = find.byKey(const ValueKey('dashboard-filter-title'));
      expect(title, findsOneWidget);
      expect(tester.widget<Text>(title).data, 'Filter');
      expect(tester.takeException(), isNull);
      final header = tester.getRect(
        find.byKey(const ValueKey('dashboard-period-header')),
      );
      final range = find.byKey(const ValueKey('dashboard-period'));
      final rangeText = tester.widget<Text>(range);
      expect(rangeText.data, 'Period: 07 Sep 2026 - 14 Sep 2026');
      expect(rangeText.maxLines, isNull);
      expect(rangeText.overflow, isNot(TextOverflow.ellipsis));
      expect(tester.getRect(range).top, greaterThan(header.bottom));
      final paragraph = tester.renderObject<RenderParagraph>(range);
      expect(paragraph.didExceedMaxLines, isFalse);

      final summary = tester.widget<Text>(
        find.byKey(const ValueKey('dashboard-applied-location')),
      );
      expect(summary.maxLines, 2);
      expect(summary.style?.color, rangeText.style?.color);
      expect(summary.style?.fontWeight, rangeText.style?.fontWeight);
      expect(summary.style?.fontSize, rangeText.style?.fontSize);
      expect(summary.overflow, TextOverflow.ellipsis);
      expect(summary.data, startsWith('Farm: SmartSheep Farm'));
      expect(summary.data, contains('  Barn: Kandang Domba'));
      expect(summary.data, isNot(contains(' · ')));
      final button = tester.getRect(
        find.byKey(const ValueKey('dashboard-filter-button')),
      );
      expect(button.bottom, lessThanOrEqualTo(header.bottom));
      expect(button.top, greaterThan(tester.getRect(title).bottom));
      expect(button.right, lessThanOrEqualTo(size.width - 14));
    });
  }

  testWidgets(
    'Filter uses a divided card header with its button on the right',
    (tester) async {
      await _pump(tester, size: const Size(393, 852), textScale: 1);

      final titleFinder = find.byKey(const ValueKey('dashboard-filter-title'));
      expect(tester.widget<Text>(titleFinder).data, 'Filter');
      final title = tester.getRect(titleFinder);
      final button = tester.getRect(
        find.byKey(const ValueKey('dashboard-filter-button')),
      );
      expect(button.left, greaterThan(title.right));
      final header = tester.widget<Container>(
        find.byKey(const ValueKey('dashboard-period-header')),
      );
      final decoration = header.decoration! as BoxDecoration;
      expect(decoration.gradient, isNotNull);
      expect(decoration.border, isNotNull);
      expect(tester.takeException(), isNull);
    },
  );
}

class _AppliedFilter extends DashboardFilterNotifier {
  @override
  DashboardFilter build() => DashboardFilter(
    startDate: DateTime(2026, 9, 7),
    endDate: DateTime(2026, 9, 14),
    farmId: 'farm',
    barnId: 'barn',
  );
}

Future<void> _pump(
  WidgetTester tester, {
  required Size size,
  required double textScale,
}) async {
  await tester.binding.setSurfaceSize(size);
  addTearDown(() => tester.binding.setSurfaceSize(null));
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        dashboardOptionsProvider.overrideWith(
          (_) async => const DashboardFilterOptions(
            farms: [
              DashboardFarmOption(
                id: 'farm',
                name:
                    'SmartSheep Farm Jawa Barat Kabupaten Sukabumi Wilayah Selatan '
                    'Peternakan Domba dan Kambing Kawasan Pengembangan',
              ),
            ],
            barns: [
              DashboardBarnOption(
                id: 'barn',
                farmId: 'farm',
                name:
                    'Kandang Domba Sukabumi Wilayah Barat Nomor 1234 '
                    'Pemeliharaan Induk dan Anak Domba',
              ),
            ],
          ),
        ),
        dashboardFilterProvider.overrideWith(_AppliedFilter.new),
        dashboardProvider.overrideWith(
          (_) async => throw StateError('Offline'),
        ),
      ],
      child: MaterialApp(
        theme: AppTheme.light,
        home: MediaQuery(
          data: MediaQueryData(
            size: size,
            textScaler: TextScaler.linear(textScale),
          ),
          child: const DashboardScreen(),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}
