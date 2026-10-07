import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../core/theme/app_theme.dart';
import '../shell/member_header.dart';
import 'dashboard_charts.dart';
import 'dashboard_data.dart';
import 'dashboard_filter_sheet.dart';
import 'dashboard_map.dart';
import 'dashboard_overview.dart';

export 'dashboard_data.dart' show dashboardProvider;

class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({super.key, this.mapTileProvider});

  final TileProvider? mapTileProvider;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final filter = ref.watch(dashboardFilterProvider);
    final options = ref.watch(dashboardOptionsProvider).value;
    final data = ref.watch(dashboardProvider);
    final dateFormat = DateFormat('dd MMM yyyy', 'en_US');
    final farm = options?.farms
        .where((item) => item.id == filter.farmId)
        .firstOrNull;
    final barn = options?.barns
        .where((item) => item.id == filter.barnId)
        .firstOrNull;

    return Scaffold(
      body: Column(
        children: [
          MemberHeader(
            title: 'Dashboard',
            subtitle: MediaQuery.textScalerOf(context).scale(12) > 15.6
                ? null
                : 'Livestock overview',
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(14, 12, 14, 8),
            child: Card(
              key: const ValueKey('dashboard-period-card'),
              clipBehavior: Clip.antiAlias,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  _FilterHeader(
                    onFilter: () async {
                      final result = await showDashboardFilterSheet(
                        context,
                        initial: filter,
                      );
                      if (result == null || !context.mounted) return;
                      ref
                          .read(dashboardFilterProvider.notifier)
                          .setFilter(result);
                      ref.invalidate(dashboardProvider);
                    },
                  ),
                  Padding(
                    padding: const EdgeInsets.all(12),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Text(
                          'Period: ${dateFormat.format(filter.startDate)} - ${dateFormat.format(filter.endDate)}',
                          key: const ValueKey('dashboard-period'),
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: AppColors.ink,
                          ),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          'Farm: ${farm?.name ?? (filter.farmId == null ? 'All' : 'Selected farm')}  Barn: ${barn?.name ?? (filter.barnId == null ? 'All' : 'Selected barn')}',
                          key: const ValueKey('dashboard-applied-location'),
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: AppColors.ink,
                            height: 1.4,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: () => ref.refresh(dashboardProvider.future),
              child: data.when(
                skipLoadingOnRefresh: false,
                loading: () => ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  children: const [
                    SizedBox(height: 100),
                    Center(child: CircularProgressIndicator()),
                    SizedBox(height: 16),
                    Center(child: Text('Loading dashboard…')),
                  ],
                ),
                error: (_, _) => ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.all(24),
                  children: [
                    const SizedBox(height: 24),
                    const Icon(
                      Icons.cloud_off_outlined,
                      size: 44,
                      color: AppColors.muted,
                    ),
                    const SizedBox(height: 12),
                    const Text(
                      'Unable to load dashboard.',
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Check your connection and try again.',
                      textAlign: TextAlign.center,
                      style: TextStyle(color: AppColors.muted),
                    ),
                    const SizedBox(height: 16),
                    Center(
                      child: OutlinedButton(
                        onPressed: () => ref.invalidate(dashboardProvider),
                        child: const Text('Try again'),
                      ),
                    ),
                  ],
                ),
                data: (dashboard) => ListView(
                  key: const PageStorageKey('dashboard-scroll'),
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.fromLTRB(14, 0, 14, 20),
                  children: [
                    DashboardFarmMapCard(
                      items: dashboard.farmMaps,
                      tileProvider: mapTileProvider,
                    ),
                    const SizedBox(height: 10),
                    DashboardOverview(data: dashboard),
                    const SizedBox(height: 10),
                    DashboardHourlyChart(items: dashboard.hourlyTrend),
                    const SizedBox(height: 10),
                    DashboardBarnChart(items: dashboard.barnComparison),
                    const SizedBox(height: 10),
                    DashboardInactiveSources(
                      items: dashboard.inactiveSourceVideos,
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _FilterHeader extends StatelessWidget {
  const _FilterHeader({required this.onFilter});

  final VoidCallback onFilter;

  @override
  Widget build(BuildContext context) => Container(
    key: const ValueKey('dashboard-period-header'),
    padding: const EdgeInsets.all(12),
    decoration: const BoxDecoration(
      gradient: LinearGradient(
        begin: Alignment.topCenter,
        end: Alignment.bottomCenter,
        colors: [Colors.white, Color(0xFFF8FCFD)],
      ),
      border: Border(bottom: BorderSide(color: Color(0xFFD8E4EC))),
    ),
    child: LayoutBuilder(
      builder: (context, constraints) {
        final title = Row(
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(8),
                gradient: const LinearGradient(
                  colors: [AppColors.teal, AppColors.tealDark],
                ),
              ),
              child: const Icon(
                Icons.date_range_outlined,
                size: 18,
                color: Colors.white,
              ),
            ),
            const SizedBox(width: 10),
            const Expanded(
              child: Text(
                'Filter',
                key: ValueKey('dashboard-filter-title'),
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w800,
                  color: Color(0xFF172033),
                ),
              ),
            ),
          ],
        );
        final button = FilledButton.icon(
          key: const ValueKey('dashboard-filter-button'),
          onPressed: onFilter,
          icon: const Icon(Icons.tune, size: 18),
          label: const Text('Filter'),
          style: FilledButton.styleFrom(
            padding: const EdgeInsets.symmetric(horizontal: 12),
          ),
        );
        final stackControls =
            constraints.maxWidth < 320 ||
            MediaQuery.textScalerOf(context).scale(14) > 18.2;
        if (stackControls) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              title,
              const SizedBox(height: 8),
              Align(alignment: Alignment.centerRight, child: button),
            ],
          );
        }
        return Row(
          children: [
            Expanded(child: title),
            const SizedBox(width: 12),
            button,
          ],
        );
      },
    ),
  );
}
