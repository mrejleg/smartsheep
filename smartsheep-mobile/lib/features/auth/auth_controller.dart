import 'dart:io';
import 'dart:async';

import 'package:device_info_plus/device_info_plus.dart';
import 'package:dio/dio.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/config/app_config.dart';
import '../../core/network/api_client.dart';

class AuthSession {
  const AuthSession({
    required this.token,
    required this.username,
    this.profile = const {},
  });
  final String token;
  final String username;
  final Map<String, dynamic> profile;
  String get fullName =>
      '${profile['FullName'] ?? profile['fullName'] ?? username}';
  String get role =>
      '${profile['Roles'] ?? profile['roles'] ?? 'SmartSheep User'}';
  String get email => '${profile['Email'] ?? profile['email'] ?? ''}'.trim();
}

final authControllerProvider =
    AsyncNotifierProvider<AuthController, AuthSession?>(AuthController.new);

class AuthController extends AsyncNotifier<AuthSession?> {
  StreamSubscription<String>? _tokenRefreshSubscription;
  int _deviceRegistrationGeneration = 0;

  @override
  Future<AuthSession?> build() async {
    final store = ref.read(secureStoreProvider);
    final token = await store.readToken();
    final username = await store.readUsername();
    if (token == null || username == null) return null;
    try {
      final raw = await ref
          .read(apiClientProvider)
          .get('/api/MobileApp/Profile', query: {'username': username});
      _listenForTokenRefresh();
      unawaited(synchronizeDeviceRegistration());
      return AuthSession(token: token, username: username, profile: _map(raw));
    } catch (error, stackTrace) {
      final statusCode = error is DioException
          ? error.response?.statusCode
          : null;
      if (statusCode == 401 || statusCode == 403) {
        await store.clear();
        return null;
      }

      if (kDebugMode) {
        debugPrint('Unable to refresh the signed-in profile: $error');
        debugPrintStack(stackTrace: stackTrace);
      }
      _listenForTokenRefresh();
      unawaited(synchronizeDeviceRegistration());
      return AuthSession(token: token, username: username);
    }
  }

  Future<void> login(String username, String password) async {
    state = await AsyncValue.guard(() async {
      final store = ref.read(secureStoreProvider);
      final deviceToken = await store.readDeviceToken();
      final deviceName = await _deviceName();
      final raw = await ref
          .read(apiClientProvider)
          .post(
            '/api/MobileApp/Login',
            data: {
              'ClientId': AppConfig.clientId,
              'ClientSecret': AppConfig.clientSecret,
              'Username': username.trim(),
              'Password': password,
              'DeviceToken': deviceToken,
              'Platform': Platform.operatingSystem,
              'DeviceName': deviceName,
            },
          );
      final data = _map(raw);
      final tokenObject = _map(data['Token'] ?? data['token']);
      final token =
          '${tokenObject['Token'] ?? tokenObject['token'] ?? data['Token'] ?? ''}';
      if (token.isEmpty) {
        throw StateError(
          'API did not return an access token. Check API_CLIENT_SECRET.',
        );
      }
      final profile = _map(data['Profile'] ?? data['profile']);
      final normalizedUsername =
          '${profile['Username'] ?? profile['username'] ?? username}';
      await store.saveSession(token, normalizedUsername);
      _listenForTokenRefresh();
      unawaited(synchronizeDeviceRegistration());
      return AuthSession(
        token: token,
        username: normalizedUsername,
        profile: profile,
      );
    });
  }

  Future<void> logout() async {
    _deviceRegistrationGeneration++;
    await _tokenRefreshSubscription?.cancel();
    _tokenRefreshSubscription = null;
    try {
      final store = ref.read(secureStoreProvider);
      final token =
          await FirebaseMessaging.instance.getToken() ??
          await store.readDeviceToken();
      if (token != null) {
        await ref
            .read(apiClientProvider)
            .delete(
              '/api/FirebaseNotification/UnregisterDevice',
              data: {'DeviceToken': token},
            );
      }
    } catch (_) {}
    await ref.read(secureStoreProvider).clear();
    state = const AsyncData(null);
  }

  Future<void> _registerRefreshedToken(String deviceToken) async {
    final generation = _deviceRegistrationGeneration;
    final normalizedToken = deviceToken.trim();
    if (normalizedToken.isEmpty) return;
    final store = ref.read(secureStoreProvider);
    await store.saveDeviceToken(normalizedToken);
    if (generation != _deviceRegistrationGeneration ||
        await store.readToken() == null) {
      return;
    }
    await _registerDeviceToken(normalizedToken, expectedGeneration: generation);
  }

  /// Synchronizes the current Firebase token after login, session restore, and
  /// every foreground resume. Firebase can publish the APNs/FCM token a few
  /// seconds after authentication, so bounded retries prevent a valid signed-in
  /// session from remaining without an active device registration.
  Future<void> synchronizeDeviceRegistration() async {
    final generation = ++_deviceRegistrationGeneration;
    const retryDelays = <Duration>[
      Duration.zero,
      Duration(seconds: 1),
      Duration(seconds: 3),
      Duration(seconds: 8),
    ];

    for (final delay in retryDelays) {
      if (generation != _deviceRegistrationGeneration) return;
      if (delay != Duration.zero) await Future<void>.delayed(delay);
      if (generation != _deviceRegistrationGeneration) return;

      final store = ref.read(secureStoreProvider);
      if (await store.readToken() == null) return;
      if (generation != _deviceRegistrationGeneration) return;

      final deviceToken = await _getNotificationToken();
      if (deviceToken == null) continue;
      if (generation != _deviceRegistrationGeneration ||
          await store.readToken() == null) {
        return;
      }

      await store.saveDeviceToken(deviceToken);
      if (generation != _deviceRegistrationGeneration) return;
      if (await _registerDeviceToken(
        deviceToken,
        expectedGeneration: generation,
      )) {
        return;
      }
    }
  }

  Future<bool> _registerDeviceToken(
    String deviceToken, {
    int? expectedGeneration,
  }) async {
    try {
      await ref
          .read(apiClientProvider)
          .post(
            '/api/FirebaseNotification/RegisterDevice',
            data: {
              'DeviceToken': deviceToken,
              'Platform': Platform.operatingSystem,
              'DeviceName': await _deviceName(),
            },
          );

      if (expectedGeneration != null &&
          (expectedGeneration != _deviceRegistrationGeneration ||
              await ref.read(secureStoreProvider).readToken() == null)) {
        await _unregisterDeviceToken(deviceToken);
        return false;
      }
      return true;
    } catch (error, stackTrace) {
      if (kDebugMode) {
        debugPrint('Device token registration failed: $error');
        debugPrintStack(stackTrace: stackTrace);
      }
      return false;
    }
  }

  Future<void> _unregisterDeviceToken(String deviceToken) async {
    try {
      await ref
          .read(apiClientProvider)
          .delete(
            '/api/FirebaseNotification/UnregisterDevice',
            data: {'DeviceToken': deviceToken},
          );
    } catch (_) {}
  }

  void _listenForTokenRefresh() {
    if (_tokenRefreshSubscription != null) return;
    _tokenRefreshSubscription = FirebaseMessaging.instance.onTokenRefresh
        .listen(
          (token) => unawaited(_registerRefreshedToken(token)),
          onError: (Object error, StackTrace stackTrace) {
            if (kDebugMode) {
              debugPrint('Firebase token refresh failed: $error');
              debugPrintStack(stackTrace: stackTrace);
            }
          },
        );
    ref.onDispose(() => _tokenRefreshSubscription?.cancel());
  }

  Future<String?> _getNotificationToken() async {
    try {
      final messaging = FirebaseMessaging.instance;
      final settings = await messaging.requestPermission(
        alert: true,
        badge: true,
        sound: true,
      );
      if (settings.authorizationStatus == AuthorizationStatus.denied) {
        if (kDebugMode) debugPrint('Notification permission was denied.');
        return null;
      }

      if (Platform.isIOS || Platform.isMacOS) {
        String? apnsToken;
        for (var attempt = 0; attempt < 20 && apnsToken == null; attempt++) {
          apnsToken = await messaging.getAPNSToken();
          if (apnsToken == null) {
            await Future<void>.delayed(const Duration(milliseconds: 500));
          }
        }
        if (apnsToken == null) {
          if (kDebugMode) {
            debugPrint(
              'APNs token is unavailable; FCM registration will retry later.',
            );
          }
          return null;
        }
      }

      final token = await messaging.getToken();
      return token?.trim().isNotEmpty == true ? token!.trim() : null;
    } catch (error, stackTrace) {
      if (kDebugMode) {
        debugPrint('Unable to obtain Firebase device token: $error');
        debugPrintStack(stackTrace: stackTrace);
      }
      return null;
    }
  }

  Future<String> _deviceName() async {
    final deviceInfo = DeviceInfoPlugin();
    if (Platform.isAndroid) {
      final info = await deviceInfo.androidInfo;
      return '${info.manufacturer} ${info.model}'.trim();
    }
    if (Platform.isIOS) {
      return (await deviceInfo.iosInfo).name;
    }
    return 'Mobile device';
  }
}

Map<String, dynamic> _map(dynamic value) =>
    value is Map ? Map<String, dynamic>.from(value) : <String, dynamic>{};
