import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/config/app_config.dart';
import '../../core/network/api_client.dart';
import '../../core/theme/app_theme.dart';
import '../auth/auth_controller.dart';
import '../notifications/notifications_screen.dart';
import '../shell/member_header.dart';
import 'assigned_farms_screen.dart';

final assignedFarmsProvider =
    FutureProvider.autoDispose<List<Map<String, dynamic>>>((ref) async {
      final session = ref.watch(authControllerProvider).value;
      if (session == null) return [];
      final raw = await ref
          .read(apiClientProvider)
          .get('/api/MobileApp/Profile', query: {'username': session.username});
      if (raw is! Map) throw StateError('Unable to load assigned farms.');
      final farms = raw['Farms'] ?? raw['farms'];
      if (farms is! List) throw StateError('Unable to load assigned farms.');
      return farms
          .whereType<Map>()
          .map((farm) => Map<String, dynamic>.from(farm))
          .toList();
    });

class AccountScreen extends ConsumerStatefulWidget {
  const AccountScreen({super.key, this.showBackButton = false});

  final bool showBackButton;

  @override
  ConsumerState<AccountScreen> createState() => _AccountScreenState();
}

class _AccountScreenState extends ConsumerState<AccountScreen> {
  bool _signingOut = false;
  DateTime? _lastRefresh;

  /// Reaching the top of a long farm list takes several drags, and each one
  /// that starts at the edge asks RefreshIndicator for another refresh. The
  /// gesture still animates; only the repeat fetches are dropped.
  static const _refreshCooldown = Duration(seconds: 5);

  Future<void> _refreshFarms() async {
    final now = DateTime.now();
    final last = _lastRefresh;
    if (last != null && now.difference(last) < _refreshCooldown) return;
    _lastRefresh = now;
    ref.invalidate(assignedFarmsProvider);
    await ref.read(assignedFarmsProvider.future);
  }

  Future<void> _confirmSignOut() async {
    if (_signingOut) return;
    setState(() => _signingOut = true);
    final navigator = Navigator.of(context, rootNavigator: true);
    final messenger = ScaffoldMessenger.of(context);
    try {
      final confirmed = await showDialog<bool>(
        context: context,
        builder: (dialogContext) => AlertDialog(
          title: const Text('Sign out?'),
          content: const Text(
            'Are you sure you want to sign out of your account?',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext, false),
              child: const Text('Cancel'),
            ),
            FilledButton(
              key: const ValueKey('confirm-sign-out'),
              style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
              onPressed: () => Navigator.pop(dialogContext, true),
              child: const Text('Sign out'),
            ),
          ],
        ),
      );
      if (confirmed != true || !mounted) return;
      await ref.read(authControllerProvider.notifier).logout();
      // Account can also be opened from the profile header on a pushed page.
      // Remove those routes so the root auth gate exposes Login immediately.
      if (navigator.mounted) navigator.popUntil((route) => route.isFirst);
    } catch (_) {
      if (messenger.mounted) {
        messenger.showSnackBar(
          const SnackBar(
            content: Text('Unable to sign out. Please try again.'),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _signingOut = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final session = ref.watch(authControllerProvider).value;
    final farms = ref.watch(assignedFarmsProvider);
    return Scaffold(
      body: Column(
        children: [
          MemberHeader(
            title: 'Account',
            subtitle: 'Profile & preferences',
            showBackButton: widget.showBackButton,
            showProfile: false,
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _refreshFarms,
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.all(16),
                children: [
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(18),
                      child: Row(
                        children: [
                          const CircleAvatar(
                            radius: 34,
                            backgroundColor: AppColors.mint,
                            child: Icon(
                              Icons.person,
                              size: 36,
                              color: AppColors.deepGreen,
                            ),
                          ),
                          const SizedBox(width: 14),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  session?.fullName ?? '-',
                                  style: const TextStyle(
                                    fontSize: 19,
                                    fontWeight: FontWeight.w800,
                                  ),
                                ),
                                Text(
                                  session?.username ?? '-',
                                  style: const TextStyle(
                                    color: AppColors.muted,
                                  ),
                                ),
                                Text(
                                  session?.role ?? '',
                                  style: const TextStyle(
                                    color: AppColors.teal,
                                    fontSize: 12,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 10),
                  Card(
                    child: Column(
                      children: [
                        ListTile(
                          leading: const Icon(Icons.notifications_outlined),
                          title: const Text('Notifications'),
                          trailing: const Icon(Icons.chevron_right),
                          onTap: () => Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => const NotificationsScreen(),
                            ),
                          ),
                        ),
                        const Divider(height: 1),
                        const ListTile(
                          leading: Icon(Icons.info_outline),
                          title: Text('About SmartSheep'),
                          subtitle: Text('Mobile 1.0.0'),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 10),
                  // A row like Notifications: the farms open on their own page
                  // because an account can hold dozens of them.
                  Card(
                    child: ListTile(
                      key: const ValueKey('account-assigned-farms'),
                      leading: const CircleAvatar(
                        backgroundColor: AppColors.mint,
                        child: Icon(
                          Icons.agriculture_outlined,
                          color: AppColors.deepGreen,
                        ),
                      ),
                      title: Text(
                        'Assigned Farms${farms.hasValue ? ' (${farms.value!.length})' : ''}',
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      subtitle: const Text('Your farm access'),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () => Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (_) => const AssignedFarmsScreen(),
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 14),
                  Text(
                    'API: ${AppConfig.apiBaseUrl}',
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                      fontSize: 11,
                      color: AppColors.muted,
                    ),
                  ),
                  const SizedBox(height: 14),
                  OutlinedButton.icon(
                    key: const ValueKey('account-sign-out'),
                    onPressed: _signingOut ? null : _confirmSignOut,
                    icon: const Icon(Icons.logout),
                    label: Text(_signingOut ? 'Signing out…' : 'Sign out'),
                    style: OutlinedButton.styleFrom(
                      foregroundColor: AppColors.danger,
                      minimumSize: const Size.fromHeight(48),
                    ),
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
