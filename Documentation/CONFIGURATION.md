# Spesifikasi Konfigurasi JSON (CONFIGURATION.md)
### Exambro-Muhipo — SMA Muhammadiyah 1 Ponorogo

Seluruh konfigurasi sistem dan profil ujian di **Exambro-Muhipo** dikelola secara terpusat melalui `ConfigurationService` menggunakan berkas standar **JSON**.

---

## 1. Konfigurasi Aplikasi (`Configuration/appsettings.json`)

Berkas ini menyimpan konfigurasi global aplikasi, hash kredensial administrator, dan nama profil aktif.

```json
{
  "appName": "Exambro-Muhipo",
  "appSubtitle": "Secure Examination Browser",
  "schoolName": "SMA Muhammadiyah 1 Ponorogo",
  "appVersion": "1.0.0",
  "activeProfileFile": "default_profile.json",
  "adminPasswordSalt": "KZPQVoMxQSTwLQfse065Ww==",
  "adminPasswordHash": "ZA+rr9Aen8mjTK/87EUf9F1YNBgTTl46VH1jsUsZH7g=",
  "enableDetailedAuditLog": true,
  "logRetentionDays": 30,
  "allowOfflineEmergencyExit": true
}
```

### Keterangan Field `appsettings.json`:
| Field | Tipe | Deskripsi |
| :--- | :--- | :--- |
| `appName` | string | Nama produk aplikasi (`Exambro-Muhipo`). |
| `appSubtitle` | string | Subtitle resmi (`Secure Examination Browser`). |
| `schoolName` | string | Nama sekolah resmi (`SMA Muhammadiyah 1 Ponorogo`). |
| `appVersion` | string | Nomor versi terpusat (`1.0.0`). |
| `activeProfileFile` | string | Berkas profil default yang dimuat di halaman awal. |
| `adminPasswordSalt` | string | Salt acak 16-byte berformat Base64 untuk hashing. |
| `adminPasswordHash` | string | Hash PBKDF2 HMAC-SHA256 berformat Base64. |
| `enableDetailedAuditLog` | boolean | Mengaktifkan logging terperinci dari setiap event. |
| `logRetentionDays` | integer | Durasi penyimpanan berkas log (hari) sebelum rotasi bersih. |
| `allowOfflineEmergencyExit` | boolean | Mengizinkan keluar darurat secara luring dengan password admin. |

---

## 2. Profil Ujian (`Profiles/*.json`)

Setiap paket ujian atau tryout didefinisikan dalam berkas profil terpisah di folder `Profiles/`.

```json
{
  "examName": "Asesmen Sumatif Bersama SMA Muhammadiyah 1 Ponorogo",
  "examDescription": "Profil standar pelaksanaan asesmen resmi sekolah berbasis CBT",
  "startUrl": "https://example.com/exam",
  "allowedDomains": [
    "example.com",
    "*.example.com",
    "muhipo.sch.id",
    "*.muhipo.sch.id"
  ],
  "allowedUrls": [],
  "fullScreen": true,
  "kioskMode": true,
  "disableContextMenu": true,
  "disableDevTools": true,
  "disableNewWindows": true,
  "disablePopups": true,
  "allowClipboard": false,
  "allowCopy": false,
  "allowPaste": false,
  "allowPrinting": false,
  "allowDownloads": false,
  "allowUploads": true,
  "allowZoom": false,
  "allowCamera": false,
  "allowMicrophone": false,
  "allowAudio": true,
  "sessionTimeout": 90,
  "requireFullscreen": true,
  "exitPolicy": "AdminPasswordOnly",
  "enforceHttps": true,
  "clearCacheOnFinish": true,
  "clearCookiesOnFinish": false
}
```

### Keterangan Lengkap Properti `ExamProfile`:
| Properti | Tipe | Default | Deskripsi |
| :--- | :--- | :--- | :--- |
| `examName` | string | - | Judul ujian yang tampil di bilah status dan beranda. |
| `examDescription` | string | - | Penjelasan singkat mata pelajaran / tingkat kelas. |
| `startUrl` | string | - | Alamat awal halaman login CBT yang dibuka WebView2. |
| `allowedDomains` | string[] | `[]` | Daftar domain yang diizinkan (mendukung subdomain wildcard `*.domain.id`). |
| `allowedUrls` | string[] | `[]` | Daftar spesifik URL yang diizinkan di luar domain whitelist. |
| `fullScreen` | boolean | `true` | Membuka jendela dalam mode layar penuh (fullscreen). |
| `kioskMode` | boolean | `true` | Mengaktifkan Low-Level Keyboard Hook untuk memblokir Win key, Alt+Tab, dll. |
| `disableContextMenu` | boolean | `true` | Mematikan menu klik kanan pada halaman web. |
| `disableDevTools` | boolean | `true` | Memblokir shortcut F12 dan antarmuka inspeksi kode. |
| `disableNewWindows` | boolean | `true` | Membatalkan `window.open` atau tautan target `_blank`. |
| `disablePopups` | boolean | `true` | Memblokir dialog pop-up browser yang tidak sah. |
| `allowClipboard` | boolean | `false` | Menentukan apakah fitur clipboard diizinkan. |
| `allowCopy` | boolean | `false` | Mengizinkan perintah salin (Ctrl+C). |
| `allowPaste` | boolean | `false` | Mengizinkan perintah tempel (Ctrl+V). |
| `allowPrinting` | boolean | `false` | Mengizinkan pencetakan halaman (Ctrl+P / window.print). |
| `allowDownloads` | boolean | `false` | Mengizinkan unduhan file ke harddisk lokal. |
| `allowUploads` | boolean | `true` | Mengizinkan unggah berkas (misal: lembar jawaban esai/foto). |
| `allowZoom` | boolean | `false` | Mengizinkan pembesaran (zoom) halaman teks. |
| `allowCamera` | boolean | `false` | Memberikan izin akses kamera jika sistem ujian memerlukan proctoring. |
| `allowMicrophone` | boolean | `false` | Memberikan izin akses mikrofon jika ujian menyertakan tes lisan. |
| `allowAudio` | boolean | `true` | Mengizinkan pemutaran audio untuk soal listening. |
| `sessionTimeout` | integer | `90` | Batas durasi pengerjaan dalam menit (`0` = tidak terbatas). |
| `requireFullscreen` | boolean | `true` | Mencegah window dikecilkan atau dipindahkan. |
| `exitPolicy` | string | `"AdminPasswordOnly"` | Kebijakan keluar: mewajibkan password admin. |
| `enforceHttps` | boolean | `true` | Memblokir seluruh koneksi `http://` yang tidak terenkripsi. |
| `clearCacheOnFinish` | boolean | `true` | Menghapus cache temporary browser setelah ujian berakhir. |
| `clearCookiesOnFinish` | boolean | `false` | Menghapus cookies sesi setelah ujian berakhir. |
