# Panduan Administrator & Proktor (ADMIN_GUIDE.md)
### Exambro-Muhipo — SMA Muhammadiyah 1 Ponorogo

Panduan ini ditujukan bagi Administrator Sistem, Teknisi Jaringan, dan Proktor Ruang Ujian di **SMA Muhammadiyah 1 Ponorogo** untuk mengelola profil ujian, whitelist domain, password admin, dan penanganan kondisi darurat.

---

## 1. Akses Masuk ke Panel Administrasi

1. Jalankan aplikasi `Exambro-Muhipo.exe`.
2. Pada halaman utama (Home Screen), klik tombol **Administrasi**.
3. Dialog **Autentikasi Administrator** akan muncul.
4. Masukkan password administrator:
   * **Password Default Pabrik**: `admin123`
   * *PENTING: Segera ubah password default saat instalasi pertama kali!*
5. Klik **Konfirmasi**. Jika password benar, aplikasi akan membuka **Panel Administrasi & Konfigurasi Ujian**.

---

## 2. Mengelola Profil Ujian (Exam Profiles)

Semua profil ujian disimpan dalam format JSON di direktori `Profiles/`.

### A. Membuat Profil Baru
1. Pada Tab **Profil & Whitelist Domain**, klik tombol **➕ Baru** di bawah daftar profil.
2. Lengkapi formulir:
   * **Nama Ujian**: Contoh `Asesmen Akhir Semester Gasal 2026/2027`.
   * **Deskripsi Ujian**: Keterangan tingkat kelas atau mata pelajaran.
   * **Start URL**: Alamat web server CBT sekolah, contoh: `https://example.com/cbt` *(Gunakan HTTPS)*.
3. Tentukan whitelist domain pada kolom **Allowed Domains**.
4. Klik tombol **💾 Simpan**.

### B. Mengatur Whitelist Domain
Sistem menggunakan whitelist ketat. Masukkan satu domain per baris:
* `example.com` — Mengizinkan domain utama `example.com`.
* `*.example.com` — Mengizinkan semua subdomain (contoh: `cbt.example.com`, `ujian.example.com`).
* `muhipo.sch.id` dan `*.muhipo.sch.id`.

### C. Menguji Kelayakan Profil (Test Profile)
Sebelum digunakan oleh siswa, klik tombol **🧪 Test Profile**:
* Sistem akan memverifikasi integritas URL dan memastikan Start URL lolos evaluasi whitelist.
* Jika URL berada di luar whitelist atau menggunakan protokol terlarang, sistem akan memberikan peringatan merah.

### D. Menetapkan Profil Aktif
Pilih profil yang diinginkan dari daftar, lalu klik tombol **⭐ Jadikan Aktif**. Profil ini akan otomatis dimuat di halaman utama siswa saat aplikasi dijalankan.

### E. Impor dan Ekspor Profil
* **Export**: Menyimpan profil ke file `.json` di flashdisk untuk didistribusikan ke komputer lain.
* **Import**: Membuka profil `.json` dari flashdisk ke dalam folder aplikasi.

---

## 3. Menyesuaikan Kebijakan Keamanan & Kiosk

Buka tab **🛡️ Kebijakan Keamanan & Kiosk**:
* **Kiosk Mode**: Wajib aktif untuk mengunci tombol Windows, Alt+Tab, Alt+Esc, Ctrl+Esc, dan Alt+F4.
* **Wajib Fullscreen**: Memastikan tampilan borderless memenuhi seluruh layar monitor.
* **Nonaktifkan Developer Tools (F12)**: Mencegah siswa membuka Inspect Element / Console browser.
* **Nonaktifkan Context Menu**: Mematikan klik kanan mouse.
* **Blokir New Window & Popups**: Mencegah tab baru atau jendela terpisah terbuka.
* **Clipboard (Copy / Paste)**: Dinonaktifkan secara default untuk mencegah salin-tempel soal atau contekan.
* **Session Timeout**: Durasi pengerjaan ujian dalam satuan menit (contoh: 90 menit). Isi `0` jika tanpa batas waktu.

---

## 4. Mengubah Password Administrator

1. Buka tab **🔐 Audit Log & Kredensial Admin**.
2. Pada bagian **Ganti Password Administrator**:
   * Masukkan **Password Saat Ini**.
   * Masukkan **Password Baru** dan **Konfirmasi Password Baru**.
3. Klik tombol **Simpan Password**.
4. Sistem akan mengacak salt baru dan menyimpan hash PBKDF2 HMAC-SHA256 (100.000 iterasi). Password tidak pernah disimpan dalam teks mentah (*plaintext*).

---

## 5. Prosedur Darurat (Emergency Exit)

Jika komputer siswa mengalami masalah teknis (freeze, masalah jaringan CBT, atau siswa telah selesai):
1. Proktor mendatangi komputer siswa.
2. Di pojok kanan atas bilah navigasi ujian, klik tombol merah **🔒 Akhiri Ujian**.
3. Masukkan password administrator pada kotak dialog otorisasi.
4. Klik **Konfirmasi**.
5. Kiosk Mode seketika dinonaktifkan, sesi browser dibersihkan, dan aplikasi kembali ke layar awal secara aman.

---

## 6. Pemulihan Setelah Crash / Mati Lampu (Crash Recovery)

Jika komputer mati mendadak saat ujian aktif:
1. Saat komputer dinyalakan kembali dan `Exambro-Muhipo.exe` dibuka, sistem mendeteksi *session recovery lock*.
2. Kotak dialog **Pemulihan Sesi Ujian** akan muncul:
   * Tombol **Muat Ulang Sesi**: Melanjutkan sesi ujian secara langsung tanpa keluar dari lockdown.
   * Otorisasi Proktor: Proktor dapat memasukkan password admin dan menekan **Keluar (Otorisasi)** untuk membuka kunci komputer siswa.
