import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../shell/member_header.dart';
import 'sources_data.dart';
import 'source_video_detail_screen.dart';

export 'sources_data.dart' show sourcesProvider;

class SourcesScreen extends ConsumerStatefulWidget {
  const SourcesScreen({super.key});
  @override
  ConsumerState<SourcesScreen> createState() => _SourcesScreenState();
}

class _SourcesScreenState extends ConsumerState<SourcesScreen> {
  SourceAvailability _filter = SourceAvailability.all;

  Future<void> _refresh() async {
    try {
      ref.invalidate(sourcesProvider);
      await ref.read(sourcesProvider.future);
    } catch (_) {
      // The provider renders the failure and Retry action on this page.
    }
  }

  @override
  Widget build(BuildContext context) {
    final value = ref.watch(sourcesProvider);
    final data = value.asData?.value;
    return Scaffold(
      body: Column(
        children: [
          const MemberHeader(
            title: 'Sources',
            subtitle: 'Barns grouped by farm',
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(14, 14, 14, 8),
            child: Row(
              children: [
                for (final status in SourceAvailability.values) ...[
                  if (status != SourceAvailability.all)
                    const SizedBox(width: 8),
                  Expanded(
                    child: _AvailabilityFilter(
                      status: status,
                      count: data == null
                          ? null
                          : switch (status) {
                              SourceAvailability.all => data.totalSources,
                              SourceAvailability.online => data.onlineCount,
                              SourceAvailability.offline => data.offlineCount,
                            },
                      selected: status == _filter,
                      onSelected: () => setState(() => _filter = status),
                    ),
                  ),
                ],
              ],
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _refresh,
              child: value.when(
                loading: () => _stateList(
                  const Padding(
                    padding: EdgeInsets.all(48),
                    child: Center(child: CircularProgressIndicator()),
                  ),
                ),
                error: (_, _) => _stateList(
                  _SourcesEmpty(
                    icon: Icons.cloud_off_rounded,
                    title: 'Unable to load sources',
                    message:
                        'Farm, barn, or source data could not be loaded. Please try again.',
                    action: FilledButton.icon(
                      key: const ValueKey('sources-retry'),
                      onPressed: _refresh,
                      icon: const Icon(Icons.refresh),
                      label: const Text('Retry'),
                    ),
                  ),
                ),
                data: _list,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _stateList(Widget child) => ListView(
    physics: const AlwaysScrollableScrollPhysics(),
    padding: const EdgeInsets.all(14),
    children: [child],
  );

  Widget _list(SourcesData data) {
    final filtered = data.filtered(_filter);
    if (filtered.farms.isEmpty) {
      return _stateList(
        _SourcesEmpty(
          icon: _filter == SourceAvailability.offline
              ? Icons.check_circle_outline_rounded
              : Icons.videocam_off_outlined,
          title: switch (_filter) {
            SourceAvailability.all => 'No sources',
            SourceAvailability.online => 'No online sources',
            SourceAvailability.offline => 'No offline sources',
          },
          message: data.totalSources == 0
              ? 'No source videos are configured yet.'
              : _filter == SourceAvailability.offline
              ? 'All configured sources are online.'
              : 'No configured sources are currently online.',
        ),
      );
    }
    return ListView.builder(
      key: const ValueKey('sources-list'),
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(14, 4, 14, 24),
      itemCount: filtered.farms.length + 1,
      itemBuilder: (context, index) {
        if (index == 0) {
          return Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: Text(
              '${filtered.totalFarms} farms · ${filtered.totalBarns} barns · ${filtered.totalSources} sources',
              key: const ValueKey('sources-summary'),
              style: const TextStyle(fontSize: 12, color: AppColors.muted),
            ),
          );
        }
        return Padding(
          padding: const EdgeInsets.only(bottom: 14),
          child: _FarmSourcesCard(farm: filtered.farms[index - 1]),
        );
      },
    );
  }
}

String _availabilityLabel(SourceAvailability status) => switch (status) {
  SourceAvailability.all => 'All',
  SourceAvailability.online => 'Online',
  SourceAvailability.offline => 'Offline',
};

class _AvailabilityFilter extends StatelessWidget {
  const _AvailabilityFilter({
    required this.status,
    required this.count,
    required this.selected,
    required this.onSelected,
  });
  final SourceAvailability status;
  final int? count;
  final bool selected;
  final VoidCallback onSelected;

  @override
  Widget build(BuildContext context) => Semantics(
    button: true,
    selected: selected,
    label: '${_availabilityLabel(status)}, ${count ?? 'loading'} sources',
    child: Material(
      color: selected ? AppColors.deepGreen : Colors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(
          color: selected ? AppColors.deepGreen : const Color(0xFFDCE7E9),
        ),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        key: ValueKey('sources-filter-${status.name}'),
        onTap: onSelected,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 10),
          child: Column(
            children: [
              Text(
                _availabilityLabel(status),
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: selected ? Colors.white : AppColors.muted,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                count?.toString() ?? '—',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.w800,
                  color: selected
                      ? Colors.white
                      : switch (status) {
                          SourceAvailability.all => AppColors.ink,
                          SourceAvailability.online => AppColors.success,
                          SourceAvailability.offline => AppColors.danger,
                        },
                ),
              ),
            ],
          ),
        ),
      ),
    ),
  );
}

class _FarmSourcesCard extends StatelessWidget {
  const _FarmSourcesCard({required this.farm});
  final SourcesFarmGroup farm;

  @override
  Widget build(BuildContext context) => Card(
    key: ValueKey('sources-farm-${farm.id ?? 'unassigned'}'),
    clipBehavior: Clip.antiAlias,
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Container(
          padding: const EdgeInsets.all(14),
          decoration: const BoxDecoration(
            gradient: LinearGradient(colors: [Colors.white, Color(0xFFF1F9F8)]),
            border: Border(bottom: BorderSide(color: Color(0xFFDCE8EA))),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    padding: const EdgeInsets.all(9),
                    decoration: BoxDecoration(
                      gradient: const LinearGradient(
                        colors: [AppColors.teal, AppColors.deepGreen],
                      ),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: const Icon(
                      Icons.agriculture_outlined,
                      color: Colors.white,
                      size: 22,
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          farm.name,
                          style: const TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.w800,
                            color: AppColors.ink,
                          ),
                        ),
                        const SizedBox(height: 3),
                        Text(
                          '${farm.totalBarns} barns · ${farm.totalSources} sources',
                          style: const TextStyle(
                            fontSize: 11,
                            color: AppColors.muted,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Wrap(
                spacing: 8,
                runSpacing: 6,
                children: [
                  _StatusBadge(online: true, count: farm.onlineCount),
                  _StatusBadge(online: false, count: farm.offlineCount),
                ],
              ),
            ],
          ),
        ),
        for (var index = 0; index < farm.barns.length; index++) ...[
          if (index > 0)
            const Divider(
              height: 1,
              indent: 14,
              endIndent: 14,
              color: Color(0xFFE8EEF0),
            ),
          _BarnSources(barn: farm.barns[index], farm: farm),
        ],
      ],
    ),
  );
}

class _BarnSources extends StatelessWidget {
  const _BarnSources({required this.barn, required this.farm});
  final SourcesBarnGroup barn;
  final SourcesFarmGroup farm;

  @override
  Widget build(BuildContext context) => Padding(
    key: ValueKey('sources-barn-${barn.id ?? 'unassigned'}'),
    padding: const EdgeInsets.all(14),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Padding(
              padding: EdgeInsets.only(top: 2),
              child: Icon(
                Icons.home_work_outlined,
                size: 19,
                color: AppColors.tealDark,
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    barn.name,
                    style: const TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.w700,
                      color: AppColors.ink,
                    ),
                  ),
                  if (barn.code?.isNotEmpty == true) ...[
                    const SizedBox(height: 3),
                    Text(
                      barn.code!,
                      style: const TextStyle(
                        fontSize: 11,
                        color: AppColors.muted,
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        if (barn.sources.isEmpty)
          const Text(
            'No sources configured',
            style: TextStyle(fontSize: 12, color: AppColors.muted),
          )
        else
          for (var index = 0; index < barn.sources.length; index++) ...[
            if (index > 0) const SizedBox(height: 8),
            _SourceRow(
              source: barn.sources[index],
              onTap: () => Navigator.of(context).push(
                MaterialPageRoute<void>(
                  builder: (_) => SourceVideoDetailScreen(
                    source: barn.sources[index],
                    farm: farm,
                    barn: barn,
                  ),
                ),
              ),
            ),
          ],
      ],
    ),
  );
}

class _SourceRow extends StatelessWidget {
  const _SourceRow({required this.source, required this.onTap});
  final SourceVideoItem source;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final color = source.isOnline ? AppColors.success : AppColors.danger;
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(9),
        child: Container(
          key: ValueKey('sources-video-${source.id}'),
          padding: const EdgeInsets.all(10),
          decoration: BoxDecoration(
            color: color.withValues(alpha: .04),
            border: Border.all(color: color.withValues(alpha: .2)),
            borderRadius: BorderRadius.circular(9),
          ),
          child: LayoutBuilder(
            builder: (context, constraints) {
              final label = Row(
                children: [
                  Icon(
                    Icons.play_circle_outline_rounded,
                    color: color,
                    size: 20,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      source.code.isEmpty ? 'Unnamed source' : source.code,
                      style: const TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                        color: AppColors.ink,
                      ),
                    ),
                  ),
                ],
              );
              final badge = _StatusBadge(online: source.isOnline);
              if (constraints.maxWidth < 245 ||
                  MediaQuery.textScalerOf(context).scale(12) > 17) {
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [label, const SizedBox(height: 8), badge],
                );
              }
              return Row(
                children: [
                  Expanded(child: label),
                  const SizedBox(width: 8),
                  badge,
                  const SizedBox(width: 4),
                  const Icon(
                    Icons.chevron_right,
                    size: 18,
                    color: AppColors.muted,
                  ),
                ],
              );
            },
          ),
        ),
      ),
    );
  }
}

class _StatusBadge extends StatelessWidget {
  const _StatusBadge({required this.online, this.count});
  final bool online;
  final int? count;

  @override
  Widget build(BuildContext context) {
    final color = online ? const Color(0xFF15803D) : const Color(0xFFB42318);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 5),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .08),
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(
        '${count == null ? '● ' : '$count '}${online ? 'Online' : 'Offline'}',
        style: TextStyle(
          color: color,
          fontSize: 11,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _SourcesEmpty extends StatelessWidget {
  const _SourcesEmpty({
    required this.icon,
    required this.title,
    required this.message,
    this.action,
  });
  final IconData icon;
  final String title;
  final String message;
  final Widget? action;

  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 32),
      child: Column(
        children: [
          Container(
            padding: const EdgeInsets.all(16),
            decoration: const BoxDecoration(
              color: AppColors.mint,
              shape: BoxShape.circle,
            ),
            child: Icon(icon, color: AppColors.tealDark, size: 30),
          ),
          const SizedBox(height: 16),
          Text(
            title,
            textAlign: TextAlign.center,
            style: const TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w700,
              color: AppColors.ink,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            message,
            textAlign: TextAlign.center,
            style: const TextStyle(
              fontSize: 12,
              color: AppColors.muted,
              height: 1.5,
            ),
          ),
          if (action != null) ...[const SizedBox(height: 18), action!],
        ],
      ),
    ),
  );
}
