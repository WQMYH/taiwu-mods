using System;
using System.Collections.Generic;
using GameData.Domains.Character;
using GameData.Domains.Character.Display;
using GameData.GameDataBridge;
using GameData.Serializer;
using GameData.Utilities;

namespace AutoMonthlyEvent.Executor.Frontend
{
    internal static class RelationConditionResolver
    {
        private static IAsyncMethodRequestHandler? _requestHandler;
        private static ExecutorConfig? _config;

        public static void Configure(IAsyncMethodRequestHandler requestHandler, ExecutorConfig config)
        {
            _requestHandler = requestHandler;
            _config = config;
        }

        public static CharacterDisplayData? FindRequester(EventModel eventModel)
        {
            var data = eventModel.DisplayingEventData;
            if (data == null)
                return null;

            int taiwuId = GetTaiwuCharId();
            if (data.MainCharacter != null && data.MainCharacter.CharacterId > 0 && data.MainCharacter.CharacterId != taiwuId)
                return data.MainCharacter;

            if (data.TargetCharacter != null && data.TargetCharacter.CharacterId > 0 && data.TargetCharacter.CharacterId != taiwuId)
                return data.TargetCharacter;

            return data.TargetCharacter ?? data.MainCharacter;
        }

        public static void Resolve(CharacterDisplayData requester, Action<RelationResult> callback)
        {
            ExecutorConfig? config = _config;
            if (config == null)
            {
                callback(RelationResult.Unresolved(requester.FavorabilityToTaiwu, "配置缺失"));
                return;
            }

            IAsyncMethodRequestHandler? handler = _requestHandler;
            if (handler == null)
            {
                callback(RelationResult.Unresolved(requester.FavorabilityToTaiwu, "异步请求处理器缺失"));
                return;
            }

            int taiwuId = GetTaiwuCharId();
            if (taiwuId <= 0 || requester.CharacterId <= 0)
            {
                callback(RelationResult.Unresolved(requester.FavorabilityToTaiwu, "角色 ID 无效"));
                return;
            }

            try
            {
                CharacterDomainMethod.AsyncCall.GetRelationBetweenCharacters(handler, taiwuId, requester.CharacterId, delegate(int offset, RawDataPool pool)
                {
                    try
                    {
                        (ushort, ushort) relation = default;
                        Serializer.Deserialize(pool, offset, ref relation);
                        ushort relationType = relation.Item1;
                        bool allowed = IsAllowedRelation(config, relationType);
                        callback(new RelationResult
                        {
                            Resolved = true,
                            RelationType = relationType,
                            Favorability = requester.FavorabilityToTaiwu,
                            ShouldGive = allowed || requester.FavorabilityToTaiwu >= config.FallbackFavorabilityThreshold,
                            Reason = allowed
                                ? "关系命中允许列表"
                                : requester.FavorabilityToTaiwu >= config.FallbackFavorabilityThreshold
                                    ? "好感达到阈值"
                                    : "关系和好感未满足条件"
                        });
                    }
                    catch (Exception ex)
                    {
                        callback(RelationResult.Unresolved(requester.FavorabilityToTaiwu, "关系查询回调失败：" + ex.GetType().Name));
                    }
                });
            }
            catch (Exception ex)
            {
                callback(RelationResult.Unresolved(requester.FavorabilityToTaiwu, "关系查询失败：" + ex.GetType().Name));
            }
        }

        private static bool IsAllowedRelation(ExecutorConfig config, ushort relationTypes)
        {
            foreach (ushort allowed in GetAllowedRelationTypes(config))
            {
                if (allowed != 0 && (relationTypes & allowed) != 0)
                    return true;
            }
            return false;
        }

        private static IEnumerable<ushort> GetAllowedRelationTypes(ExecutorConfig config)
        {
            if (config.AllowedRelationTypes.Count > 0)
                return config.AllowedRelationTypes;

            switch (config.RequestRelationMode)
            {
                case 1:
                    return new ushort[] { 1024, 1, 2 };
                case 2:
                    return new ushort[] { 1024, 1, 2, 64, 128, 512 };
                default:
                    return new ushort[] { 1024, 1, 2, 64, 128, 512, 8192 };
            }
        }

        private static int GetTaiwuCharId()
        {
            try
            {
                return SingletonObject.getInstance<BasicGameData>().TaiwuCharId;
            }
            catch
            {
                return -1;
            }
        }
    }

    internal sealed class RelationResult
    {
        public bool Resolved { get; set; }
        public ushort RelationType { get; set; }
        public short Favorability { get; set; } = short.MinValue;
        public bool ShouldGive { get; set; }
        public string Reason { get; set; } = string.Empty;

        public static RelationResult Unresolved(short favorability, string reason)
        {
            return new RelationResult
            {
                Resolved = false,
                RelationType = 0,
                Favorability = favorability,
                ShouldGive = false,
                Reason = reason
            };
        }
    }
}
