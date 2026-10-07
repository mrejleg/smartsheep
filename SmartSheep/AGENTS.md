# SmartSheep Agreement

- Kerjakan setiap permintaan dalam batch kecil yang tuntas: temukan penyebab berdasarkan bukti, lakukan patch, build/test, lalu verifikasi hasil pada API dan Web yang sedang berjalan sebelum memperluas pekerjaan.
- Jangan melakukan audit repository yang melebar tanpa checkpoint, tanpa hasil konkret, atau berlangsung lama tanpa laporan progres dan temuan yang sudah terverifikasi.
- Untuk audit dan perbaikan, gunakan data dummy atau data lokal yang representatif. Jangan menyimpulkan berdasarkan perkiraan saja.
- Saat menguji approval, validasi alur dari API sampai Web, termasuk `IsAllAccess` dan hak approve berdasarkan assigned Position ID.
- Jangan mengubah signature method atau endpoint kecuali diminta secara eksplisit.
- Abaikan build warning yang tidak relevan sesuai arahan pengguna, tetapi selesaikan atau laporkan build error dan kegagalan runtime.
- Pertahankan perubahan pengguna yang tidak berkaitan dan jangan menyentuh solution lain di luar ruang lingkup permintaan.
- Jangan menyentuh atau mengaudit ulang mapping `SUPER.ADMIN`, client ID Web, dan client scope kecuali diminta secara eksplisit.
- Jangan menambah class, field, folder, abstraction, atau pola berdasarkan asumsi. Ikuti struktur dan formatting existing secara persis, dan untuk model WebApp selalu samakan dengan kontrak API yang benar-benar digunakan.
- Saat pengguna mengatakan `run`, jalankan API dan Web memakai launch profile serta port project, biarkan keduanya tetap hidup, lalu buka URL API dan Web dalam satu Chrome tab group bernama `SMARTSHEEP` bila memungkinkan.
