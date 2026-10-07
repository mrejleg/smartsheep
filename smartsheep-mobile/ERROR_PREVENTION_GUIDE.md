# SmartSheep Mobile - Error Prevention Guide

## Common Issues & Solutions

### ❌ Error 1: "Unable to connect to the SmartSheep server"

**Cause:** API URL di `assets/config.json` tidak sesuai dengan target platform

**Details:**
- **localhost** = hanya bisa diakses dari iOS Simulator (simulator berjalan di Mac)
- **10.0.2.2** = Android emulator yang akses host machine (Mac/Windows yang run emulator)
- **Production URL** = URL real API server

**Solution:**

✅ **For iOS Simulator:**
```json
{
  "API_BASE_URL": "https://localhost:9900/",
  ...
}
```

✅ **For Android Emulator:**
```json
{
  "API_BASE_URL": "https://10.0.2.2:9900/",
  ...
}
```

✅ **For Production:**
```json
{
  "API_BASE_URL": "https://api.smartsheep.com/",
  ...
}
```

**Prevention:** 
- Always use correct URL untuk platform yang sedang ditest
- Dokumentasi di config file sendiri

---

### ❌ Error 2: "The application login configuration is incomplete"

**Cause 1 - StateError:** AppConfig not initialized

**Solution:**
- Pastikan `AppConfig.initialize()` dipanggil di `main()` SEBELUM Firebase setup
- File sudah fix di `lib/main.dart`

**Cause 2 - API Error:** Server validation error untuk ClientId/ClientSecret

**Solution:**
- Verify `API_CLIENT_ID` dan `API_CLIENT_SECRET` di `assets/config.json`
- Pastikan nilai sudah sesuai dengan yang di server
- Test API credentials dengan Postman/Insomnia

**Prevention:**
```dart
// lib/main.dart - Ensure correct order
Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await AppConfig.initialize();  // ← MUST be first
  try {
    await Firebase.initializeApp(...);  // Firebase after config
    ...
```

---

### ❌ Error 3: "ALLOW_DEV_CERTIFICATE validation failed"

**Cause:** Setting `ALLOW_DEV_CERTIFICATE: false` saat dev dengan self-signed certificate

**Solution:**

✅ **For Development (self-signed cert):**
```json
{
  "ALLOW_DEV_CERTIFICATE": true,
  ...
}
```

✅ **For Production (real cert):**
```json
{
  "ALLOW_DEV_CERTIFICATE": false,
  ...
}
```

**Prevention:**
- Dev: Always use `true`
- Prod: Always use `false`
- Add comment di config file

---

## Configuration Best Practices

### 1. **Create Environment-Specific Config Files**

```
assets/
├── config.json              (git-tracked, default/template)
├── config.dev.json          (development)
├── config.staging.json      (staging)
└── config.prod.json         (production)
```

Then load based on build flavor:
```dart
static Future<void> initialize() async {
  final flavor = const String.fromEnvironment('FLAVOR', defaultValue: 'dev');
  final configFile = 'assets/config.$flavor.json';
  ...
}
```

Build dengan flavor:
```bash
flutter run --flavor dev
flutter run --flavor staging
flutter build apk --flavor prod
```

### 2. **Add Configuration Validation**

```dart
static Future<void> initialize() async {
  ...
  // Validate required fields
  final requiredFields = ['API_BASE_URL', 'API_CLIENT_ID', 'API_CLIENT_SECRET'];
  for (final field in requiredFields) {
    if (_config[field]?.toString().isEmpty == true) {
      throw StateError('Missing required config: $field');
    }
  }
}
```

### 3. **Add Logging for Debugging**

```dart
static Future<void> initialize() async {
  ...
  if (kDebugMode) {
    print('🔧 Config loaded:');
    print('  API URL: ${_config["API_BASE_URL"]}');
    print('  Client ID: ${_config["API_CLIENT_ID"]}');
    print('  Dev Cert: ${_config["ALLOW_DEV_CERTIFICATE"]}');
  }
}
```

### 4. **Gitignore Strategy**

```bash
# .gitignore
# Environment-specific configs dengan secrets
assets/config.prod.json
assets/config.staging.json

# Tapi git-track template untuk dev
!assets/config.dev.json
assets/config.json  # Template/fallback
```

### 5. **Add Config Reset for Testing**

```dart
@visibleForTesting
static void resetForTesting() {
  _initialized = false;
  _config = {};
}
```

Usage di tests:
```dart
test('Login flow', () async {
  AppConfig.resetForTesting();
  await AppConfig.initialize();
  // ... test ...
});
```

---

## Debugging Checklist

### Jika App Connection Error:

- [ ] Pastikan API server running di `https://10.0.2.2:9900/` (atau URL yang sesuai)
- [ ] Verify URL di `assets/config.json`
- [ ] Check platform:
  - iOS Simulator → gunakan `localhost`
  - Android Emulator → gunakan `10.0.2.2`
- [ ] Pastikan ALLOW_DEV_CERTIFICATE = `true` untuk dev
- [ ] Check network connectivity: Buka browser di emulator, test URL

### Jika Login Error "Configuration Incomplete":

- [ ] Verify CLIENT_ID dan CLIENT_SECRET di `assets/config.json`
- [ ] Test credentials dengan API client (Postman)
- [ ] Check Firebase initialization (lihat logs)
- [ ] Pastikan `AppConfig.initialize()` dipanggil di main.dart

### Jika Build Error:

- [ ] `flutter clean`
- [ ] `flutter pub get`
- [ ] Delete build folder: `rm -rf build/`
- [ ] Rebuild: `flutter run`

---

## Quick Reference: Config by Platform

| Platform | API_BASE_URL | ALLOW_DEV_CERTIFICATE | Notes |
|----------|-------------|----------------------|-------|
| iOS Simulator | `https://localhost:9900/` | `true` | Simulator runs on Mac |
| Android Emulator | `https://10.0.2.2:9900/` | `true` | Emulator needs special host IP |
| Physical Device (Dev) | `https://[YOUR-IP]:9900/` | `true` | Your dev machine IP |
| Staging Server | `https://staging-api.smartsheep.com/` | `true` | Staging might have self-signed cert |
| Production | `https://api.smartsheep.com/` | `false` | Real certificate |

---

## Complete Example Config Files

### Development (Android Emulator)
```json
{
  "API_BASE_URL": "https://10.0.2.2:9900/",
  "API_CLIENT_ID": "SmartSheep",
  "API_CLIENT_SECRET": "dev_secret_key_here",
  "ALLOW_DEV_CERTIFICATE": true
}
```

### Staging
```json
{
  "API_BASE_URL": "https://api-staging.smartsheep.com/",
  "API_CLIENT_ID": "SmartSheep",
  "API_CLIENT_SECRET": "staging_secret_key",
  "ALLOW_DEV_CERTIFICATE": true
}
```

### Production
```json
{
  "API_BASE_URL": "https://api.smartsheep.com/",
  "API_CLIENT_ID": "SmartSheep",
  "API_CLIENT_SECRET": "prod_secret_key_from_env_var",
  "ALLOW_DEV_CERTIFICATE": false
}
```

---

## Summary: Jangan Sampai Terjadi Lagi

✅ **DO:**
- Gunakan URL yang sesuai platform (localhost untuk iOS, 10.0.2.2 untuk Android)
- Ganti config.json sesuai environment yang ditest
- Selalu validate credentials dengan server
- Call `AppConfig.initialize()` di main.dart sebelum everything else
- Add logging untuk debug configuration issues

❌ **DON'T:**
- Hardcode URL di source code (sudah pindah ke config.json ✓)
- Forget to update config.json saat switch platform/environment
- Use `localhost` di Android Emulator
- Call AppConfig getters sebelum initialize()
- Mix environment configs

---

## Next Steps

1. ✅ Config JSON sudah terpusat
2. ✅ Platform-specific URL handling
3. ⏳ TODO: Add environment-specific config files (dev/staging/prod)
4. ⏳ TODO: Add config validation saat initialize
5. ⏳ TODO: Add config logging untuk debugging

---

**Last Updated:** Sep 17, 2026
**Status:** All critical errors fixed ✅
