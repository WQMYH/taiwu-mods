using System;
using System.Collections.Generic;

namespace AutoMonthlyEvent.Executor.Frontend
{
    internal enum MonthlyHandlerKind
    {
        DefaultChoice,
        RelationRequest,
        BirthAndNaming,
        SkipResult,
        Adoption
    }

    internal sealed class SupportedMonthlyEvent
    {
        public SupportedMonthlyEvent(short recordType, string guid, string name, MonthlyHandlerKind handler, string group)
        {
            RecordType = recordType;
            EventGuid = guid;
            Name = name;
            Handler = handler;
            Group = group;
        }

        public short RecordType { get; }
        public string EventGuid { get; }
        public string Name { get; }
        public MonthlyHandlerKind Handler { get; }
        public string Group { get; }
    }

    internal static class SupportedMonthlyEvents
    {
        private static readonly Dictionary<short, SupportedMonthlyEvent> Items = Build();

        public static bool TryGet(short recordType, out SupportedMonthlyEvent value) => Items.TryGetValue(recordType, out value);

        public static bool IsEnabled(SupportedMonthlyEvent item, MonthlyAutomationSettings settings)
        {
            if (!settings.Enabled)
                return false;
            if (item.Handler == MonthlyHandlerKind.RelationRequest)
                return settings.EnableRequests;
            if (item.Group == "家庭与生育")
                return settings.EnableFamily;
            if (item.Handler == MonthlyHandlerKind.SkipResult)
                return settings.EnableResultSkip;
            return settings.EnableSocial;
        }

        private static Dictionary<short, SupportedMonthlyEvent> Build()
        {
            var result = new Dictionary<short, SupportedMonthlyEvent>();
            Add(result, 13, "a73cc160-a95d-42c3-b986-a0353df434f0", "母亲胎教", MonthlyHandlerKind.DefaultChoice, "家庭与生育");
            AddRange(result, MonthlyHandlerKind.SkipResult, "生育结果",
                (14, "b2d104a6-b1ea-4cbb-8043-54d8da07176c", "痛失骨肉"),
                (15, "be5c842e-d69f-40e3-961d-b49ba7186fc2", "痛失骨肉"),
                (16, "c6a8fff1-8d8e-4f14-91b8-3bf49bdd1a29", "难产双亡"),
                (17, "1f5bbc25-26db-46cc-bd85-54833cf2367a", "难产双亡"),
                (18, "7c8b6585-2d4a-4ae6-a0f8-a2228486271c", "难产失子"),
                (19, "26c12b8e-2808-43d7-a00e-af970cb459bf", "难产失子"),
                (24, "4a3a0d8a-3140-400f-b444-f9ecb209cfba", "难产剩子"),
                (25, "2a0e5ea0-8418-4cc3-ba47-20bd9b2c5707", "难产剩女"),
                (26, "2c353211-ec1c-49dd-94cc-b396821348de", "难产剩子"),
                (27, "ac07bc64-eb8b-4214-92e4-15a63b77af40", "难产剩女"));
            AddRange(result, MonthlyHandlerKind.BirthAndNaming, "家庭与生育",
                (20, "a86f7e1e-921b-42b8-9555-dec674e2df25", "喜得贵子"),
                (21, "2425d411-1f0b-4482-b610-89ef5a7db33f", "喜得千金"),
                (22, "699239c9-8293-4b21-b479-daf07983156e", "喜得贵子"),
                (23, "1612d6ee-f4fe-4ce1-9174-4fe434a8225a", "喜得千金"),
                (28, "a572973e-af1e-4ff8-8e12-884b28671281", "难产得子"),
                (29, "fd9e4d4d-c8c1-4858-880b-7d6cb01f959b", "难产得女"),
                (30, "78513fac-0ba8-4c46-b892-26d8d1dc79f3", "难产得子"),
                (31, "a224107b-2909-4f02-882e-a84f2cad38ee", "难产得女"));
            Add(result, 32, "8812d517-ca0a-4f9d-83e8-936376ae11c4", "无名遗孤", MonthlyHandlerKind.Adoption, "家庭与生育");

            string[] requestGuids = {
                "3af56371-f736-4aa9-ae9b-c3c98ba0f4b0","d58fceec-2d53-4532-9893-d834db626b35",
                "3f335f54-d401-4951-9b9e-61c82c7d5bbc","3c71d6c1-f5a0-4e20-9eca-280dde1b2f84",
                "1deadcc9-cca4-4091-8276-5f3a8b136fe0","d799f11c-7ff7-4f9e-83a6-192753411e7b",
                "66fe331f-7bb2-422b-9a3e-892859eedb55","03b005e1-e0c6-4726-abc7-4e3121116049",
                "d2e80cb3-4c6f-471d-83f9-422bbcab2d7f","d7ec7e02-ee62-4137-a8cb-a4961ec28bb0",
                "560cee7d-3955-4650-8fbf-cf66e80a321d","e6a1795d-e1a1-46af-b977-f821fc1441f5",
                "16bddd08-6083-4c11-9342-78922ed19b6b","7ac0429a-a48f-4adb-be4b-4cb767d978e4",
                "a61df3c7-c29b-4a81-b4c8-da7651c931e8","7386d3bc-3ebe-4603-b771-1f61f7a166b2",
                "cbb59e89-a125-42ab-8692-e522c44a0bc8","eb7f0c2a-60f9-4221-97e5-662d513620c1",
                "1735b4a9-4ece-4ff9-83f3-fa005cfa33e8","9804522b-8380-4950-9996-f15a8387d802",
                "6c91f53d-6eb8-43e5-812d-e1838cab5c57","c485c693-5ff0-45ec-933c-62c75a045fee"
            };
            for (short id = 66; id <= 87; id++)
                Add(result, id, requestGuids[id - 66], "人物请求 " + id, MonthlyHandlerKind.RelationRequest, "人物请求");

            Add(result, 109, "eb338657-9fec-4129-bbbe-3e4390606d7e", "推恩施义", MonthlyHandlerKind.DefaultChoice, "人情往来");
            Add(result, 110, "657c9c77-5690-4e32-bbfc-245b44489374", "笼络人心", MonthlyHandlerKind.DefaultChoice, "人情往来");
            Add(result, 280, "54a700fe-f097-4cec-a5cd-9cbb68587524", "指点武学", MonthlyHandlerKind.DefaultChoice, "人情往来");
            Add(result, 281, "11211fd5-8cfd-4113-a948-f5e587cdea1a", "身怀六甲", MonthlyHandlerKind.DefaultChoice, "家庭与生育");
            return result;
        }

        // [指定月度事件扩展点]
        // 只有在此登记 RecordType、当前游戏 GUID、中文名称、处理器和 UI 分组的事件才会被接管。
        // 新增事件必须同时补充选项策略、后续页面处理、失败转人工测试；禁止添加“未知事件选第一项”。
        private static void Add(Dictionary<short, SupportedMonthlyEvent> map, short id, string guid, string name, MonthlyHandlerKind handler, string group)
            => map[id] = new SupportedMonthlyEvent(id, guid, name, handler, group);

        private static void AddRange(Dictionary<short, SupportedMonthlyEvent> map, MonthlyHandlerKind handler, string group,
            params (short Id, string Guid, string Name)[] items)
        {
            foreach (var item in items)
                Add(map, item.Id, item.Guid, item.Name, handler, group);
        }
    }
}
