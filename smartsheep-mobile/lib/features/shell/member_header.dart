import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/widgets/app_header.dart';
import '../account/account_screen.dart';
import '../auth/auth_controller.dart';
import '../notifications/notifications_screen.dart';
import 'header_identity.dart';

/// The shared signed-in header, with real account and notification state.
class MemberHeader extends ConsumerWidget {
  const MemberHeader({
    super.key,
    required this.title,
    this.subtitle,
    this.greeting,
    this.expanded = false,
    this.showBackButton = false,
    this.includeTopInset = true,
    this.showNotifications = true,
    this.showProfile = true,
    this.actions = const [],
    this.onProfile,
    this.footer,
  });

  final String title;
  final String? subtitle;
  final String? greeting;
  final bool expanded;
  final bool showBackButton;
  final bool includeTopInset;
  final bool showNotifications;
  final bool showProfile;
  final List<Widget> actions;
  final VoidCallback? onProfile;
  final Widget? footer;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(authControllerProvider).asData?.value;
    final profileImage = showProfile
        ? ref.watch(headerProfileImageProvider).asData?.value
        : null;
    final notificationCount = showNotifications
        ? ref.watch(unreadNotificationCountProvider)
        : 0;

    return AppHeader(
      title: title,
      subtitle: subtitle,
      greeting: greeting,
      expanded: expanded,
      showBackButton: showBackButton,
      includeTopInset: includeTopInset,
      profileName: showProfile ? session?.fullName : null,
      profileImage: profileImage,
      onProfile: showProfile
          ? onProfile ??
                () => Navigator.of(context).push(
                  MaterialPageRoute<void>(
                    builder: (_) => const AccountScreen(showBackButton: true),
                  ),
                )
          : null,
      actions: [
        if (showNotifications)
          NotificationAction(
            count: notificationCount,
            onPressed: () => Navigator.of(context).push(
              MaterialPageRoute<void>(
                builder: (_) => const NotificationsScreen(),
              ),
            ),
          ),
        ...actions,
      ],
      footer: footer,
    );
  }
}
