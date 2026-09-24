using System.Collections.Generic;
using ExambroMuhipo.Models;

namespace ExambroMuhipo.Services;

/// <summary>
/// Kontrak layanan manajemen konfigurasi terpusat.
/// </summary>
public interface IConfigurationService
{
    AppConfiguration Config { get; }
    ExamProfile ActiveProfile { get; }
    string ActiveProfilePath { get; }

    void LoadConfiguration();
    void SaveConfiguration();
    bool VerifyAdminPassword(string plainPassword);
    void SetAdminPassword(string newPassword);
    bool VerifyExitPassword(string plainPassword);
    void SetExitPassword(string newPassword);
    ExamProfile LoadProfile(string filePath);
    void SaveProfile(ExamProfile profile, string filePath);
    void SetActiveProfile(ExamProfile profile, string filePath);
    string GetProfilesDirectory();
    string GetConfigFilePath();
    List<string> GetAvailableProfilePaths();
}
