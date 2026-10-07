import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class SecureStore {
  const SecureStore();
  static const _storage = FlutterSecureStorage(aOptions: AndroidOptions());
  static const _deviceTokenKey = 'firebase_device_token';

  Future<String?> readToken() => _storage.read(key: 'access_token');
  Future<String?> readUsername() => _storage.read(key: 'username');
  Future<String?> readDeviceToken() => _storage.read(key: _deviceTokenKey);

  Future<void> saveSession(String token, String username) async {
    await _storage.write(key: 'access_token', value: token);
    await _storage.write(key: 'username', value: username);
  }

  Future<void> saveDeviceToken(String token) =>
      _storage.write(key: _deviceTokenKey, value: token);

  Future<void> clear() => _storage.deleteAll();
}
