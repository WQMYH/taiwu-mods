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
            if (!HandledSignatures.Add(signature))
                return;

            string candidateType = EventClassifier.Classify(data);
            if (string.IsNullOrEmpty(candidateType))
                return;

            if (!config.EnableAutoExecute)
                return;

            if (EventClassifier.IsExplicitlyUnsupported(candidateType))
            {
                LogSkip(config, data, candidateType, "事件不在执行器 v1 支持范围内");
                return;
            }

            if (EventClassifier.IsAdoptionType(candidateType))
            {
                HandleAdoption(eventModel, config, data, signature);
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
            if (!config.AutoContinueWhitelistedResults)
            {
                LogSkip(config, data, candidateType, "结果继续总开关未启用");
                return;
            }

            if (candidateType == EventClassifier.RequestResult && !config.EnableRequestResultContinue)
            {
                LogSkip(config, data, candidateType, "请求结果继续未启用");
                return;
            }

            if (candidateType == EventClassifier.GuidanceResult && !config.EnableGuidanceResultContinue)
            {
                LogSkip(config, data, candidateType, "指点结果继续未启用");
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

        private static void HandleRequest(EventModel eventModel, ExecutorConfig config, TaiwuEventDisplayData data, string candidateType, string signature)
        {
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
                LogSkip(config, data, candidateType, reason);
                return;
            }

            CharacterDisplayData? requester = RelationConditionResolver.FindRequester(eventModel);
            if (requester == null || requester.CharacterId <= 0)
            {
                LogSkip(config, data, candidateType, "无法确认请求者角色");
                return;
            }

            RelationConditionResolver.Resolve(requester, result =>
            {
                try
                {
                    if (!IsCurrentSignature(eventModel, signature))
                    {
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
                    var decision = new EventDecision
                    {
                        EventGuid = data.EventGuid ?? string.Empty,
                        CandidateType = candidateType,
                        RequesterCharacterId = requester.CharacterId,
                        Decision = config.DryRun ? wouldAction : action,
                        OptionKey = selectedOption.OptionKey ?? string.Empty,
                        Reason = result.Reason,
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
                return;

            if (!IsCurrentSignature(eventModel, signature))
            {
                decision.Decision = "skip";
                decision.Reason = "执行前事件窗口已变化";
                decision.Skipped = true;
                ActionLogger.Log(decision);
                return;
            }

            if (!IsCurrentOptionAvailable(eventModel, optionKey))
            {
                decision.Decision = "skip";
                decision.Reason = "目标选项已不可用";
                decision.Skipped = true;
                ActionLogger.Log(decision);
                return;
            }

            eventModel.Select(optionKey);
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
    }

    internal static class EventModel_OnNotifyGameData_Patch
    {
        public static void Postfix(EventModel __instance)
        {
            EventExecutionController.OnEventModelUpdated(__instance);
        }
    }
}
