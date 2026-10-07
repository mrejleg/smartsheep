import 'dart:math' as math;

import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import 'dashboard_data.dart';

const _teal = Color(0xFF009F97);
const _tealDark = Color(0xFF006B66);
const _yellow = Color(0xFFE5B629);
const _ink = Color(0xFF172033);
const _muted = Color(0xFF667085);
const _grid = Color(0x14101828);
const _axisStyle = TextStyle(color: _muted, fontSize: 9);
final _number = NumberFormat.decimalPattern('en_US');

/// The same two series and shared value axis as the Web dashboard.
class DashboardHourlyChart extends StatefulWidget {
  const DashboardHourlyChart({super.key, required this.items});

  final List<DashboardHourlyTrend> items;

  @override
  State<DashboardHourlyChart> createState() => _DashboardHourlyChartState();
}

class _DashboardHourlyChartState extends State<DashboardHourlyChart> {
  int? _selected;

  @override
  void didUpdateWidget(covariant DashboardHourlyChart oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.items != widget.items) _selected = null;
  }

  @override
  Widget build(BuildContext context) {
    final items = widget.items;
    final hasData = items.any(
      (item) => item.frequency > 0 || item.durationSeconds > 0,
    );
    final minutes = items
        .map((item) => (item.durationSeconds / 60).round())
        .toList();
    final values = <num>[...items.map((item) => item.frequency), ...minutes];
    final scale = _chartScale(values);
    final selected = _selected == null ? null : items[_selected!];

    return _ChartCard(
      title: 'Suckling Activity Trend (Hourly)',
      icon: Icons.trending_up,
      child: !hasData
          ? const DashboardChartNoData(
              message: 'Hourly activity data is not available',
            )
          : Column(
              children: [
                const Align(
                  alignment: Alignment.centerRight,
                  child: Wrap(
                    spacing: 14,
                    runSpacing: 6,
                    children: [
                      _Legend(color: _teal, label: 'Frequency'),
                      _Legend(color: _yellow, label: 'Duration (min)'),
                    ],
                  ),
                ),
                const SizedBox(height: 20),
                LayoutBuilder(
                  builder: (context, constraints) => TapRegion(
                    // A tap anywhere off the plot dismisses the tooltip, so it
                    // never needs a close button of its own.
                    onTapOutside: (_) {
                      if (_selected != null) setState(() => _selected = null);
                    },
                    child: GestureDetector(
                      key: const ValueKey('dashboard-hourly-plot'),
                      behavior: HitTestBehavior.opaque,
                      // Only a tap recognizer: swiping on the chart continues to
                      // scroll the dashboard instead of entering a chart pan.
                      onTapUp: (details) {
                        final plotWidth = math.max(
                          1.0,
                          constraints.maxWidth - 44,
                        );
                        final index =
                            ((details.localPosition.dx - 44) /
                                    plotWidth *
                                    math.max(1, items.length - 1))
                                .round()
                                .clamp(0, items.length - 1);
                        setState(
                          () => _selected = _selected == index ? null : index,
                        );
                      },
                      child: SizedBox(
                        height: 220,
                        child: Stack(
                          children: [
                            Positioned.fill(
                              child: IgnorePointer(
                                child: LineChart(
                                  duration: const Duration(milliseconds: 650),
                                  LineChartData(
                                    minX: 0,
                                    maxX: math
                                        .max(1, items.length - 1)
                                        .toDouble(),
                                    minY: 0,
                                    maxY: scale.max,
                                    gridData: _gridData(scale.interval),
                                    borderData: _borderData(),
                                    lineTouchData: const LineTouchData(
                                      enabled: false,
                                    ),
                                    titlesData: _titlesData(
                                      interval: scale.interval,
                                      bottom: SideTitles(
                                        showTitles: true,
                                        reservedSize: 30,
                                        interval: math
                                            .max(
                                              1,
                                              ((items.length - 1) / 4).ceil(),
                                            )
                                            .toDouble(),
                                        getTitlesWidget: (value, meta) {
                                          final index = value.round();
                                          if (index >= items.length ||
                                              value != index) {
                                            return const SizedBox.shrink();
                                          }
                                          return SideTitleWidget(
                                            meta: meta,
                                            fitInside:
                                                SideTitleFitInsideData.fromTitleMeta(
                                                  meta,
                                                ),
                                            child: Text(
                                              _hour(items[index].hour),
                                              style: _axisStyle,
                                            ),
                                          );
                                        },
                                      ),
                                    ),
                                    extraLinesData: ExtraLinesData(
                                      verticalLines: [
                                        if (_selected != null)
                                          VerticalLine(
                                            x: _selected!.toDouble(),
                                            color: _tealDark.withValues(
                                              alpha: .28,
                                            ),
                                            strokeWidth: 1,
                                            dashArray: [4, 4],
                                          ),
                                      ],
                                    ),
                                    lineBarsData: [
                                      _line(
                                        items
                                            .map((item) => item.frequency)
                                            .toList(),
                                        color: _teal,
                                        area: true,
                                      ),
                                      _line(minutes, color: _yellow),
                                    ],
                                  ),
                                ),
                              ),
                            ),
                            if (selected != null)
                              Positioned(
                                top: 0,
                                left: 46,
                                right: 0,
                                child: IgnorePointer(
                                  child: _ChartTooltip(
                                    heading: 'Activity time',
                                    title: _hour(selected.hour),
                                    values: [
                                      (
                                        label: 'Frequency',
                                        value: selected.frequency,
                                        color: _teal,
                                      ),
                                      (
                                        label: 'Duration (min)',
                                        value: minutes[_selected!],
                                        color: _yellow,
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ],
            ),
    );
  }
}

class DashboardBarnChart extends StatefulWidget {
  const DashboardBarnChart({super.key, required this.items});

  final List<DashboardBarnComparison> items;

  @override
  State<DashboardBarnChart> createState() => _DashboardBarnChartState();
}

class _DashboardBarnChartState extends State<DashboardBarnChart> {
  static const _pageSize = 5;
  int _page = 0;
  int? _selected;

  @override
  void didUpdateWidget(covariant DashboardBarnChart oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.items != widget.items) {
      _page = 0;
      _selected = null;
    }
  }

  @override
  Widget build(BuildContext context) {
    final hasData = widget.items.any((item) => item.frequency > 0);
    final pageCount = math.max(1, (widget.items.length / _pageSize).ceil());
    final items = widget.items.skip(_page * _pageSize).take(_pageSize).toList();
    final scale = _chartScale(items.map((item) => item.frequency));
    final selected = _selected == null ? null : items[_selected!];

    return _ChartCard(
      title: 'Activity by Barn (Comparison)',
      icon: Icons.bar_chart,
      actions: !hasData || pageCount <= 1
          ? null
          : Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                IconButton(
                  tooltip: 'Previous barn page',
                  onPressed: _page == 0 ? null : () => _changePage(_page - 1),
                  icon: const Icon(Icons.chevron_left, size: 20),
                ),
                Text(
                  '${_page + 1}/$pageCount',
                  key: const ValueKey('dashboard-barn-page'),
                  style: const TextStyle(
                    color: _muted,
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                IconButton(
                  tooltip: 'Next barn page',
                  onPressed: _page == pageCount - 1
                      ? null
                      : () => _changePage(_page + 1),
                  icon: const Icon(Icons.chevron_right, size: 20),
                ),
              ],
            ),
      child: !hasData
          ? const DashboardChartNoData(
              message: 'Barn activity data is not available',
            )
          : LayoutBuilder(
              builder: (context, constraints) {
                final plotWidth = math.max(1.0, constraints.maxWidth - 44);
                return TapRegion(
                  // A tap anywhere off the plot dismisses the tooltip, so it
                  // never needs a close button of its own.
                  onTapOutside: (_) {
                    if (_selected != null) setState(() => _selected = null);
                  },
                  child: GestureDetector(
                    key: const ValueKey('dashboard-barn-plot'),
                    behavior: HitTestBehavior.opaque,
                    onTapUp: (details) {
                      final index =
                          ((details.localPosition.dx - 44) /
                                  plotWidth *
                                  items.length)
                              .floor()
                              .clamp(0, items.length - 1);
                      setState(
                        () => _selected = _selected == index ? null : index,
                      );
                    },
                    child: SizedBox(
                      height: 246,
                      child: Stack(
                        children: [
                          Positioned.fill(
                            child: IgnorePointer(
                              child: BarChart(
                                key: ValueKey((widget.items, _page)),
                                duration: const Duration(milliseconds: 650),
                                BarChartData(
                                  minY: 0,
                                  maxY: scale.max,
                                  alignment: BarChartAlignment.spaceAround,
                                  gridData: _gridData(scale.interval),
                                  borderData: _borderData(),
                                  titlesData: _titlesData(
                                    interval: scale.interval,
                                    bottom: SideTitles(
                                      showTitles: true,
                                      reservedSize: 48,
                                      getTitlesWidget: (value, meta) {
                                        final index = value.round();
                                        if (index < 0 ||
                                            index >= items.length) {
                                          return const SizedBox.shrink();
                                        }
                                        final name = _barnName(items[index]);
                                        return SideTitleWidget(
                                          meta: meta,
                                          child: SizedBox(
                                            width: math.max(
                                              1,
                                              plotWidth / items.length - 4,
                                            ),
                                            child: Text(
                                              name,
                                              maxLines: 2,
                                              overflow: TextOverflow.ellipsis,
                                              textAlign: TextAlign.center,
                                              style: _axisStyle.copyWith(
                                                height: 1.25,
                                              ),
                                            ),
                                          ),
                                        );
                                      },
                                    ),
                                  ),
                                  barTouchData: BarTouchData(
                                    enabled: false,
                                    handleBuiltInTouches: false,
                                    touchTooltipData: BarTouchTooltipData(
                                      getTooltipColor: (_) =>
                                          Colors.transparent,
                                      tooltipPadding: EdgeInsets.zero,
                                      tooltipMargin: 6,
                                      fitInsideHorizontally: true,
                                      fitInsideVertically: true,
                                      getTooltipItem:
                                          (group, groupIndex, rod, rodIndex) =>
                                              BarTooltipItem(
                                                _number.format(rod.toY),
                                                const TextStyle(
                                                  color: _ink,
                                                  fontSize: 9,
                                                  fontWeight: FontWeight.w800,
                                                ),
                                              ),
                                    ),
                                  ),
                                  barGroups: [
                                    for (
                                      var index = 0;
                                      index < items.length;
                                      index++
                                    )
                                      BarChartGroupData(
                                        x: index,
                                        showingTooltipIndicators: [0],
                                        barRods: [
                                          BarChartRodData(
                                            toY: items[index].frequency
                                                .toDouble(),
                                            width: math.min(
                                              34,
                                              plotWidth / items.length * .52,
                                            ),
                                            borderRadius:
                                                const BorderRadius.vertical(
                                                  top: Radius.circular(7),
                                                ),
                                            gradient: const LinearGradient(
                                              begin: Alignment.bottomCenter,
                                              end: Alignment.topCenter,
                                              colors: [_tealDark, _teal],
                                            ),
                                          ),
                                        ],
                                      ),
                                  ],
                                ),
                              ),
                            ),
                          ),

                          if (selected != null)
                            Positioned(
                              top: 0,
                              left: 46,
                              right: 0,
                              child: IgnorePointer(
                                child: _ChartTooltip(
                                  heading: 'Barn',
                                  title: _barnName(selected),
                                  values: [
                                    (
                                      label: 'Frequency',
                                      value: selected.frequency,
                                      color: _teal,
                                    ),
                                  ],
                                ),
                              ),
                            ),
                        ],
                      ),
                    ),
                  ),
                );
              },
            ),
    );
  }

  void _changePage(int page) => setState(() {
    _page = page;
    _selected = null;
  });
}

/// Matches the Web dashboard's centered, teal empty-state treatment.
class DashboardChartNoData extends StatelessWidget {
  const DashboardChartNoData({super.key, required this.message});

  final String message;

  @override
  Widget build(BuildContext context) => SizedBox(
    height: 246,
    child: Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 38,
            height: 38,
            decoration: BoxDecoration(
              color: const Color(0xFFE8F7F5),
              shape: BoxShape.circle,
              border: Border.all(color: _teal.withValues(alpha: .16)),
            ),
            child: const Icon(
              Icons.inventory_2_outlined,
              size: 23,
              color: Color(0xFF008F87),
            ),
          ),
          const SizedBox(height: 10),
          const Text(
            'No data',
            style: TextStyle(
              color: _ink,
              fontSize: 13,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 5),
          Text(
            message,
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: _muted,
              fontSize: 10.5,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    ),
  );
}

class _ChartCard extends StatelessWidget {
  const _ChartCard({
    required this.title,
    required this.icon,
    required this.child,
    this.actions,
  });

  final String title;
  final IconData icon;
  final Widget child;
  final Widget? actions;

  @override
  Widget build(BuildContext context) => Card(
    clipBehavior: Clip.antiAlias,
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(14, 14, 14, 12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  Icon(icon, color: _tealDark, size: 18),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      title,
                      style: const TextStyle(
                        color: _ink,
                        fontSize: 14,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                ],
              ),
              if (actions != null)
                Align(alignment: Alignment.centerRight, child: actions!),
            ],
          ),
        ),
        const Divider(height: 1, color: _grid),
        Padding(
          padding: const EdgeInsets.fromLTRB(10, 18, 14, 8),
          child: child,
        ),
      ],
    ),
  );
}

class _Legend extends StatelessWidget {
  const _Legend({required this.color, required this.label});
  final Color color;
  final String label;

  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      Container(
        width: 8,
        height: 8,
        decoration: BoxDecoration(color: color, shape: BoxShape.circle),
      ),
      const SizedBox(width: 6),
      Text(label, style: const TextStyle(color: _muted, fontSize: 10)),
    ],
  );
}

class _ChartTooltip extends StatelessWidget {
  const _ChartTooltip({
    required this.heading,
    required this.title,
    required this.values,
  });

  final String heading;
  final String title;
  final List<({String label, int value, Color color})> values;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(10),
    decoration: BoxDecoration(
      color: Colors.white.withValues(alpha: .98),
      borderRadius: BorderRadius.circular(12),
      border: Border.all(color: _teal.withValues(alpha: .18)),
      boxShadow: [
        BoxShadow(
          color: _ink.withValues(alpha: .16),
          blurRadius: 24,
          offset: const Offset(0, 8),
        ),
      ],
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          heading.toUpperCase(),
          style: const TextStyle(
            color: Color(0xFF98A2B3),
            fontSize: 9,
            fontWeight: FontWeight.w800,
            letterSpacing: .8,
          ),
        ),
        const SizedBox(height: 4),
        Text(
          title,
          style: const TextStyle(
            color: _ink,
            fontSize: 11,
            fontWeight: FontWeight.w800,
          ),
        ),
        const Divider(height: 14, color: Color(0xFFEDF2F4)),
        for (final value in values)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 4),
            child: Row(
              children: [
                Container(
                  width: 8,
                  height: 8,
                  decoration: BoxDecoration(
                    color: value.color,
                    shape: BoxShape.circle,
                  ),
                ),
                const SizedBox(width: 7),
                Expanded(
                  child: Text(
                    value.label,
                    style: const TextStyle(
                      color: _muted,
                      fontSize: 11,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                Text(
                  _number.format(value.value),
                  style: const TextStyle(
                    color: _ink,
                    fontSize: 12,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ],
            ),
          ),
      ],
    ),
  );
}

String _hour(int hour) => '${hour.toString().padLeft(2, '0')}:00';

String _barnName(DashboardBarnComparison item) =>
    (item.barnName?.isNotEmpty ?? false)
    ? item.barnName!
    : (item.barnCode?.isNotEmpty ?? false)
    ? item.barnCode!
    : '-';

({double max, double interval}) _chartScale(Iterable<num> values) {
  final maximum = values.fold<double>(
    0,
    (result, value) => math.max(result, value.toDouble()),
  );
  if (maximum <= 0) return (max: 1, interval: 1);
  final roughStep = maximum / 4;
  final magnitude = math
      .pow(10, (math.log(roughStep) / math.ln10).floor())
      .toDouble();
  final normalized = roughStep / magnitude;
  final step = math.max(
    1.0,
    (normalized <= 1
            ? 1
            : normalized <= 2
            ? 2
            : normalized <= 5
            ? 5
            : 10) *
        magnitude,
  );
  return (max: (maximum / step).ceil() * step + step, interval: step);
}

FlGridData _gridData(double interval) => FlGridData(
  drawVerticalLine: false,
  horizontalInterval: interval,
  getDrawingHorizontalLine: (_) => const FlLine(color: _grid, strokeWidth: 1),
);

FlBorderData _borderData() => FlBorderData(
  show: true,
  border: const Border(bottom: BorderSide(color: _grid)),
);

FlTitlesData _titlesData({
  required double interval,
  required SideTitles bottom,
}) => FlTitlesData(
  topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
  rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
  leftTitles: AxisTitles(
    sideTitles: SideTitles(
      showTitles: true,
      reservedSize: 44,
      interval: interval,
      getTitlesWidget: (value, meta) => SideTitleWidget(
        meta: meta,
        child: SizedBox(
          width: 34,
          child: FittedBox(
            fit: BoxFit.scaleDown,
            alignment: Alignment.centerRight,
            child: Text(_number.format(value), style: _axisStyle),
          ),
        ),
      ),
    ),
  ),
  bottomTitles: AxisTitles(sideTitles: bottom),
);

LineChartBarData _line(
  List<int> values, {
  required Color color,
  bool area = false,
}) => LineChartBarData(
  isCurved: true,
  preventCurveOverShooting: true,
  color: color,
  barWidth: 3,
  spots: [
    for (var index = 0; index < values.length; index++)
      FlSpot(index.toDouble(), values[index].toDouble()),
  ],
  dotData: FlDotData(
    getDotPainter: (_, _, _, _) => FlDotCirclePainter(
      radius: 3,
      color: Colors.white,
      strokeWidth: 2,
      strokeColor: color,
    ),
  ),
  belowBarData: BarAreaData(
    show: area,
    gradient: LinearGradient(
      begin: Alignment.topCenter,
      end: Alignment.bottomCenter,
      colors: [_teal.withValues(alpha: .24), _teal.withValues(alpha: 0)],
    ),
  ),
);
