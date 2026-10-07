import 'package:flutter/painting.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/api_client.dart';
import '../auth/auth_controller.dart';
import '../notifications/notifications_screen.dart';

/// The profile image endpoint is public; getBytes never sends a bearer token.
/// A missing photo or an unavailable API leaves the header's initials visible.
final headerProfileImageProvider = FutureProvider<MemoryImage?>((ref) async {
  final session = ref.watch(authControllerProvider).value;
  final username = session?.username.trim();
  if (username == null || username.isEmpty) return null;

  final path = Uri(
    path: '/api/MobileApp/ProfileImage',
    queryParameters: {'username': username},
  ).toString();
  try {
    final bytes = await ref.watch(apiClientProvider).getBytes(path);
    return bytes.isEmpty ? null : MemoryImage(bytes);
  } catch (_) {
    return null;
  }
});

/// Counts only confirmed unread API records, not read history or unknown flags.
final unreadNotificationCountProvider = Provider<int>((ref) {
  final items = ref.watch(notificationsProvider).asData?.value;
  if (items == null) return 0;
  return items.whereType<Map>().where((item) {
    final isRead = item.containsKey('IsRead') ? item['IsRead'] : item['isRead'];
    return isRead == false;
  }).length;
});
