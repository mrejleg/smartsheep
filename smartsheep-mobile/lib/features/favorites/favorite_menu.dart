const maxFavoriteMenus = 4;

enum FavoriteMenuType {
  home,
  liveStreams,
  streamers,
  reminders,
  reports,
  statistics,
  news,
  dashboard,
  farms,
  barns,
  livestock,
  other,
}

class MobileFavoriteMenu {
  const MobileFavoriteMenu({
    required this.id,
    required this.name,
    required this.controller,
    required this.sequenceNumber,
    required this.type,
    this.iconName,
    this.iconActiveName,
    this.parentId,
  });

  final String id;
  final String name;
  final String controller;
  final String sequenceNumber;
  final FavoriteMenuType type;
  final String? iconName;
  final String? iconActiveName;
  final String? parentId;

  String get label => name.isEmpty ? 'Menu' : name;

  String get description => switch (type) {
    FavoriteMenuType.home => 'Return to the Home page',
    FavoriteMenuType.liveStreams => 'View video sources and live cameras',
    FavoriteMenuType.streamers => 'Monitor the condition of video streams',
    FavoriteMenuType.reminders => 'View reminder data',
    FavoriteMenuType.reports => 'Review livestock activity reports',
    FavoriteMenuType.statistics => 'View monitoring statistics',
    FavoriteMenuType.news => 'Read the latest news',
    FavoriteMenuType.dashboard => 'Open the operational dashboard',
    FavoriteMenuType.farms => 'Open farm information',
    FavoriteMenuType.barns => 'Open barn information',
    FavoriteMenuType.livestock => 'Open livestock information',
    FavoriteMenuType.other => 'Open the $label menu',
  };

  static MobileFavoriteMenu? fromJson(Map<String, dynamic> json) {
    final id = '${json['Id'] ?? json['id'] ?? ''}'.trim();
    final name = '${json['Name'] ?? json['name'] ?? ''}'.trim();
    final controller = '${json['Controller'] ?? json['controller'] ?? ''}'
        .trim();
    final sequence = '${json['SequenceNumber'] ?? json['sequenceNumber'] ?? ''}'
        .trim();
    final isSection = json['IsSection'] == true || json['isSection'] == true;
    final isActive = json['IsActive'] ?? json['isActive'];
    final isMobile = json.containsKey('IsMobile')
        ? json['IsMobile']
        : json['isMobile'];
    final type = _typeFor(controller, name);

    if (id.isEmpty || isSection || isActive == false || isMobile != true) {
      return null;
    }

    return MobileFavoriteMenu(
      id: id,
      name: name,
      controller: controller,
      sequenceNumber: sequence,
      type: type,
      iconName: _iconName(json['Icon'] ?? json['icon']),
      iconActiveName: _iconName(json['IconActive'] ?? json['iconActive']),
      parentId: _iconName(json['FkParentId'] ?? json['fkParentId']),
    );
  }
}

class FavoriteMenuCatalog {
  const FavoriteMenuCatalog({required this.available, required this.selected});

  const FavoriteMenuCatalog.empty() : available = const [], selected = const [];

  final List<MobileFavoriteMenu> available;
  final List<MobileFavoriteMenu> selected;

  /// Walk actual parent IDs, keeping orphans visible and breaking invalid cycles.
  List<({MobileFavoriteMenu menu, int depth})> get groupedMenus {
    final ids = available.map((menu) => menu.id).toSet();
    final seen = <String>{};
    final rows = <({MobileFavoriteMenu menu, int depth})>[];
    void visit(MobileFavoriteMenu menu, int depth) {
      if (!seen.add(menu.id)) return;
      rows.add((menu: menu, depth: depth));
      for (final child in available.where((item) => item.parentId == menu.id)) {
        visit(child, depth + 1);
      }
    }

    for (final menu in available.where(
      (item) => !ids.contains(item.parentId),
    )) {
      visit(menu, 0);
    }
    for (final menu in available) {
      visit(menu, 0);
    }
    return rows;
  }

  factory FavoriteMenuCatalog.fromData({
    required dynamic accessMenuResponse,
    required dynamic favoriteResponse,
  }) {
    final availableById = <String, MobileFavoriteMenu>{};
    for (final json in _accessMenuMaps(accessMenuResponse)) {
      final menu = MobileFavoriteMenu.fromJson(json);
      if (menu != null) availableById.putIfAbsent(menu.id, () => menu);
    }
    final available = availableById.values.toList()..sort(_compareMenus);
    final selected = <MobileFavoriteMenu>[];
    final selectedIds = <String>{};
    for (final json in _menuMaps(favoriteResponse)) {
      final id = '${json['Id'] ?? json['id'] ?? ''}'.trim();
      final menu = availableById[id];
      if (menu == null || !selectedIds.add(id)) continue;
      selected.add(menu);
      if (selected.length == maxFavoriteMenus) break;
    }
    return FavoriteMenuCatalog(
      available: List.unmodifiable(available),
      selected: List.unmodifiable(selected),
    );
  }
}

String? _iconName(Object? value) {
  if (value is! String) return null;
  final name = value.trim();
  return name.isEmpty ? null : name;
}

FavoriteMenuType _typeFor(String controller, String name) {
  final normalizedController = controller.toLowerCase();
  final normalizedName = name.toLowerCase();
  final direct = switch (normalizedController) {
    'home' => FavoriteMenuType.home,
    'sourcevideo' => FavoriteMenuType.liveStreams,
    'sourcevideostatuslog' => FavoriteMenuType.streamers,
    'remindertemplate' ||
    'reminderlog' ||
    'reminderreport' => FavoriteMenuType.reminders,
    'sucklingactivityreport' ||
    'sucklingstatisticreport' => FavoriteMenuType.reports,
    'sucklingstatistic' => FavoriteMenuType.statistics,
    'news' => FavoriteMenuType.news,
    'dashboard' => FavoriteMenuType.dashboard,
    'farm' => FavoriteMenuType.farms,
    'barn' => FavoriteMenuType.barns,
    'livestock' => FavoriteMenuType.livestock,
    _ => null,
  };
  if (direct != null) return direct;
  if (normalizedController.contains('reminder')) {
    return FavoriteMenuType.reminders;
  }
  if (normalizedController.contains('report') ||
      normalizedName.contains('report')) {
    return FavoriteMenuType.reports;
  }
  if (normalizedController.contains('statistic')) {
    return FavoriteMenuType.statistics;
  }
  return FavoriteMenuType.other;
}

int _compareMenus(MobileFavoriteMenu a, MobileFavoriteMenu b) {
  final bySequence = a.sequenceNumber.compareTo(b.sequenceNumber);
  return bySequence != 0 ? bySequence : a.label.compareTo(b.label);
}

Iterable<Map<String, dynamic>> _accessMenuMaps(dynamic value) sync* {
  final menus = _asMap(value);
  yield* _menuMaps(menus['MenuParents'] ?? menus['menuParents']);
  yield* _menuMaps(menus['MenuChilds'] ?? menus['menuChilds']);
}

Iterable<Map<String, dynamic>> _menuMaps(dynamic value) sync* {
  if (value is! List) return;
  for (final item in value) {
    if (item is Map) yield Map<String, dynamic>.from(item);
  }
}

Map<String, dynamic> _asMap(dynamic value) =>
    value is Map ? Map<String, dynamic>.from(value) : <String, dynamic>{};
