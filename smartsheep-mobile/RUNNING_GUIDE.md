# SmartSheep Mobile — Running Guide

## Jalankan

```bash
flutter pub get
flutter run
```

Emulator Android harus hidup dulu:

```bash
flutter emulators --launch Medium_Phone_API_36.0
```

iOS Simulator:

```bash
open -a Simulator
```

## Konfigurasi

Semua nilai environment ada di satu file: `assets/config.json`.

```json
{
  "API_BASE_URL": "https://10.0.2.2:9900/",
  "API_CLIENT_ID": "SmartSheep",
  "API_CLIENT_SECRET": "...",
  "ALLOW_DEV_CERTIFICATE": true
}
```

Ubah `API_BASE_URL` sesuai target — ini penyebab paling sering error
"Unable to connect to the SmartSheep server":

| Target | API_BASE_URL | ALLOW_DEV_CERTIFICATE |
|---|---|---|
| Android Emulator | `https://10.0.2.2:9900/` | `true` |
| iOS Simulator | `https://localhost:9900/` | `true` |
| HP fisik (dev) | `https://<IP-laptop>:9900/` | `true` |
| Production | `https://api.smartsheep.com/` | `false` |

`10.0.2.2` adalah alamat khusus emulator Android untuk menunjuk ke laptop
host. `localhost` di dalam emulator menunjuk ke emulator itu sendiri, bukan
ke server Anda.

File ini ikut ter-bundle saat publish, jadi berlaku juga untuk build release.
Setelah mengubahnya cukup restart app (`R` di terminal `flutter run`) — tidak
perlu rebuild.

Jika file hilang atau rusak, app memakai nilai default per-platform dan
mencatat peringatan di debug log — tidak crash.

## Build release

```bash
flutter build apk --release
```

```bash
flutter build ios --release
```

## Cek sebelum commit

```bash
flutter analyze && flutter test
```

## Troubleshooting

| Gejala | Penyebab & solusi |
|---|---|
| "Unable to connect to the SmartSheep server" | `API_BASE_URL` tidak cocok dengan target — lihat tabel di atas |
| "The application login configuration is incomplete" | `API_CLIENT_ID`/`API_CLIENT_SECRET` kosong atau ditolak server |
| Perubahan kode tidak muncul | `flutter clean && flutter pub get && flutter run` |
