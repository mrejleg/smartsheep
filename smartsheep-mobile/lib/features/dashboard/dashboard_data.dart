import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/api_client.dart';

final dashboardFilterProvider =
    NotifierProvider<DashboardFilterNotifier, DashboardFilter>(
      DashboardFilterNotifier.new,
    );

final dashboardProvider = FutureProvider<DashboardData>((ref) async {
  final filter = ref.watch(dashboardFilterProvider);
  final response = await ref
      .watch(apiClientProvider)
      .get('/api/SmartSheepReport/Dashboard', query: filter.toQuery());
  return DashboardData.fromJson(response);
});

final dashboardOptionsProvider = FutureProvider<DashboardFilterOptions>((
  ref,
) async {
  final client = ref.watch(apiClientProvider);
  final responses = await Future.wait([
    client.get('/api/Farm/All'),
    client.get('/api/Barn/All'),
  ]);
  return DashboardFilterOptions.fromJson(
    farms: responses[0],
    barns: responses[1],
  );
});

class DashboardFilterNotifier extends Notifier<DashboardFilter> {
  @override
  DashboardFilter build() => DashboardFilter.defaults();

  void setFilter(DashboardFilter filter) {
    if (!filter.isValid) {
      throw ArgumentError('Start date must be before or equal to end date.');
    }
    state = filter;
  }

  void reset({DateTime? now}) => state = DashboardFilter.defaults(now: now);
}

class DashboardFilter {
  DashboardFilter({
    required DateTime startDate,
    required DateTime endDate,
    this.farmId,
    this.barnId,
  }) : startDate = DateTime(startDate.year, startDate.month, startDate.day),
       endDate = DateTime(endDate.year, endDate.month, endDate.day);

  factory DashboardFilter.defaults({DateTime? now}) {
    final date = now ?? DateTime.now();
    final today = DateTime(date.year, date.month, date.day);
    return DashboardFilter(
      startDate: DateTime(today.year, today.month, today.day - 7),
      endDate: today,
    );
  }

  final DateTime startDate;
  final DateTime endDate;
  final String? farmId;
  final String? barnId;

  bool get isValid => !startDate.isAfter(endDate);

  DashboardFilter copyWith({
    DateTime? startDate,
    DateTime? endDate,
    Object? farmId = _unchanged,
    Object? barnId = _unchanged,
  }) => DashboardFilter(
    startDate: startDate ?? this.startDate,
    endDate: endDate ?? this.endDate,
    farmId: identical(farmId, _unchanged) ? this.farmId : farmId as String?,
    barnId: identical(barnId, _unchanged) ? this.barnId : barnId as String?,
  );

  Map<String, dynamic> toQuery() {
    if (!isValid) {
      throw ArgumentError('Start date must be before or equal to end date.');
    }
    return {
      'startDate': _dateQuery(startDate),
      'endDate': _dateQuery(endDate),
      if (farmId?.isNotEmpty == true) 'farmId': farmId,
      if (barnId?.isNotEmpty == true) 'barnId': barnId,
    };
  }

  @override
  bool operator ==(Object other) =>
      other is DashboardFilter &&
      startDate == other.startDate &&
      endDate == other.endDate &&
      farmId == other.farmId &&
      barnId == other.barnId;

  @override
  int get hashCode => Object.hash(startDate, endDate, farmId, barnId);
}

const _unchanged = Object();

String _dateQuery(DateTime date) =>
    '${date.year.toString().padLeft(4, '0')}-'
    '${date.month.toString().padLeft(2, '0')}-'
    '${date.day.toString().padLeft(2, '0')}';

class DashboardData {
  const DashboardData({
    this.date,
    this.totalFarms = 0,
    this.totalBarns = 0,
    this.totalSheep = 0,
    this.todayActivities = 0,
    this.todayFrequency = 0,
    this.todayDurationSeconds = 0,
    this.statisticsRequiringReminder = 0,
    this.hourlyTrend = const [],
    this.barnComparison = const [],
    this.reminders = const [],
    this.inactiveSourceVideos = const [],
    this.inactiveBarns = const [],
    this.farmMaps = const [],
  });

  factory DashboardData.fromJson(Object? response) {
    final json = _object(response, 'Dashboard');
    return DashboardData(
      date: _date(json, 'Date'),
      totalFarms: _integer(json, 'TotalFarms'),
      totalBarns: _integer(json, 'TotalBarns'),
      totalSheep: _integer(json, 'TotalSheep'),
      todayActivities: _integer(json, 'TodayActivities'),
      todayFrequency: _integer(json, 'TodayFrequency'),
      todayDurationSeconds: _integer(json, 'TodayDurationSeconds'),
      statisticsRequiringReminder: _integer(
        json,
        'StatisticsRequiringReminder',
      ),
      hourlyTrend: _list(json, 'HourlyTrend', DashboardHourlyTrend.fromJson),
      barnComparison: _list(
        json,
        'BarnComparison',
        DashboardBarnComparison.fromJson,
      ),
      reminders: _list(json, 'Reminders', DashboardReminderStatus.fromJson),
      inactiveSourceVideos: _list(
        json,
        'InactiveSourceVideos',
        DashboardInactiveSourceVideo.fromJson,
      ),
      inactiveBarns: _list(
        json,
        'InactiveBarns',
        DashboardInactiveBarn.fromJson,
      ),
      farmMaps: _list(json, 'FarmMaps', DashboardFarmMap.fromJson),
    );
  }

  final DateTime? date;
  final int totalFarms;
  final int totalBarns;
  final int totalSheep;
  final int todayActivities;
  final int todayFrequency;
  final int todayDurationSeconds;
  final int statisticsRequiringReminder;
  final List<DashboardHourlyTrend> hourlyTrend;
  final List<DashboardBarnComparison> barnComparison;
  final List<DashboardReminderStatus> reminders;
  final List<DashboardInactiveSourceVideo> inactiveSourceVideos;
  final List<DashboardInactiveBarn> inactiveBarns;
  final List<DashboardFarmMap> farmMaps;
}

class DashboardHourlyTrend {
  const DashboardHourlyTrend({
    this.hour = 0,
    this.frequency = 0,
    this.durationSeconds = 0,
  });

  factory DashboardHourlyTrend.fromJson(Map<String, dynamic> json) =>
      DashboardHourlyTrend(
        hour: _integer(json, 'Hour'),
        frequency: _integer(json, 'Frequency'),
        durationSeconds: _integer(json, 'DurationSeconds'),
      );

  final int hour;
  final int frequency;
  final int durationSeconds;
}

class DashboardBarnComparison {
  const DashboardBarnComparison({
    this.barnId,
    this.barnCode,
    this.barnName,
    this.frequency = 0,
    this.durationSeconds = 0,
  });

  factory DashboardBarnComparison.fromJson(Map<String, dynamic> json) =>
      DashboardBarnComparison(
        barnId: _string(json, 'BarnId'),
        barnCode: _string(json, 'BarnCode'),
        barnName: _string(json, 'BarnName'),
        frequency: _integer(json, 'Frequency'),
        durationSeconds: _integer(json, 'DurationSeconds'),
      );

  final String? barnId;
  final String? barnCode;
  final String? barnName;
  final int frequency;
  final int durationSeconds;
}

class DashboardReminderStatus {
  const DashboardReminderStatus({this.status = '', this.total = 0});

  factory DashboardReminderStatus.fromJson(Map<String, dynamic> json) =>
      DashboardReminderStatus(
        status: _string(json, 'Status') ?? '',
        total: _integer(json, 'Total'),
      );

  final String status;
  final int total;
}

class DashboardInactiveSourceVideo {
  const DashboardInactiveSourceVideo({
    required this.id,
    this.code,
    this.barnName,
    this.sourceVideoUrl,
    this.lastStatus,
    this.lastCheckedAt,
  });

  factory DashboardInactiveSourceVideo.fromJson(Map<String, dynamic> json) =>
      DashboardInactiveSourceVideo(
        id: _requiredString(json, 'Id'),
        code: _string(json, 'Code'),
        barnName: _string(json, 'BarnName'),
        sourceVideoUrl: _string(json, 'SourceVideoUrl'),
        lastStatus: _string(json, 'LastStatus'),
        lastCheckedAt: _date(json, 'LastCheckedAt'),
      );

  final String id;
  final String? code;
  final String? barnName;
  final String? sourceVideoUrl;
  final String? lastStatus;
  final DateTime? lastCheckedAt;
}

class DashboardInactiveBarn {
  const DashboardInactiveBarn({
    required this.id,
    this.code,
    this.name,
    this.farmName,
    this.totalSourceVideos = 0,
    this.onlineSourceVideos = 0,
  });

  factory DashboardInactiveBarn.fromJson(Map<String, dynamic> json) =>
      DashboardInactiveBarn(
        id: _requiredString(json, 'Id'),
        code: _string(json, 'Code'),
        name: _string(json, 'Name'),
        farmName: _string(json, 'FarmName'),
        totalSourceVideos: _integer(json, 'TotalSourceVideos'),
        onlineSourceVideos: _integer(json, 'OnlineSourceVideos'),
      );

  final String id;
  final String? code;
  final String? name;
  final String? farmName;
  final int totalSourceVideos;
  final int onlineSourceVideos;
}

class DashboardFarmMap {
  const DashboardFarmMap({
    required this.id,
    this.code,
    this.name,
    this.location,
    this.address,
    this.latitude,
    this.longitude,
    this.totalBarns = 0,
    this.totalSheep = 0,
    this.totalSourceVideos = 0,
    this.onlineSourceVideos = 0,
  });

  factory DashboardFarmMap.fromJson(Map<String, dynamic> json) =>
      DashboardFarmMap(
        id: _requiredString(json, 'Id'),
        code: _string(json, 'Code'),
        name: _string(json, 'Name'),
        location: _string(json, 'Location'),
        address: _string(json, 'Address'),
        latitude: _string(json, 'Latitude'),
        longitude: _string(json, 'Longitude'),
        totalBarns: _integer(json, 'TotalBarns'),
        totalSheep: _integer(json, 'TotalSheep'),
        totalSourceVideos: _integer(json, 'TotalSourceVideos'),
        onlineSourceVideos: _integer(json, 'OnlineSourceVideos'),
      );

  final String id;
  final String? code;
  final String? name;
  final String? location;
  final String? address;
  final String? latitude;
  final String? longitude;
  final int totalBarns;
  final int totalSheep;
  final int totalSourceVideos;
  final int onlineSourceVideos;
}

class DashboardFarmOption {
  const DashboardFarmOption({required this.id, required this.name});

  final String id;
  final String name;
}

class DashboardBarnOption {
  const DashboardBarnOption({
    required this.id,
    required this.name,
    this.farmId,
  });

  final String id;
  final String name;
  final String? farmId;
}

class DashboardFilterOptions {
  const DashboardFilterOptions({this.farms = const [], this.barns = const []});

  factory DashboardFilterOptions.fromJson({
    required Object? farms,
    required Object? barns,
  }) {
    final farmOptions =
        _activeOptions(farms, 'Farms')
            .map(
              (json) => DashboardFarmOption(
                id: _requiredString(json, 'Id'),
                name: _requiredString(json, 'Name'),
              ),
            )
            .toList()
          ..sort(
            (a, b) => a.name.toLowerCase().compareTo(b.name.toLowerCase()),
          );
    final barnOptions =
        _activeOptions(barns, 'Barns')
            .map(
              (json) => DashboardBarnOption(
                id: _requiredString(json, 'Id'),
                name: _requiredString(json, 'Name'),
                farmId: _string(json, 'FkFarmId'),
              ),
            )
            .toList()
          ..sort(
            (a, b) => a.name.toLowerCase().compareTo(b.name.toLowerCase()),
          );
    return DashboardFilterOptions(
      farms: List.unmodifiable(farmOptions),
      barns: List.unmodifiable(barnOptions),
    );
  }

  final List<DashboardFarmOption> farms;
  final List<DashboardBarnOption> barns;
}

Map<String, dynamic> _object(Object? value, String field) {
  if (value is! Map || value.keys.any((key) => key is! String)) {
    throw FormatException('Invalid $field response: expected an object.');
  }
  return Map<String, dynamic>.from(value);
}

Object? _field(Map<String, dynamic> json, String name) => json.containsKey(name)
    ? json[name]
    : json['${name[0].toLowerCase()}${name.substring(1)}'];

int _integer(Map<String, dynamic> json, String name) {
  final value = _field(json, name);
  if (value == null) return 0;
  if (value is num && value.isFinite && value == value.roundToDouble()) {
    return value.toInt();
  }
  throw FormatException('Invalid $name: expected an integer.');
}

String? _string(Map<String, dynamic> json, String name) {
  final value = _field(json, name);
  if (value == null || value is String) return value as String?;
  throw FormatException('Invalid $name: expected text.');
}

String _requiredString(Map<String, dynamic> json, String name) {
  final value = _string(json, name);
  if (value == null || value.trim().isEmpty) {
    throw FormatException('Invalid $name: expected non-empty text.');
  }
  return value;
}

DateTime? _date(Map<String, dynamic> json, String name) {
  final value = _string(json, name);
  if (value == null || value.isEmpty) return null;
  final date = DateTime.tryParse(value);
  if (date == null) throw FormatException('Invalid $name: expected a date.');
  return date;
}

List<T> _list<T>(
  Map<String, dynamic> json,
  String name,
  T Function(Map<String, dynamic>) parse,
) {
  final value = _field(json, name);
  if (value == null) return [];
  if (value is! List) {
    throw FormatException('Invalid $name: expected a list.');
  }
  return List.unmodifiable(value.map((item) => parse(_object(item, name))));
}

Iterable<Map<String, dynamic>> _activeOptions(Object? value, String name) {
  if (value is! List) {
    throw FormatException('Invalid $name response: expected a list.');
  }
  return value.map((item) => _object(item, name)).where((json) {
    final active = _field(json, 'IsActive');
    if (active is! bool) {
      throw FormatException('Invalid $name IsActive: expected a boolean.');
    }
    return active;
  });
}
