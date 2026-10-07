import 'dart:async';
import 'dart:collection';

import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../core/text/html_text.dart';
import '../../core/theme/app_theme.dart';

/// Serializes foreground messages so a burst never stacks modal routes.
class NotificationPopupPresenter {
  NotificationPopupPresenter(this.navigatorKey);

  final GlobalKey<NavigatorState> navigatorKey;
  final Queue<RemoteMessage> _pending = Queue();
  final Queue<String> _recentIds = Queue();
  bool _presenting = false;

  void receive(RemoteMessage message) {
    final title =
        message.notification?.title?.trim() ??
        message.data['title']?.toString().trim() ??
        '';
    final body =
        message.notification?.body?.trim() ??
        message.data['body']?.toString().trim() ??
        '';
    if (title.isEmpty && body.isEmpty) return;
    final id = message.messageId;
    if (id != null && _recentIds.contains(id)) return;
    if (id != null) {
      _recentIds.add(id);
      if (_recentIds.length > 50) _recentIds.removeFirst();
    }
    _pending.add(message);
    if (!_presenting) unawaited(_showNext());
  }

  Future<void> _showNext() async {
    _presenting = true;
    // A push can arrive while MaterialApp is building its first frame.
    await WidgetsBinding.instance.endOfFrame;
    try {
      while (_pending.isNotEmpty) {
        final context = navigatorKey.currentContext;
        if (context == null || !context.mounted) return;
        final message = _pending.removeFirst();
        await showDialog<void>(
          context: context,
          // Tapping the scrim closes the popup, so it is never a dead end.
          barrierDismissible: true,
          barrierColor: AppColors.ink.withValues(alpha: .45),
          builder: (_) => NotificationPopup(
            title:
                message.notification?.title ??
                message.data['title']?.toString() ??
                '',
            // The tray gets plain text; the original markup rides along in
            // body_html so the popup keeps its bold labels and line breaks.
            body:
                message.data['body_html']?.toString() ??
                message.notification?.body ??
                message.data['body']?.toString() ??
                '',
            isReminder:
                message.data['type']?.toString().startsWith('reminder') ??
                false,
            receivedAt: message.sentTime ?? DateTime.now(),
          ),
        );
      }
    } finally {
      _presenting = false;
    }
  }
}

class NotificationPopup extends StatelessWidget {
  const NotificationPopup({
    required this.title,
    required this.body,
    required this.receivedAt,
    this.isReminder = false,
    super.key,
  });

  final String title;
  final String body;
  final DateTime receivedAt;
  final bool isReminder;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final label = isReminder ? 'Reminder' : 'Notification';
    return Dialog(
      key: const ValueKey('foreground-notification-popup'),
      insetPadding: const EdgeInsets.symmetric(horizontal: 24, vertical: 24),
      backgroundColor: theme.colorScheme.surface,
      surfaceTintColor: Colors.transparent,
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxWidth: 400,
          maxHeight: MediaQuery.sizeOf(context).height * .8,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: double.infinity,
              padding: const EdgeInsets.fromLTRB(20, 18, 12, 18),
              decoration: const BoxDecoration(
                gradient: LinearGradient(
                  colors: [AppColors.reminderDark, AppColors.reminder],
                ),
              ),
              child: Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: .15),
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(
                        color: Colors.white.withValues(alpha: .2),
                      ),
                    ),
                    child: Icon(
                      isReminder
                          ? Icons.notifications_active_outlined
                          : Icons.notifications_outlined,
                      color: Colors.white,
                      size: 26,
                    ),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Text(
                      label,
                      style: theme.textTheme.titleMedium?.copyWith(
                        color: Colors.white,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  IconButton(
                    tooltip: 'Close notification',
                    onPressed: () => Navigator.of(context).pop(),
                    icon: const Icon(
                      Icons.close_rounded,
                      size: 24,
                      color: Colors.white,
                    ),
                  ),
                ],
              ),
            ),
            Flexible(
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(24, 24, 24, 8),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Semantics(
                      header: true,
                      child: Text(
                        title.trim().isEmpty ? label : title.trim(),
                        style: theme.textTheme.titleLarge?.copyWith(
                          color: AppColors.ink,
                          fontWeight: FontWeight.w700,
                          height: 1.3,
                        ),
                      ),
                    ),
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        const Icon(
                          Icons.schedule_rounded,
                          size: 16,
                          color: AppColors.reminderDark,
                        ),
                        const SizedBox(width: 6),
                        Expanded(
                          child: Text(
                            DateFormat(
                              'dd MMM yyyy HH:mm',
                            ).format(receivedAt.toLocal()),
                            style: theme.textTheme.bodySmall?.copyWith(
                              color: AppColors.muted,
                            ),
                          ),
                        ),
                      ],
                    ),
                    if (body.trim().isNotEmpty) ...[
                      const SizedBox(height: 20),
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: AppColors.reminder,
                          borderRadius: BorderRadius.circular(14),
                        ),
                        child: Text.rich(
                          htmlToSpan(body),
                          style: theme.textTheme.bodyMedium?.copyWith(
                            color: Colors.white,
                            height: 1.6,
                          ),
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(24, 16, 24, 24),
              child: SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: () => Navigator.of(context).pop(),
                  icon: const Icon(Icons.check_rounded, size: 20),
                  label: const Text('Got it'),
                  style: FilledButton.styleFrom(
                    backgroundColor: AppColors.reminder,
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(
                      horizontal: 20,
                      vertical: 14,
                    ),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(14),
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
