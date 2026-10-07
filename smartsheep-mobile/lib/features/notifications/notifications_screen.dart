import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../core/network/api_client.dart';
import '../../core/text/html_text.dart';
import '../../core/theme/app_theme.dart';
import '../auth/auth_controller.dart';
import '../shell/member_header.dart';

final notificationsProvider = FutureProvider<List<dynamic>>((ref) async {
  final session = ref.watch(authControllerProvider).value;
  if (session == null) return const [];
  final raw = await ref
      .read(apiClientProvider)
      .get('/api/Auth/SystemNotification/${session.username}');
  return raw is List ? raw : const [];
});

class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    body: Column(
      children: [
        const MemberHeader(
          title: 'Notifications',
          subtitle: 'Updates for your account',
          showBackButton: true,
          showNotifications: false,
        ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: () => ref.refresh(notificationsProvider.future),
            child: ref
                .watch(notificationsProvider)
                .when(
                  loading: () => const _LoadingList(),
                  error: (_, _) => _NotificationStatus(
                    message: 'Unable to load notifications.',
                    onRetry: () => ref.invalidate(notificationsProvider),
                  ),
                  data: (items) => _NotificationList(items: items),
                ),
          ),
        ),
      ],
    ),
  );
}

class _NotificationList extends ConsumerStatefulWidget {
  const _NotificationList({required this.items});
  final List<dynamic> items;

  @override
  ConsumerState<_NotificationList> createState() => _NotificationListState();
}

class _NotificationListState extends ConsumerState<_NotificationList> {
  final _reading = <String>{};

  Future<void> _markRead(String id) async {
    if (_reading.contains(id)) return;
    setState(() => _reading.add(id));
    try {
      final result = await ref
          .read(apiClientProvider)
          .get('/api/Auth/ReadNotification/${Uri.encodeComponent(id)}');
      if (result is! Map || (result['IsRead'] ?? result['isRead']) != true) {
        throw StateError('Read status was not confirmed.');
      }
      ref.invalidate(notificationsProvider);
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
              'Unable to mark notification as read. Please try again.',
            ),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _reading.remove(id));
    }
  }

  @override
  Widget build(BuildContext context) {
    final rows = widget.items.whereType<Map>().toList();
    if (rows.isEmpty) {
      return const _NotificationStatus(message: 'No notifications yet.');
    }
    return ListView.separated(
      key: const ValueKey('notifications-list'),
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(14),
      itemCount: rows.length,
      separatorBuilder: (_, _) => const SizedBox(height: 8),
      itemBuilder: (context, index) {
        final item = rows[index];
        final id = (item['Id'] ?? item['id'])?.toString();
        final readFlag = item.containsKey('IsRead')
            ? item['IsRead']
            : item['isRead'];
        final read = readFlag == true;
        final unread = readFlag == false;
        final reading = _reading.contains(id);
        final status = read
            ? 'Read'
            : unread
            ? 'Unread'
            : 'Status unavailable';
        final color = read ? AppColors.success : AppColors.tealDark;
        final created =
            item['DateCreated'] ??
            item['dateCreated'] ??
            item['CreatedDate'] ??
            item['createdDate'] ??
            item['DateModified'] ??
            item['dateModified'];
        return Card(
          key: ValueKey('notification-${id ?? index}'),
          color: unread ? AppColors.mint.withValues(alpha: .35) : Colors.white,
          child: InkWell(
            borderRadius: BorderRadius.circular(14),
            onTap: id == null || !unread || reading
                ? null
                : () => _markRead(id),
            child: Padding(
              padding: const EdgeInsets.all(14),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  CircleAvatar(
                    backgroundColor: color.withValues(alpha: .12),
                    child: reading
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : Icon(
                            read
                                ? Icons.mark_email_read_outlined
                                : Icons.mark_email_unread_outlined,
                            color: color,
                          ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          '${item['Title'] ?? item['title'] ?? 'Notification'}',
                          style: TextStyle(
                            fontWeight: read
                                ? FontWeight.w600
                                : FontWeight.w700,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text.rich(
                          // Bodies arrive as HTML, same as the popup, so the
                          // list shows the text rather than the markup.
                          htmlToSpan(
                            '${item['Content'] ?? item['content'] ?? item['Message'] ?? item['message'] ?? ''}',
                          ),
                          maxLines: 3,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            fontSize: 13,
                            color: AppColors.muted,
                          ),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          _time(created),
                          key: ValueKey('notification-date-${id ?? index}'),
                          style: const TextStyle(
                            fontSize: 12,
                            color: AppColors.ink,
                          ),
                        ),
                        const SizedBox(height: 6),
                        Text(
                          reading ? 'Marking as read…' : status,
                          key: ValueKey('notification-status-${id ?? index}'),
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w700,
                            color: color,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }

  String _time(dynamic value) {
    final date = DateTime.tryParse('$value');
    return date == null
        ? 'Date unavailable'
        : DateFormat('dd MMM yyyy HH:mm', 'en_US').format(date.toLocal());
  }
}

class _NotificationStatus extends StatelessWidget {
  const _NotificationStatus({required this.message, this.onRetry});
  final String message;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) => ListView(
    physics: const AlwaysScrollableScrollPhysics(),
    padding: const EdgeInsets.all(24),
    children: [
      const SizedBox(height: 80),
      const Icon(Icons.notifications_none, size: 40, color: AppColors.muted),
      const SizedBox(height: 12),
      Text(message, textAlign: TextAlign.center),
      if (onRetry != null)
        Center(
          child: TextButton(onPressed: onRetry, child: const Text('Retry')),
        ),
    ],
  );
}

class _LoadingList extends StatelessWidget {
  const _LoadingList();
  @override
  Widget build(BuildContext context) => ListView(
    physics: const AlwaysScrollableScrollPhysics(),
    children: const [
      SizedBox(height: 240),
      Center(child: CircularProgressIndicator()),
    ],
  );
}
