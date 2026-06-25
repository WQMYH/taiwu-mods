using System;
using System.Collections.Generic;
using System.Linq;
using GameData.Domains.TaiwuEvent.DisplayEvent;

namespace AutoMonthlyEvent.Executor.Frontend
{
    internal static class EventClassifier
    {
        public const string ResourceRequest = "resourceRequest";
        public const string TeaWineOrItemRequest = "teaWineOrItemRequest";
        public const string RequestResult = "requestResult";
        public const string GuidanceResult = "guidanceResult";
        public const string SparringRequest = "sparringRequest";
        public const string ChallengeOrContestRequest = "challengeOrContestRequest";
        public const string AdventureOrStory = "adventureOrStory";

        private static readonly HashSet<string> GiveOptionKeys = new HashSet<string> { "Option_1502572995" };
        private static readonly HashSet<string> RejectOptionKeys = new HashSet<string> { "Option_-1313833903" };
        private static readonly HashSet<string> ContinueOptionKeys = new HashSet<string>
        {
            "Option_530451827",
            "Option_526304089",
            "Option_-72431870"
        };

        public static string Classify(TaiwuEventDisplayData data)
        {
            string text = BuildSearchText(data);

            if (ContainsAny(text, "切磋", "请太吾指点招式", "不妨切磋"))
                return SparringRequest;

            if (ContainsAny(text, "较艺", "比武", "挑战", "邀战"))
                return ChallengeOrContestRequest;

            if (ContainsAny(text, "奇遇", "外道巢穴", "赶往", "前往"))
                return AdventureOrStory;

            if (ContainsAny(text, "解囊相助", "希望太吾", "希望能解囊", "身陷困窘", "赠予")
                && ContainsAny(text, "银钱", "木材", "金石", "织物", "药材", "食材"))
                return ResourceRequest;

            if (ContainsAny(text, "赠予", "馈赠", "讨要", "想要", "希望太吾")
                && ContainsAny(text, "茶", "酒", "物品", "兵器", "衣着", "宝物", "代步", "促织", "蛐蛐"))
                return TeaWineOrItemRequest;

            if (ContainsAny(text, "说与你听", "指点", "历练", "原来如此"))
                return GuidanceResult;

            if (ContainsAny(text, "甚是感动", "颇感失望", "只好如此", "转身离去", "关系似乎又亲近"))
                return RequestResult;

            return string.Empty;
        }

        public static bool IsRequestType(string candidateType)
        {
            return candidateType == ResourceRequest || candidateType == TeaWineOrItemRequest;
        }

        public static bool IsAutoContinueType(string candidateType)
        {
            return candidateType == RequestResult || candidateType == GuidanceResult;
        }

        public static bool IsExplicitlyUnsupported(string candidateType)
        {
            return candidateType == SparringRequest
                || candidateType == ChallengeOrContestRequest
                || candidateType == AdventureOrStory;
        }

        public static bool TryFindRequestOptions(TaiwuEventDisplayData data, out EventOptionInfo giveOption, out EventOptionInfo rejectOption, out string reason)
        {
            giveOption = default;
            rejectOption = default;
            reason = string.Empty;

            if (data.EventOptionInfos == null)
            {
                reason = "missing options";
                return false;
            }

            bool foundGive = false;
            bool foundReject = false;

            foreach (EventOptionInfo option in data.EventOptionInfos)
            {
                if (option.OptionState != 0)
                    continue;

                if (!foundGive
                    && GiveOptionKeys.Contains(option.OptionKey ?? string.Empty)
                    && ContainsAny(option.OptionContent ?? string.Empty, "赠予", "给予", "将"))
                {
                    giveOption = option;
                    foundGive = true;
                }

                if (!foundReject
                    && RejectOptionKeys.Contains(option.OptionKey ?? string.Empty)
                    && ContainsAny(option.OptionContent ?? string.Empty, "拒绝", "婉言"))
                {
                    rejectOption = option;
                    foundReject = true;
                }
            }

            if (!foundGive || !foundReject)
            {
                reason = $"request options not whitelisted or not available: give={foundGive}, reject={foundReject}";
                return false;
            }

            return true;
        }

        public static bool TryFindContinueOption(TaiwuEventDisplayData data, out EventOptionInfo option, out string reason)
        {
            option = default;
            reason = string.Empty;

            if (data.EventOptionInfos == null)
            {
                reason = "missing options";
                return false;
            }

            List<EventOptionInfo> available = data.EventOptionInfos.Where(item => item.OptionState == 0).ToList();
            if (available.Count != 1)
            {
                reason = $"available option count is {available.Count}";
                return false;
            }

            EventOptionInfo current = available[0];
            if (!ContinueOptionKeys.Contains(current.OptionKey ?? string.Empty))
            {
                reason = $"continue option key is not whitelisted: {current.OptionKey}";
                return false;
            }

            if (!ContainsAny(current.OptionContent ?? string.Empty, "如此便好", "原来如此", "只好如此"))
            {
                reason = $"continue option content is not whitelisted: {current.OptionContent}";
                return false;
            }

            option = current;
            return true;
        }

        public static string BuildSignature(TaiwuEventDisplayData data)
        {
            string optionKeys = data.EventOptionInfos == null
                ? string.Empty
                : string.Join("|", data.EventOptionInfos.Select(item => item.OptionKey ?? string.Empty));

            int mainId = data.MainCharacter?.CharacterId ?? -1;
            int targetId = data.TargetCharacter?.CharacterId ?? -1;
            return $"{data.EventGuid}|{optionKeys}|{mainId}|{targetId}|{StableHash(data.EventContent ?? string.Empty)}";
        }

        private static string BuildSearchText(TaiwuEventDisplayData data)
        {
            string options = data.EventOptionInfos == null
                ? string.Empty
                : string.Join(" ", data.EventOptionInfos.Select(option => option.OptionContent ?? string.Empty));
            return string.Join(" ", data.EventGuid ?? string.Empty, data.EventContent ?? string.Empty, options);
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            foreach (string needle in needles)
            {
                if (value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char ch in value)
                {
                    hash ^= ch;
                    hash *= 16777619;
                }
                return hash;
            }
        }
    }
}
