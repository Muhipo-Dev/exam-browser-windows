# Exambro-Muhipo (Secure Examination Browser)
### Khusus SMA Muhammadiyah 1 Ponorogo (Muhipo)

**Exambro-Muhipo** adalah aplikasi desktop Windows (*Secure Exam Browser / Lockdown Browser*) resmi untuk **SMA Muhammadiyah 1 Ponorogo**. Aplikasi ini dirancang dari nol (*clean room architecture*) menggunakan bahasa pemrograman **C#**, **.NET 8 LTS**, **WPF**, dan browser engine **Microsoft Edge WebView2**.

Aplikasi ini memberikan lingkungan ujian yang aman (*Secure Examination Environment*), mencegah kecurangan umum siswa, membatasi tombol navigasi Windows, memblokir akses ke luar domain ujian, dan mencatat seluruh aktivitas penting ke dalam berkas audit log terenkripsi/tersanitasi.

---

## Daftar Dokumen Resmi

* 📖 [Panduan Siswa (STUDENT_GUIDE.md)](STUDENT_GUIDE.md)
* 🛠️ [Panduan Administrator & Proktor (ADMIN_GUIDE.md)](ADMIN_GUIDE.md)
* ⚙️ [Spesifikasi Konfigurasi JSON (CONFIGURATION.md)](CONFIGURATION.md)
* 🛡️ [Arsitektur Keamanan & Batasan Sistem (SECURITY.md)](SECURITY.md)
* 🏗️ [Petunjuk Build & Packaging (BUILD.md)](BUILD.md)

---

## Fitur Utama

1. **Branding Resmi Sekolah**:
   * Nama Institusi: **SMA Muhammadiyah 1 Ponorogo**
   * Produk: **Exambro-Muhipo**
   * Subtitle: **Secure Examination Browser**
   * Bahasa Indonesia sebagai antarmuka default.
   * Dilengkapi logo resmi dan Windows Application Icon beresolusi tinggi.

2. **Kiosk Mode & System Lockdown**:
   * Fullscreen borderless otomatis saat ujian dimulai.
   * Intersepsi tombol Windows (LWIN, RWIN, Apps), Alt+Tab, Alt+Esc, Ctrl+Esc, Alt+F4 menggunakan Low-Level Keyboard Hook resmi (`WH_KEYBOARD_LL`).
   * Tombol F11 dan shortcut browser (Ctrl+N, Ctrl+T, Ctrl+W, Ctrl+H, Ctrl+J, Ctrl+U, Ctrl+P, F12) diblokir.
   * Siswa dilarang menutup jendela aplikasi secara paksa dari tombol close biasa.

3. **Whitelist Domain & Protokol Ketat**:
   * Evaluasi ketat hostname, subdomain wildcard (misal: `*.muhipo.sch.id`), dan exact URL.
   * Penegakan protokol HTTPS (*HTTPS Enforcement*).
   * Pemblokiran otomatis terhadap protokol eksternal berbahaya (`mailto:`, `tel:`, `file:`, `ms-settings:`, `powershell:`, `cmd:`, `javascript:`).

4. **Exit Mechanism Berbasis Otorisasi**:
   * Sesi ujian hanya dapat diakhiri dengan verifikasi password proktor/administrator.
   * Password diverifikasi menggunakan kriptografi PBKDF2 (HMAC-SHA256) dengan salt 16-byte dan perbandingan waktu konstan (*constant-time*).

5. **Crash Recovery & Heartbeat**:
   * Menangani kegagalan proses WebView2 secara anggun tanpa membuka lockdown.
   * Mendeteksi pemutusan daya mendadak (*unexpected shutdown*) melalui *recovery lock file*.

6. **Structured Audit Logging**:
   * Pencatatan log harian berputar di folder `Logs/`.
   * Privasi terjamin: password, token, cookie, dan jawaban siswa **tidak pernah** dicatat ke log.

---

## Persyaratan Sistem

* **Sistem Operasi**: Windows 10 (64-bit) atau Windows 11 (64-bit)
* **Arsitektur**: x64 Native
* **Browser Runtime**: Microsoft Edge WebView2 Evergreen Runtime
* **Jaringan**: Terhubung ke jaringan intranet / internet sekolah

---

## Menjalankan Aplikasi

1. Jalankan berkas `Exambro-Muhipo.exe`.
2. Halaman pembuka menampilkan identitas sekolah, status WebView2, status koneksi, dan profil ujian aktif.
3. Klik tombol **MULAI UJIAN** untuk memasuki sesi ujian aman.
4. Di akhir sesi, pengawas ruang menekan tombol **Akhiri Ujian** dan memasukkan password administrator.
