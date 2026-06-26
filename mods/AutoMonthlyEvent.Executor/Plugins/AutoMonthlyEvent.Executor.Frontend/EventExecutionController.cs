using System;
using System.Collections.Generic;
using GameData.Domains.Character.Display;
using GameData.Domains.TaiwuEvent.DisplayEvent;
using GameData.Utilities;
using HarmonyLib;

namespace AutoMonthlyEvent.Executor.Frontend
{
    internal static class EventExecutionController
    {
        private static ExecutorConfig? _config;
        private static readonly HashSet<string> HandledSignatures = new HashSet<string>();

        public static void Configure(ExecutorConfig config)
        {
            _config = config;
            HandledSignatures.Clear();
            FrontendAutoSelector.Configure(config);
        }

        public static void InstallPatches(Harmony harmony)
        {
            var method = AccessTools.Method(typeof(EventModel), "OnNotifyGameData");
            if (method == null)
            {
                AdaptableLog.Warning("[AutoMonthlyEvent.Executor] EventModel.OnNotifyGameData not found; executor is disabled.");
                return;
            }

            harmony.Patch(method,
                postfix: new HarmonyMethod(typeof(EventModel_OnNotifyGameData_Patch), nameof(EventModel_OnNotifyGameData_Patch.Postfix)));
            FrontendAutoSelector.InstallPatches(harmony);
        }

        public static void OnEventModelUpdated(EventModel eventModel)
        {
            ExecutorConfig? config = _config;
            if (eventModel == null)
                return;

            TaiwuEventDisplayData? data = eventModel.DisplayingEventData;
            if (config == null || data == null)
                return;

            string signature = EventClassifier.BuildSignature(data);
            string candidateType = EventClassifier.Classify(data);
            ActionLogger.Debug("observe", data.EventGuid ?? string.Empty, candidateType, BuildDebugSnapshot(data, signature));

            if (!HandledSignatures.Add(signature))
            {
                ActionLogger.Debug("dedupe", data.EventGuid ?? string.Empty, candidateType, "相同签名已处理，跳过重复窗口");
                return;
            }

            if (string.IsNullOrEmpty(candidateType))
            {
                if (config.EnableAutoExecute && config.EnableFrontendMemorySelect && FrontendAutoSelector.TryGetRememberedOption(signature, data, out string rememberedOptionKey))
                {
                    var rememberedDecision = new EventDecision
                    {
                        EventGuid = data.EventGuid ?? string.Empty,
                        CandidateType = "rememberedSelection",
                        Decision = config.DryRun ? "将复用玩家选择" : "复用玩家选择",
                        OptionKey = rememberedOptionKey,
                        Reason = "严格签名命中玩家记忆选择",
                        DryRun = config.DryRun,
                        Skipped = false
                    };
                    ActionLogger.Log(rememberedDecision);
                    ExecuteOptionIfAllowed(eventModel, config, signature, rememberedOptionKey, rememberedDecision);
                    return;
                }

                if (config.EnableAutoExecute && TryHandleFrontendSingleOption(eventModel, config, data, signature, "singleOptionContinue", "未命中支持分类，但命中安全单选项继续"))
                    return;

                ActionLogger.Debug("classify-empty", data.EventGuid ?? string.Empty, candidateType, "未命中 Executor 支持分类；不会自动执行");
                return;
            }

            if (!config.EnableAutoExecute)
            {
                ActionLogger.Debug("disabled", data.EventGuid ?? string.Empty, candidateType, "EnableAutoExecute=false；不会自动执行");
                return;
            }

            if (EventClassifier.IsExplicitlyUnsupported(candidateType))
            {
                ActionLogger.Debug("unsupported", data.EventGuid ?? string.Empty, candidateType, "显式排除事件类型");
                LogSkip(config, data, candidateType, "事件不在执行器 v1 支持范围内");
                return;
            }

            if (EventClassifier.IsAdoptionType(candidateType))
            {
                HandleAdoption(eventModel, config, data, signature);
                return;
            }

            if (EventClassifier.IsPrenatalEducationType(candidateType))
            {
                HandlePrenatalEducation(eventModel, config, data, signature);
                return;
            }

            if (EventClassifier.IsPrenatalEducationResultType(candidateType))
            {
                HandlePrenatalEducationResult(eventModel, config, data, signature);
                return;
            }

            if (EventClassifier.IsAutoContinueType(candidateType))
            {
                HandleContinue(eventModel, config, data, candidateType, signature);
                return;
            }

            if (EventClassifier.IsRequestType(candidateType))
            {
                HandleRequest(eventModel, config, data, candidateType, signature);
            }
        }

        private static void HandleContinue(EventModel eventModel, ExecutorConfig config, TaiwuEventDisplayData data, string candidateType, string signature)
        {
            bool enabled;
            string disabledReason;
            if (candidateType == EventClassifier.RequestResult)
            {
                enabled = config.EnableRequestResultContinue;
                disabledReason = "请求结果继续未启用";
            }
            else if (candidateType == EventClassifier.GuidanceResult)
            {
                enabled = config.EnableGuidanceResultContinue;
                disabledReason = "指点结果继续未启用";
            }
            else
            {
                enabled = config.AutoContinueWhitelistedResults;
                disabledReason = "白名单结果继续未启用";
            }

            if (!enabled)
            {
                if (TryHandleFrontendSingleOption(eventModel, config, data, signature, "singleOptionContinue", disabledReason + "，改由全局安全单选项继续处理"))
                    return;

                LogSkip(config, data, candidateType, disabledReason);
                return;
            }

            if (!EventClassifier.TryFindContinueOption(data, out EventOptionInfo option, out string reason))
            {
                LogSkip(config, data, candidateType, reason);
                return;
            }

            var decision = new EventDecision
            {
                EventGuid = data.EventGuid ?? string.Empty,
                CandidateType = candidateType,
                Decision = config.DryRun ? "将继续" : "继续",
                OptionKey = option.OptionKey ?? string.Empty,
                Reason = "白名单单一继续选项",
                DryRun = config.DryRun,
                Skipped = false
            };
            ActionLogger.Log(decision);

            ExecuteOptionIfAllowed(eventModel, config, signature, option.OptionKey ?? string.Empty, decision);
        }

        private static bool TryHandleFrontendSingleOption(EventModel eventModel, ExecutorConfig config, TaiwuEventDisplayData data, string signature, string candidateType, string reasonPrefix)
        {
            if (!config.EnableFrontendSingleOptionContinue)
                return false;

            if (!EventClassifier.TryFindSafeSingleContinueOption(data, out EventOptionInfo option, out string reason))
            {
                ActionLogger.Debug("single-option-skip", data.EventGuid ?? string.Empty, candidateType, reason);
                return false;
            }

            var decision = new EventDecision
            {
                EventGuid = data.EventGuid ?? string.Empty,
                CandidateType = candidateType,
                Decision = config.DryRun ? "将全局单选项继续" : "全局单选项继续",
                OptionKey = option.OptionKey ?? string.Empty,
                Reason = reasonPrefix,
                SummaryZh = $"全局单选项继续：{option.OptionContent}",
                DryRun = config.DryRun,
                Skipped = false
            };
            ActionLogger.Log(decision);
            ExecuteOptionIfAllowed(eventModel, config, signature, option.OptionKey ?? string.Empty, decision);
            return true;
        }

        private static void HandleRequest(EventModel eventModel, ExecutorConfig config, TaiwuEventDisplayData data, string candidateType, string signature)
        {
            if (candidateType == EventClassifier.MonthlyRequest && !config.EnableMonthlyRequest)
            {
                ActionLogger.Debug("request-disabled", data.EventGuid ?? string.Empty, candidateType, "EnableMonthlyRequest=false");
                LogSkip(config, data, candidateType, "请求系列 66-86 自动处理未启用");
                return;
            }

            if (candidateType == EventClassifier.ResourceRequest && !config.EnableResourceRequest)
            {
                LogSkip(config, data, candidateType, "资源请求自动处理未启用");
                return;
            }

            if (candidateType == EventClassifier.TeaWineOrItemRequest && !config.EnableTeaWineItemRequest)
            {
                LogSkip(config, data, candidateType, "茶酒物品请求自动处理未启用");
                return;
            }

            if (!EventClassifier.TryFindRequestOptions(data, out EventOptionInfo giveOption, out EventOptionInfo rejectOption, out string reason))
            {
                ActionLogger.Debug("request-options-failed", data.EventGuid ?? string.Empty, candidateType, reason + "；" + BuildOptionsText(data));
                LogSkip(config, data, candidateType, reason);
                return;
            }
            ActionLogger.Debug("request-options-ok", data.EventGuid ?? string.Empty, candidateType, $"正向={giveOption.OptionKey}/{giveOption.OptionContent}；婉拒={rejectOption.OptionKey}/{rejectOption.OptionContent}");

            CharacterDisplayData? requester = RelationConditionResolver.FindRequester(eventModel);
            if (requester == null || requester.CharacterId <= 0)
            {
                ActionLogger.Debug("requester-missing", data.EventGuid ?? string.Empty, candidateType, $"main={data.MainCharacter?.CharacterId ?? -1}; target={data.TargetCharacter?.CharacterId ?? -1}");
                LogSkip(config, data, candidateType, "无法确认请求者角色");
                return;
            }
            ActionLogger.Debug("requester-found", data.EventGuid ?? string.Empty, candidateType, $"requester={requester.CharacterId}; favorability={requester.FavorabilityToTaiwu}");

            RelationConditionResolver.Resolve(requester, result =>
            {
                try
                {
                    ActionLogger.Debug("relation-callback", data.EventGuid ?? string.Empty, candidateType, $"resolved={result.Resolved}; relation={result.RelationType}; favorability={result.Favorability}; shouldGive={result.ShouldGive}; reason={result.Reason}");
                    if (!IsCurrentSignature(eventModel, signature))
                    {
                        ActionLogger.Debug("relation-stale", data.EventGuid ?? string.Empty, candidateType, "关系回调返回时窗口签名已经变化");
                        var staleDecision = new EventDecision
                        {
                            EventGuid = data.EventGuid ?? string.Empty,
                            CandidateType = candidateType,
                            RequesterCharacterId = requester.CharacterId,
                            Decision = "skip",
                            Reason = "关系查询返回前事件窗口已变化",
                            RelationType = result.RelationType,
                            Favorability = result.Favorability,
                            RelationResolved = result.Resolved,
                            DryRun = config.DryRun,
                            Skipped = true
                        };
                        ActionLogger.Log(staleDecision);
                        return;
                    }

                    EventOptionInfo selectedOption = result.ShouldGive ? giveOption : rejectOption;
                    string action = result.ShouldGive ? "给予" : "拒绝";
                    string wouldAction = result.ShouldGive ? "将给予" : "将拒绝";
                    int templateId = EventClassifier.GetRequestTemplateId(data);
                    var decision = new EventDecision
                    {
                        EventGuid = data.EventGuid ?? string.Empty,
                        CandidateType = candidateType,
                        RequesterCharacterId = requester.CharacterId,
                        Decision = config.DryRun ? wouldAction : action,
                        OptionKey = selectedOption.OptionKey ?? string.Empty,
                        Reason = templateId > 0 ? $"{result.Reason}；templateId={templateId}" : result.Reason,
                        RelationType = result.RelationType,
                        Favorability = result.Favorability,
                        RelationResolved = result.Resolved,
                        DryRun = config.DryRun,
                        Skipped = false
                    };
                    ActionLogger.Log(decision);

                    ExecuteOptionIfAllowed(eventModel, config, signature, selectedOption.OptionKey ?? string.Empty, decision);
                }
                catch (Exception ex)
                {
                    AdaptableLog.Error($"[AutoMonthlyEvent.Executor] Request callback failed: {ex}");
                }
            });
        }

        private static void HandlePrenatalEducation(EventModel eventModel, ExecutorConfig config, TaiwuEventDisplayData data, string signature)
        {
            const string candidateType = EventClassifier.PrenatalEducation;
            if (!config.EnablePrenatalEducation)
            {
                LogSkip(config, data, candidateType, "胎教自动处理未启用");
                return;
            }

            if (!EventClassifier.TryFindPrenatalEducationOption(data, config.PrenatalEducationChoice, out EventOptionInfo option, out string reason))
            {
                ActionLogger.Debug("prenatal-option-failed", data.EventGuid ?? string.Empty, candidateType, reason + "；" + BuildOptionsText(data));
                LogSkip(config, data, candidateType, reason);
                return;
            }
            ActionLogger.Debug("prenatal-option-ok", data.EventGuid ?? string.Empty, candidateType, $"choice={config.PrenatalEducationChoice}; option={option.OptionKey}/{option.OptionContent}");

            var decision = new EventDecision
            {
                EventGuid = data.EventGuid ?? string.Empty,
                CandidateType = candidateType,
                Decision = config.DryRun ? "将胎教" : "胎教",
                OptionKey = option.OptionKey ?? string.Empty,
                Reason = $"胎教默认选项：{config.PrenatalEducationChoice}",
                SummaryZh = $"胎教：选择第 {config.PrenatalEducationChoice} 项 {option.OptionContent}",
                DryRun = config.DryRun,
                Skipped = false
            };
            ActionLogger.Log(decision);

            ExecuteOptionIfAllowed(eventModel, config, signature, option.OptionKey ?? string.Empty, decision);
        }

        private static void HandlePrenatalEducationResult(EventModel eventModel, ExecutorConfig config, TaiwuEventDisplayData data, string signature)
        {
            const string candidateType = EventClassifier.PrenatalEducationResult;
            if (!config.EnablePrenatalEducationResultContinue)
            {
                LogSkip(config, data, candidateType, "胎教结果退出未启用");
                return;
            }

            if (!EventClassifier.TryFindPrenatalEducationResultExitOption(data, out EventOptionInfo option, out string reason))
            {
                ActionLogger.Debug("prenatal-result-option-failed", data.EventGuid ?? string.Empty, candidateType, reason + "；" + BuildOptionsText(data));
                LogSkip(config, data, candidateType, reason);
                return;
            }
            ActionLogger.Debug("prenatal-result-option-ok", data.EventGuid ?? string.Empty, candidateType, $"option={option.OptionKey}/{option.OptionContent}");

            var decision = new EventDecision
            {
                EventGuid = data.EventGuid ?? string.Empty,
                CandidateType = candidateType,
                Decision = config.DryRun ? "将退出胎教结果" : "退出胎教结果",
                OptionKey = option.OptionKey ?? string.Empty,
                Reason = "胎教 3->1 链路：结果窗口确认退出",
                SummaryZh = $"胎教结果：{option.OptionContent}",
                DryRun = config.DryRun,
                Skipped = false
            };
            ActionLogger.Log(decision);

            ExecuteOptionIfAllowed(eventModel, config, signature, option.OptionKey ?? string.Empty, decision);
        }

        private static void HandleAdoption(EventModel eventModel, ExecutorConfig config, TaiwuEventDisplayData data, string signature)
        {
            const string candidateType = EventClassifier.AdoptAbandonedBaby;
            if (!config.EnableAdoptAbandonedBaby)
            {
                LogSkip(config, data, candidateType, "收养弃婴自动处理未启用");
                return;
            }

            if (!EventClassifier.TryFindAdoptionOptions(data, out EventOptionInfo adoptOption, out EventOptionInfo postponeOption, out string reason))
            {
                ActionLogger.Debug("adoption-options-failed", data.EventGuid ?? string.Empty, candidateType, reason + "；" + BuildOptionsText(data));
                LogSkip(config, data, candidateType, reason);
                return;
            }

            CharacterDisplayData? child = FindAdoptionChild(data, config, out string childReason);
            bool shouldAdopt = child != null && config.AllowedAdoptionBehaviorTypes.Contains((sbyte)child.BehaviorType);
            EventOptionInfo selectedOption = shouldAdopt ? adoptOption : postponeOption;
            string behaviorName = child == null ? "未知" : GetBehaviorTypeName((sbyte)child.BehaviorType);
            string decisionText = shouldAdopt
                ? (config.DryRun ? "将收养" : "收养")
                : (config.DryRun ? "将搁置" : "搁置");
            string reasonText = child == null
                ? "无法确认婴孩角色，按保守规则搁置"
                : shouldAdopt
                    ? $"婴孩立场允许：{behaviorName}"
                    : $"婴孩立场不在允许列表：{behaviorName}";

            if (!string.IsNullOrEmpty(childReason))
                reasonText = reasonText + "；" + childReason;

            var decision = new EventDecision
            {
                EventGuid = data.EventGuid ?? string.Empty,
                CandidateType = candidateType,
                SubjectCharacterId = child?.CharacterId ?? -1,
                BehaviorType = child == null ? -1 : (sbyte)child.BehaviorType,
                Decision = decisionText,
                OptionKey = selectedOption.OptionKey ?? string.Empty,
                Reason = reasonText,
                SummaryZh = $"收养弃婴：角色={child?.CharacterId ?? -1}，立场={behaviorName}，决定={decisionText}",
                DryRun = config.DryRun,
                Skipped = false
            };
            ActionLogger.Log(decision);

            ExecuteOptionIfAllowed(eventModel, config, signature, selectedOption.OptionKey ?? string.Empty, decision);
        }

        private static void ExecuteOptionIfAllowed(EventModel eventModel, ExecutorConfig config, string signature, string optionKey, EventDecision decision)
        {
            if (config.DryRun)
            {
                ActionLogger.Debug("execute-dryrun", decision.EventGuid, decision.CandidateType, $"DryRun=true; option={optionKey}");
                return;
            }

            if (!IsCurrentSignature(eventModel, signature))
            {
                ActionLogger.Debug("execute-stale-before", decision.EventGuid, decision.CandidateType, $"执行前签名变化；option={optionKey}");
                decision.Decision = "skip";
                decision.Reason = "执行前事件窗口已变化";
                decision.Skipped = true;
                ActionLogger.Log(decision);
                return;
            }

            if (!IsCurrentOptionAvailable(eventModel, optionKey))
            {
                ActionLogger.Debug("execute-option-unavailable", decision.EventGuid, decision.CandidateType, $"执行前目标选项不可用；option={optionKey}; current={BuildOptionsText(eventModel.DisplayingEventData)}");
                decision.Decision = "skip";
                decision.Reason = "目标选项已不可用";
                decision.Skipped = true;
                ActionLogger.Log(decision);
                return;
            }

            ActionLogger.Debug("execute-enqueue-before", decision.EventGuid, decision.CandidateType, $"提交给 EventWindow.Update 延迟选择；option={optionKey}");
            FrontendAutoSelector.Enqueue(signature, optionKey, decision);
        }

        private static bool IsCurrentSignature(EventModel eventModel, string signature)
        {
            TaiwuEventDisplayData? data = eventModel?.DisplayingEventData;
            return data != null && EventClassifier.BuildSignature(data) == signature;
        }

        private static bool IsCurrentOptionAvailable(EventModel eventModel, string optionKey)
        {
            TaiwuEventDisplayData? data = eventModel?.DisplayingEventData;
            if (data?.EventOptionInfos == null)
                return false;

            foreach (EventOptionInfo option in data.EventOptionInfos)
            {
                if (option.OptionKey == optionKey && option.OptionState == 0)
                    return true;
            }
            return false;
        }

        private static void LogSkip(ExecutorConfig config, TaiwuEventDisplayData data, string candidateType, string reason)
        {
            ActionLogger.Debug("skip", data.EventGuid ?? string.Empty, candidateType, reason);
            ActionLogger.Log(new EventDecision
            {
                EventGuid = data.EventGuid ?? string.Empty,
                CandidateType = candidateType,
                Decision = "skip",
                Reason = reason,
                DryRun = config.DryRun,
                Skipped = true
            });
        }

        private static CharacterDisplayData? FindAdoptionChild(TaiwuEventDisplayData data, ExecutorConfig config, out string reason)
        {
            reason = string.Empty;
            CharacterDisplayData? main = IsValidCharacter(data.MainCharacter) ? data.MainCharacter : null;
            CharacterDisplayData? target = IsValidCharacter(data.TargetCharacter) ? data.TargetCharacter : null;

            bool mainIsChild = IsAdoptionAgeCandidate(main, config);
            bool targetIsChild = IsAdoptionAgeCandidate(target, config);
            if (mainIsChild && targetIsChild)
                return CompareYounger(main, target) <= 0 ? main : target;

            if (mainIsChild)
                return main;

            if (targetIsChild)
                return target;

            reason = $"未找到年龄不超过 {config.AdoptionMaxChildAge} 岁的候选角色";
            return null;
        }

        private static bool IsValidCharacter(CharacterDisplayData? character)
        {
            return character != null && character.CharacterId > 0;
        }

        private static bool IsAdoptionAgeCandidate(CharacterDisplayData? character, ExecutorConfig config)
        {
            return IsValidCharacter(character)
                && character!.CurrAge >= 0
                && character.CurrAge <= config.AdoptionMaxChildAge;
        }

        private static int CompareYounger(CharacterDisplayData? left, CharacterDisplayData? right)
        {
            if (left == null)
                return right == null ? 0 : 1;
            if (right == null)
                return -1;

            int ageCompare = left.CurrAge.CompareTo(right.CurrAge);
            if (ageCompare != 0)
                return ageCompare;
            return left.PhysiologicalAge.CompareTo(right.PhysiologicalAge);
        }

        private static string GetBehaviorTypeName(sbyte behaviorType)
        {
            switch (behaviorType)
            {
                case 0:
                    return "刚正";
                case 1:
                    return "仁善";
                case 2:
                    return "中庸";
                case 3:
                    return "叛逆";
                case 4:
                    return "唯我";
                default:
                    return "未知(" + behaviorType + ")";
            }
        }

        private static string BuildDebugSnapshot(TaiwuEventDisplayData data, string signature)
        {
            return $"signature={signature}; main={data.MainCharacter?.CharacterId ?? -1}; target={data.TargetCharacter?.CharacterId ?? -1}; optionCount={data.EventOptionInfos?.Count ?? 0}; options={BuildOptionsText(data)}";
        }

        private static string BuildOptionsText(TaiwuEventDisplayData? data)
        {
            if (data?.EventOptionInfos == null)
                return "null";

            var parts = new List<string>();
            foreach (EventOptionInfo option in data.EventOptionInfos)
                parts.Add($"{option.OptionKey}|state={option.OptionState}|behavior={option.Behavior}|content={option.OptionContent}");
            return string.Join(" || ", parts);
        }
    }

    internal static class EventModel_OnNotifyGameData_Patch
    {
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(EventModel __instance)
        {
            EventExecutionController.OnEventModelUpdated(__instance);
        }
    }
}
