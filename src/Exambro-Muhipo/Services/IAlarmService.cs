using System;

namespace ExambroMuhipo.Services;

/// <summary>
/// Layanan suara peringatan keamanan (Alarm, Buzzer, dan Sirine Alert)
/// saat siswa mencoba keluar dari aplikasi ujian atau melakukan tindakan tidak sah.
/// </summary>
public interface IAlarmService
{
    /// <summary>
    /// Memulai sirine alarm peringatan saat jendela konfirmasi keluar / otorisasi pengawas aktif.
    /// </summary>
    void PlayExitSiren();

    /// <summary>
    /// Membunyikan suara buzzer peringatan pelanggaran keamanan atau kesalahan input password pengawas.
    /// </summary>
    void PlaySecurityBuzzer();

    /// <summary>
    /// Menghentikan seluruh suara alarm / sirine yang sedang berbunyi.
    /// </summary>
    void StopAlarm();
}
