import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../theme/app_theme.dart';

/// Shared brand header. Page data and navigation are supplied by the caller.
class AppHeader extends StatelessWidget {
  const AppHeader({
    super.key,
    required this.title,
    this.subtitle,
    this.greeting,
    this.expanded = false,
    this.showBackButton = false,
    this.includeTopInset = true,
    this.actions = const [],
    this.onProfile,
    this.profileName,
    this.profileImage,
    this.footer,
  });

  final String title;
  final String? subtitle;
  final String? greeting;
  final bool expanded;
  final bool showBackButton;
  final bool includeTopInset;
  final List<Widget> actions;
  final VoidCallback? onProfile;
  final String? profileName;
  final ImageProvider? profileImage;
  final Widget? footer;

  static const gradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0xFF00575A), Color(0xFF003E41), Color(0xFF003234)],
    stops: [0, .62, 1],
  );

  @override
  Widget build(BuildContext context) => AnnotatedRegion<SystemUiOverlayStyle>(
    value: SystemUiOverlayStyle.light.copyWith(
      statusBarColor: Colors.transparent,
    ),
    child: Container(
      key: const ValueKey('app-header-surface'),
      width: double.infinity,
      padding: EdgeInsets.fromLTRB(
        16,
        (includeTopInset ? MediaQuery.paddingOf(context).top : 0) + 12,
        16,
        expanded ? 22 : 14,
      ),
      decoration: BoxDecoration(
        gradient: gradient,
        borderRadius: expanded
            ? const BorderRadius.vertical(bottom: Radius.circular(22))
            : null,
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final largeText = MediaQuery.textScalerOf(context).scale(18) > 25;
          final stackedActions =
              (largeText && constraints.maxWidth < 440) ||
              (actions.length > 2 && constraints.maxWidth < 360);
          final trailing = <Widget>[
            ...actions,
            if (onProfile != null || profileName != null) ...[
              const SizedBox(width: 8),
              HeaderProfileAvatar(
                name: profileName ?? title,
                image: profileImage,
                onPressed: onProfile,
                size: expanded ? 48 : 42,
              ),
            ],
          ];
          return Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  if (showBackButton) ...[
                    SizedBox(
                      width: 32,
                      height: 44,
                      child: IconButton(
                        padding: EdgeInsets.zero,
                        tooltip: MaterialLocalizations.of(
                          context,
                        ).backButtonTooltip,
                        color: Colors.white,
                        icon: const Icon(Icons.chevron_left_rounded, size: 30),
                        onPressed: () => Navigator.maybePop(context),
                      ),
                    ),
                    const SizedBox(width: 6),
                  ],
                  Expanded(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        if (greeting != null) ...[
                          Text(
                            greeting!,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              fontSize: 12,
                              color: Color(0xFFD7EBEA),
                              height: 1.4,
                            ),
                          ),
                          const SizedBox(height: 1),
                        ],
                        Semantics(
                          header: true,
                          child: Text(
                            title,
                            key: const ValueKey('app-header-title'),
                            maxLines: largeText ? 2 : 1,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(
                              color: Colors.white,
                              fontSize: expanded ? 22 : 19,
                              fontWeight: FontWeight.w600,
                              height: 1.2,
                              letterSpacing: -.25,
                            ),
                          ),
                        ),
                        if (!expanded && subtitle != null) ...[
                          const SizedBox(height: 3),
                          _HeaderSubtitle(subtitle!),
                        ],
                      ],
                    ),
                  ),
                  if (!stackedActions && trailing.isNotEmpty) ...[
                    const SizedBox(width: 8),
                    ...trailing,
                  ],
                ],
              ),
              if (expanded && subtitle != null) ...[
                const SizedBox(height: 12),
                _HeaderSubtitle(subtitle!),
              ],
              if (stackedActions && trailing.isNotEmpty) ...[
                const SizedBox(height: 8),
                Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: trailing,
                ),
              ],
              if (footer != null) ...[const SizedBox(height: 16), footer!],
            ],
          );
        },
      ),
    ),
  );
}

class _HeaderSubtitle extends StatelessWidget {
  const _HeaderSubtitle(this.text);
  final String text;

  @override
  Widget build(BuildContext context) => Text(
    text,
    maxLines: 2,
    overflow: TextOverflow.ellipsis,
    style: const TextStyle(color: Color(0xFFD7EBEA), fontSize: 12, height: 1.3),
  );
}

class HeaderProfileAvatar extends StatelessWidget {
  const HeaderProfileAvatar({
    super.key,
    required this.name,
    this.image,
    this.onPressed,
    this.size = 42,
  });

  final String name;
  final ImageProvider? image;
  final VoidCallback? onPressed;
  final double size;

  String get _initials {
    final words = name
        .trim()
        .split(RegExp(r'\s+'))
        .where((word) => word.isNotEmpty);
    if (words.isEmpty) return '?';
    return words
        .take(2)
        .map((word) => word.characters.first)
        .join()
        .toUpperCase();
  }

  @override
  Widget build(BuildContext context) => Semantics(
    button: onPressed != null,
    label: 'Profile: $name',
    child: Tooltip(
      message: 'Open profile',
      child: Material(
        color: Colors.transparent,
        shape: const CircleBorder(),
        child: InkWell(
          key: const ValueKey('app-header-profile'),
          customBorder: const CircleBorder(),
          onTap: onPressed,
          child: Padding(
            padding: const EdgeInsets.all(2),
            child: Container(
              width: size,
              height: size,
              padding: const EdgeInsets.all(1.5),
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                border: Border.all(color: const Color(0xFF87B7AE), width: 1.5),
              ),
              child: ClipOval(
                child: ColoredBox(
                  color: const Color(0xFFD7EBE5),
                  child: image == null
                      ? _fallback()
                      : Image(
                          image: image!,
                          fit: BoxFit.cover,
                          excludeFromSemantics: true,
                          errorBuilder: (_, _, _) => _fallback(),
                        ),
                ),
              ),
            ),
          ),
        ),
      ),
    ),
  );

  Widget _fallback() => Center(
    child: Text(
      _initials,
      textScaler: TextScaler.noScaling,
      style: TextStyle(
        color: AppColors.deepGreen,
        fontSize: size * .34,
        fontWeight: FontWeight.w700,
      ),
    ),
  );
}

class NotificationAction extends StatelessWidget {
  const NotificationAction({
    super.key,
    required this.onPressed,
    this.count = 0,
  });

  final VoidCallback onPressed;
  final int count;

  @override
  Widget build(BuildContext context) => IconButton(
    tooltip: count > 0 ? 'Notifications ($count unread)' : 'Notifications',
    onPressed: onPressed,
    color: Colors.white,
    padding: const EdgeInsets.all(10),
    constraints: const BoxConstraints(minWidth: 44, minHeight: 44),
    icon: Badge(
      isLabelVisible: count > 0,
      backgroundColor: const Color(0xFFED263C),
      textColor: Colors.white,
      label: Text(count > 99 ? '99+' : '$count'),
      child: const Icon(Icons.notifications_none_rounded, size: 25),
    ),
  );
}
