using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Config;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Character.Ai;
using GameData.Domains.Character.Creation;
using GameData.Domains.Character.ParallelModifications;
using GameData.Domains.CombatSkill;
using GameData.Domains.Item;
using GameData.Domains.Organization;
using HarmonyLib;
using Redzen.Random;
using TaiwuModdingLib.Core.Plugin;
using CharacterEntity = GameData.Domains.Character.Character;
using CombatSkillConfig = Config.CombatSkill;

namespace RespectTheStrongBackend;

[PluginConfig("RespectTheStrongBackendPlugin", "wwkk208", "2.0.0")]
public sealed class BackendEntry : TaiwuRemakePlugin
{
    private const string Prefix = "[RespectTheStrongBackend]";
    private Harmony _harmony;

    public override void Initialize()
    {
        ModLog.StartSession();
        try
        {
            ModSettings.Reload(ModIdStr);
            LogSettings();
            _harmony = new Harmony(GetGuid() + ".backend");
            InstallAll();
            OrganizationEquipmentOverride.Refresh();
            ModLog.Info("Lifecycle", $"initialized; assembly={typeof(CharacterDomain).Assembly.FullName}");
        }
        catch (Exception ex)
        {
            ModLog.Error("Lifecycle", "initialization failed", ex);
            throw;
        }
    }

    public override void OnModSettingUpdate()
    {
        try
        {
            ModSettings.Reload(ModIdStr);
            OrganizationEquipmentOverride.Refresh();
            LogSettings();
            ModLog.Info("Settings", "settings reloaded successfully");
        }
        catch (Exception ex)
        {
            ModLog.Error("Settings", "settings reload failed; previous values may remain active", ex);
            throw;
        }
    }

    public override void Dispose()
    {
        try
        {
            _harmony?.UnpatchSelf();
            _harmony = null;
            OrganizationEquipmentOverride.Restore();
            BreakoutPatches.ClearGuard();
            ModLog.Info("Lifecycle", "all Harmony patches removed and mutable configuration restored");
        }
        catch (Exception ex)
        {
            ModLog.Error("Lifecycle", "dispose failed", ex);
            throw;
        }
        finally
        {
            ModLog.EndSession();
        }
    }

    private void InstallAll()
    {
        Install("GiftLevel", typeof(ItemTemplateHelper), "GetGiftLevel",
            postfix: nameof(LowRiskPatches.GiftLevelPostfix), patchType: typeof(LowRiskPatches),
            args: new[] { typeof(sbyte), typeof(short) });
        Install("LearnLifeSkill", typeof(CharacterEntity), "LearnNewLifeSkill",
            postfix: nameof(LowRiskPatches.LearnLifeSkillPostfix), patchType: typeof(LowRiskPatches),
            args: new[] { typeof(DataContext), typeof(short), typeof(byte) });
        Install("LearnCombatSkill", typeof(CharacterEntity), "LearnNewCombatSkill",
            postfix: nameof(LowRiskPatches.LearnCombatSkillPostfix), patchType: typeof(LowRiskPatches),
            args: new[] { typeof(DataContext), typeof(short), typeof(ushort) });

        foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(CharacterCreation)))
        {
            // Three-parameter convenience overloads delegate to the terminal overloads.
            // Patching both would apply a percentage twice.
            if (method.GetParameters().Length < 4)
                continue;
            if (method.Name == "CreateMainAttributes")
                InstallMethod("ScaleMainAttributes", method, null, typeof(CreationPatches), nameof(CreationPatches.MainAttributesPostfix));
            else if (method.Name == "CreateLifeSkillQualifications")
                InstallMethod("ScaleLifeQualifications", method, null, typeof(CreationPatches), nameof(CreationPatches.LifeQualificationsPostfix));
            else if (method.Name == "CreateCombatSkillQualifications")
                InstallMethod("ScaleCombatQualifications", method, null, typeof(CreationPatches), nameof(CreationPatches.CombatQualificationsPostfix));
        }

        Install("CreationSpan", typeof(CharacterCreation), "NormalDistribute",
            prefix: nameof(CreationPatches.NormalDistributePrefix), patchType: typeof(CreationPatches),
            args: new[] { typeof(IRandomSource), typeof(int), typeof(int) });
        Install("HeroCreation", typeof(CharacterDomain), "ParallelCreateIntelligentCharacter",
            prefix: nameof(HeroPatches.CreatePrefix), postfix: nameof(HeroPatches.CreatePostfix),
            patchType: typeof(HeroPatches),
            args: new[] { typeof(DataContext), typeof(IntelligentCharacterCreationInfo).MakeByRefType(), typeof(bool) });

        Install("Promotion", typeof(SettlementCharacter), "CalcInfluencePower",
            postfix: nameof(PromotionPatches.CalcInfluencePowerPostfix), patchType: typeof(PromotionPatches),
            args: new[]
            {
                typeof(CharacterEntity), typeof(short),
                typeof(Dictionary<int, (CharacterEntity character, short baseInfluencePower)>),
                typeof(HashSet<int>)
            });

        MethodInfo breakout = AccessTools.Method(typeof(Equipping), "OfflineBreakoutCombatSkill");
        MethodInfo injury = AccessTools.Method(
            typeof(GameData.Domains.CombatSkill.CombatSkillHelper),
            "CalcForceBreakoutInjuriesAndDisorderOfQi");
        if (breakout != null && injury != null)
        {
            InstallMethod("BreakoutGuard", breakout, nameof(BreakoutPatches.BreakoutPrefix),
                typeof(BreakoutPatches), null, nameof(BreakoutPatches.BreakoutFinalizer));
            InstallMethod("BreakoutInjury", injury, nameof(BreakoutPatches.InjuryPrefix),
                typeof(BreakoutPatches), null);
        }
        else
        {
            ModLog.Warn("BreakoutNoInjury", "Disabled: target method missing");
        }

        // The current game already implements the intended sect/neili/grade/power/breakout/book scoring.
        MethodInfo score = AccessTools.Method(typeof(Equipping), "CalcCombatSkillScore");
        ModLog.Info("ImproveSkillChoiceLogic",
            $"{(score == null ? "Disabled" : "Installed(no-op compatibility)")}: current score target {Describe(score)}; no duplicate bonus installed");
    }

    private void Install(string feature, Type targetType, string targetName,
        string prefix = null, string postfix = null, Type patchType = null, Type[] args = null)
    {
        MethodInfo target = AccessTools.Method(targetType, targetName, args);
        if (target == null)
        {
            ModLog.Warn(feature, $"Disabled: {targetType.FullName}.{targetName} not found");
            return;
        }
        InstallMethod(feature, target, prefix, patchType, postfix);
    }

    private void InstallMethod(string feature, MethodInfo target, string prefix,
        Type patchType, string postfix, string finalizer = null)
    {
        try
        {
            _harmony.Patch(target,
                prefix: prefix == null ? null : new HarmonyMethod(patchType, prefix),
                postfix: postfix == null ? null : new HarmonyMethod(patchType, postfix),
                finalizer: finalizer == null
                    ? new HarmonyMethod(typeof(BackendEntry), nameof(PatchFinalizer))
                    : new HarmonyMethod(patchType, finalizer));
            ModLog.Info(feature, $"Installed: {Describe(target)}");
        }
        catch (Exception ex)
        {
            ModLog.Error(feature, $"Failed to install: {Describe(target)}", ex);
        }
    }

    private static string Describe(MethodBase method) =>
        method == null ? "<missing>" :
        $"{method.DeclaringType?.Assembly.GetName().Name}!{method.DeclaringType?.FullName}.{method.Name}({string.Join(",", method.GetParameters().Select(p => p.ParameterType.Name))})";

    private static Exception PatchFinalizer(Exception __exception, MethodBase __originalMethod)
    {
        if (__exception != null)
            ModLog.Error("RuntimePatch", $"exception in {Describe(__originalMethod)}", __exception);
        return __exception;
    }

    private static void LogSettings()
    {
        ModLog.Info("Settings",
            $"FixGrowingSectGrade={ModSettings.FixGrowingSectGrade}, GrowingSectGrade={ModSettings.GrowingSectGrade}, " +
            $"CustomSpan={ModSettings.CustomSpan}, MainScale={ModSettings.GlobalMainAttributeScale}, " +
            $"LifeScale={ModSettings.GlobalLifeSkillScale}, CombatScale={ModSettings.GlobalCombatSkillScale}, " +
            $"HeroProb={ModSettings.HeroProb}, HeroBonuses={ModSettings.HeroMainAttributeBonus}/" +
            $"{ModSettings.HeroLifeSkillBonus}/{ModSettings.HeroCombatSkillBonus}, " +
            $"HeroPerfectFeature={ModSettings.HeroPerfectFeature}, PragmaticNpc={ModSettings.PragmaticNpc}, " +
            $"CopyBooks={ModSettings.NpcCopyLifeSkillBook}/{ModSettings.NpcCopyCombatSkillBook}, " +
            $"NoBreakoutInjury={ModSettings.NpcNoBreakoutInjury}, Promotion={ModSettings.NewPromotionRule}, " +
            $"OrganizationEquipment={ModSettings.ImproveOrganizationEquipment}, Debug={ModSettings.EnableDebugLog}");
    }
}
