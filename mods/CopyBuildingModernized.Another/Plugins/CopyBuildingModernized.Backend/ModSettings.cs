using System;
using GameData.Domains;
using NLog;

namespace CopyBuildingModernized.Backend
{
    internal static class ModSettings
    {
        internal static bool TrySetLeader;
        internal static bool CleanOperationStateOnImport;
        internal static int AddSkillGrade = -1;

        internal static void Load(string modId, Logger logger)
        {
            Get(modId, "trySetLeader", ref TrySetLeader, logger);
            Get(modId, "cleanOperationStateOnImport", ref CleanOperationStateOnImport, logger);
            Get(modId, "AddSkillGrade", ref AddSkillGrade, logger);
        }

        private static void Get(string modId, string key, ref bool value, Logger logger)
        {
            try { DomainManager.Mod.GetSetting(modId, key, ref value); }
            catch (Exception ex) { logger.Warn(ex, "Failed to read setting {0}", key); }
        }

        private static void Get(string modId, string key, ref int value, Logger logger)
        {
            try { DomainManager.Mod.GetSetting(modId, key, ref value); }
            catch (Exception ex) { logger.Warn(ex, "Failed to read setting {0}", key); }
        }
    }
}
