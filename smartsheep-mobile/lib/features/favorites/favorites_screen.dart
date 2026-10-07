import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/database_menu_icon.dart';
import '../auth/auth_controller.dart';
import '../shell/member_header.dart';
import 'favorite_menu.dart';
import 'favorites_controller.dart';

class FavoritesScreen extends ConsumerStatefulWidget {
  const FavoritesScreen({super.key});

  @override
  ConsumerState<FavoritesScreen> createState() => _FavoritesScreenState();
}

class _FavoritesScreenState extends ConsumerState<FavoritesScreen> {
  Set<String>? _selectedIds;
  bool _saving = false;

  @override
  Widget build(BuildContext context) {
    final catalogValue = ref.watch(favoriteMenuCatalogProvider);
    final catalog = catalogValue.asData?.value;
    if (_selectedIds == null && catalog != null) {
      _selectedIds = catalog.selected
          .take(maxFavoriteMenus)
          .map((menu) => menu.id)
          .toSet();
    }
    if (catalog != null) {
      final availableIds = catalog.available.map((menu) => menu.id).toSet();
      _selectedIds?.removeWhere((id) => !availableIds.contains(id));
    }

    return Scaffold(
      body: Column(
        children: [
          const MemberHeader(
            title: 'Favorites',
            subtitle: 'Your Home shortcuts',
            showBackButton: true,
          ),
          Expanded(
            child: catalogValue.when(
              skipLoadingOnRefresh: false,
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (_, _) => _LoadError(
                onRetry: () => ref.invalidate(favoriteMenuCatalogProvider),
              ),
              data: (data) => RefreshIndicator(
                onRefresh: () async {
                  final refreshed = await ref.refresh(
                    favoriteMenuCatalogProvider.future,
                  );
                  if (!mounted) return;
                  setState(() {
                    _selectedIds = refreshed.selected
                        .take(maxFavoriteMenus)
                        .map((menu) => menu.id)
                        .toSet();
                  });
                },
                child: _MenuList(
                  catalog: data,
                  selectedIds: _selectedIds ?? const {},
                  onChanged: (menu, selected) {
                    if (selected && _selectedIds!.length >= maxFavoriteMenus) {
                      _showMessage('You can select up to four favorite menus.');
                      return;
                    }
                    setState(() {
                      if (selected) {
                        _selectedIds!.add(menu.id);
                      } else {
                        _selectedIds!.remove(menu.id);
                      }
                    });
                  },
                ),
              ),
            ),
          ),
          SafeArea(
            top: false,
            minimum: const EdgeInsets.fromLTRB(16, 8, 16, 16),
            child: SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                key: const ValueKey('favorites-save'),
                onPressed: catalog == null || _saving
                    ? null
                    : () => _save(catalog),
                icon: _saving
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : const Icon(Icons.save_outlined, size: 20),
                label: const Text('Save'),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _save(FavoriteMenuCatalog catalog) async {
    final session = ref.read(authControllerProvider).value;
    if (session == null || session.email.isEmpty) {
      _showMessage('The account email is unavailable. Please sign in again.');
      return;
    }

    setState(() => _saving = true);
    try {
      final ids = catalog.available
          .where((menu) => _selectedIds!.contains(menu.id))
          .map((menu) => menu.id)
          .take(maxFavoriteMenus)
          .toList();
      await ref
          .read(favoritesRepositoryProvider)
          .save(email: session.email, ids: ids);
      ref.invalidate(favoriteMenuCatalogProvider);
      if (!mounted) return;
      final messenger = ScaffoldMessenger.of(context);
      setState(() => _saving = false);
      Navigator.pop(context, true);
      messenger.showSnackBar(
        const SnackBar(content: Text('Favorites updated.')),
      );
    } catch (_) {
      if (mounted) {
        setState(() => _saving = false);
        _showMessage('Favorites could not be saved. Please try again.');
      }
    }
  }

  void _showMessage(String message) {
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(message)));
  }
}

class _MenuList extends StatelessWidget {
  const _MenuList({
    required this.catalog,
    required this.selectedIds,
    required this.onChanged,
  });

  final FavoriteMenuCatalog catalog;
  final Set<String> selectedIds;
  final void Function(MobileFavoriteMenu menu, bool selected) onChanged;

  @override
  Widget build(BuildContext context) => ListView(
    physics: const AlwaysScrollableScrollPhysics(),
    padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
    children: [
      Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: AppColors.mint,
          borderRadius: BorderRadius.circular(14),
        ),
        child: const Row(
          children: [
            Icon(Icons.touch_app_outlined, color: AppColors.deepGreen),
            SizedBox(width: 12),
            Expanded(
              child: Text(
                'Choose up to four menus to display in Favorites on Home.',
                style: TextStyle(color: AppColors.ink, height: 1.35),
              ),
            ),
          ],
        ),
      ),
      const SizedBox(height: 18),
      Wrap(
        alignment: WrapAlignment.spaceBetween,
        crossAxisAlignment: WrapCrossAlignment.center,
        runSpacing: 4,
        children: [
          const Text(
            'Available menus',
            style: TextStyle(fontSize: 17, fontWeight: FontWeight.w700),
          ),
          Text(
            '${selectedIds.length} of $maxFavoriteMenus selected',
            style: const TextStyle(color: AppColors.muted, fontSize: 12),
          ),
        ],
      ),
      const SizedBox(height: 10),
      if (catalog.available.isEmpty)
        const Card(
          child: Padding(
            padding: EdgeInsets.all(20),
            child: Text(
              'No mobile menus are available for this account.',
              textAlign: TextAlign.center,
              style: TextStyle(color: AppColors.muted),
            ),
          ),
        )
      else
        ...catalog.groupedMenus.map((entry) {
          final menu = entry.menu;
          final hasChildren = catalog.available.any(
            (item) => item.parentId == menu.id,
          );
          final selected = selectedIds.contains(menu.id);
          return Padding(
            key: ValueKey('favorite-menu-row-${menu.id}'),
            padding: EdgeInsets.only(
              left: entry.depth.clamp(0, 3) * 18.0,
              bottom: hasChildren ? 4 : 9,
            ),
            child: Card(
              color: hasChildren ? AppColors.mint : Colors.white,
              child: CheckboxListTile(
                value: selected,
                onChanged: (value) => onChanged(menu, value ?? false),
                activeColor: AppColors.teal,
                controlAffinity: ListTileControlAffinity.trailing,
                secondary: Container(
                  width: 44,
                  height: 44,
                  decoration: BoxDecoration(
                    color: AppColors.mint,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Center(
                    child: DatabaseMenuIcon(
                      key: ValueKey('favorite-menu-icon-${menu.id}'),
                      icon: menu.iconName,
                      activeIcon: menu.iconActiveName,
                      active: selected,
                      color: AppColors.deepGreen,
                    ),
                  ),
                ),
                title: Text(
                  menu.label,
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
                subtitle: Text(
                  hasChildren
                      ? '${catalog.available.where((item) => item.parentId == menu.id).length} menus'
                      : menu.description,
                ),
              ),
            ),
          );
        }),
    ],
  );
}

class _LoadError extends StatelessWidget {
  const _LoadError({required this.onRetry});

  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(
            Icons.cloud_off_outlined,
            size: 42,
            color: AppColors.muted,
          ),
          const SizedBox(height: 12),
          const Text('Favorites could not be loaded.'),
          const SizedBox(height: 8),
          FilledButton.tonal(
            onPressed: onRetry,
            child: const Text('Try again'),
          ),
        ],
      ),
    ),
  );
}
