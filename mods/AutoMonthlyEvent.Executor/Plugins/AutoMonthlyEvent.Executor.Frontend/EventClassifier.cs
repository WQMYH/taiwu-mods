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
        public const string MonthlyRequest = "monthlyRequest";
        public const string RequestResult = "requestResult";
        public const string GuidanceResult = "guidanceResult";
        public const string AdoptAbandonedBaby = "adoptAbandonedBaby";
        public const string PrenatalEducation = "prenatalEducation";
        public const string PrenatalEducationResult = "prenatalEducationResult";
        public const string SparringRequest = "sparringRequest";
        public const string ChallengeOrContestRequest = "challengeOrContestRequest";
        public const string AdventureOrStory = "adventureOrStory";

        private const string AdoptAbandonedBabyEventGuid = "35dbcaf7-a830-419e-9fea-2b2cf88b8bfb";
        private const string AdoptBabyOptionKey = "Option_1751715976";
        private const string PostponeAdoptionOptionKey = "Option_-899805435";
        private const string PrenatalEducationEventGuid = "a73cc160-a95d-42c3-b986-a0353df434f0";
        private const string PrenatalEducationResultEventGuid = "757f2f50-9ed0-406e-b753-b6e76b8e87b1";
        private const string PrenatalEducationResultExitOptionKey = "Option_-754469636";

        private static readonly Dictionary<string, int> RequestTemplateIdsByGuid = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "3af56371-f736-4aa9-ae9b-c3c98ba0f4b0", 66 },
            { "d58fceec-2d53-4532-9893-d834db626b35", 67 },
            { "3f335f54-d401-4951-9b9e-61c82c7d5bbc", 68 },
            { "3c71d6c1-f5a0-4e20-9eca-280dde1b2f84", 69 },
            { "1deadcc9-cca4-4091-8276-5f3a8b136fe0", 70 },
            { "d799f11c-7ff7-4f9e-83a6-192753411e7b", 71 },
            { "66fe331f-7bb2-422b-9a3e-892859eedb55", 72 },
            { "03b005e1-e0c6-4726-abc7-4e3121116049", 73 },
            { "d2e80cb3-4c6f-471d-83f9-422bbcab2d7f", 74 },
            { "d7ec7e02-ee62-4137-a8cb-a4961ec28bb0", 75 },
            { "560cee7d-3955-4650-8fbf-cf66e80a321d", 76 },
            { "e6a1795d-e1a1-46af-b977-f821fc1441f5", 77 },
            { "16bddd08-6083-4c11-9342-78922ed19b6b", 78 },
            { "7ac0429a-a48f-4adb-be4b-4cb767d978e4", 79 },
            { "a61df3c7-c29b-4a81-b4c8-da7651c931e8", 80 },
            { "7386d3bc-3ebe-4603-b771-1f61f7a166b2", 81 },
            { "cbb59e89-a125-42ab-8692-e522c44a0bc8", 82 },
            { "eb7f0c2a-60f9-4221-97e5-662d513620c1", 83 },
            { "1735b4a9-4ece-4ff9-83f3-fa005cfa33e8", 84 },
            { "9804522b-8380-4950-9996-f15a8387d802", 85 },
            { "6c91f53d-6eb8-43e5-812d-e1838cab5c57", 86 },
        };

        private static readonly Dictionary<int, string> PrenatalEducationOptionsByChoice = new Dictionary<int, string>
        {
            { 1, "Option_-1741174624" },
            { 2, "Option_863571446" },
            { 3, "Option_-1326978337" },
        };

        private static readonly HashSet<string> ContinueOptionKeys = new HashSet<string>
        {
            "Option_530451827",
            "Option_526304089",
            "Option_-72431870"
        };

        public static string Classify(TaiwuEventDisplayData data)
        {
            string text = BuildSearchText(data);
            string eventGuid = data.EventGuid ?? string.Empty;

            if (RequestTemplateIdsByGuid.ContainsKey(eventGuid))
                return MonthlyRequest;

            if (string.Equals(eventGuid, PrenatalEducationEventGuid, StringComparison.OrdinalIgnoreCase))
                return PrenatalEducation;

            if (string.Equals(eventGuid, PrenatalEducationResultEventGuid, StringComparison.OrdinalIgnoreCase))
                return PrenatalEducationResult;

            if (string.Equals(eventGuid, AdoptAbandonedBabyEventGuid, StringComparison.OrdinalIgnoreCase)
                || (ContainsAny(text, "收养弃婴") && ContainsAny(text, "收养至太吾村", "暂且搁置")))
                return AdoptAbandonedBaby;

            if (ContainsAny(text, "如此便好", "甚是感动", "颇感失望", "只好如此", "转身离去", "关系似乎又亲近"))
                return RequestResult;

            if (ContainsAny(text, "原来如此") && !ContainsAny(text, "请求", "相助"))
                return GuidanceResult;

            if (ContainsAny(text, "切磋", "请太吾指点招式", "不妨切磋"))
                return SparringRequest;

            if (ContainsAny(text, "较艺", "比武", "挑战", "邀战"))
                return ChallengeOrContestRequest;

            if (ContainsAny(text, "奇遇", "外道巢穴", "赶往", "前往"))
                return AdventureOrStory;

            if (ContainsAny(text, "解囊相助", "希望太吾", "希望能解囊", "身陷困窘", "赠予")
                && ContainsAny(text, "银钱", "木材", "金石", "金铁", "玉石", "织物", "药材", "食材"))
                return ResourceRequest;

            if (ContainsAny(text, "赠予", "赠与", "馈赠", "讨要", "想要", "希望太吾")
                && ContainsAny(text, "茶", "酒", "物品", "兵器", "衣着", "宝物", "代步", "促织", "蛐蛐"))
                return TeaWineOrItemRequest;

            if (ContainsAny(text, "说与你听", "指点", "历练", "原来如此"))
                return GuidanceResult;

            return string.Empty;
        }

        public static bool IsRequestType(string candidateType)
        {
            return candidateType == MonthlyRequest || candidateType == ResourceRequest || candidateType == TeaWineOrItemRequest;
        }

        public static bool IsAutoContinueType(string candidateType)
        {
            return candidateType == RequestResult || candidateType == GuidanceResult;
        }

        public static bool IsAdoptionType(string candidateType)
        {
            return candidateType == AdoptAbandonedBaby;
        }

        public static bool IsPrenatalEducationType(string candidateType)
        {
            return candidateType == PrenatalEducation;
        }

        public static bool IsPrenatalEducationResultType(string candidateType)
        {
            return candidateType == PrenatalEducationResult;
        }

        public static int GetRequestTemplateId(TaiwuEventDisplayData data)
        {
            return data.EventGuid != null && RequestTemplateIdsByGuid.TryGetValue(data.EventGuid, out int templateId)
                ? templateId
                : -1;
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
                reason = "缺少事件选项";
                return false;
            }

            bool foundGive = false;
            bool foundReject = false;

            foreach (EventOptionInfo option in data.EventOptionInfos)
            {
                if (option.OptionState != 0)
                    continue;

                string optionContent = option.OptionContent ?? string.Empty;
                if (!foundGive && IsPositiveRequestOption(option, optionContent))
                {
                    giveOption = option;
                    foundGive = true;
                }

                if (!foundReject && IsPoliteRejectOption(option, optionContent))
                {
                    rejectOption = option;
                    foundReject = true;
                }
            }

            if (!foundGive || !foundReject)
            {
                reason = $"请求选项未找到或不可用：正向={foundGive}, 婉拒={foundReject}";
                return false;
            }

            return true;
        }

        public static bool TryFindPrenatalEducationOption(TaiwuEventDisplayData data, int choice, out EventOptionInfo option, out string reason)
        {
            option = default;
            reason = string.Empty;

            if (!PrenatalEducationOptionsByChoice.TryGetValue(choice, out string optionKey))
            {
                reason = $"胎教配置非法：{choice}";
                return false;
            }

            if (data.EventOptionInfos == null)
            {
                reason = "缺少胎教选项";
                return false;
            }

            foreach (EventOptionInfo current in data.EventOptionInfos)
            {
                if (current.OptionState == 0 && current.OptionKey == optionKey)
                {
                    option = current;
                    return true;
                }
            }

            reason = $"胎教选项不可用：choice={choice}, optionKey={optionKey}";
            return false;
        }

        public static bool TryFindPrenatalEducationResultExitOption(TaiwuEventDisplayData data, out EventOptionInfo option, out string reason)
        {
            option = default;
            reason = string.Empty;

            if (data.EventOptionInfos == null)
            {
                reason = "缺少胎教结果选项";
                return false;
            }

            foreach (EventOptionInfo current in data.EventOptionInfos)
            {
                string optionContent = current.OptionContent ?? string.Empty;
                if (current.OptionState == 0
                    && current.OptionKey == PrenatalEducationResultExitOptionKey
                    && ContainsAny(optionContent, "心满意足"))
                {
                    option = current;
                    return true;
                }
            }

            reason = $"胎教结果退出选项不可用：optionKey={PrenatalEducationResultExitOptionKey}";
            return false;
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
            if (!ContainsAny(current.OptionContent ?? string.Empty, "如此便好", "原来如此", "只好如此"))
            {
                reason = $"continue option content is not whitelisted: {current.OptionContent}";
                return false;
            }

            option = current;
            return true;
        }

        public static bool TryFindSafeSingleContinueOption(TaiwuEventDisplayData data, out EventOptionInfo option, out string reason)
        {
            option = default;
            reason = string.Empty;

            if (data.EventOptionInfos == null)
            {
                reason = "缺少事件选项";
                return false;
            }

            if (HasBlockingExtraData(data))
            {
                reason = "事件包含选择器或输入请求，不做全局单选项继续";
                return false;
            }

            List<EventOptionInfo> available = data.EventOptionInfos.Where(item => item.OptionState == 0).ToList();
            if (available.Count != 1)
            {
                reason = $"可用选项数量不是 1：{available.Count}";
                return false;
            }

            string text = BuildSearchText(data);
            if (ContainsAny(text,
                "切磋", "较艺", "比武", "挑战", "邀战", "战斗", "迎战", "出手相攻",
                "奇遇", "检定", "抢夺", "夺取", "偷盗", "打开箱子", "宝箱", "获取", "获得",
                "取几卷", "天予不取", "破解", "习练", "采集", "打坐休息", "横闯", "潜行", "闪躲", "回避"))
            {
                reason = "事件文本包含风险关键词，不做全局单选项继续";
                return false;
            }

            EventOptionInfo current = available[0];
            string optionContent = current.OptionContent ?? string.Empty;
            if (!ContainsAny(optionContent, "如此便好", "如此甚好", "如此便罢", "我知晓了", "我知道了", "原来如此", "心满意足", "只好如此"))
            {
                reason = $"单选项文本不在安全继续词内：{optionContent}";
                return false;
            }

            option = current;
            return true;
        }

        public static bool TryFindAdoptionOptions(TaiwuEventDisplayData data, out EventOptionInfo adoptOption, out EventOptionInfo postponeOption, out string reason)
        {
            adoptOption = default;
            postponeOption = default;
            reason = string.Empty;

            if (data.EventOptionInfos == null)
            {
                reason = "缺少事件选项";
                return false;
            }

            bool foundAdopt = false;
            bool foundPostpone = false;
            foreach (EventOptionInfo option in data.EventOptionInfos)
            {
                if (option.OptionState != 0)
                    continue;

                string optionKey = option.OptionKey ?? string.Empty;
                string optionContent = option.OptionContent ?? string.Empty;
                if (!foundAdopt
                    && optionKey == AdoptBabyOptionKey
                    && ContainsAny(optionContent, "收养"))
                {
                    adoptOption = option;
                    foundAdopt = true;
                }

                if (!foundPostpone
                    && optionKey == PostponeAdoptionOptionKey
                    && ContainsAny(optionContent, "搁置"))
                {
                    postponeOption = option;
                    foundPostpone = true;
                }
            }

            if (!foundAdopt || !foundPostpone)
            {
                reason = $"收养弃婴选项未命中白名单或不可用：收养={foundAdopt}, 搁置={foundPostpone}";
                return false;
            }

            return true;
        }

        private static bool HasBlockingExtraData(TaiwuEventDisplayData data)
        {
            TaiwuEventDisplayExtraData? extraData = data.ExtraData;
            return extraData != null
                && (extraData.InputRequestData != null
                    || extraData.SelectItemData != null
                    || extraData.SelectCharacterData != null
                    || extraData.SelectNeigongLoopingCountData != null
                    || extraData.SelectReadingBookCountData != null
                    || extraData.SelectFuyuFaithCountData != null
                    || extraData.SelectFameData != null);
        }

        public static string BuildSignature(TaiwuEventDisplayData data)
        {
            string optionKeys = data.EventOptionInfos == null
                ? string.Empty
                : string.Join("|", data.EventOptionInfos.Select(item => (item.OptionKey ?? string.Empty) + "#" + StableHash(item.OptionContent ?? string.Empty)));

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

        private static bool IsPositiveRequestOption(EventOptionInfo option, string optionContent)
        {
            if (option.Behavior != 0)
                return false;

            if (ContainsAny(optionContent, "视而不见", "置之不理", "转身离去", "拒绝", "婉言", "呵责", "愚弄", "代价"))
                return false;

            return ContainsAny(optionContent, "赠予", "赠与", "相助", "指点", "解读", "突破", "修理", "淬毒", "助其", "将");
        }

        private static bool IsPoliteRejectOption(EventOptionInfo option, string optionContent)
        {
            return ContainsAny(optionContent, "婉言拒绝", "婉言谢绝", "暂且作罢", "暂无他事", "转身离去")
                || (option.Behavior == 3 && ContainsAny(optionContent, "拒绝", "谢绝"))
                || ContainsAny(optionContent, "拒绝", "谢绝");
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
