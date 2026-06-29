using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using GameData.Common;
using GameData.Domains.Character;
using HarmonyLib;

namespace AutoMonthlyEvent.Executor.Backend
{
    internal static class BackendEventInterceptor
    {
        private static BackendExecutorConfig? _config;
        private static MethodInfo? _requestApplyChanges;
        private static MethodInfo? _giftApplyChanges;
        private static FieldInfo? _requestAgreeField;
        private static FieldInfo? _giftRefusePoisonField;
        private static long _diagnosticSequence;

        public static void Configure(BackendExecutorConfig config)
        {
            _config = config;
        }

        public static void Install(Harmony harmony)
        {
            try
            {
                Type? requestType = AccessTools.TypeByName("GameData.Domains.Character.Ai.GeneralAction.WealthDemand.RequestItemAction");
                Type? giftType = AccessTools.TypeByName("GameData.Domains.Character.Ai.GeneralAction.BehaviorAction.GiveItemAction");
                Type[] parameters = { typeof(DataContext), typeof(Character), typeof(Character) };

                if (requestType != null)
                {
                    MethodInfo? requestInitial = AccessTools.Method(requestType, "ApplyInitialChangesForTaiwu", parameters);
                    _requestApplyChanges = AccessTools.Method(requestType, "ApplyChanges", parameters);
                    _requestAgreeField = AccessTools.Field(requestType, "AgreeToRequest");
                    if (requestInitial != null && _requestApplyChanges != null && _requestAgreeField != null)
                    {
                        harmony.Patch(requestInitial, prefix: new HarmonyMethod(typeof(BackendEventInterceptor), nameof(RequestItemPrefix)));
                        BackendActionLogger.Log("install-request-item", "installed", "RequestItemAction patch installed");
                    }
                    else
                    {
                        BackendActionLogger.Log("install-request-item", "skip", "RequestItemAction method or field missing");
                    }
                }

                if (giftType != null)
                {
                    MethodInfo? giftInitial = AccessTools.Method(giftType, "ApplyInitialChangesForTaiwu", parameters);
                    _giftApplyChanges = AccessTools.Method(giftType, "ApplyChanges", parameters);
                    _giftRefusePoisonField = AccessTools.Field(giftType, "RefusePoisonousItem");
                    if (giftInitial != null && _giftApplyChanges != null && _giftRefusePoisonField != null)
                    {
                        harmony.Patch(giftInitial, prefix: new HarmonyMethod(typeof(BackendEventInterceptor), nameof(GiveItemPrefix)));
                        BackendActionLogger.Log("install-give-item", "installed", "GiveItemAction patch installed");
                    }
                    else
                    {
                        BackendActionLogger.Log("install-give-item", "skip", "GiveItemAction method or field missing");
                    }
                }

                InstallSettlementDiagnostics(harmony);
                BackendActionLogger.Log("install-prenatal-education", "observe-only", "AddPrenatalEducationTaiwu is intentionally not swallowed in v1");
            }
            catch (Exception ex)
            {
                BackendActionLogger.Log("install", "failed", "Backend interceptor install failed", exception: ex);
            }
        }

        private static void InstallSettlementDiagnostics(Harmony harmony)
        {
            var patched = new HashSet<MethodBase>();
            Type? monthlyCollectionType = AccessTools.TypeByName("GameData.Domains.World.MonthlyEvent.MonthlyEventCollection");
            Type? taiwuEventDomainType = AccessTools.TypeByName("GameData.Domains.TaiwuEvent.TaiwuEventDomain");

            PatchNamedMethods(harmony, monthlyCollectionType, patched,
                name => name == "AddMakeLoveWithTaiwu");
            PatchNamedMethods(harmony, taiwuEventDomainType, patched,
                name => name == "EventSelect");
            PatchNamedMethods(harmony, typeof(CharacterDomain), patched,
                IsSettlementDiagnosticMethod);
            PatchNamedMethods(harmony, typeof(Character), patched,
                IsSettlementDiagnosticMethod);

            BackendActionLogger.Log(
                "install-settlement-diagnostics",
                "installed",
                $"observer patches={patched.Count}; no original method is skipped");
        }

        private static void PatchNamedMethods(
            Harmony harmony,
            Type? type,
            HashSet<MethodBase> patched,
            Func<string, bool> predicate)
        {
            if (type == null)
                return;

            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
            {
                if (!predicate(method.Name) || method.IsAbstract || method.ContainsGenericParameters || !patched.Add(method))
                    continue;

                try
                {
                    harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(typeof(BackendEventInterceptor), nameof(SettlementDiagnosticPrefix)),
                        postfix: new HarmonyMethod(typeof(BackendEventInterceptor), nameof(SettlementDiagnosticPostfix)));
                    BackendActionLogger.Log(
                        "install-settlement-observer",
                        "installed",
                        DescribeMethod(method));
                }
                catch (Exception ex)
                {
                    patched.Remove(method);
                    BackendActionLogger.Log(
                        "install-settlement-observer",
                        "failed",
                        DescribeMethod(method),
                        exception: ex);
                }
            }
        }

        private static bool IsSettlementDiagnosticMethod(string name)
        {
            return name.IndexOf("Pregnant", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Pregnancy", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("MakeLove", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void SettlementDiagnosticPrefix(MethodBase __originalMethod, object[] __args, out long __state)
        {
            __state = Interlocked.Increment(ref _diagnosticSequence);
            BackendExecutorConfig? config = _config;
            if (config == null || !config.EnableDebugLog)
                return;

            string reason = $"seq={__state}; phase=enter; method={DescribeMethod(__originalMethod)}; args={DescribeArgs(__args)}";
            if (__originalMethod.Name == "AddMakeLoveWithTaiwu")
                reason += "; stack=" + CompactStackTrace();
            BackendActionLogger.Log("settlement-trace", "observe", reason);
        }

        private static void SettlementDiagnosticPostfix(MethodBase __originalMethod, object[] __args, long __state)
        {
            BackendExecutorConfig? config = _config;
            if (config == null || !config.EnableDebugLog)
                return;

            BackendActionLogger.Log(
                "settlement-trace",
                "observe",
                $"seq={__state}; phase=exit; method={DescribeMethod(__originalMethod)}; args={DescribeArgs(__args)}");
        }

        private static string DescribeMethod(MethodBase method)
        {
            return $"{method.DeclaringType?.FullName}.{method.Name}";
        }

        private static string DescribeArgs(object[]? args)
        {
            if (args == null || args.Length == 0)
                return "[]";

            var builder = new StringBuilder("[");
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    builder.Append(", ");
                builder.Append(i).Append('=').Append(DescribeArg(args[i]));
            }
            return builder.Append(']').ToString();
        }

        private static string DescribeArg(object? value)
        {
            if (value == null)
                return "null";
            if (value is Character character)
                return $"Character#{GetCharId(character)}";
            if (value is string text)
                return text.Length <= 80 ? $"\"{text}\"" : $"\"{text.Substring(0, 80)}...\"";
            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is decimal || value is Guid)
                return Convert.ToString(value) ?? type.Name;
            return type.FullName ?? type.Name;
        }

        private static string CompactStackTrace()
        {
            string[] lines = Environment.StackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int count = Math.Min(lines.Length, 8);
            return string.Join(" <- ", lines, 0, count);
        }

        private static bool RequestItemPrefix(object __instance, DataContext context, Character selfChar, Character targetChar)
        {
            BackendExecutorConfig? config = _config;
            if (config == null || !config.EffectiveItemRequestInterceptor)
            {
                BackendActionLogger.Log("request-item", "pass", "backend item request interceptor disabled", GetCharId(selfChar), GetCharId(targetChar));
                return true;
            }

            if (targetChar == null || !targetChar.IsTaiwu())
            {
                BackendActionLogger.Log("request-item", "pass", "target is not Taiwu", GetCharId(selfChar), GetCharId(targetChar));
                return true;
            }

            try
            {
                // v1 backend strategy is conservative: reject item requests only when the explicit backend switch is enabled.
                _requestAgreeField?.SetValue(__instance, false);
                _requestApplyChanges?.Invoke(__instance, new object[] { context, selfChar, targetChar });
                BackendActionLogger.Log("request-item", "handled-reject", "called original ApplyChanges and skipped event window", GetCharId(selfChar), GetCharId(targetChar));
                return false;
            }
            catch (Exception ex)
            {
                BackendActionLogger.Log("request-item", "pass-exception", "ApplyChanges failed; falling back to original flow", GetCharId(selfChar), GetCharId(targetChar), ex);
                return true;
            }
        }

        private static bool GiveItemPrefix(object __instance, DataContext context, Character selfChar, Character targetChar)
        {
            BackendExecutorConfig? config = _config;
            if (config == null || !config.EffectiveGiftInterceptor)
            {
                BackendActionLogger.Log("give-item", "pass", "backend gift interceptor disabled", GetCharId(selfChar), GetCharId(targetChar));
                return true;
            }

            if (targetChar == null || !targetChar.IsTaiwu())
            {
                BackendActionLogger.Log("give-item", "pass", "target is not Taiwu", GetCharId(selfChar), GetCharId(targetChar));
                return true;
            }

            try
            {
                _giftRefusePoisonField?.SetValue(__instance, true);
                _giftApplyChanges?.Invoke(__instance, new object[] { context, selfChar, targetChar });
                BackendActionLogger.Log("give-item", "handled", "called original ApplyChanges and skipped event window", GetCharId(selfChar), GetCharId(targetChar));
                return false;
            }
            catch (Exception ex)
            {
                BackendActionLogger.Log("give-item", "pass-exception", "ApplyChanges failed; falling back to original flow", GetCharId(selfChar), GetCharId(targetChar), ex);
                return true;
            }
        }

        private static int GetCharId(Character? character)
        {
            return character == null ? -1 : character.GetId();
        }
    }
}
