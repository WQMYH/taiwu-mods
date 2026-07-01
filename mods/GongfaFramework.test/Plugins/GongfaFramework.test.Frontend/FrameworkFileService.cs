using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GongfaFramework.Test.Contracts;

namespace GongfaFramework.Test.Frontend;

internal static class FrameworkFileService
{
    internal static string Export(IEnumerable<GongfaRecord> records, string label)
    {
        string directory = Path.Combine(FrontendRuntime.ModDirectory, "UserData", "exports",
            DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, Sanitize(label) + ".json");
        AtomicWrite(path, FrameworkJson.Serialize(new
        {
            schemaVersion = 1,
            frameworkVersion = "0.1.0.0",
            gameVersion = "1.0.40",
            frontendHash = FrontendRuntime.Snapshot.Hash,
            backendHash = FrontendRuntime.BackendHash,
            records = records.ToList()
        }));
        FrontendRuntime.Log("导出完成：" + path);
        return path;
    }

    internal static string SavePatch(PatchDefinition patch, bool formal)
    {
        string root = formal
            ? Path.Combine(FrontendRuntime.ModDirectory, "Definitions", "user")
            : Path.Combine(FrontendRuntime.ModDirectory, "UserData", "drafts");
        Directory.CreateDirectory(root);
        if (formal) BackupDefinitions();
        string path = Path.Combine(root, $"{patch.Table}-{patch.TemplateId}-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        AtomicWrite(path, FrameworkJson.Serialize(patch));
        FrontendRuntime.Log((formal ? "正式定义" : "草稿") + "已保存：" + path);
        return path;
    }

    internal static List<string> FindImports() =>
        Directory.Exists(Path.Combine(FrontendRuntime.ModDirectory, "UserData", "imports"))
            ? Directory.EnumerateFiles(Path.Combine(FrontendRuntime.ModDirectory, "UserData", "imports"), "*.*")
                .Where(x => x.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                            x.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x).ToList()
            : new List<string>();

    internal static string InstallImport(string source)
    {
        BackupDefinitions();
        string targetDirectory = Path.Combine(FrontendRuntime.ModDirectory, "Definitions", "user");
        Directory.CreateDirectory(targetDirectory);
        string target = Path.Combine(targetDirectory,
            $"{Path.GetFileNameWithoutExtension(source)}-{DateTime.Now:yyyyMMdd-HHmmss}{Path.GetExtension(source)}");
        AtomicWrite(target, File.ReadAllText(source));
        FrontendRuntime.Log("导入定义已安装：" + target);
        return target;
    }

    private static void BackupDefinitions()
    {
        string source = Path.Combine(FrontendRuntime.ModDirectory, "Definitions", "user");
        if (!Directory.Exists(source)) return;
        string target = Path.Combine(FrontendRuntime.ModDirectory, "UserData", "backups",
            DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(target);
        foreach (string file in Directory.EnumerateFiles(source, "*.*", SearchOption.AllDirectories))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
    }

    private static void AtomicWrite(string path, string content)
    {
        string temp = path + ".tmp";
        File.WriteAllText(temp, content, new System.Text.UTF8Encoding(false));
        if (File.Exists(path)) File.Delete(path);
        File.Move(temp, path);
    }

    private static string Sanitize(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
        return string.IsNullOrWhiteSpace(value) ? "gongfa-export" : value;
    }
}
