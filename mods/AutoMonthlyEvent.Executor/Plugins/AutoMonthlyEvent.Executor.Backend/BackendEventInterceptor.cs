using System;
using System.Collections.Generic;
using System.Reflection;
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
                Type? monthlyCollectionType = AccessTools.TypeByName("GameData.Domains.World.MonthlyEvent.MonthlyEventCollection");
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

                MethodInfo? makeLoveMethod = null;
                if (monthlyCollectionType != null)
                {
                    List<MethodInfo> methods = AccessTools.GetDeclaredMethods(monthlyCollectionType);
                    foreach (MethodInfo method in methods)
                    {
                        if (method.Name == "AddMakeLoveWithTaiwu")
                        {
                            makeLoveMethod = method;
                            break;
                        }
                    }
                }

                if (makeLoveMethod != null)
                {
                    harmony.Patch(makeLoveMethod, prefix: new HarmonyMethod(typeof(BackendEventInterceptor), nameof(MakeLoveEventPrefix)));
                    BackendActionLogger.Log("install-make-love", "installed", "MonthlyEventCollection.AddMakeLoveWithTaiwu patch installed");
                }
                else
                {
                    BackendActionLogger.Log("install-make-love", "skip", "AddMakeLoveWithTaiwu method missing");
                }

                BackendActionLogger.Log("install-prenatal-education", "observe-only", "AddPrenatalEducationTaiwu is intentionally not swallowed in v1");
            }
            catch (Exception ex)
            {
                BackendActionLogger.Log("install", "failed", "Backend interceptor install failed", exception: ex);
            }
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

        private static bool MakeLoveEventPrefix()
        {
            BackendExecutorConfig? config = _config;
            if (config == null || !config.EffectiveMakeLoveInterceptor)
            {
                BackendActionLogger.Log("make-love", "pass", "backend make-love interceptor disabled");
                return true;
            }

            BackendActionLogger.Log("make-love", "skip-add", "blocked AddMakeLoveWithTaiwu monthly event");
            return false;
        }

        private static int GetCharId(Character? character)
        {
            return character == null ? -1 : character.GetId();
        }
    }
}
