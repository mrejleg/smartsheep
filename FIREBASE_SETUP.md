# Panduan Setup Firebase SmartSheep — Dari Awal sampai Test Reminder

Tanggal dokumentasi: 14 September 2026.

Panduan ini mengikuti proses yang dilakukan pada Firebase Console dan struktur source code SmartSheep. Nama menu dapat berubah mengikuti pembaruan Firebase/Apple. Nilai private key sengaja tidak disalin ke dokumentasi; gunakan file JSON credential yang diunduh dari akun pemilik project.

## Daftar isi

1. [Tujuan, alur, dan status terakhir](#1-tujuan-alur-dan-status-terakhir)
2. [Persiapan](#2-persiapan)
3. [Membuat project Firebase](#3-membuat-project-firebase)
4. [Mendaftarkan aplikasi Android](#4-mendaftarkan-aplikasi-android)
5. [Mendaftarkan aplikasi iOS](#5-mendaftarkan-aplikasi-ios)
6. [Memeriksa Cloud Messaging](#6-memeriksa-cloud-messaging)
7. [Membuat credential Firebase Admin](#7-membuat-credential-firebase-admin)
8. [Memasang credential ke appsettings API](#8-memasang-credential-ke-appsettings-api)
9. [Menyamakan konfigurasi Flutter](#9-menyamakan-konfigurasi-flutter)
10. [Setup APNs Apple untuk iPhone](#10-setup-apns-apple-untuk-iphone)
11. [Menjalankan API, Web, dan mobile](#11-menjalankan-api-web-dan-mobile)
12. [Registrasi perangkat dan migrasi token](#12-registrasi-perangkat-dan-migrasi-token)
13. [Pengujian Test Reminder dari Web](#13-pengujian-test-reminder-dari-web)
14. [Troubleshooting](#14-troubleshooting)
15. [Checklist penyelesaian dan keamanan](#15-checklist-penyelesaian-dan-keamanan)

## 1. Tujuan, alur, dan status terakhir

### Tujuan

Mengirim isi Reminder Template dari database SmartSheep ke aplikasi mobile melalui Firebase Cloud Messaging (FCM).

```text
Mobile login → memperoleh FCM token → RegisterDevice → database UserDeviceToken

Web: Configs → Reminder Templates → Test Reminder → Send Test
  → API membaca template dan token milik user yang login
  → Firebase Admin SDK menggunakan credential server
  → FCM
      ├─ Android → aplikasi Android
      └─ APNs Apple → aplikasi iOS
```

Web adalah pemicu pengiriman. Pada alur ini, Chrome bukan penerima push. Tidak perlu memasang private key Firebase di Web, membuat service worker Web, atau mengaktifkan Firebase Authentication untuk login SmartSheep.

### Identitas project yang sudah dibuat

| Item | Nilai |
| --- | --- |
| Akun Google saat setup | `smartsheep.ipb@gmail.com` |
| Nama project | `SmartSheep` |
| Project ID | `smartsheep-45a28` |
| Project number / Messaging Sender ID | `977390546151` |
| Plan saat setup | Spark |
| Google Analytics saat setup | Tidak diaktifkan |
| Android app nickname | `SmartSheep Android` |
| Android package name | `com.pertamina.smartsheep_mobile` |
| Android Firebase App ID | `1:977390546151:android:510be98ea1693cf462c642` |
| iOS app nickname | `SmartSheep iOS` |
| iOS Bundle ID | `com.pertamina.smartsheepMobile` |
| iOS Firebase App ID | `1:977390546151:ios:0dceb56bd8bf5ca062c642` |
| Firebase service account | `firebase-adminsdk-fbsvc@smartsheep-45a28.iam.gserviceaccount.com` |

Buka [project SmartSheep di Firebase Console](https://console.firebase.google.com/project/smartsheep-45a28/overview). Jika project tidak terlihat, periksa akun Google yang aktif melalui avatar kanan atas. Nomor akun `/u/3/` pada URL saat setup hanya urutan sesi Chrome, bukan identitas project yang harus diikuti di komputer lain.

### Hasil yang benar-benar sudah diverifikasi

- Project, aplikasi Android, dan aplikasi iOS berhasil dibuat.
- Kedua file konfigurasi mobile berhasil diunduh.
- Service-account private key berhasil dibuat dan dipasang ke `appsettings.json` API.
- `firebase_options.dart`, Android JSON, iOS plist, dan API menggunakan project yang sama.
- Cloud Messaging API v1 berstatus Enabled.
- API direstart; mobile berhasil dibuild ulang di iPhone 17 Pro Simulator.
- `flutter analyze lib/firebase_options.dart` lulus.
- Tombol Test Reminder pada Web benar-benar ditekan dan pengiriman diproses Firebase.

**Belum selesai:** penerimaan push di iPhone. Firebase Console belum memiliki APNs authentication key/certificate. Hasil uji terakhir mencatat satu `ThirdPartyAuthError` dengan pesan `Invalid APNs credential`, serta tiga `SenderIdMismatch` untuk token project lama. Belum ada pengiriman sukses ke perangkat pada pengujian tersebut. Token lama belum dihapus atau dinonaktifkan melalui pekerjaan setup ini.

Bagian APNs dan checklist penerimaan di bawah adalah langkah lanjutan, bukan klaim bahwa langkah tersebut sudah dilakukan.

## 2. Persiapan

Siapkan:

- Chrome dengan akun Google pemilik/administrator project Firebase.
- Source code SmartSheep API, Web, dan `smartsheep-mobile`.
- Database lokal yang dipakai API, berikut akun login aplikasi yang sudah berfungsi.
- .NET SDK sesuai project; Flutter SDK dan dependencies sesuai lockfile repository.
- Xcode dan CocoaPods untuk build iOS; Android SDK/emulator untuk Android.
- Akses Apple Developer yang dapat mengelola identifier, signing, dan APNs key jika menguji iPhone.
- Koneksi internet dari API dan perangkat ke layanan Google/Apple.

Semua path relatif dalam panduan ini dihitung dari root repository `smartsheep`, kecuali dinyatakan lain. Contoh terminal memakai macOS/zsh.

### Bedakan empat jenis konfigurasi

| File / nilai | Dipakai oleh | Fungsi | Lokasi pemasangan |
| --- | --- | --- | --- |
| Service-account JSON, berisi `private_key` | Backend API | Mengautentikasi server ke Firebase | Section `Firebase` pada appsettings API |
| `google-services.json` | Android | Identitas project/aplikasi Android | `smartsheep-mobile/android/app/` |
| `GoogleService-Info.plist` | iOS | Identitas project/aplikasi iOS | `smartsheep-mobile/ios/Runner/` |
| APNs `.p8`, Key ID, Team ID | Firebase → Apple | Mengizinkan Firebase mengirim ke APNs | Firebase Console → Cloud Messaging → Apple app configuration |
| FCM registration token | API dan FCM | Alamat satu instalasi aplikasi | Database `UserDeviceToken`, melalui RegisterDevice |

`apiKey` pada FirebaseOptions **bukan** private key server. APNs `.p8` juga **bukan** pengganti `Firebase:Credential:PrivateKey`. Jangan memasukkan service-account JSON ke Flutter assets, APK/IPA, JavaScript, atau folder `wwwroot`.

## 3. Membuat project Firebase

Jika melanjutkan konfigurasi yang sudah dibuat, buka project `smartsheep-45a28`; jangan membuat project baru lagi.

Untuk setup benar-benar dari awal:

1. Buka [Firebase Console](https://console.firebase.google.com/).
2. Klik avatar kanan atas, pilih akun `smartsheep.ipb@gmail.com`.
3. Klik tombol pembuatan project, misalnya **Create a project / Add project**.
4. Isi nama project `SmartSheep`.
5. Periksa Project ID yang disediakan wizard. Catat ID aktual, bukan hanya nama tampilannya.
6. Jika muncul Terms of Service, pemilik akun membaca dan menyetujuinya sebelum melanjutkan.
7. Pada pilihan Google Analytics, nonaktifkan Analytics untuk mengikuti setup lokal ini. Analytics tidak digunakan oleh kode Test Reminder SmartSheep.
8. Jika wizard sudah masuk halaman konfigurasi Analytics, klik **Previous**, lalu nonaktifkan Analytics.
9. Klik **Create project**.
10. Tunggu sampai proses selesai, lalu klik **Continue** menuju **Project Overview**.
11. Periksa nama project dan plan. Setup yang sudah dilakukan memakai Spark tanpa upgrade billing.

Hasil proses sebelumnya adalah Project ID `smartsheep-45a28`. Jika membuat project berbeda, seluruh contoh Project ID, Sender ID, App ID, credential, dan file konfigurasi dalam panduan harus mengikuti project baru itu. Project ID tidak dapat diganti setelah project dibuat. Lihat [alur resmi pembuatan project dan registrasi Android](https://firebase.google.com/docs/android/setup).

## 4. Mendaftarkan aplikasi Android

### 4.1 Menu dan pengisian wizard

1. Buka **Project Overview**.
2. Klik **Add app**.
3. Pilih ikon Android, dengan label **Create an Android app**.
4. Pada langkah **Register app**, isi:

   | Field | Nilai SmartSheep |
   | --- | --- |
   | Android package name | `com.pertamina.smartsheep_mobile` |
   | App nickname (optional) | `SmartSheep Android` |

5. Klik **Register app** dan tunggu selesai.
6. Pada langkah **Download and then add config file**, klik **Download google-services.json**.
7. Simpan file unduhan. Pastikan file yang dipakai bukan milik project lama.
8. Pada wizard, langkah berikutnya adalah **Add Firebase SDK** dan **Next steps**. Karena SmartSheep sudah memiliki dependency/plugin, tidak perlu menambahkan ulang snippet native dari wizard. Setelah unduhan tersimpan, wizard dapat ditutup untuk melanjutkan konfigurasi lokal.

Package name di atas berasal dari `applicationId` dalam `smartsheep-mobile/android/app/build.gradle.kts`. Jangan menyamakannya secara paksa dengan Bundle ID iOS; keduanya memang berbeda.

### 4.2 Pemasangan file dan plugin

1. Backup file konfigurasi Android lama di lokasi privat jika sudah ada.
2. Pasang isi file hasil unduhan sebagai:

   ```text
   smartsheep-mobile/android/app/google-services.json
   ```

3. Nama file harus persis `google-services.json`, tanpa tambahan `(1)` atau `(2)`.
4. Periksa nilai berikut, tanpa perlu membagikan seluruh isi file:

   ```text
   project_info.project_id = smartsheep-45a28
   project_info.project_number = 977390546151
   client[].client_info.android_client_info.package_name = com.pertamina.smartsheep_mobile
   ```

5. Pada repository ini, `android/settings.gradle.kts` sudah mendeklarasikan plugin `com.google.gms.google-services`, dan `android/app/build.gradle.kts` sudah menerapkannya. Pertahankan versi yang ada; tidak perlu menyalin versi berbeda dari wizard.
6. Gunakan perangkat/emulator dengan Google Play services/Google APIs untuk pengujian FCM. Ikuti minimum SDK project yang sedang digunakan, bukan minimum dari contoh lama.

## 5. Mendaftarkan aplikasi iOS

### 5.1 Menu dan pengisian wizard

1. Kembali ke **Project Overview → Add app**.
2. Pilih ikon Apple, label **Create an Apple app**.
3. Pada langkah **Register app**, isi:

   | Field | Nilai SmartSheep |
   | --- | --- |
   | Apple bundle ID | `com.pertamina.smartsheepMobile` |
   | App nickname (optional) | `SmartSheep iOS` |
   | App Store ID (optional) | Kosong untuk setup lokal ini |

4. Klik **Register app**.
5. Pada langkah **Download config file**, klik **Download GoogleService-Info.plist**.
6. Simpan file. Wizard berikutnya berisi **Add Firebase SDK**, **Add initialization code**, dan **Next steps**. SmartSheep sudah menginisialisasi Firebase lewat Flutter; jangan menggandakannya dengan menyalin seluruh contoh native ke AppDelegate.

### 5.2 Pemasangan file

1. Backup file lama jika ada.
2. Pasang file sebagai:

   ```text
   smartsheep-mobile/ios/Runner/GoogleService-Info.plist
   ```

3. Buka `smartsheep-mobile/ios/Runner.xcworkspace` di Xcode.
4. Pastikan file terlihat pada project Runner dan termasuk target **Runner**. Untuk project baru, gunakan **Add Files to “Runner”…** bila referensinya belum ada. Pada project existing, jangan membuat referensi duplikat.
5. Periksa `PROJECT_ID`, `GCM_SENDER_ID`, `GOOGLE_APP_ID`, dan `BUNDLE_ID` sesuai tabel identitas di awal.

Registrasi dan file konfigurasi mengikuti [panduan Firebase untuk Apple](https://firebase.google.com/docs/ios/setup). Registrasi aplikasi iOS saja belum cukup untuk push: lanjutkan bagian APNs.

## 6. Memeriksa Cloud Messaging

1. Pada navigasi kiri Firebase, buka **Settings → General**.
2. Di halaman **Project settings**, pilih tab **Cloud Messaging**.
3. Periksa bagian **Firebase Cloud Messaging API (V1)**.
4. Status yang diharapkan: **Enabled**.
5. Cocokkan **Sender ID** dengan `977390546151` untuk project ini.
6. **Cloud Messaging API (Legacy)** boleh tetap Disabled. Kode SmartSheep menggunakan Firebase Admin SDK, bukan legacy server key.
7. Pada **Apple app configuration**, pilih `SmartSheep iOS` dan periksa APNs. Saat dokumentasi dibuat, development/production key dan certificate masih kosong.
8. Bagian **Web configuration → Web Push certificates** tidak perlu diisi untuk tombol Web yang mengirim push ke mobile. VAPID baru dibutuhkan jika browser sendiri akan menjadi penerima push; fitur tersebut berada di luar setup saat ini.

Pada setup ini, FCM v1 sudah otomatis Enabled; tidak ada langkah aktivasi manual atau upgrade billing yang dilakukan.

## 7. Membuat credential Firebase Admin

### 7.1 Jalur menu yang digunakan

1. Buka **Settings → Service accounts**. Alternatif: dari **Project settings**, pilih tab **Service accounts**.
2. Pada navigasi bagian tersebut, pilih **Firebase Admin SDK**.
3. Periksa baris **Firebase service account**:

   ```text
   firebase-adminsdk-fbsvc@smartsheep-45a28.iam.gserviceaccount.com
   ```

4. Klik **Generate new private key**.
5. Dialog **Generate new private key** menjelaskan bahwa key memberi akses ke layanan Firebase project.
6. Setelah pemilik akun menyetujui pembuatan credential, klik **Generate key**.
7. Browser mengunduh JSON service account. Simpan di lokasi privat; jangan unggah ke repository publik.

Credential server berasal dari JSON ini, bukan dari snippet Node.js yang ditampilkan di halaman. Bahasa snippet tidak mengubah isi credential. Lihat [Firebase Admin SDK setup](https://firebase.google.com/docs/admin/setup).

### 7.2 File yang dihasilkan saat setup ini

```text
/Users/swa/Downloads/smartsheep-45a28-firebase-adminsdk-fbsvc-93b2b6ddd3.json
```

File ini telah dibatasi permission lokal menjadi `600`. Lokasi di atas hanya catatan mesin setup, bukan path yang harus ada di komputer lain.

Pada setup saat ini, API membaca field dari appsettings. API tidak membaca file Downloads tersebut secara langsung. Jangan mengira cukup mengisi nama file atau menetapkan `GOOGLE_APPLICATION_CREDENTIALS` saja: `FirebasePushService` SmartSheep sekarang membentuk credential secara eksplisit dari `AppSetting`.

## 8. Memasang credential ke appsettings API

### 8.1 File yang benar

Edit file API berikut, bukan appsettings Web:

```text
SmartSheep/src/02.Applications/01.WebApi/04.Api/appsettings.json
```

Sebelum edit:

1. Buat backup lokal privat dari file existing.
2. Jangan mengganti keseluruhan appsettings dengan JSON Firebase.
3. Pertahankan `ConnectionStrings`, `Security`, `Jwt`, konfigurasi aplikasi, dan section lain.
4. Ubah hanya section `Firebase`.
5. Periksa `appsettings.Development.json` di folder API yang sama. Section Firebase di file environment tersebut dapat menimpa sebagian nilai base.
6. Periksa juga override environment seperti `Firebase__ProjectId` dan `Firebase__Credential__PrivateKey` jika pernah dipasang. Jangan mencetak seluruh environment karena dapat berisi secret lain.

Pada saat setup ini, `appsettings.Development.json` tidak memiliki override Firebase. Konfigurasi baru dipasang pada base `appsettings.json`.

### 8.2 Pemetaan lengkap field

Salin nilai dari satu JSON service account yang sama. Jangan mencampur email/key dari project lama.

| Field dalam JSON unduhan | Path pada appsettings API |
| --- | --- |
| `project_id` | `Firebase:ProjectId` |
| `type` | `Firebase:Credential:Type` |
| `private_key_id` | `Firebase:Credential:PrivateKeyId` |
| `private_key` | `Firebase:Credential:PrivateKey` |
| `client_email` | `Firebase:Credential:ClientEmail` |
| `client_id` | `Firebase:Credential:ClientId` |
| `auth_uri` | `Firebase:Credential:AuthUri` |
| `token_uri` | `Firebase:Credential:TokenUri` |
| `auth_provider_x509_cert_url` | `Firebase:Credential:AuthProviderX509CertUrl` |
| `client_x509_cert_url` | `Firebase:Credential:ClientX509CertUrl` |
| `universe_domain` | `Firebase:Credential:UniverseDomain` |

### 8.3 Contoh section Firebase

Contoh berikut valid sebagai struktur JSON, tetapi placeholder harus diganti dengan nilai asli dari file unduhan. Merge section ini ke file existing; jangan menghapus section lain.

```json
{
  "Firebase": {
    "ProjectId": "smartsheep-45a28",
    "Credential": {
      "Type": "service_account",
      "PrivateKeyId": "ISI_DARI_private_key_id",
      "PrivateKey": "-----BEGIN PRIVATE KEY-----\nISI_PRIVATE_KEY_ASLI\n-----END PRIVATE KEY-----\n",
      "ClientEmail": "firebase-adminsdk-fbsvc@smartsheep-45a28.iam.gserviceaccount.com",
      "ClientId": "ISI_DARI_client_id",
      "AuthUri": "https://accounts.google.com/o/oauth2/auth",
      "TokenUri": "https://oauth2.googleapis.com/token",
      "AuthProviderX509CertUrl": "https://www.googleapis.com/oauth2/v1/certs",
      "ClientX509CertUrl": "ISI_DARI_client_x509_cert_url",
      "UniverseDomain": "googleapis.com"
    }
  }
}
```

### 8.4 Aturan penyalinan private key

- Copy nilai `private_key` lengkap, termasuk baris BEGIN, END, dan newline.
- Dalam teks JSON, newline ditulis sebagai escape `\n`; setelah JSON diparse, nilainya menjadi newline sebenarnya.
- Jangan menggantinya menjadi `\\n` karena akan menghasilkan karakter backslash dan huruf n, bukan newline.
- Jangan memasukkan newline mentah di tengah JSON string satu nilai.
- Jangan mengganti dengan API key Android/iOS, APNs key, atau seluruh JSON service account.
- Jangan menambahkan komentar atau trailing comma ke contoh JSON.

### 8.5 Validasi tanpa menampilkan secret

Jalankan dari root repository:

```sh
ruby -rjson -ropenssl -e '
settings = JSON.parse(File.read("SmartSheep/src/02.Applications/01.WebApi/04.Api/appsettings.json"))
firebase = settings.fetch("Firebase")
credential = firebase.fetch("Credential")
required = %w[Type PrivateKeyId PrivateKey ClientEmail ClientId AuthUri TokenUri AuthProviderX509CertUrl ClientX509CertUrl UniverseDomain]
abort "Ada field credential kosong" unless required.all? { |key| !credential[key].to_s.empty? }
abort "Project ID kosong" if firebase["ProjectId"].to_s.empty?
abort "Private key tidak valid" unless OpenSSL::PKey.read(credential.fetch("PrivateKey")).private?
puts "Struktur JSON dan private key valid; secret tidak ditampilkan."
'
```

Validasi ini memeriksa format, **bukan** membuktikan key masih aktif atau dapat mengirim push. Pembuktian koneksi dilakukan dengan Test Reminder.

### 8.6 Cara konfigurasi dipakai di source

- `04.Api/Program.cs` membaca `Firebase:*` ke `AppSetting`.
- `04.Api/Services/FirebasePushService.cs` membentuk service-account JSON, membuat Firebase app, lalu mengirim multicast melalui Admin SDK.
- Firebase messaging diinisialisasi secara lazy: API dapat menyala walaupun credential bermasalah. Karena itu startup sukses saja bukan bukti FCM sukses.
- `AppSetting` dan Firebase service dipakai sebagai singleton. **Restart proses API** setelah mengganti credential, jangan hanya reload browser.

## 9. Menyamakan konfigurasi Flutter

### 9.1 Tiga file mobile harus konsisten

Selain kedua file native yang sudah dipasang, edit:

```text
smartsheep-mobile/lib/firebase_options.dart
```

`main.dart` menggunakan `DefaultFirebaseOptions.currentPlatform`. Mengganti hanya `google-services.json` atau plist belum cukup bila Dart masih berisi project lama.

Pemetaan nilai:

| FirebaseOptions | Sumber Android JSON | Sumber iOS plist |
| --- | --- | --- |
| `apiKey` | `client` yang package-nya cocok → `api_key[].current_key` | `API_KEY` |
| `appId` | `client_info.mobilesdk_app_id` | `GOOGLE_APP_ID` |
| `messagingSenderId` | `project_info.project_number` | `GCM_SENDER_ID` |
| `projectId` | `project_info.project_id` | `PROJECT_ID` |
| `storageBucket` | `project_info.storage_bucket` | `STORAGE_BUCKET` |
| `iosBundleId` | Tidak diisi untuk Android | `BUNDLE_ID` |

Contoh nilai yang diharapkan pada project saat ini; `apiKey` diambil dari masing-masing file unduhan:

```dart
static const FirebaseOptions android = FirebaseOptions(
  apiKey: 'ISI_API_KEY_ANDROID_DARI_GOOGLE_SERVICES_JSON',
  appId: '1:977390546151:android:510be98ea1693cf462c642',
  messagingSenderId: '977390546151',
  projectId: 'smartsheep-45a28',
  storageBucket: 'smartsheep-45a28.firebasestorage.app',
);

static const FirebaseOptions ios = FirebaseOptions(
  apiKey: 'ISI_API_KEY_IOS_DARI_PLIST',
  appId: '1:977390546151:ios:0dceb56bd8bf5ca062c642',
  messagingSenderId: '977390546151',
  projectId: 'smartsheep-45a28',
  storageBucket: 'smartsheep-45a28.firebasestorage.app',
  iosBundleId: 'com.pertamina.smartsheepMobile',
);
```

API key Android dan iOS tidak harus sama. Yang harus konsisten adalah project/sender dan kecocokan App ID dengan platformnya. Nama storage bucket pada konfigurasi bukan bukti Firebase Storage sudah diprovision; Test Reminder tidak menggunakan Storage.

### 9.2 Inisialisasi yang sudah ada

Pada `smartsheep-mobile/lib/main.dart` sudah tersedia:

- `Firebase.initializeApp(options: DefaultFirebaseOptions.currentPlatform)`.
- Background message handler.
- Foreground presentation untuk alert, badge, dan sound iOS.
- Listener Android foreground yang menampilkan isi notification melalui SnackBar.

`auth_controller.dart` sudah meminta permission notifikasi, memperoleh token, dan meregistrasikannya ke API. Jangan menambahkan handler yang sama dua kali.

Untuk source existing, jalankan `flutter pub get`, bukan upgrade semua package tanpa kebutuhan. Firebase native dependency mengikuti konfigurasi Flutter project. Setelah mengganti project atau file native, lakukan full rebuild/relaunch, bukan hot reload saja.

## 10. Setup APNs Apple untuk iPhone

**Status bagian ini: belum dilakukan pada setup terakhir, karena file `.p8`, Key ID, dan Team ID belum tersedia.** Firebase service-account key sudah benar, tetapi tidak menggantikan credential APNs.

### 10.1 Persiapan Apple Developer dan identifier

1. Masuk ke [Apple Developer Account](https://developer.apple.com/account/) dengan akun pengelola aplikasi.
2. Pastikan akun/team memiliki akses yang diperlukan untuk mengelola signing dan push. Akun Google Firebase tidak otomatis menjadi akun Apple Developer.
3. Buka **Certificates, Identifiers & Profiles → Identifiers**.
4. Temukan App ID untuk `com.pertamina.smartsheepMobile`. Jika belum ada, administrator perlu mendaftarkan identifier untuk bundle tersebut.
5. Pastikan kapabilitas **Push Notifications** tersedia/diaktifkan untuk App ID yang digunakan.
6. Catat **Team ID** dari informasi membership akun/team Apple. Team ID bukan Project ID Firebase, bukan bundle identifier, dan bukan Apple ID/email.

### 10.2 Membuat dan mengunduh APNs key

1. Buka **Certificates, Identifiers & Profiles → Keys → +**.
2. Isi nama key, misalnya `SmartSheep APNs Development`.
3. Pilih **Apple Push Notification service (APNs)**, lalu **Configure** jika tersedia.
4. Pilih environment yang sesuai, serta tipe **Team Scoped** atau **Topic Specific**. Jika Topic Specific, pilih topic/bundle SmartSheep saja. Jangan memilih akses lebih luas tanpa kebutuhan.
5. Untuk build debug/development, siapkan key environment development/sandbox. Untuk distribusi production, siapkan credential production yang sesuai. Jangan menganggap semua key baru otomatis berlaku untuk kedua environment.
6. Ikuti **Continue**, review konfigurasi, lalu **Confirm** sesuai wizard.
7. Klik **Download** dan simpan `.p8`; catat **Key ID**.
8. Klik **Done**. Simpan file dengan aman karena unduhan key tidak dapat diulang setelah sudah diunduh.

Jika memerlukan pasangan environment untuk Topic Specific, halaman detail key menyediakan alur **Edit → Create Related Key** sesuai dukungan akun. Rujukan langkah dan pilihan terbaru: [Apple — Create a private key](https://developer.apple.com/help/account/keys/create-a-private-key/).

### 10.3 Mengunggah APNs key ke Firebase

1. Buka project SmartSheep di Firebase.
2. Masuk **Settings → General → Cloud Messaging**.
3. Pada **Apple app configuration**, pilih `SmartSheep iOS` dengan bundle `com.pertamina.smartsheepMobile`.
4. Temukan **APNs Authentication Key**.
5. Klik **Upload** pada development atau production sesuai environment key.
6. Pilih file `.p8`, isi **Key ID** dan **Team ID**, lalu simpan.
7. Pastikan tabel menampilkan key untuk environment tersebut; jangan berhenti hanya setelah membuka dialog upload.

APNs `.p8` diunggah ke Firebase, bukan ke appsettings SmartSheep. Untuk tahap ini minimal credential environment yang diuji harus tersedia. Rujukan: [FCM Flutter — Apple configuration](https://firebase.google.com/docs/cloud-messaging/flutter/get-started#ios).

### 10.4 Memeriksa Xcode

1. Buka `smartsheep-mobile/ios/Runner.xcworkspace`.
2. Pilih project **Runner → TARGETS: Runner → Signing & Capabilities**.
3. Periksa Bundle Identifier dan pilih Team Apple yang benar untuk perangkat fisik/distribusi.
4. Pastikan ada **Push Notifications**.
5. Pastikan **Background Modes** mencakup **Background fetch** dan **Remote notifications**.
6. Periksa signing/provisioning sesuai environment build. Jangan sekadar mengubah entitlement development menjadi production secara manual untuk memaksa pengiriman.

Repository saat ini sudah memiliki `Runner.entitlements` dengan `aps-environment=development` dan `Info.plist` dengan `fetch` serta `remote-notification`. Ini belum membuktikan signing production siap. Firebase Flutter membutuhkan method swizzling pada Apple; jangan menonaktifkannya. Lihat [persyaratan FCM Flutter](https://firebase.google.com/docs/cloud-messaging/flutter/get-started).

### 10.5 Memastikan token tersedia

Pada implementasi SmartSheep, iOS menunggu APNs token sebelum meminta FCM token. Jika muncul log `APNs token is unavailable; FCM registration will retry later.`, periksa permission, signing, environment, dan konektivitas perangkat. Uji akhir sebaiknya juga dilakukan pada iPhone fisik dengan signing yang benar; berhasil menjalankan UI simulator tidak otomatis membuktikan penerimaan push.

## 11. Menjalankan API, Web, dan mobile

Perintah berikut adalah petunjuk saat proses belum berjalan atau setelah proses lama dihentikan dengan baik. Jangan menyalakan instance kedua pada port yang sama. Gunakan terminal terpisah agar ketiga proses tetap hidup.

### 11.1 API — terminal pertama

Dari root repository:

```sh
cd SmartSheep/src/02.Applications/01.WebApi/04.Api
dotnet build
dotnet run --launch-profile Api --no-build
```

Launch profile `Api` memakai environment Development dengan URL:

- API HTTPS: `https://localhost:9900`
- API HTTP: `http://localhost:59909`
- Dokumentasi API: `https://localhost:9900/scalar/`

Jika hanya mengganti appsettings dan binary existing sudah sesuai, proses dapat direstart dengan `dotnet run --launch-profile Api --no-build`. Jangan lupa restart karena credential ditahan instance service.

### 11.2 Web — terminal kedua

Dari root repository:

```sh
cd SmartSheep/src/02.Applications/02.WebApp/04.Web
dotnet build
dotnet run --launch-profile Web --no-build
```

Launch profile `Web` memakai:

- Web HTTPS: `https://localhost:44325`
- Web HTTP: `http://localhost:59105`

Buka API Scalar dan Web dalam satu Chrome tab group bernama `SMARTSHEEP`. Jika Web sudah berjalan dan hanya credential API berubah, tidak wajib restart Web; reload halaman yang dipakai untuk tes.

### 11.3 Mobile — terminal ketiga

Dari root repository:

```sh
cd smartsheep-mobile
flutter pub get
flutter devices
```

Pilih ID perangkat dari hasil `flutter devices`, lalu jalankan:

```sh
ruby tool/run-local.rb -d <device-id>
```

Ganti `<device-id>` sebelum menjalankan. ID simulator yang dipakai pada setup ini:

```sh
ruby tool/run-local.rb -d CC65DF94-A8D7-46F7-98A7-D2FA3F05C10B
```

Helper `tool/run-local.rb` membuat/memakai `.env.local.json` privat untuk credential client API. **Jangan menjalankan plain `flutter run`** karena client secret login lokal tidak ikut dimasukkan. Alternatif setelah file private tersebut tersedia:

```sh
flutter run --dart-define-from-file=.env.local.json -d <device-id>
```

Untuk perubahan Firebase native: tekan `q` pada sesi Flutter lama, lalu jalankan helper lagi. `r` hanya hot reload dan tidak cukup untuk mengganti seluruh konfigurasi native.

Default API URL mobile pada source saat ini adalah `https://10.0.2.2:9900/` untuk Android dan `https://localhost:9900/` untuk platform non-Android. Perangkat fisik memerlukan alamat API yang dapat dijangkau perangkat, bukan localhost perangkat; gunakan `API_BASE_URL` yang sesuai serta HTTPS/certificate yang dipercaya. Jangan menonaktifkan validasi TLS secara global.

## 12. Registrasi perangkat dan migrasi token

### 12.1 Registrasi normal

1. Buka mobile yang sudah dibuild dengan project baru.
2. Login dengan akun aplikasi yang juga akan dipakai pada Web.
3. Izinkan notifikasi saat dialog sistem muncul.
4. Jika pernah ditolak, aktifkan kembali melalui pengaturan notifikasi perangkat untuk SmartSheep.
5. Beri waktu proses registrasi. Source melakukan retry terbatas setelah login, session restore, atau aplikasi kembali ke foreground.

Mobile mengirim:

```text
POST /api/FirebaseNotification/RegisterDevice
```

Body sesuai source:

```json
{
  "DeviceToken": "FCM_TOKEN_DARI_INSTALASI_APLIKASI",
  "Platform": "ios",
  "DeviceName": "NAMA_PERANGKAT"
}
```

Endpoint menentukan user dari autentikasi request, bukan dari username bebas dalam body. API membuat atau memperbarui `UserDeviceToken` dan menandainya aktif. Subscription `onTokenRefresh` menangani token baru.

FCM token berbeda dari JWT login SmartSheep dan berbeda dari APNs token. Jangan memakai salah satunya sebagai pengganti yang lain atau mencetak token ke dokumentasi publik.

### 12.2 Saat berpindah project Firebase

Project sebelumnya adalah `smartsheep-app-2026`; project baru adalah `smartsheep-45a28`. Token dari project lama tidak dapat begitu saja digunakan oleh sender project baru.

Langkah penanganan:

1. Cocokkan keempat konfigurasi: API, Dart, Android JSON, iOS plist.
2. Full rebuild/relaunch aplikasi.
3. Login/restore session, lalu pastikan registrasi token baru berhasil.
4. Uji token baru sebelum membersihkan catatan lama.
5. Bila log menunjukkan token lama masih aktif, identifikasi record yang tepat dan buat backup sebelum menonaktifkannya. Jangan menghapus semua `UserDeviceToken` atau menonaktifkan perangkat lain secara massal.
6. Endpoint `DELETE /api/FirebaseNotification/UnregisterDevice` dapat menonaktifkan token yang dimiliki user terautentikasi, dengan body `DeviceToken`. Jangan menebak token atau menjalankan SQL massal tanpa memastikan target.

Registrasi berdasarkan token pada source saat ini tidak otomatis membersihkan semua token project lama. Logout/login bukan jaminan seluruh record historis sudah hilang.

## 13. Pengujian Test Reminder dari Web

### 13.1 Langkah menu

1. Pastikan API dan mobile sudah memakai konfigurasi baru.
2. Login Web dengan user aplikasi yang sama dengan mobile.
3. Buka **Configs → Reminder Templates**.
4. URL lokal: `https://localhost:44325/ReminderTemplate`.
5. Cari template yang akan diuji. Template yang dipakai saat setup:

   | Field | Nilai |
   | --- | --- |
   | Code | `LOW-SUCKLING` |
   | Name | `Low Suckling Activity` |
   | Reminder Text | `Suckling activity is below the expected threshold. Please check the barn and sheep condition.` |

6. Pada kolom **Actions**, klik **Test Reminder**.
7. Dialog konfirmasi **Test Reminder** muncul.
8. Klik **Send Test** untuk benar-benar mengirim; **Cancel** tidak mengirim.
9. Tunggu loading pada tombol selesai dan baca pesan hasil.
10. Periksa notifikasi pada perangkat. Untuk menguji system notification, letakkan aplikasi di background tanpa melakukan force-stop, kemudian kirim tes.

### 13.2 Apa yang dikirim API

Endpoint:

```text
POST /api/ReminderTemplate/{id}/Test
```

Untuk template yang diuji:

```text
POST /api/ReminderTemplate/5374cac7-12cb-4cc8-a7de-fc3e411e0be6/Test
```

Implementasi saat ini:

- Membaca `Name` dan `ReminderText` langsung dari database template.
- Membaca token aktif milik user aktif yang sedang login pada Web.
- Menggunakan nama template sebagai notification title dan teks template sebagai body.
- Mengirim data `type=reminder-test` serta `id` template.
- Memakai scope `FirebaseNotification.Add` yang sudah ada. Jangan mengubah mapping role/client hanya untuk memaksa tes berhasil.
- Mengembalikan `RegisteredDeviceCount`, `SuccessCount`, `FailureCount`, dan `FailureCode` pada hasil terstruktur.

Tes ini tidak memilih seluruh user atau broadcast ke seluruh instalasi. Jika Web login user A dan mobile user B, perangkat B bukan target test user A.

### 13.3 Kriteria berhasil

- Respons menyatakan ada pengiriman berhasil, misalnya `Test reminder sent to 1 mobile device(s).`.
- Periksa juga `FailureCount`; sukses parsial masih dapat menyisakan perangkat gagal.
- Pastikan judul dan isi notifikasi benar-benar muncul di perangkat yang dituju.
- Android foreground pada implementasi sekarang memakai SnackBar; jangan hanya mencari banner sistem saat aplikasi terbuka.
- Status berhasil dari FCM berarti pesan diterima layanan untuk dikirim, bukan bukti visual pengguna sudah melihatnya. Verifikasi perangkat tetap diperlukan.
- Count lonceng Web bukan indikator keberhasilan push ini. Method Test yang sekarang tidak membuat record history notifikasi baru hanya karena pengiriman diuji.

### 13.4 Hasil tes aktual 14 September 2026

Pesan Web:

```text
The mobile device is registered, but the Firebase APNs credential for iOS is missing or invalid.
```

Log API yang relevan:

```text
MessagingErrorCode=ThirdPartyAuthError; Message=Invalid APNs credential.
MessagingErrorCode=SenderIdMismatch; Message=SenderId mismatch
```

Terdapat satu kegagalan APNs dan tiga kegagalan sender mismatch. Ini membuktikan request sudah mencapai layanan Firebase dengan credential server, tetapi **belum membuktikan delivery sukses**. Langkah berikutnya adalah memasang APNs sesuai bagian 10 dan menangani token lama sesuai bagian 12, lalu mengulang tes.

## 14. Troubleshooting

| Gejala / pesan | Kemungkinan penyebab dan pemeriksaan |
| --- | --- |
| Project tidak terlihat di Firebase | Periksa avatar akun Google dan pemilihan project; nama project dapat sama, Project ID yang membedakan. |
| API menyala, tetapi Test gagal konfigurasi | Inisialisasi Firebase bersifat lazy. Periksa JSON, field credential, private key, override environment, dan restart API. |
| `No active mobile device is registered for your account.` | User Web tidak memiliki token aktif. Login mobile dengan user yang sama; periksa izin notifikasi dan keberhasilan RegisterDevice. |
| `ThirdPartyAuthError` / `Invalid APNs credential` | APNs key/certificate kosong, tidak valid, dicabut, atau environment/team tidak cocok. Periksa tab Cloud Messaging bagian Apple. |
| `SenderIdMismatch` | Token berasal dari project/sender berbeda. Cocokkan semua konfigurasi, rebuild mobile, registrasikan token baru, tangani record lama secara terarah. |
| `Unregistered` | Token tidak lagi valid. Minta aplikasi meregistrasikan token baru; hentikan penggunaan token yang telah terkonfirmasi tidak valid. |
| `APNs token is unavailable` | Periksa signing, entitlement, izin, koneksi, dan perangkat uji iOS. Mengganti private key server saja tidak menyelesaikan ini. |
| `Firebase initialization failed` | Periksa `firebase_options.dart`, file native, App ID/bundle/package, serta hasil build. |
| HTTP 401/403 saat Test | Periksa sesi login/otorisasi endpoint. Jangan mengubah credential Firebase untuk masalah login aplikasi atau memberi akses role secara sembarang. |
| HTTP 502 dari endpoint Test | Source membungkus exception pemrosesan dalam Bad Gateway; baca log API untuk membedakan konfigurasi, autentikasi layanan, dan jaringan. |
| Success sebagian, Failure sebagian | Beberapa token valid, lainnya bermasalah. Baca hasil per error di log; jangan menganggap semua perangkat sukses. |
| Success dari FCM, notification tidak terlihat | Periksa foreground/background, permission perangkat, Focus/Do Not Disturb, konektivitas, serta aplikasi/instalasi target yang benar. |
| Mobile tidak bisa login setelah dijalankan ulang | Gunakan `ruby tool/run-local.rb`; periksa API URL. Plain Flutter run tidak membawa client secret lokal. Ini berbeda dari masalah Firebase. |
| Port already in use | Proses API/Web lama masih aktif. Hentikan hanya instance yang sesuai, lalu jalankan ulang; jangan mematikan semua proses dotnet. |
| HTTP API dokumentasi `/swagger/index.html` 404 | Pada setup ini gunakan `/scalar/`. 404 pada path Swagger bukan bukti API mati. |
| Mengganti APNs key tidak mengubah hasil | Pastikan key tersimpan di project dan aplikasi iOS yang benar, environment benar, dan lihat error terbaru, bukan toast/log tes sebelumnya. |

Arti error FCM lintas platform dapat diperiksa pada [FCM Error Codes](https://firebase.google.com/docs/cloud-messaging/error-codes). Pesan HTTP dan teks toast di tabel juga merujuk implementasi controller SmartSheep, sehingga tidak selalu identik dengan pesan mentah Google.

### Batas pengujian ini

Test Reminder membuktikan jalur manual Web → API → FCM → perangkat. Tes ini tidak dengan sendirinya membuktikan scheduler reminder, perhitungan threshold, atau job periodik berjalan. Uji otomatis terjadwal merupakan verifikasi terpisah.

## 15. Checklist penyelesaian dan keamanan

### Checklist berdasarkan status terakhir

- [x] Akun Google dan Project ID sudah dipastikan.
- [x] Project SmartSheep dibuat tanpa upgrade billing.
- [x] Android app terdaftar dengan package yang benar.
- [x] iOS app terdaftar dengan bundle yang benar.
- [x] File Android JSON dan iOS plist dipasang.
- [x] Dart FirebaseOptions disamakan dengan project baru.
- [x] Service-account key dibuat dan dipasang ke appsettings API.
- [x] FCM API v1 Enabled.
- [x] API direstart dan mobile berhasil dibuild ulang.
- [x] Tombol Test Reminder dari Web telah diuji dan error aktual dicatat.
- [ ] APNs `.p8`, Key ID, dan Team ID tersedia.
- [ ] APNs credential diunggah untuk environment yang diuji.
- [ ] Signing dan push diverifikasi pada iPhone fisik.
- [ ] Tiga token lama yang mismatch ditangani setelah identifikasi/backup.
- [ ] Test Reminder diulang dengan hasil sukses.
- [ ] Judul dan isi reminder benar-benar diterima perangkat.

### Perlindungan credential dan rollback

- Private key telah diizinkan disimpan di appsettings lokal untuk setup ini. Lokal tidak berarti aman untuk dipush ke Git atau dibagikan lewat screenshot/log.
- Simpan service-account JSON, APNs `.p8`, `.env.local.json`, dan backup konfigurasi secara privat. Jangan menyertakan key asli dalam panduan ini.
- Periksa perubahan sebelum commit. Meng-ignore file tidak menghapus secret yang sudah pernah masuk history Git.
- Jika key bocor, lakukan pencabutan/rotasi secara terkontrol oleh administrator, pasang pengganti, restart server, lalu uji kembali. Jangan mencabut key yang masih dipakai aplikasi lain tanpa pemeriksaan.
- Untuk deployment bersama/production, gunakan penyimpanan secret atau credential runtime yang sesuai. Implementasi API sekarang membaca field appsettings; mengganti ke file-path/ADC membutuhkan penyesuaian kode, bukan hanya mengganti satu nama konfigurasi.
- Jika rollback project diperlukan, pulihkan keempat konfigurasi secara konsisten, restart API, rebuild mobile, dan registrasikan ulang perangkat ke project yang dipilih. Jangan rollback hanya satu file.

Backup sebelum perubahan Firebase pada mesin setup disimpan di:

```text
/var/folders/9q/gf9kqtwj37lcwd4d_hfyrfph0000gn/T/smartsheep-firebase-before-20260914-59033-hbjp8x/
```

Folder tersebut berisi backup API appsettings, Dart options, Android JSON, dan iOS plist. Karena berada di direktori sementara, pindahkan ke penyimpanan privat yang permanen jika diperlukan untuk pemeliharaan jangka panjang. Jangan mengandalkan folder sementara sebagai satu-satunya backup.

### Source code yang menjadi acuan panduan

- [Pemetaan konfigurasi API](SmartSheep/src/02.Applications/01.WebApi/04.Api/Program.cs)
- [Pengiriman Firebase server](SmartSheep/src/02.Applications/01.WebApi/04.Api/Services/FirebasePushService.cs)
- [Endpoint Test Reminder](SmartSheep/src/02.Applications/01.WebApi/04.Api/Controllers/Configs/ReminderTemplateController.cs)
- [Registrasi perangkat API](SmartSheep/src/02.Applications/01.WebApi/04.Api/Controllers/FirebaseNotificationController.cs)
- [FirebaseOptions mobile](smartsheep-mobile/lib/firebase_options.dart)
- [Inisialisasi dan foreground handling](smartsheep-mobile/lib/main.dart)
- [Permission dan registrasi token mobile](smartsheep-mobile/lib/features/auth/auth_controller.dart)
- [Helper menjalankan mobile lokal](smartsheep-mobile/tool/run-local.rb)

Dokumentasi ini tidak berisi private key asli dan tidak mengubah konfigurasi aplikasi saat dibaca.
