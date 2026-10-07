class NewsItem {
  const NewsItem({
    required this.id,
    required this.title,
    required this.shortContent,
    required this.content,
    required this.imageThumbnail,
    required this.startDate,
    required this.dateCreated,
    required this.createdBy,
  });

  final String id;
  final String title;
  final String shortContent;
  final String content;
  final String? imageThumbnail;
  final DateTime? startDate;
  final DateTime? dateCreated;
  final String createdBy;

  /// The publication date is the primary ordering key used by the News API.
  /// The upload date is a safe fallback for manually created news.
  DateTime? get latestDate => startDate ?? dateCreated;

  factory NewsItem.fromJson(Map<String, dynamic> json) => NewsItem(
    id: _readString(json, 'Id', 'id'),
    title: _readString(json, 'Title', 'title'),
    shortContent: _readString(json, 'ShortContent', 'shortContent'),
    content: _readString(json, 'Content', 'content'),
    imageThumbnail: _readNullableString(
      json,
      'ImageThumbnail',
      'imageThumbnail',
    ),
    startDate: _readDate(json, 'StartDate', 'startDate'),
    dateCreated: _readDate(json, 'DateCreated', 'dateCreated'),
    createdBy: _readString(json, 'CreatedBy', 'createdBy'),
  );
}

const latestNewsLimit = 5;
const newsPageSize = 5;

List<NewsItem> sortNewsByLatest(Iterable<NewsItem> items) {
  final sorted = items.toList(growable: false);
  sorted.sort((left, right) {
    final leftDate = left.latestDate;
    final rightDate = right.latestDate;
    if (leftDate == null && rightDate == null) return 0;
    if (leftDate == null) return 1;
    if (rightDate == null) return -1;
    return rightDate.compareTo(leftDate);
  });
  return sorted;
}

List<NewsItem> latestNews(
  Iterable<NewsItem> items, {
  int limit = latestNewsLimit,
}) {
  if (limit <= 0) return const [];
  return sortNewsByLatest(items).take(limit).toList(growable: false);
}

/// Returns a one-based page from [items]. Invalid or out-of-range pages are
/// represented by an empty list.
List<NewsItem> paginateNews(
  List<NewsItem> items, {
  required int pageNumber,
  int pageSize = newsPageSize,
}) {
  if (pageNumber < 1 || pageSize < 1 || items.isEmpty) return const [];
  final start = (pageNumber - 1) * pageSize;
  if (start >= items.length) return const [];
  final end = (start + pageSize).clamp(0, items.length);
  return List<NewsItem>.unmodifiable(items.sublist(start, end));
}

int newsPageCount(int itemCount, {int pageSize = newsPageSize}) {
  if (itemCount <= 0 || pageSize < 1) return 0;
  return (itemCount + pageSize - 1) ~/ pageSize;
}

String _readString(
  Map<String, dynamic> json,
  String pascalCaseKey,
  String camelCaseKey,
) => '${json[pascalCaseKey] ?? json[camelCaseKey] ?? ''}'.trim();

String? _readNullableString(
  Map<String, dynamic> json,
  String pascalCaseKey,
  String camelCaseKey,
) {
  final value = _readString(json, pascalCaseKey, camelCaseKey);
  return value.isEmpty ? null : value;
}

DateTime? _readDate(
  Map<String, dynamic> json,
  String pascalCaseKey,
  String camelCaseKey,
) {
  final value = json[pascalCaseKey] ?? json[camelCaseKey];
  if (value is DateTime) return value;
  return value == null ? null : DateTime.tryParse('$value');
}
