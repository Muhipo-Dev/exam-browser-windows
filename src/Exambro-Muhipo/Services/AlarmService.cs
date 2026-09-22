using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using ExambroMuhipo.Models;

namespace ExambroMuhipo.Services;

/// <summary>
/// Implementasi layanan audio sirine alarm dan buzzer alert peringatan keluar aplikasi
/// menggunakan sintesis gelombang PCM terpadu (100% offline bebas dependensi file eksternal).
/// </summary>
public class AlarmService : IAlarmService
{
    private readonly ILoggingService? _logger;
    private SoundPlayer? _sirenPlayer;
    private SoundPlayer? _buzzerPlayer;
    private readonly byte[] _sirenWavBytes;
    private readonly byte[] _buzzerWavBytes;
    private readonly object _lock = new();
    private bool _isPlayingSiren;

    public AlarmService(ILoggingService? logger = null)
    {
        _logger = logger;
        _sirenWavBytes = GenerateSirenWav(durationSeconds: 2.5);
        _buzzerWavBytes = GenerateBuzzerWav(durationSeconds: 0.8);
    }

    public void PlayExitSiren()
    {
        lock (_lock)
        {
            try
            {
                StopAlarm();
                _isPlayingSiren = true;

                _sirenPlayer = new SoundPlayer(new MemoryStream(_sirenWavBytes));
                _sirenPlayer.Load();
                _sirenPlayer.PlayLooping();

                _logger?.LogWarning(AuditEventType.ExitAttempt, "Alarm sirine peringatan keluar dari aplikasi dibunyikan.");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(AuditEventType.ApplicationError, "Gagal memutar sirine alarm melalui SoundPlayer, mencoba fallback beep.", ex.Message);
                Task.Run(() =>
                {
                    try
                    {
                        SystemSounds.Exclamation.Play();
                    }
                    catch { }
                });
            }
        }
    }

    public void PlaySecurityBuzzer()
    {
        lock (_lock)
        {
            try
            {
                _buzzerPlayer?.Stop();
                _buzzerPlayer?.Dispose();
                _buzzerPlayer = new SoundPlayer(new MemoryStream(_buzzerWavBytes));
                _buzzerPlayer.Load();
                _buzzerPlayer.Play();

                _logger?.LogWarning(AuditEventType.ExitDenied, "Buzzer peringatan pelanggaran keamanan / otorisasi salah dibunyikan.");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(AuditEventType.ApplicationError, "Gagal memutar buzzer melalui SoundPlayer.", ex.Message);
                Task.Run(() =>
                {
                    try
                    {
                        SystemSounds.Hand.Play();
                    }
                    catch { }
                });
            }
        }
    }

    public void StopAlarm()
    {
        lock (_lock)
        {
            try
            {
                if (_isPlayingSiren)
                {
                    _sirenPlayer?.Stop();
                    _sirenPlayer?.Dispose();
                    _sirenPlayer = null;
                    _isPlayingSiren = false;
                }

                _buzzerPlayer?.Stop();
                _buzzerPlayer?.Dispose();
                _buzzerPlayer = null;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(AuditEventType.ApplicationError, "Kesalahan saat menghentikan alarm.", ex.Message);
            }
        }
    }

    /// <summary>
    /// Menghasilkan format audio gelombang PCM RIFF/WAVE sintetis untuk nada sirine (sweep frekuensi 700Hz - 1500Hz).
    /// </summary>
    private static byte[] GenerateSirenWav(double durationSeconds)
    {
        int sampleRate = 44100;
        int numSamples = (int)(sampleRate * durationSeconds);
        short[] samples = new short[numSamples];

        double phase = 0.0;
        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / sampleRate;
            // Modulasi gelombang segitiga naik-turun frekuensi 700Hz sampai 1600Hz setiap 0.5 detik
            double sweepProgress = (Math.Sin(2.0 * Math.PI * 2.0 * t) + 1.0) / 2.0;
            double currentFreq = 700.0 + (sweepProgress * 900.0);

            phase += 2.0 * Math.PI * currentFreq / sampleRate;
            if (phase > 2.0 * Math.PI)
                phase -= 2.0 * Math.PI;

            // Kombinasi gelombang sinus tajam dengan harmonik
            double sampleValue = (Math.Sin(phase) * 0.75) + (Math.Sin(phase * 2.0) * 0.25);
            samples[i] = (short)(sampleValue * 28000);
        }

        return CreateWavStream(samples, sampleRate);
    }

    /// <summary>
    /// Menghasilkan format audio gelombang PCM RIFF/WAVE sintetis untuk nada buzzer peringatan (pulsa 450Hz).
    /// </summary>
    private static byte[] GenerateBuzzerWav(double durationSeconds)
    {
        int sampleRate = 44100;
        int numSamples = (int)(sampleRate * durationSeconds);
        short[] samples = new short[numSamples];

        double freq = 480.0;
        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / sampleRate;
            // Pulsa bip berselang: 100ms bunyi, 60ms hening
            bool soundOn = (t % 0.16) < 0.10;

            if (soundOn)
            {
                double sinVal = Math.Sin(2.0 * Math.PI * freq * t);
                double squareVal = sinVal >= 0 ? 0.85 : -0.85;
                samples[i] = (short)(squareVal * 26000);
            }
            else
            {
                samples[i] = 0;
            }
        }

        return CreateWavStream(samples, sampleRate);
    }

    private static byte[] CreateWavStream(short[] samples, int sampleRate)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        int byteRate = sampleRate * 1 * 2; // SampleRate * NumChannels * BitsPerSample / 8
        int subChunk2Size = samples.Length * 2;
        int chunkSize = 36 + subChunk2Size;

        // RIFF Header
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(chunkSize);
        writer.Write(new[] { 'W', 'A', 'V', 'E' });

        // Subchunk 1: "fmt "
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16); // Subchunk1Size for PCM
        writer.Write((short)1); // AudioFormat (1 = PCM)
        writer.Write((short)1); // NumChannels (1 = Mono)
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)2); // BlockAlign
        writer.Write((short)16); // BitsPerSample

        // Subchunk 2: "data"
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(subChunk2Size);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }

        writer.Flush();
        return ms.ToArray();
    }
}
