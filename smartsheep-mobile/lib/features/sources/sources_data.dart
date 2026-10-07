import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/api_client.dart';

final sourcesProvider = FutureProvider<SourcesData>((ref) async {
  final client = ref.watch(apiClientProvider);
  final responses = await Future.wait([
    client.get('/api/SourceVideo/All'),
    client.get('/api/Barn/All'),
    client.get('/api/Farm/All'),
  ]);
  return SourcesData.fromJson(
    sources: responses[0],
    barns: responses[1],
    farms: responses[2],
  );
});

enum SourceAvailability { all, online, offline }

class SourcesData {
  const SourcesData({this.farms = const []});

  factory SourcesData.fromJson({
    required Object? sources,
    required Object? barns,
    required Object? farms,
  }) {
    final sourceRows = _indexedRows(sources, 'Source videos');
    final barnRows = _indexedRows(barns, 'Barns');
    final farmRows = _indexedRows(farms, 'Farms');
    final enabledBarns = <String, Map<String, dynamic>>{};
    final grouped = <String?, Map<String?, List<SourceVideoItem>>>{};

    String? farmIdFor(Map<String, dynamic> barn) {
      final farmId = _text(barn, 'FkFarmId');
      return farmRows.containsKey(farmId) ? farmId : null;
    }

    // Start from the current barn master, so a barn without a configured source
    // is still visible. Never infer a camera or an offline status for that barn.
    for (final entry in barnRows.entries) {
      final barn = entry.value;
      if (!_active(barn)) continue;
      final farm = farmRows[_text(barn, 'FkFarmId')];
      if (farm != null && !_active(farm)) continue;
      enabledBarns[entry.key] = barn;
      grouped.putIfAbsent(farmIdFor(barn), () => {})[entry.key] = [];
    }

    for (final entry in sourceRows.entries) {
      final source = entry.value;
      if (!_active(source)) continue;
      final barnId = _text(source, 'FkBarnId');
      final masterBarn = barnRows[barnId];
      if (masterBarn != null && !enabledBarns.containsKey(barnId)) continue;

      // Navigation objects can be stale or absent. Foreign-key IDs joined to
      // the master endpoints are authoritative for current names/ownership.
      final farmId = masterBarn == null ? null : farmIdFor(masterBarn);
      final resolvedBarnId = masterBarn == null ? null : barnId;
      grouped
          .putIfAbsent(farmId, () => {})
          .putIfAbsent(resolvedBarnId, () => [])
          .add(
            SourceVideoItem(
              id: entry.key,
              code: _text(source, 'Code') ?? '',
              isOnline: _field(source, 'IsOnline') == true,
              sourceVideoUrl: _text(source, 'SourceVideoUrl'),
            ),
          );
    }

    final farmGroups = grouped.entries.map((farmEntry) {
      final farm = farmRows[farmEntry.key];
      final barnGroups = farmEntry.value.entries.map((barnEntry) {
        final barn = barnRows[barnEntry.key];
        final items = barnEntry.value
          ..sort((a, b) => _compare(a.code, a.id, b.code, b.id));
        return SourcesBarnGroup(
          id: barnEntry.key,
          name: barn == null ? 'Unassigned Barn' : _name(barn, 'Unnamed Barn'),
          code: barn == null ? null : _text(barn, 'Code'),
          sources: List.unmodifiable(items),
        );
      }).toList()..sort((a, b) => _compare(a.name, a.id, b.name, b.id));
      return SourcesFarmGroup(
        id: farmEntry.key,
        name: farm == null ? 'Unassigned Farm' : _name(farm, 'Unnamed Farm'),
        code: farm == null ? null : _text(farm, 'Code'),
        barns: List.unmodifiable(barnGroups),
      );
    }).toList()..sort((a, b) => _compare(a.name, a.id, b.name, b.id));
    return SourcesData(farms: List.unmodifiable(farmGroups));
  }

  final List<SourcesFarmGroup> farms;

  int get totalFarms => farms.where((farm) => !farm.isUnassigned).length;
  int get totalBarns => farms.fold(0, (total, farm) => total + farm.totalBarns);
  int get totalSources =>
      farms.fold(0, (total, farm) => total + farm.totalSources);
  int get onlineCount =>
      farms.fold(0, (total, farm) => total + farm.onlineCount);
  int get offlineCount => totalSources - onlineCount;

  SourcesData filtered(SourceAvailability availability) {
    if (availability == SourceAvailability.all) return this;
    final online = availability == SourceAvailability.online;
    final visibleFarms = <SourcesFarmGroup>[];
    for (final farm in farms) {
      final visibleBarns = <SourcesBarnGroup>[];
      for (final barn in farm.barns) {
        final sources = barn.sources
            .where((source) => source.isOnline == online)
            .toList(growable: false);
        if (sources.isEmpty) continue;
        visibleBarns.add(
          SourcesBarnGroup(
            id: barn.id,
            name: barn.name,
            code: barn.code,
            sources: List.unmodifiable(sources),
          ),
        );
      }
      if (visibleBarns.isEmpty) continue;
      visibleFarms.add(
        SourcesFarmGroup(
          id: farm.id,
          name: farm.name,
          code: farm.code,
          barns: List.unmodifiable(visibleBarns),
        ),
      );
    }
    return SourcesData(farms: List.unmodifiable(visibleFarms));
  }
}

class SourcesFarmGroup {
  const SourcesFarmGroup({
    this.id,
    required this.name,
    this.code,
    this.barns = const [],
  });

  final String? id;
  final String name;
  final String? code;
  final List<SourcesBarnGroup> barns;

  bool get isUnassigned => id == null;
  int get totalBarns => barns.where((barn) => !barn.isUnassigned).length;
  int get totalSources =>
      barns.fold(0, (total, barn) => total + barn.totalSources);
  int get onlineCount =>
      barns.fold(0, (total, barn) => total + barn.onlineCount);
  int get offlineCount => totalSources - onlineCount;
}

class SourcesBarnGroup {
  const SourcesBarnGroup({
    this.id,
    required this.name,
    this.code,
    this.sources = const [],
  });

  final String? id;
  final String name;
  final String? code;
  final List<SourceVideoItem> sources;

  bool get isUnassigned => id == null;
  bool get hasSources => sources.isNotEmpty;
  int get totalSources => sources.length;
  int get onlineCount => sources.where((source) => source.isOnline).length;
  int get offlineCount => totalSources - onlineCount;
}

class SourceVideoItem {
  const SourceVideoItem({
    required this.id,
    required this.code,
    required this.isOnline,
    this.sourceVideoUrl,
  });

  final String id;
  final String code;
  final bool isOnline;
  final String? sourceVideoUrl;
}

Object? _field(Map<String, dynamic> row, String name) => row.containsKey(name)
    ? row[name]
    : row['${name[0].toLowerCase()}${name.substring(1)}'];

String? _text(Map<String, dynamic> row, String name) {
  final value = _field(row, name);
  if (value == null) return null;
  if (value is! String) {
    throw FormatException('Invalid $name: expected text.');
  }
  return value.trim().isEmpty ? null : value.trim();
}

bool _active(Map<String, dynamic> row) => _field(row, 'IsActive') != false;

String _name(Map<String, dynamic> row, String fallback) =>
    _text(row, 'Name') ?? _text(row, 'Code') ?? fallback;

Map<String, Map<String, dynamic>> _indexedRows(Object? value, String name) {
  if (value is! List) {
    throw FormatException('Invalid $name response: expected a list.');
  }
  final rows = <String, Map<String, dynamic>>{};
  for (final item in value) {
    if (item is! Map || item.keys.any((key) => key is! String)) {
      throw FormatException('Invalid $name row: expected an object.');
    }
    final row = Map<String, dynamic>.from(item);
    final id = _text(row, 'Id');
    if (id == null) throw FormatException('Invalid $name row: missing Id.');
    if (rows.containsKey(id)) {
      throw FormatException('Invalid $name response: duplicate Id.');
    }
    rows[id] = row;
  }
  return rows;
}

int _compare(String aName, String? aId, String bName, String? bId) {
  if (aId == null && bId != null) return 1;
  if (aId != null && bId == null) return -1;
  final names = aName.toLowerCase().compareTo(bName.toLowerCase());
  return names == 0 ? (aId ?? '').compareTo(bId ?? '') : names;
}
