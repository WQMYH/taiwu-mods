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

            if (!config.EnableAutoExecute)
                return;

            string signature = EventClassifier.BuildSignature(data);
            if (!HandledSignatures.Add(signature))
                return;

            string candidateType = EventClassifier.Classify(data);
            if (string.IsNullOrEmpty(candidateType))
                return;

            if (EventClassifier.IsExplicitlyUnsupported(candidateType))
            {
                LogSkip(config, data, candidateType, "candidate type is outside executor v1 scope");
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
                LogSkip(config, data, candidateType, "auto continue disabled");
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
                Decision = config.DryRun ? "wouldContinue" : "continue",
                OptionKey = option.OptionKey ?? string.Empty,
                Reason = "whitelisted single continue option",
                DryRun = config.DryRun,
                Skipped = false
            };
            ActionLogger.Log(decision);

            ExecuteOptionIfAllowed(eventModel, config, signature, option.OptionKey ?? string.Empty, decision);
        }

        private static void HandleRequest(EventModel eventModel, ExecutorConfig config, TaiwuEventDisplayData data, string candidateType, string signature)
        {
            if (!EventClassifier.TryFindRequestOptions(data, out EventOptionInfo giveOption, out EventOptionInfo rejectOption, out string reason))
            {
                LogSkip(config, data, candidateType, reason);
                return;
            }

            CharacterDisplayData? requester = RelationConditionResolver.FindRequester(eventModel);
            if (requester == null || requester.CharacterId <= 0)
            {
                LogSkip(config, data, candidateType, "requester character not found");
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
                            Reason = "event window changed before relation callback",
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
                    string action = result.ShouldGive ? "give" : "reject";
                    string wouldAction = result.ShouldGive ? "wouldGive" : "wouldReject";
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

        private static void ExecuteOptionIfAllowed(EventModel eventModel, ExecutorConfig config, string signature, string optionKey, EventDecision decision)
        {
            if (config.DryRun)
                return;

            if (!IsCurrentSignature(eventModel, signature))
            {
                decision.Decision = "skip";
                decision.Reason = "event window changed before execute";
                decision.Skipped = true;
                ActionLogger.Log(decision);
                return;
            }

            if (!IsCurrentOptionAvailable(eventModel, optionKey))
            {
                decision.Decision = "skip";
                decision.Reason = "target option is no longer available";
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
    }

    internal static class EventModel_OnNotifyGameData_Patch
    {
        public static void Postfix(EventModel __instance)
        {
            EventExecutionController.OnEventModelUpdated(__instance);
        }
    }
}
