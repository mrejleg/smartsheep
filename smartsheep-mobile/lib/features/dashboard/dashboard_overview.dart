import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../core/theme/app_theme.dart';
import 'dashboard_data.dart';

String dashboardDuration(int seconds) {
  // Match the web's TimeSpan hh:mm:ss, including its 24-hour component.
  final duration = Duration(seconds: seconds);
  return [
    duration.inHours % 24,
    duration.inMinutes % 60,
    duration.inSeconds % 60,
  ].map((part) => part.toString().padLeft(2, '0')).join(':');
}

class DashboardOverview extends StatelessWidget {
  const DashboardOverview({super.key, required this.data});
  final DashboardData data;

  @override
  Widget build(BuildContext context) {
    final number = NumberFormat.decimalPattern('en_US');
    final livestock = _OverviewCard(
      title: 'Livestock Overview',
      icon: Icons.home_outlined,
      rows: [
        _MetricRow(
          icon: Icons.location_on_outlined,
          label: 'Total Farms',
          value: number.format(data.totalFarms),
        ),
        _MetricRow(
          icon: Icons.inventory_2_outlined,
          label: 'Total Barns',
          value: number.format(data.totalBarns),
        ),
        _MetricRow(
          icon: Icons.favorite_border,
          label: 'Total Sheep',
          value: number.format(data.totalSheep),
        ),
      ],
    );
    final activity = _OverviewCard(
      title: "Today's Activity",
      icon: Icons.monitor_heart_outlined,
      rows: [
        _MetricRow(
          icon: Icons.monitor_heart_outlined,
          label: 'Activities',
          value: number.format(data.todayActivities),
        ),
        _MetricRow(
          icon: Icons.repeat,
          label: 'Frequency',
          value: number.format(data.todayFrequency),
        ),
        _MetricRow(
          icon: Icons.access_time,
          label: 'Duration',
          value: dashboardDuration(data.todayDurationSeconds),
        ),
      ],
    );
    return LayoutBuilder(
      builder: (context, constraints) => constraints.maxWidth >= 640
          ? Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(child: livestock),
                const SizedBox(width: 10),
                Expanded(child: activity),
              ],
            )
          : Column(children: [livestock, const SizedBox(height: 10), activity]),
    );
  }
}

class _OverviewCard extends StatelessWidget {
  const _OverviewCard({
    required this.title,
    required this.icon,
    required this.rows,
  });
  final String title;
  final IconData icon;
  final List<Widget> rows;

  @override
  Widget build(BuildContext context) => Card(
    clipBehavior: Clip.antiAlias,
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _PanelHeader(title: title, icon: icon),
        Padding(
          padding: const EdgeInsets.all(10),
          child: Column(
            children: [
              for (var i = 0; i < rows.length; i++) ...[
                if (i > 0) const SizedBox(height: 8),
                rows[i],
              ],
            ],
          ),
        ),
      ],
    ),
  );
}

class _MetricRow extends StatelessWidget {
  const _MetricRow({
    required this.icon,
    required this.label,
    required this.value,
  });
  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 14),
    decoration: BoxDecoration(
      color: const Color(0xFFFBFDFD),
      border: Border.all(color: const Color(0xFFEDF2F4)),
      borderRadius: BorderRadius.circular(9),
    ),
    child: Row(
      children: [
        Container(
          padding: const EdgeInsets.all(8),
          decoration: BoxDecoration(
            color: AppColors.mint,
            borderRadius: BorderRadius.circular(8),
          ),
          child: Icon(icon, size: 20, color: AppColors.tealDark),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Text(
            label,
            style: const TextStyle(
              color: AppColors.muted,
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            value,
            textAlign: TextAlign.end,
            style: const TextStyle(
              color: Color(0xFF172033),
              fontSize: 20,
              fontWeight: FontWeight.w800,
            ),
          ),
        ),
      ],
    ),
  );
}

class DashboardInactiveSources extends StatelessWidget {
  const DashboardInactiveSources({super.key, required this.items});
  final List<DashboardInactiveSourceVideo> items;

  @override
  Widget build(BuildContext context) => Card(
    clipBehavior: Clip.antiAlias,
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _PanelHeader(
          title: 'Inactive Source Videos',
          icon: Icons.videocam_off_outlined,
          badge:
              '${NumberFormat.decimalPattern('en_US').format(items.length)} Sources',
        ),
        if (items.isEmpty)
          const Padding(
            padding: EdgeInsets.symmetric(vertical: 32, horizontal: 16),
            child: Text(
              'All source videos are online.',
              textAlign: TextAlign.center,
              style: TextStyle(color: AppColors.muted, fontSize: 12),
            ),
          )
        else
          Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              children: [
                for (var index = 0; index < items.length; index++) ...[
                  if (index > 0) const Divider(height: 24),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Padding(
                        padding: EdgeInsets.only(top: 3),
                        child: Icon(
                          Icons.videocam_off_outlined,
                          color: Color(0xFFEF334B),
                          size: 22,
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              items[index].code ?? '—',
                              style: const TextStyle(
                                fontWeight: FontWeight.w700,
                                fontSize: 13,
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              '${items[index].barnName ?? '—'} · ${items[index].lastStatus ?? 'No Status'}',
                              style: const TextStyle(
                                fontSize: 12,
                                color: AppColors.muted,
                                height: 1.4,
                              ),
                            ),
                            const SizedBox(height: 6),
                            _Badge(
                              text: items[index].lastCheckedAt == null
                                  ? 'Never Checked'
                                  : DateFormat(
                                      'yyyy-MM-dd HH:mm',
                                    ).format(items[index].lastCheckedAt!),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ),
      ],
    ),
  );
}

class _PanelHeader extends StatelessWidget {
  const _PanelHeader({required this.title, required this.icon, this.badge});
  final String title;
  final IconData icon;
  final String? badge;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(12),
    decoration: const BoxDecoration(
      gradient: LinearGradient(
        begin: Alignment.topCenter,
        end: Alignment.bottomCenter,
        colors: [Colors.white, Color(0xFFF8FCFD)],
      ),
      border: Border(bottom: BorderSide(color: Color(0xFFD8E4EC))),
    ),
    child: Row(
      children: [
        Container(
          padding: const EdgeInsets.all(8),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(8),
            gradient: const LinearGradient(
              colors: [AppColors.teal, AppColors.tealDark],
            ),
          ),
          child: Icon(icon, size: 18, color: Colors.white),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Text(
            title,
            style: const TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w800,
              color: Color(0xFF172033),
            ),
          ),
        ),
        if (badge != null) ...[const SizedBox(width: 8), _Badge(text: badge!)],
      ],
    ),
  );
}

class _Badge extends StatelessWidget {
  const _Badge({required this.text});
  final String text;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 4),
    decoration: BoxDecoration(
      color: AppColors.mint,
      borderRadius: BorderRadius.circular(7),
    ),
    child: Text(
      text,
      style: const TextStyle(
        fontSize: 10,
        fontWeight: FontWeight.w700,
        color: AppColors.tealDark,
      ),
    ),
  );
}
