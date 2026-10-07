import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';

/// Every environment value the app needs, read from `assets/config.json`.
///
/// `main()` calls [initialize] before the first frame so the bundled file is
/// loaded once. Reading a value before then — or when the file is missing or
/// malformed — falls back to the platform defaults below rather than throwing,
/// so a configuration problem degrades into a wrong host instead of a crash.
abstract final class AppConfig {
  static Map<String, dynamic>? _config;

  /// Loads `assets/config.json`. Safe to call more than once.
  static Future<void> initialize() async {
    if (_config != null) return;
    try {
      final raw = await rootBundle.loadString('assets/config.json');
      final decoded = jsonDecode(raw);
      if (decoded is! Map) {
        throw const FormatException('config.json must contain a JSON object');
      }
      _config = Map<String, dynamic>.from(decoded);
    } catch (error) {
      if (kDebugMode) {
        debugPrint(
          'AppConfig: assets/config.json unusable ($error); '
          'falling back to defaults.',
        );
      }
      _config = _defaults;
    }
  }

  /// Drops the loaded configuration so a test can load it again.
  @visibleForTesting
  static void resetForTesting() => _config = null;

  static Map<String, dynamic> get _defaults => {
    'API_BASE_URL': defaultTargetPlatform == TargetPlatform.android
        ? 'https://10.0.2.2:9900/'
        : 'https://localhost:9900/',
    'API_CLIENT_ID': 'SmartSheep',
    'API_CLIENT_SECRET': '',
    'ALLOW_DEV_CERTIFICATE': kDebugMode,
  };

  static T _read<T>(String key, T fallback) {
    final value = (_config ?? _defaults)[key];
    return value is T ? value : fallback;
  }

  static String get apiBaseUrl {
    final configured = _read('API_BASE_URL', '');
    return configured.isNotEmpty
        ? configured
        : _defaults['API_BASE_URL'] as String;
  }

  static String get clientId => _read('API_CLIENT_ID', 'SmartSheep');

  static String get clientSecret => _read('API_CLIENT_SECRET', '');

  static bool get allowDevelopmentCertificate =>
      _read('ALLOW_DEV_CERTIFICATE', kDebugMode);
}
