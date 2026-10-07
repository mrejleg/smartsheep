import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../core/network/api_client.dart';
import '../../core/theme/app_theme.dart';
import '../dashboard/dashboard_charts.dart';
import '../dashboard/dashboard_data.dart';
import '../dashboard/dashboard_filter_sheet.dart';
import '../shell/member_header.dart';

enum SucklingReportView { activities, statistics }

const sucklingReportPageSize = 5;
typedef SucklingReportQuery = ({
  SucklingReportView view,
  DashboardFilter filter,
  int page,
});

final sucklingReportProvider = FutureProvider.autoDispose
    .family<List<Map<String, dynamic>>, SucklingReportQuery>((
      ref,
      request,
    ) async {
      final filter = request.filter;
      final date = DateFormat('yyyy-MM-dd');
      final raw = await ref
          .watch(apiClientProvider)
          .get(
            '/api/SmartSheepReport/${request.view == SucklingReportView.activities ? 'SucklingActivities' : 'SucklingStatistics'}',
            query: {
              'from': '${date.format(filter.startDate)}T00:00:00',
              // Match the web report's inclusive end of day, including .NET ticks.
              'to': '${date.format(filter.endDate)}T23:59:59.9999999',
              if (filter.farmId != null) 'farmId': filter.farmId,
              if (filter.barnId != null) 'barnId': filter.barnId,
              'page': request.page,
              'pageSize': sucklingReportPageSize,
            },
          );
      if (raw is! List || raw.any((row) => row is! Map)) {
        throw const FormatException('Invalid report response');
      }
      return raw.map((row) => Map<String, dynamic>.from(row as Map)).toList();
    });

class StatisticsScreen extends ConsumerStatefulWidget {
  const StatisticsScreen({
    super.key,
    this.view = SucklingReportView.statistics,
  });
  final SucklingReportView view;

  @override
  ConsumerState<StatisticsScreen> createState() => _StatisticsScreenState();
}

class _StatisticsScreenState extends ConsumerState<StatisticsScreen> {
  DashboardFilter _filter = DashboardFilter.defaults();
  int _page = 1;
  final _scroll = ScrollController();
  SucklingReportQuery get _query =>
      (view: widget.view, filter: _filter, page: _page);

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _refresh() async {
    try {
      ref.invalidate(sucklingReportProvider(_query));
      await ref.read(sucklingReportProvider(_query).future);
    } catch (_) {
      // The provider renders an explicit error, never an empty success state.
    }
  }

  Future<void> _openFilters() async {
    final next = await showDashboardFilterSheet(
      context,
      initial: _filter,
      title: 'Report Filters',
    );
    if (next == null || !mounted) return;
    setState(() {
      _filter = next;
      _page = 1;
    });
    if (_scroll.hasClients) _scroll.jumpTo(0);
  }

  void _changePage(int page) {
    setState(() => _page = page);
    if (_scroll.hasClients) _scroll.jumpTo(0);
  }

  @override
  Widget build(BuildContext context) {
    final report = ref.watch(sucklingReportProvider(_query));
    final options = ref.watch(dashboardOptionsProvider).asData?.value;
    final date = DateFormat('dd MMM yyyy', 'en_US');
    final farm = _filter.farmId == null
        ? 'All'
        : options?.farms
                  .where((item) => item.id == _filter.farmId)
                  .firstOrNull
                  ?.name ??
              'Selected farm';
    final barn = _filter.barnId == null
        ? 'All'
        : options?.barns
                  .where((item) => item.id == _filter.barnId)
                  .firstOrNull
                  ?.name ??
              'Selected barn';
    return Scaffold(
      body: Column(
        children: [
          MemberHeader(
            title: widget.view == SucklingReportView.activities
                ? 'Suckling Activities'
                : 'Suckling Statistics',
            subtitle: 'Livestock activity report',
            showBackButton: true,
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _refresh,
              child: ListView(
                controller: _scroll,
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.fromLTRB(14, 12, 14, 24),
                children: [
                  Card(
                    clipBehavior: Clip.antiAlias,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Padding(
                          padding: const EdgeInsets.all(12),
                          child: Wrap(
                            alignment: WrapAlignment.spaceBetween,
                            crossAxisAlignment: WrapCrossAlignment.center,
                            spacing: 12,
                            runSpacing: 8,
                            children: [
                              const _PanelTitle(
                                title: 'Filter',
                                icon: Icons.tune,
                              ),
                              FilledButton.icon(
                                key: const ValueKey('suckling-report-filter'),
                                onPressed: _openFilters,
                                icon: const Icon(Icons.tune, size: 18),
                                label: const Text('Filter'),
                              ),
                            ],
                          ),
                        ),
                        const Divider(height: 1),
                        Padding(
                          padding: const EdgeInsets.all(12),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                'Period: ${date.format(_filter.startDate)} - ${date.format(_filter.endDate)}',
                                style: const TextStyle(
                                  color: AppColors.ink,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                              const SizedBox(height: 6),
                              Text(
                                'Farm: $farm  Barn: $barn',
                                style: const TextStyle(
                                  color: AppColors.ink,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 12),
                  Padding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 4,
                      vertical: 8,
                    ),
                    child: LayoutBuilder(
                      builder: (context, constraints) {
                        const title = _PanelTitle(
                          title: 'Report Data',
                          icon: Icons.receipt_long_outlined,
                        );
                        final pager =
                            _page > 1 || report.asData?.value.isNotEmpty == true
                            ? Row(
                                key: const ValueKey(
                                  'suckling-report-pagination',
                                ),
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  IconButton(
                                    tooltip: 'Previous',
                                    onPressed: _page > 1 && !report.isLoading
                                        ? () => _changePage(_page - 1)
                                        : null,
                                    icon: const Icon(Icons.chevron_left),
                                  ),
                                  Text(
                                    'Page $_page',
                                    style: const TextStyle(
                                      fontSize: 12,
                                      fontWeight: FontWeight.w600,
                                    ),
                                  ),
                                  IconButton(
                                    tooltip: 'Next',
                                    onPressed:
                                        !report.isLoading &&
                                            (report.asData?.value.length ?? 0) >
                                                sucklingReportPageSize
                                        ? () => _changePage(_page + 1)
                                        : null,
                                    icon: const Icon(Icons.chevron_right),
                                  ),
                                ],
                              )
                            : const SizedBox.shrink();
                        if (constraints.maxWidth < 330 ||
                            MediaQuery.textScalerOf(context).scale(14) > 20) {
                          return Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              title,
                              Align(
                                alignment: Alignment.centerRight,
                                child: pager,
                              ),
                            ],
                          );
                        }
                        return Row(
                          children: [
                            const Expanded(child: title),
                            const SizedBox(width: 8),
                            pager,
                          ],
                        );
                      },
                    ),
                  ),
                  ...report.when<List<Widget>>(
                    skipLoadingOnRefresh: false,
                    loading: () => [
                      const Padding(
                        padding: EdgeInsets.all(40),
                        child: Center(child: CircularProgressIndicator()),
                      ),
                    ],
                    error: (_, _) => [
                      Card(
                        child: Padding(
                          padding: const EdgeInsets.all(24),
                          child: Column(
                            children: [
                              const Text(
                                'Unable to load report. Please check your connection or access.',
                              ),
                              TextButton(
                                onPressed: _refresh,
                                child: const Text('Retry'),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ],
                    data: (rows) => [
                      if (rows.isEmpty)
                        const Card(
                          child: DashboardChartNoData(
                            message: 'Data is not available',
                          ),
                        ),
                      for (final row in rows.take(sucklingReportPageSize))
                        _ReportRow(row: row, view: widget.view),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _PanelTitle extends StatelessWidget {
  const _PanelTitle({required this.title, required this.icon});
  final String title;
  final IconData icon;
  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      Container(
        padding: const EdgeInsets.all(8),
        decoration: BoxDecoration(
          color: AppColors.tealDark,
          borderRadius: BorderRadius.circular(8),
        ),
        child: Icon(icon, size: 18, color: Colors.white),
      ),
      const SizedBox(width: 10),
      Flexible(
        child: Text(
          title,
          style: const TextStyle(
            fontSize: 15,
            fontWeight: FontWeight.w800,
            color: AppColors.ink,
          ),
        ),
      ),
    ],
  );
}

class _ReportRow extends StatelessWidget {
  const _ReportRow({required this.row, required this.view});
  final Map<String, dynamic> row;
  final SucklingReportView view;

  @override
  Widget build(BuildContext context) {
    final statistics = view == SucklingReportView.statistics;
    final rawBarn = _field(row, 'Barn');
    final title = statistics
        ? _text(row, 'Code')
        : rawBarn is Map
        ? _text(Map<String, dynamic>.from(rawBarn), 'Name')
        : '-';
    final fields = statistics
        ? <(String, String)>[
            ('Period Description', _text(row, 'PeriodDescription')),
            ('Total Frequency', _text(row, 'TotalFrequency')),
            ('Total Duration (Seconds)', _text(row, 'TotalDurationSeconds')),
            (
              'Requires Reminder',
              switch (_field(row, 'RequiresReminder')) {
                true => 'Yes',
                false => 'No',
                _ => '-',
              },
            ),
            ('Activity From', _date(row, 'ActivityFrom')),
            ('Activity To', _date(row, 'ActivityTo')),
            ('Hour Number', _text(row, 'HourNumber')),
          ]
        : <(String, String)>[
            ('Activity Start', _date(row, 'ActivityStart')),
            ('Activity End', _date(row, 'ActivityEnd')),
            ('Total Frequency', _text(row, 'TotalFrequency')),
            ('Total Duration (Seconds)', _text(row, 'TotalDurationSeconds')),
          ];
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              '${statistics ? 'Code' : 'Barn'}: $title',
              style: const TextStyle(
                color: AppColors.ink,
                fontSize: 16,
                fontWeight: FontWeight.w700,
              ),
            ),
            const Divider(height: 22),
            for (final (label, value) in fields)
              Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: LayoutBuilder(
                  builder: (context, constraints) {
                    final narrow =
                        constraints.maxWidth < 300 ||
                        MediaQuery.textScalerOf(context).scale(14) > 20;
                    final caption = Text(
                      label,
                      style: const TextStyle(color: AppColors.muted),
                    );
                    final content = Text(
                      value,
                      style: const TextStyle(
                        color: AppColors.ink,
                        fontWeight: FontWeight.w600,
                      ),
                    );
                    return narrow
                        ? Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              caption,
                              const SizedBox(height: 3),
                              content,
                            ],
                          )
                        : Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Expanded(child: caption),
                              const SizedBox(width: 12),
                              Expanded(child: content),
                            ],
                          );
                  },
                ),
              ),
          ],
        ),
      ),
    );
  }
}

Object? _field(Map<String, dynamic> row, String key) => row.containsKey(key)
    ? row[key]
    : row['${key[0].toLowerCase()}${key.substring(1)}'];
String _text(Map<String, dynamic> row, String key) =>
    _field(row, key)?.toString() ?? '-';
String _date(Map<String, dynamic> row, String key) {
  final value = DateTime.tryParse(_text(row, key));
  // Display report timestamps as returned, matching the web's report fields.
  return value == null
      ? '-'
      : DateFormat('dd MMM yyyy HH:mm', 'en_US').format(value);
}
