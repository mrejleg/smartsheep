import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../core/theme/app_theme.dart';
import 'dashboard_data.dart';

/// Edits a local draft. The caller applies the returned filter only on Apply.
Future<DashboardFilter?> showDashboardFilterSheet(
  BuildContext context, {
  required DashboardFilter initial,
  String title = 'Dashboard Filters',
}) => showModalBottomSheet<DashboardFilter>(
  context: context,
  isScrollControlled: true,
  useSafeArea: true,
  backgroundColor: Colors.white,
  constraints: const BoxConstraints(maxWidth: 640),
  shape: const RoundedRectangleBorder(
    borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
  ),
  clipBehavior: Clip.antiAlias,
  builder: (_) => DashboardFilterSheet(initial: initial, title: title),
);

class DashboardFilterSheet extends ConsumerStatefulWidget {
  const DashboardFilterSheet({
    super.key,
    required this.initial,
    this.title = 'Dashboard Filters',
  });

  final DashboardFilter initial;
  final String title;

  @override
  ConsumerState<DashboardFilterSheet> createState() =>
      _DashboardFilterSheetState();
}

class _DashboardFilterSheetState extends ConsumerState<DashboardFilterSheet> {
  late DashboardFilter _draft = widget.initial;
  String? _expandedField;
  String? _dateError;
  final _searchController = TextEditingController();

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  void _toggleField(String field) {
    FocusManager.instance.primaryFocus?.unfocus();
    setState(() {
      _expandedField = _expandedField == field ? null : field;
      _searchController.clear();
    });
  }

  void _selectFarm(String? id, DashboardFilterOptions options) {
    final currentBarn = options.barns.where((barn) => barn.id == _draft.barnId);
    final keepBarn = id == null || currentBarn.any((barn) => barn.farmId == id);
    _finishSelection(
      _draft.copyWith(farmId: id, barnId: keepBarn ? _draft.barnId : null),
    );
  }

  void _finishSelection(DashboardFilter value) {
    FocusManager.instance.primaryFocus?.unfocus();
    setState(() {
      _draft = value;
      _expandedField = null;
      _searchController.clear();
    });
  }

  Future<void> _pickDate({required bool start}) async {
    FocusManager.instance.primaryFocus?.unfocus();
    setState(() => _expandedField = null);
    final current = start ? _draft.startDate : _draft.endDate;
    final chosen = await showDatePicker(
      context: context,
      initialDate: current,
      firstDate: DateTime(current.year < 1900 ? current.year : 1900),
      lastDate: DateTime(current.year > 2100 ? current.year : 2100, 12, 31),
      helpText: start ? 'Start Date' : 'End Date',
      builder: (context, child) => Theme(
        data: Theme.of(context).copyWith(
          datePickerTheme: DatePickerThemeData(
            backgroundColor: Colors.white,
            surfaceTintColor: Colors.transparent,
            headerBackgroundColor: AppColors.deepGreen,
            headerForegroundColor: Colors.white,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(18),
            ),
          ),
        ),
        child: child!,
      ),
    );
    if (chosen == null || !mounted) return;
    setState(() {
      _draft = start
          ? _draft.copyWith(startDate: chosen)
          : _draft.copyWith(endDate: chosen);
      _dateError = _draft.endDate.isBefore(_draft.startDate)
          ? 'End Date must be on or after Start Date.'
          : null;
    });
  }

  void _apply() {
    if (_draft.endDate.isBefore(_draft.startDate)) {
      setState(() => _dateError = 'End Date must be on or after Start Date.');
      return;
    }
    Navigator.of(context).pop(_draft);
  }

  @override
  Widget build(BuildContext context) {
    final options = ref.watch(dashboardOptionsProvider);
    final keyboardInset = MediaQuery.viewInsetsOf(context).bottom;

    return AnimatedPadding(
      duration: const Duration(milliseconds: 180),
      curve: Curves.easeOut,
      padding: EdgeInsets.only(bottom: keyboardInset),
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxHeight: MediaQuery.sizeOf(context).height * .9,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const SizedBox(height: 10),
            Container(
              width: 38,
              height: 4,
              decoration: BoxDecoration(
                color: const Color(0xFFD5DEDF),
                borderRadius: BorderRadius.circular(10),
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 12, 8, 10),
              child: Row(
                children: [
                  Container(
                    key: const ValueKey('dashboard-filter-sheet-icon'),
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(8),
                      gradient: const LinearGradient(
                        colors: [AppColors.teal, AppColors.tealDark],
                      ),
                    ),
                    child: const Icon(
                      Icons.tune_rounded,
                      size: 18,
                      color: Colors.white,
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      widget.title,
                      style: const TextStyle(
                        fontSize: 19,
                        fontWeight: FontWeight.w700,
                        color: AppColors.ink,
                      ),
                    ),
                  ),
                  IconButton(
                    tooltip: 'Close filters',
                    onPressed: () => Navigator.of(context).pop(),
                    icon: const Icon(Icons.close_rounded, size: 24),
                    padding: const EdgeInsets.all(8),
                    constraints: const BoxConstraints(
                      minWidth: 48,
                      minHeight: 48,
                    ),
                    style: IconButton.styleFrom(foregroundColor: Colors.black),
                  ),
                ],
              ),
            ),
            const Divider(height: 1, color: Color(0xFFE5EBF0)),
            Flexible(
              child: SingleChildScrollView(
                key: const ValueKey('dashboard-filter-scroll'),
                keyboardDismissBehavior:
                    ScrollViewKeyboardDismissBehavior.onDrag,
                padding: const EdgeInsets.fromLTRB(20, 20, 20, 24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    options.when(
                      loading: () => const Padding(
                        padding: EdgeInsets.only(bottom: 16),
                        child: Row(
                          children: [
                            SizedBox(
                              width: 18,
                              height: 18,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            ),
                            SizedBox(width: 10),
                            Expanded(child: Text('Loading farms and barns…')),
                          ],
                        ),
                      ),
                      error: (_, _) => Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              'Unable to load farms and barns.',
                              style: TextStyle(color: AppColors.danger),
                            ),
                            TextButton.icon(
                              onPressed: () =>
                                  ref.invalidate(dashboardOptionsProvider),
                              icon: const Icon(Icons.refresh_rounded),
                              label: const Text('Retry'),
                            ),
                          ],
                        ),
                      ),
                      data: (_) => const SizedBox.shrink(),
                    ),
                    _optionFields(options.asData?.value),
                    const SizedBox(height: 20),
                    LayoutBuilder(
                      builder: (context, constraints) {
                        final start = _dateField(start: true);
                        final end = _dateField(start: false);
                        if (constraints.maxWidth < 330 ||
                            MediaQuery.textScalerOf(context).scale(14) > 20) {
                          return Column(
                            children: [start, const SizedBox(height: 20), end],
                          );
                        }
                        return Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Expanded(child: start),
                            const SizedBox(width: 12),
                            Expanded(child: end),
                          ],
                        );
                      },
                    ),
                    if (_dateError != null)
                      Padding(
                        padding: const EdgeInsets.only(top: 10),
                        child: Text(
                          _dateError!,
                          style: const TextStyle(
                            color: AppColors.danger,
                            fontSize: 13,
                          ),
                        ),
                      ),
                  ],
                ),
              ),
            ),
            const Divider(height: 1, color: Color(0xFFE5EBF0)),
            SafeArea(
              top: false,
              minimum: const EdgeInsets.fromLTRB(20, 16, 20, 16),
              child: Builder(
                builder: (context) {
                  final reset = OutlinedButton.icon(
                    key: const ValueKey('dashboard-filter-reset'),
                    style: OutlinedButton.styleFrom(
                      minimumSize: const Size(0, 48),
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 12,
                      ),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(10),
                      ),
                    ),
                    onPressed: () {
                      _finishSelection(DashboardFilter.defaults());
                      setState(() => _dateError = null);
                    },
                    icon: const Icon(Icons.restart_alt_rounded, size: 19),
                    label: const Text('Reset'),
                  );
                  final apply = FilledButton.icon(
                    style: FilledButton.styleFrom(
                      minimumSize: const Size(0, 48),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(10),
                      ),
                    ),
                    onPressed: _apply,
                    icon: const Icon(Icons.check_rounded, size: 19),
                    label: const Text('Apply Filters'),
                  );
                  if (MediaQuery.textScalerOf(context).scale(14) > 18.2) {
                    return Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [reset, const SizedBox(height: 12), apply],
                    );
                  }
                  return Row(
                    children: [
                      Expanded(child: reset),
                      const SizedBox(width: 12),
                      Expanded(flex: 2, child: apply),
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

  Widget _optionFields(DashboardFilterOptions? options) {
    final farms = options?.farms ?? const <DashboardFarmOption>[];
    final barns = options?.barns ?? const <DashboardBarnOption>[];
    final farmName = _draft.farmId == null
        ? 'All Farms'
        : farms
                  .where((farm) => farm.id == _draft.farmId)
                  .map((farm) => farm.name)
                  .firstOrNull ??
              'Selected farm';
    final barnName = _draft.barnId == null
        ? 'All Barns'
        : barns
                  .where((barn) => barn.id == _draft.barnId)
                  .map((barn) => barn.name)
                  .firstOrNull ??
              'Selected barn';
    final selectableBarns = barns.where(
      (barn) => _draft.farmId == null || barn.farmId == _draft.farmId,
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _selector(
          field: 'farm',
          label: 'Farm',
          value: farmName,
          enabled: options != null,
        ),
        if (_expandedField == 'farm' && options != null)
          _searchableOptions(
            field: 'farm',
            allLabel: 'All Farms',
            selectedId: _draft.farmId,
            items: farms.map((farm) => (id: farm.id, name: farm.name)).toList(),
            onSelect: (id) => _selectFarm(id, options),
          ),
        const SizedBox(height: 20),
        _selector(
          field: 'barn',
          label: 'Barn',
          value: barnName,
          enabled: options != null,
        ),
        if (_expandedField == 'barn' && options != null)
          _searchableOptions(
            field: 'barn',
            allLabel: 'All Barns',
            selectedId: _draft.barnId,
            items: selectableBarns
                .map((barn) => (id: barn.id, name: barn.name))
                .toList(),
            onSelect: (id) => _finishSelection(_draft.copyWith(barnId: id)),
          ),
      ],
    );
  }

  Widget _selector({
    required String field,
    required String label,
    required String value,
    required bool enabled,
  }) => _FilterField(
    label: label,
    child: InkWell(
      key: ValueKey('dashboard-filter-$field'),
      borderRadius: BorderRadius.circular(11),
      onTap: enabled ? () => _toggleField(field) : null,
      child: InputDecorator(
        decoration: InputDecoration(
          enabled: enabled,
          contentPadding: const EdgeInsets.fromLTRB(12, 14, 10, 14),
        ),
        child: Row(
          children: [
            Expanded(
              child: Text(
                value,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: enabled ? AppColors.ink : AppColors.muted,
                  fontSize: 14,
                ),
              ),
            ),
            Icon(
              _expandedField == field
                  ? Icons.keyboard_arrow_up_rounded
                  : Icons.keyboard_arrow_down_rounded,
              color: AppColors.muted,
              size: 22,
            ),
          ],
        ),
      ),
    ),
  );

  Widget _searchableOptions({
    required String field,
    required String allLabel,
    required String? selectedId,
    required List<({String id, String name})> items,
    required ValueChanged<String?> onSelect,
  }) {
    final query = _searchController.text.trim().toLowerCase();
    final matches = items
        .where((item) => item.name.toLowerCase().contains(query))
        .toList();
    final showAll = query.isEmpty || allLabel.toLowerCase().contains(query);
    return Container(
      margin: const EdgeInsets.only(top: 8),
      decoration: BoxDecoration(
        border: Border.all(color: const Color(0xFFDDE7E7)),
        borderRadius: BorderRadius.circular(11),
      ),
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(8),
            child: TextField(
              key: ValueKey('dashboard-filter-$field-search'),
              controller: _searchController,
              onChanged: (_) => setState(() {}),
              decoration: InputDecoration(
                hintText: field == 'farm' ? 'Search farms' : 'Search barns',
                prefixIcon: const Icon(Icons.search_rounded, size: 20),
                isDense: true,
                contentPadding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 12,
                ),
              ),
            ),
          ),
          ConstrainedBox(
            constraints: const BoxConstraints(maxHeight: 180),
            child: ListView(
              shrinkWrap: true,
              primary: false,
              padding: const EdgeInsets.only(bottom: 6),
              children: [
                if (showAll)
                  _optionTile(
                    label: allLabel,
                    selected: selectedId == null,
                    onTap: () => onSelect(null),
                  ),
                for (final item in matches)
                  _optionTile(
                    label: item.name,
                    selected: item.id == selectedId,
                    onTap: () => onSelect(item.id),
                  ),
                if (!showAll && matches.isEmpty)
                  const Padding(
                    padding: EdgeInsets.all(14),
                    child: Text(
                      'No matching results',
                      style: TextStyle(color: AppColors.muted),
                    ),
                  ),
                if (query.isEmpty && items.isEmpty)
                  Padding(
                    padding: const EdgeInsets.all(14),
                    child: Text(
                      field == 'farm'
                          ? 'No farms available'
                          : 'No barns available for this farm',
                      style: const TextStyle(color: AppColors.muted),
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _optionTile({
    required String label,
    required bool selected,
    required VoidCallback onTap,
  }) => ListTile(
    dense: true,
    selected: selected,
    selectedTileColor: AppColors.mint,
    contentPadding: const EdgeInsets.symmetric(horizontal: 12),
    title: Text(label, style: const TextStyle(fontSize: 14)),
    trailing: selected
        ? const Icon(Icons.check_rounded, size: 19, color: AppColors.tealDark)
        : null,
    onTap: onTap,
  );

  Widget _dateField({required bool start}) => _FilterField(
    label: start ? 'Start Date' : 'End Date',
    child: InkWell(
      key: ValueKey('dashboard-filter-${start ? 'start' : 'end'}-date'),
      borderRadius: BorderRadius.circular(11),
      onTap: () => _pickDate(start: start),
      child: InputDecorator(
        decoration: InputDecoration(
          contentPadding: const EdgeInsets.fromLTRB(12, 14, 10, 14),
          enabledBorder: _dateError != null
              ? OutlineInputBorder(
                  borderRadius: BorderRadius.circular(11),
                  borderSide: const BorderSide(color: AppColors.danger),
                )
              : null,
        ),
        child: Row(
          children: [
            Expanded(
              child: Text(
                DateFormat(
                  'dd MMM yyyy',
                  'en_US',
                ).format(start ? _draft.startDate : _draft.endDate),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(fontSize: 14, color: AppColors.ink),
              ),
            ),
            const Icon(
              Icons.calendar_today_outlined,
              size: 18,
              color: AppColors.muted,
            ),
          ],
        ),
      ),
    ),
  );
}

class _FilterField extends StatelessWidget {
  const _FilterField({required this.label, required this.child});

  final String label;
  final Widget child;

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      Text(
        label,
        style: const TextStyle(
          color: AppColors.ink,
          fontSize: 13,
          fontWeight: FontWeight.w600,
        ),
      ),
      const SizedBox(height: 8),
      child,
    ],
  );
}
