using System;
using System.IO;
using Config;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Building;
using GameData.Domains.Mod;
using GameData.Serializer;
using HarmonyLib;
using NLog;
using TaiwuModdingLib.Core.Plugin;

namespace CopyBuildingModernized.Backend
{
    [PluginConfig("CopyBuildingModernized", "Slimoon", "2.0.0")]
    public sealed class BackendEntry : TaiwuRemakePlugin
    {
        internal static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private Harmony _harmony;

        public override void Initialize()
        {
            ModSettings.Load(ModIdStr, Logger);
            InstallHarmonyPatches();
            RegisterModMethods();
            Logger.Info("[CopyBuildingModernized] Backend initialized.");
        }

        public override void OnModSettingUpdate()
        {
            ModSettings.Load(ModIdStr, Logger);
        }

        public override void Dispose()
        {
            _harmony?.UnpatchSelf();
            _harmony = null;
            Logger.Info("[CopyBuildingModernized] Backend disposed.");
        }

        private void InstallHarmonyPatches()
        {
            _harmony = new Harmony(GetGuid() + ".backend");

            var target = AccessTools.Method(typeof(BuildingDomain), "GetBuildingOperationLeftTime",
                new[] { typeof(DataContext), typeof(BuildingBlockKey), typeof(sbyte) });
            if (target == null)
            {
                Logger.Warn("[CopyBuildingModernized] GetBuildingOperationLeftTime target not found.");
                return;
            }

            _harmony.Patch(target,
                prefix: new HarmonyMethod(typeof(BackendEntry), nameof(GetBuildingOperationLeftTimePrefix)),
                finalizer: new HarmonyMethod(typeof(BackendEntry), nameof(GetBuildingOperationLeftTimeFinalizer)));
            Logger.Info("[CopyBuildingModernized] Patched GetBuildingOperationLeftTime.");
        }

        private static bool GetBuildingOperationLeftTimePrefix(BuildingDomain __instance,
            DataContext context, BuildingBlockKey blockKey, sbyte operationType, ref int __result)
        {
            try
            {
                if (!__instance.TryGetElement_BuildingBlocks(blockKey, out BuildingBlockData blockData) ||
                    blockData == null || blockData.TemplateId <= 0)
                {
                    __result = -1;
                    return false;
                }

                BuildingBlockItem blockConfig;
                try
                {
                    blockConfig = BuildingBlock.Instance[blockData.TemplateId];
                }
                catch
                {
                    __result = -1;
                    return false;
                }

                if (blockConfig?.OperationTotalProgress == null ||
                    operationType < 0 || operationType >= blockConfig.OperationTotalProgress.Length)
                {
                    __result = -1;
                    return false;
                }

                if (!__instance.TryGetElement_BuildingOperatorDict(blockKey, out _))
                {
                    __result = -1;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "[CopyBuildingModernized] Guarded GetBuildingOperationLeftTime failed before original method.");
                __result = -1;
                return false;
            }
        }

        private static Exception GetBuildingOperationLeftTimeFinalizer(Exception __exception,
            BuildingBlockKey blockKey, sbyte operationType, ref int __result)
        {
            if (__exception == null)
                return null;

            Logger.Warn(__exception,
                "[CopyBuildingModernized] Suppressed GetBuildingOperationLeftTime exception. Key={0}, OperationType={1}",
                blockKey, operationType);
            __result = -1;
            return null;
        }

        private void RegisterModMethods()
        {
            // 注册方法供前端 CallModMethodWithParamAndRet 调用
            DomainManager.Mod.AddModMethod(ModIdStr, "ExportVillage",
                (Func<DataContext, SerializableModData, SerializableModData>)ExportVillage);
            DomainManager.Mod.AddModMethod(ModIdStr, "ImportVillage",
                (Func<DataContext, SerializableModData, SerializableModData>)ImportVillage);
            DomainManager.Mod.AddModMethod(ModIdStr, "ConvertVillageWidth",
                (Func<DataContext, SerializableModData, SerializableModData>)ConvertVillageWidth);
        }

        // ==== 导出 ====

        public SerializableModData ExportVillage(DataContext context, SerializableModData parameter)
        {
            Logger.Info("[CopyBuildingModernized] ExportVillage called.");
            var result = new SerializableModData();
            try
            {
                parameter.Get("Path", out string exportPath);
                if (string.IsNullOrWhiteSpace(exportPath))
                {
                    result.Set("Success", false);
                    result.Set("Message", "路径为空。");
                    return result;
                }

                var domain = DomainManager.Taiwu;
                var data = BuildingDataCollector.Collect(domain,
                    msg => Logger.Info("[CM] " + msg),
                    msg => Logger.Warn("[CM] " + msg),
                    msg => Logger.Error("[CM] " + msg));
                var bytes = BuildingDataSerializer.SerializeAll(data);

                string dir = Path.GetDirectoryName(exportPath);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllBytes(exportPath, bytes);

                result.Set("Success", true);
                result.Set("Message", $"导出完成！文件：{Path.GetFileName(exportPath)}");
                Logger.Info("[CopyBuildingModernized] Exported -> {0}", exportPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[CopyBuildingModernized] Export failed");
                result.Set("Success", false);
                result.Set("Message", $"导出失败：{ex.Message}");
            }
            return result;
        }

        // ==== 导入 ====

        public SerializableModData ImportVillage(DataContext context, SerializableModData parameter)
        {
            Logger.Info("[CopyBuildingModernized] ImportVillage called.");
            var result = new SerializableModData();
            try
            {
                parameter.Get("Path", out string importPath);
                if (string.IsNullOrWhiteSpace(importPath) || !File.Exists(importPath))
                {
                    result.Set("Success", false);
                    result.Set("Message", "文件不存在。");
                    return result;
                }

                byte[] bytes = File.ReadAllBytes(importPath);
                var data = BuildingDataSerializer.DeserializeAll(bytes);
                var domain = DomainManager.Taiwu;
                BuildingDataCollector.Apply(domain, data,
                    msg => Logger.Info("[CM] " + msg),
                    msg => Logger.Warn("[CM] " + msg),
                    msg => Logger.Error("[CM] " + msg),
                    ModSettings.TrySetLeader, ModSettings.AddSkillGrade,
                    true);

                result.Set("Success", true);
                result.Set("Message", $"导入完成：{Path.GetFileName(importPath)}");
                Logger.Info("[CopyBuildingModernized] Imported from {0}", importPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[CopyBuildingModernized] Import failed");
                result.Set("Success", false);
                result.Set("Message", $"导入失败：{ex.Message}");
            }
            return result;
        }

        // ==== 宽度转换 ====

        public SerializableModData ConvertVillageWidth(DataContext context, SerializableModData parameter)
        {
            Logger.Info("[CopyBuildingModernized] ConvertVillageWidth called.");
            var result = new SerializableModData();
            try
            {
                parameter.Get("InputPath", out string inputPath);
                parameter.Get("OutputPath", out string outputPath);
                parameter.Get("TargetWidth", out string targetWidthStr);
                
                if (!sbyte.TryParse(targetWidthStr, out sbyte targetWidth))
                {
                    result.Set("Success", false);
                    result.Set("Message", $"无效的目标宽度: {targetWidthStr}");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
                {
                    result.Set("Success", false);
                    result.Set("Message", "输入文件不存在。");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    result.Set("Success", false);
                    result.Set("Message", "输出路径为空。");
                    return result;
                }

                bool success = WidthConverter.ConvertWidth(inputPath, outputPath, targetWidth);

                result.Set("Success", success);
                if (success)
                {
                    result.Set("Message", "转换成功！");
                }
                else
                {
                    // ConvertWidth内部已经通过Console.WriteLine输出详细错误
                    result.Set("Message", "转换失败，请查看Backend日志（NLog）。");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[CopyBuildingModernized] ConvertVillageWidth failed");
                result.Set("Success", false);
                result.Set("Message", $"转换失败：{ex.Message}");
            }
            return result;
        }
    }
}
