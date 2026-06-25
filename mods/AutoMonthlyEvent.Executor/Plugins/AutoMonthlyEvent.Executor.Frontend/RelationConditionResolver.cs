using System;
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
                callback(RelationResult.Unresolved(requester.FavorabilityToTaiwu, "config missing"));
                return;
            }

            IAsyncMethodRequestHandler? handler = _requestHandler;
            if (handler == null)
            {
                callback(RelationResult.Unresolved(requester.FavorabilityToTaiwu, "async handler missing"));
                return;
            }

            int taiwuId = GetTaiwuCharId();
            if (taiwuId <= 0 || requester.CharacterId <= 0)
            {
                callback(RelationResult.Unresolved(requester.FavorabilityToTaiwu, "invalid character id"));
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
                                ? "allowed relation"
                                : requester.FavorabilityToTaiwu >= config.FallbackFavorabilityThreshold
                                    ? "favorability threshold reached"
                                    : "relation and favorability not enough"
                        });
                    }
                    catch (Exception ex)
                    {
                        callback(RelationResult.Unresolved(requester.FavorabilityToTaiwu, "relation callback failed: " + ex.GetType().Name));
                    }
                });
            }
            catch (Exception ex)
            {
                callback(RelationResult.Unresolved(requester.FavorabilityToTaiwu, "relation query failed: " + ex.GetType().Name));
            }
        }

        private static bool IsAllowedRelation(ExecutorConfig config, ushort relationTypes)
        {
            foreach (ushort allowed in config.AllowedRelationTypes)
            {
                if (allowed != 0 && (relationTypes & allowed) != 0)
                    return true;
            }
            return false;
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
