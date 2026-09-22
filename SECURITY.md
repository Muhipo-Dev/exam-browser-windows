# Kebijakan Keamanan & Batasan Sistem (SECURITY.md)
### Exambro-Muhipo — SMA Muhammadiyah 1 Ponorogo

Dokumen ini menguraikan arsitektur keamanan, model ancaman, perlindungan privasi, serta batasan teknis (*security limitations*) dari aplikasi **Exambro-Muhipo**.

---

## 1. Filosofi Keamanan: *Secure Examination Environment*

Exambro-Muhipo dirancang dengan konsep **Secure Examination Environment** — lingkungan terkontrol yang bertujuan meminimalkan distraksi dan mencegah kecurangan umum peserta didik selama ujian daring.

### Pernyataan Transparansi & Batasan Keamanan (Security Limitations)

> [!IMPORTANT]
> **PEMBERITAHUAN RESMI TENTANG BATASAN KEAMANAN SISTEM:**
> Sesuai prinsip arsitektur sistem operasi modern, setiap aplikasi pengguna (*user-mode application*) pada Windows **tidak dapat menjamin lockdown absolut 100%** terhadap seluruh kemungkinan tindakan pengguna yang memiliki akses fisik langsung (*physical access*) atau hak akses administrator lokal (*administrator privileges*) terhadap perangkat keras.
>
> Pengembang dan pihak sekolah **TIDAK MENGKLAIM** bahwa Exambro-Muhipo adalah sistem yang "100% anti-cheat" atau "mustahil dibypass".
>
> Integritas ujian berbasis komputer di SMA Muhammadiyah 1 Ponorogo disandarkan pada kombinasi sinergis antara:
> 1. **Lapisan Perangkat Lunak**: Exambro-Muhipo yang membatasi navigasi web, mengunci tombol sistem, dan mencatat pelanggaran ke audit log.
> 2. **Lapisan Pengawasan Fisik**: Kehadiran dan kewaspadaan proktor/pengawas ruang ujian profesional.
> 3. **Lapisan Jaringan**: Isolasi jaringan lokal (VLAN ujian) dan firewall sekolah.

---

## 2. Kebijakan Privasi & Etika Perangkat Lunak

Exambro-Muhipo mematuhi standar etika rekayasa perangkat lunak pendidikan yang ketat. Aplikasi ini **TIDAK MEMILIKI DAN TIDAK MENGGUNAKAN**:
* ❌ Keylogger (perekam tombol ketikan siswa di luar kendali aplikasi).
* ❌ Credential stealer (pencuri kata sandi atau data akun siswa).
* ❌ Spyware atau adware.
* ❌ Perekaman layar tersembunyi (*hidden screen recording*).
* ❌ Perekaman webcam atau mikrofon tersembunyi tanpa izin resmi.
* ❌ Teknik injeksi proses (*process injection*) ke aplikasi pihak ketiga.
* ❌ Antivirus bypass atau teknik evasif malware.
* ❌ UAC bypass atau eskalasi privilege ilegal.
* ❌ Layanan tersembunyi (*hidden Windows services*) atau persistensi tersembunyi di registry sistem.
* ❌ Akses kendali jarak jauh tersembunyi (*hidden remote control*).

Akses kamera atau mikrofon hanya diizinkan apabila profil ujian secara eksplisit mengaktifkannya dan WebView2 menampilkan dialog izin resmi kepada pengguna.

---

## 3. Komponen Pertahanan Keamanan

### A. Evaluasi Whitelist Domain & Protokol
* Navigasi diperiksa secara real-time pada event `NavigationStarting`.
* Protokol eksternal seperti `file:`, `cmd:`, `powershell:`, `mailto:`, `tel:`, `ms-settings:`, dan `javascript:` diblokir secara mutlak.
* Dukungan *HTTPS Enforcement* memastikan koneksi siswa terenkripsi SSL/TLS.
* Percobaan membuka tautan di luar whitelist akan dibatalkan (`e.Cancel = true`) dan memicu event `NavigationBlocked`.

### B. Kiosk Mode & Keyboard Hook Resmi
* Menggunakan Win32 API resmi: `SetWindowsHookEx(WH_KEYBOARD_LL, ...)`.
* Tombol yang diintersepsi selama sesi aktif:
  * Tombol Windows (`VK_LWIN`, `VK_RWIN`, `VK_APPS`)
  * Alt + Tab (perpindahan task)
  * Alt + Esc dan Ctrl + Esc (Start menu)
  * Alt + F4 (penutupan paksa)
  * F11 (toggle fullscreen ilegal)
  * F12 dan Ctrl + Shift + I (Developer Tools / Inspect Element)
  * Browser shortcuts: Ctrl+N, Ctrl+T, Ctrl+W, Ctrl+H, Ctrl+J, Ctrl+U, Ctrl+P.
* Hook dicabut secara aman (`UnhookWindowsHookEx`) saat sesi berakhir atau aplikasi ditutup.

### C. Isolasi Data Browser (WebView2 Isolation)
* Cache, riwayat, dan sesi browser disimpan dalam direktori terisolasi:
  `%LOCALAPPDATA%\Exambro-Muhipo\WebView2Data`
* Tidak memengaruhi profil browser Microsoft Edge atau Google Chrome utama milik pengguna.
* Dapat dibersihkan secara otomatis saat ujian selesai sesuai konfigurasi profil.

### D. Keamanan Kredensial Administrator
* Password admin tidak pernah disimpan dalam bentuk teks biasa (*plaintext*).
* Menggunakan algoritma **PBKDF2** (Password-Based Key Derivation Function 2) dengan **HMAC-SHA256**, 100.000 iterasi, dan *cryptographically secure random salt* 16-byte.
* Verifikasi password menggunakan `CryptographicOperations.FixedTimeEquals` untuk mencegah *timing side-channel attack*.

### E. Audit Logging yang Bersih dan Terstruktur
* Log mencatat event keamanan: login admin, start/stop ujian, navigasi diblokir, popup diblokir, download diblokir, upaya keluar tidak sah, dan crash.
* Algoritma sanitasi menyaring dan menyensor parameter URL sensitif sehingga token, cookie, password, dan jawaban siswa **tidak pernah** tersimpan di berkas log.
