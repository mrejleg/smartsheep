import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../shell/member_header.dart';
import 'account_screen.dart';

/// The farm list on its own page, opened from the Account row. An account can
/// hold dozens of farms, which is why Account only shows the count.
class AssignedFarmsScreen extends ConsumerStatefulWidget {
  const AssignedFarmsScreen({super.key});

  @override
  ConsumerState<AssignedFarmsScreen> createState() =>
      _AssignedFarmsScreenState();
}

class _AssignedFarmsScreenState extends ConsumerState<AssignedFarmsScreen> {
  DateTime? _lastRefresh;

  /// Reaching the top of a long list takes several drags, and each one that
  /// starts at the edge asks for another refresh. The pull still animates;
  /// only the repeat fetches are dropped.
  static const _refreshCooldown = Duration(seconds: 5);

  Future<void> _refresh() async {
    final now = DateTime.now();
    final last = _lastRefresh;
    if (last != null && now.difference(last) < _refreshCooldown) return;
    _lastRefresh = now;
    ref.invalidate(assignedFarmsProvider);
    await ref.read(assignedFarmsProvider.future);
  }

  @override
  Widget build(BuildContext context) {
    final farms = ref.watch(assignedFarmsProvider);
    return Scaffold(
      body: Column(
        children: [
          MemberHeader(
            title: 'Assigned Farms',
            subtitle: farms.hasValue
                ? '${farms.value!.length} farm access'
                : 'Your farm access',
            showBackButton: true,
            showNotifications: false,
            showProfile: false,
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _refresh,
              child: farms.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (_, _) => ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.all(24),
                  children: [
                    const Text(
                      'Unable to load assigned farms.',
                      textAlign: TextAlign.center,
                    ),
                    TextButton(
                      onPressed: () => ref.invalidate(assignedFarmsProvider),
                      child: const Text('Try again'),
                    ),
                  ],
                ),
                data: (rows) => ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.all(16),
                  children: [
                    if (rows.isEmpty)
                      const Padding(
                        padding: EdgeInsets.all(24),
                        child: Text(
                          'No farms assigned. Contact your administrator.',
                          textAlign: TextAlign.center,
                          style: TextStyle(color: AppColors.muted),
                        ),
                      )
                    else
                      Card(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            for (final farm in rows)
                              ListTile(
                                leading: const Icon(
                                  Icons.location_on_outlined,
                                  color: AppColors.teal,
                                ),
                                title: Text(
                                  '${farm['Name'] ?? farm['name'] ?? '-'}',
                                ),
                                subtitle: Text(
                                  '${farm['Code'] ?? farm['code'] ?? '-'}',
                                ),
                              ),
                          ],
                        ),
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
