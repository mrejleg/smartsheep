import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/api_client.dart';
import 'news_item.dart';

final newsRepositoryProvider = Provider<NewsRepository>(
  (ref) => NewsRepository(ref.watch(apiClientProvider)),
);

final newsItemsProvider = FutureProvider<List<NewsItem>>(
  (ref) => ref.watch(newsRepositoryProvider).fetchNews(),
);

class NewsRepository {
  const NewsRepository(this._apiClient);

  final ApiClient _apiClient;

  Future<List<NewsItem>> fetchNews() async {
    final response = await _apiClient.get('/api/MobileApp/News');
    if (response is! List) return const [];

    final items = response
        .whereType<Map>()
        .map((item) => NewsItem.fromJson(Map<String, dynamic>.from(item)))
        .toList(growable: false);
    return List<NewsItem>.unmodifiable(sortNewsByLatest(items));
  }
}
