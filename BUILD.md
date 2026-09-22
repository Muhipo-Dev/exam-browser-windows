# Petunjuk Kompilasi & Build (BUILD.md)
### Exambro-Muhipo — SMA Muhammadiyah 1 Ponorogo

Dokumen ini menjelaskan alur kompilasi, pengujian otomatis, penerbitan (*publish*), dan pembuatan paket instalasi Windows (*installer*) untuk proyek **Exambro-Muhipo**.

---

## 1. Prasyarat Sistem

* **Sistem Operasi**: Windows 10 / Windows 11 (64-bit)
* **SDK**: .NET SDK 8.0 (v8.0.424 atau LTS yang kompatibel) dengan Windows Desktop Workload (WPF)
* **Inno Setup**: Inno Setup 6 (v6.7.3+) dengan `ISCC.exe` untuk membangun installer Windows.
* **IDE**: Visual Studio 2022 / Visual Studio Code / Antigravity IDE

---

## 2. Struktur Pipeline Proyek

Pipeline otomatis mengikuti urutan:
```
RESTORE  ──>  BUILD (Debug & Release)  ──>  TEST (Automated Suite)  ──>  PUBLISH (win-x64)  ──>  PACKAGE (Installer)
```

---

## 3. Menjalankan Pipeline Menggunakan PowerShell

### A. Build & Automated Tests
Jalankan skrip `Scripts/build.ps1`:
```powershell
powershell -ExecutionPolicy Bypass -File "Scripts\build.ps1"
```
Skrip ini akan:
1. Menjalankan `dotnet restore Exambro-Muhipo.sln`
2. Mengompilasi konfigurasi `Debug`
3. Mengompilasi konfigurasi `Release`
4. Menjalankan seluruh pengujian unit test dan functional test via `dotnet test`

### B. Publish Release Native win-x64
Jalankan skrip `Scripts/publish.ps1`:
```powershell
powershell -ExecutionPolicy Bypass -File "Scripts\publish.ps1"
```
Skrip ini mempublikasikan build *self-contained* 64-bit:
* Target runtime: `win-x64`
* Konfigurasi: `Release`
* Direktori keluaran: `publish\win-x64\`
* Executable utama: `publish\win-x64\Exambro-Muhipo.exe`

### C. Pembuatan Paket Installer Windows
Jalankan skrip `Scripts/package.ps1`:
```powershell
powershell -ExecutionPolicy Bypass -File "Scripts\package.ps1"
```
Skrip ini memanggil Inno Setup Compiler (`ISCC.exe`) untuk mengompilasi `Installer\Exambro-Muhipo-Setup.iss`.
* Hasil installer: `Installer\Output\Exambro-Muhipo-Setup-v1.0.0.exe`

---

## 4. Perintah Manual .NET CLI

Jika Anda ingin menjalankan langkah-langkah secara terpisah:

```powershell
# Restore dependencies
dotnet restore Exambro-Muhipo.sln

# Build Release
dotnet build Exambro-Muhipo.sln -c Release

# Run automated tests
dotnet test Exambro-Muhipo.sln -c Release

# Publish win-x64 self-contained
dotnet publish src/Exambro-Muhipo/Exambro-Muhipo.csproj -c Release -r win-x64 --self-contained true -o publish/win-x64
```

---

## 5. Verifikasi Binary

Verifikasi kesehatan dan metadata berkas executable:
```powershell
& "publish\win-x64\Exambro-Muhipo.exe" --check-config
& "publish\win-x64\Exambro-Muhipo.exe" --smoke-test
```
