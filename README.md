# Exambro-Muhipo (Secure Examination Browser)
### Khusus SMA Muhammadiyah 1 Ponorogo (Muhipo)
**Versi: v1.3.6-beta** | **Platform: Windows 10/11 (64-bit)** | **Teknologi: .NET 8 WPF & Edge WebView2**

**Exambro-Muhipo** adalah aplikasi desktop Windows (*Secure Exam Browser / Lockdown Browser*) resmi untuk **SMA Muhammadiyah 1 Ponorogo**. Aplikasi ini dirancang dari nol (*clean room architecture*) menggunakan bahasa pemrograman **C#**, **.NET 8 LTS**, **WPF**, dan browser engine **Microsoft Edge WebView2**.

Aplikasi ini memberikan lingkungan ujian yang aman (*Secure Examination Environment*), mencegah kecurangan umum siswa, membatasi tombol navigasi Windows, memblokir akses ke luar domain ujian, dan mencatat seluruh aktivitas penting ke dalam berkas audit log terenkripsi/tersanitasi.

---

## 📑 Daftar Dokumen Resmi

* 📖 [Panduan Siswa (Documentation/STUDENT_GUIDE.md)](Documentation/STUDENT_GUIDE.md)
* 🛠️ [Panduan Administrator & Proktor (Documentation/ADMIN_GUIDE.md)](Documentation/ADMIN_GUIDE.md)
* ⚙️ [Spesifikasi Konfigurasi JSON (Documentation/CONFIGURATION.md)](Documentation/CONFIGURATION.md)
* 🛡️ [Arsitektur Keamanan & Batasan Sistem (Documentation/SECURITY.md)](Documentation/SECURITY.md)
* 🏗️ [Petunjuk Build & Packaging (Documentation/BUILD.md)](Documentation/BUILD.md)

---

## ✨ Fitur Utama & Pembaruan (v1.3.6-beta)

1. **Pengaturan Cepat & Fleksibel Alamat IP Server Ujian**:
   * Panel Administrasi memfasilitasi pengisian instan **Alamat IP / URL Server CBT** sekolah (mendukung format `http://192.168.x.x:8080`, `https://cbt.muhipo.sch.id`, dll.).
   * Otomatis melakukan sinkronisasi whitelist domain dan URL saat IP server disimpan.
   * Tombol **"Simpan & Mulai Ujian"** langsung dari panel konfigurasi untuk mempercepat setup proktor ruang.

2. **Branding Resmi Sekolah**:
   * Nama Institusi: **SMA Muhammadiyah 1 Ponorogo**
   * Produk: **Exambro-Muhipo**
   * Subtitle: **Secure Examination Browser**
   * Antarmuka modern bernuansa *dark navy & cyan* yang nyaman di mata siswa.
   * Dilengkapi logo resmi dan Windows Application Icon beresolusi tinggi.

3. **Kiosk Mode & System Lockdown Tingkat Lanjut**:
   * Fullscreen borderless otomatis saat ujian dimulai.
   * Intersepsi tombol Windows (LWIN, RWIN, Apps), Alt+Tab, Alt+Esc, Ctrl+Esc, Alt+F4 menggunakan Low-Level Keyboard Hook resmi (`WH_KEYBOARD_LL`).
   * Tombol F11 dan shortcut browser (Ctrl+N, Ctrl+T, Ctrl+W, Ctrl+H, Ctrl+J, Ctrl+U, Ctrl+P, F12) diblokir.
   * Blokir klik kanan (Context Menu), seleksi teks yang dilarang, serta pencegahan jendela pop-up/tab baru.

4. **Whitelist Domain & Protokol Ketat**:
   * Evaluasi ketat hostname, subdomain wildcard (misal: `*.muhipo.sch.id`), dan exact URL.
   * Pemblokiran otomatis terhadap protokol eksternal berbahaya (`mailto:`, `tel:`, `file:`, `ms-settings:`, `powershell:`, `cmd:`, `javascript:`).

5. **Exit Mechanism Berbasis Otorisasi & Darurat**:
   * Sesi ujian hanya dapat diakhiri dengan verifikasi password proktor/administrator.
   * Password diverifikasi menggunakan kriptografi PBKDF2 (HMAC-SHA256) dengan salt 16-byte dan perbandingan waktu konstan (*constant-time*).
   * Fitur pintu darurat offline (*offline emergency exit*) opsional jika server terputus total.

6. **Crash Recovery & Heartbeat**:
   * Menangani kegagalan proses WebView2 secara anggun tanpa membuka lockdown.
   * Dialog pemulihan crash (*Crash Recovery Dialog*) dengan verifikasi proktor.
   * Mendeteksi pemutusan daya mendadak (*unexpected shutdown*) melalui *recovery lock file*.

7. **Structured Audit Logging**:
   * Pencatatan log harian berputar di folder `Logs/`.
   * Privasi terjamin: password, token, cookie, dan jawaban siswa **tidak pernah** dicatat ke log.

---

## 💻 Persyaratan Sistem

* **Sistem Operasi**: Windows 10 (64-bit) atau Windows 11 (64-bit)
* **Arsitektur**: x64 Native
* **Framework**: .NET 8 Runtime (Desktop Runtime)
* **Browser Engine**: Microsoft Edge WebView2 Evergreen Runtime
* **Jaringan**: Terhubung ke jaringan LAN / Wi-Fi Intranet sekolah atau Internet

---

## 🚀 Panduan Menjalankan Aplikasi

1. **Jalankan Aplikasi**: Buka berkas `Exambro-Muhipo.exe`.
2. **Konfigurasi Server (Proktor/IT)**:
   * Klik tombol **Administrasi** di halaman utama (atau tekan shortcut admin).
   * Masukkan password admin (default pabrik: `admin123`).
   * Masukkan IP/URL Server CBT Sekolah pada kolom alamat server.
   * Klik **Simpan Pengaturan** atau **Simpan & Mulai Ujian**.
3. **Memulai Sesi Ujian (Siswa)**:
   * Pada Beranda, periksa status kesiapan WebView2 dan koneksi server.
   * Klik tombol **MULAI UJIAN**. Layar akan terkunci dalam Kiosk Mode.
4. **Mengakhiri Ujian**:
   * Pengawas/Proktor menekan tombol **Keluar / Akhiri Ujian** di bilah atas ujian.
   * Masukkan password admin untuk mengembalikan Windows ke kondisi normal.

---

## 📦 Build & Distribusi

Untuk mengompilasi dan mengemas aplikasi menjadi installer mandiri:

```powershell
# Jalankan skrip packaging otomatis
.\Scripts\package.ps1
```

Installer setup siap pakai akan dihasilkan di folder `Output/Exambro-Muhipo-Setup-v1.3.6-beta.exe`.

---

© 2026 **SMA Muhammadiyah 1 Ponorogo (Muhipo Dev)**. All Rights Reserved.
