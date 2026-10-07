import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/database_menu_icon.dart';
import '../auth/auth_controller.dart';
import '../breeding/sheep_breeding_screen.dart';
import '../dashboard/dashboard_screen.dart';
import '../favorites/favorite_menu.dart';
import '../favorites/favorites_controller.dart';
import '../favorites/favorites_screen.dart';
import '../news/news_data.dart';
import '../news/news_feed.dart';
import '../news/news_screen.dart';
import '../reminders/reminders_screen.dart';
import '../shell/member_header.dart';
import '../sources/sources_screen.dart';
import '../statistics/statistics_screen.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key, this.onSelectTab});

  final ValueChanged<int>? onSelectTab;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(authControllerProvider).value;
    return Scaffold(
      body: Column(
        children: [
          MemberHeader(
            title: session?.fullName ?? 'User',
            greeting: _greeting(),
            subtitle: session?.role ?? 'User',
            expanded: true,
            onProfile: onSelectTab == null ? null : () => onSelectTab!(3),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: () async {
                await Future.wait([
                  ref.refresh(newsItemsProvider.future),
                  ref.refresh(favoriteMenuCatalogProvider.future),
                ]);
              },
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.all(16),
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Expanded(
                        child: Text(
                          'Favorites',
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ),
                      TextButton(
                        onPressed: () => _editFavorites(context, ref),
                        child: const Text('Edit'),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  ref
                      .watch(favoriteMenuCatalogProvider)
                      .when(
                        loading: () => const _FavoritesLoading(),
                        error: (_, _) => _FavoritesLoadError(
                          onRetry: () =>
                              ref.invalidate(favoriteMenuCatalogProvider),
                        ),
                        data: (catalog) => catalog.selected.isEmpty
                            ? _EmptyFavorites(
                                onTap: () => _editFavorites(context, ref),
                              )
                            : SizedBox(
                                height: 92,
                                child: ListView.separated(
                                  scrollDirection: Axis.horizontal,
                                  itemCount: catalog.selected.length,
                                  separatorBuilder: (_, _) =>
                                      const SizedBox(width: 8),
                                  itemBuilder: (context, index) {
                                    final menu = catalog.selected[index];
                                    return _Shortcut(
                                      menu: menu,
                                      label: menu.label,
                                      onTap: () => _openMenu(context, menu),
                                    );
                                  },
                                ),
                              ),
                      ),
                  const SizedBox(height: 22),
                  const Text(
                    'Latest News',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
                  ),
                  const SizedBox(height: 8),
                  const NewsFeed(),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  String _greeting() {
    final hour = DateTime.now().hour;
    if (hour < 12) return 'Good morning,';
    if (hour < 18) return 'Good afternoon,';
    return 'Good evening,';
  }

  Future<void> _editFavorites(BuildContext context, WidgetRef ref) async {
    ref.invalidate(favoriteMenuCatalogProvider);
    final changed = await Navigator.push<bool>(
      context,
      MaterialPageRoute(builder: (_) => const FavoritesScreen()),
    );
    if (changed == true) ref.invalidate(favoriteMenuCatalogProvider);
  }

  void _openMenu(BuildContext context, MobileFavoriteMenu menu) {
    final controller = menu.controller.toLowerCase();
    if (controller == 'sheepbreeding') {
      Navigator.push(
        context,
        MaterialPageRoute(builder: (_) => const SheepBreedingScreen()),
      );
      return;
    }
    if (const {
      'sucklingactivity',
      'sucklingactivityreport',
      'sucklingstatistic',
      'sucklingstatisticreport',
    }.contains(controller)) {
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (_) => StatisticsScreen(
            view: controller.contains('statistic')
                ? SucklingReportView.statistics
                : SucklingReportView.activities,
          ),
        ),
      );
      return;
    }
    switch (menu.type) {
      case FavoriteMenuType.home:
        onSelectTab?.call(0);
      case FavoriteMenuType.liveStreams:
        if (onSelectTab != null) {
          onSelectTab!(2);
        } else {
          Navigator.push(
            context,
            MaterialPageRoute(builder: (_) => const SourcesScreen()),
          );
        }
      case FavoriteMenuType.streamers:
      case FavoriteMenuType.dashboard:
        if (onSelectTab != null) {
          onSelectTab!(1);
        } else {
          Navigator.push(
            context,
            MaterialPageRoute(builder: (_) => const DashboardScreen()),
          );
        }
      case FavoriteMenuType.reminders:
        Navigator.push(
          context,
          MaterialPageRoute(
            builder: (_) => RemindersScreen(
              view: switch (menu.controller.toLowerCase()) {
                'reminderlog' => ReminderView.logs,
                'reminderreport' => ReminderView.report,
                _ => ReminderView.templates,
              },
            ),
          ),
        );
      case FavoriteMenuType.reports:
      case FavoriteMenuType.statistics:
        Navigator.push(
          context,
          MaterialPageRoute(builder: (_) => const StatisticsScreen()),
        );
      case FavoriteMenuType.news:
        Navigator.push(
          context,
          MaterialPageRoute(builder: (_) => const NewsScreen()),
        );
      case FavoriteMenuType.farms:
      case FavoriteMenuType.barns:
      case FavoriteMenuType.livestock:
      case FavoriteMenuType.other:
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('${menu.label} is not available on mobile yet.'),
          ),
        );
    }
  }
}

class _FavoritesLoading extends StatelessWidget {
  const _FavoritesLoading();

  @override
  Widget build(BuildContext context) => const SizedBox(
    height: 92,
    child: Center(child: CircularProgressIndicator(strokeWidth: 2)),
  );
}

class _EmptyFavorites extends StatelessWidget {
  const _EmptyFavorites({required this.onTap});

  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Card(
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(14),
      child: const Padding(
        padding: EdgeInsets.symmetric(horizontal: 16, vertical: 15),
        child: Row(
          children: [
            CircleAvatar(
              backgroundColor: AppColors.mint,
              child: Icon(Icons.add_rounded, color: AppColors.deepGreen),
            ),
            SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Add favorite menus',
                    style: TextStyle(fontWeight: FontWeight.w700),
                  ),
                  SizedBox(height: 2),
                  Text(
                    'Choose shortcuts for faster access from Home.',
                    style: TextStyle(fontSize: 12, color: AppColors.muted),
                  ),
                ],
              ),
            ),
            Icon(Icons.chevron_right_rounded, color: AppColors.muted),
          ],
        ),
      ),
    ),
  );
}

class _FavoritesLoadError extends StatelessWidget {
  const _FavoritesLoadError({required this.onRetry});

  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => Card(
    child: ListTile(
      leading: const Icon(Icons.cloud_off_outlined, color: AppColors.muted),
      title: const Text('Favorites could not be loaded.'),
      trailing: TextButton(onPressed: onRetry, child: const Text('Retry')),
    ),
  );
}

class _Shortcut extends StatelessWidget {
  const _Shortcut({
    required this.menu,
    required this.label,
    required this.onTap,
  });
  final MobileFavoriteMenu menu;
  final String label;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(12),
    child: SizedBox(
      width: 78,
      child: Column(
        children: [
          Container(
            width: 54,
            height: 54,
            decoration: BoxDecoration(
              color: AppColors.mint,
              borderRadius: BorderRadius.circular(14),
            ),
            child: Center(
              child: DatabaseMenuIcon(
                key: ValueKey('home-menu-icon-${menu.id}'),
                icon: menu.iconName,
                activeIcon: menu.iconActiveName,
                active: true,
                color: AppColors.deepGreen,
              ),
            ),
          ),
          const SizedBox(height: 5),
          SizedBox(
            height: 28,
            child: Text(
              label,
              textAlign: TextAlign.center,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                fontSize: 11,
                height: 1.1,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    ),
  );
}
