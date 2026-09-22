using System.Collections.Generic;
using ExambroMuhipo.Models;

namespace ExambroMuhipo.Services;

/// <summary>
/// Kontrak layanan manajemen daftar profil ujian untuk Admin Mode.
/// </summary>
public interface IProfileManager
{
    IReadOnlyList<string> GetProfileList();
    ExamProfile CreateNewProfile(string name);
    ExamProfile DuplicateProfile(string sourceFilePath, string newName);
    void DeleteProfile(string filePath);
    ExamProfile ImportProfile(string sourceFilePath, string destinationFileName);
    void ExportProfile(ExamProfile profile, string destinationFilePath);
    List<string> ValidateProfile(ExamProfile profile);
}
