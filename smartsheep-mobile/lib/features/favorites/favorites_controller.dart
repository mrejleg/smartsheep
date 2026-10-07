import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/api_client.dart';
import '../auth/auth_controller.dart';
import 'favorite_menu.dart';

final favoritesRepositoryProvider = Provider<FavoritesRepository>(
  (ref) => FavoritesRepository(ref.read(apiClientProvider)),
);

final favoriteMenuCatalogProvider = FutureProvider<FavoriteMenuCatalog>((
  ref,
) async {
  final session = ref.watch(authControllerProvider).value;
  if (session == null) return const FavoriteMenuCatalog.empty();

  final repository = ref.read(favoritesRepositoryProvider);
  final responses = await Future.wait([
    repository.fetchAccessMenu(session.username),
    repository.fetchFavorites(session.username),
  ]);
  return FavoriteMenuCatalog.fromData(
    accessMenuResponse: responses[0],
    favoriteResponse: responses[1],
  );
});

class FavoritesRepository {
  const FavoritesRepository(this._client);

  final ApiClient _client;

  Future<dynamic> fetchAccessMenu(String username) =>
      _client.get('/api/MobileApp/AccessMenu', query: {'username': username});

  Future<dynamic> fetchFavorites(String username) =>
      _client.get('/api/MobileApp/MenuFavorite', query: {'username': username});

  Future<void> save({required String email, required List<String> ids}) async {
    final result = await _client.post(
      '/api/MobileApp/MobilePostFavoriteMenu',
      data: {'Email': email, 'FkAppMenuIds': ids},
    );
    if (result != true) {
      throw StateError('The API did not confirm the Favorites update.');
    }
  }
}
