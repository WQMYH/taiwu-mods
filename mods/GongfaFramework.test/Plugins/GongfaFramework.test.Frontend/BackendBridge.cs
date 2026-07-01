using System;
using GameData.Common;
using GameData.Domains.Mod;
using GameData.Serializer;
using HarmonyLib;

namespace GongfaFramework.Test.Frontend;

internal static class BackendBridge
{
    internal static void RequestSummary()
    {
        try
        {
            ModDomainMethod.AsyncCall.CallModMethodWithParamAndRet(
                null, FrontendEntry.ModId, "GetSnapshotSummary", new SerializableModData(),
                (offset, pool) =>
                {
                    SerializableModData result = Deserialize(offset, pool);
                    if (result == null) return;
                    result.Get("Hash", out FrontendRuntime.BackendHash);
                    result.Get("Count", out FrontendRuntime.BackendCount);
                    result.Get("Errors", out FrontendRuntime.BackendErrors);
                    result.Get("Message", out FrontendRuntime.Status);
                    FrontendRuntime.Log($"后端摘要：Count={FrontendRuntime.BackendCount}, Hash={FrontendRuntime.BackendHash}");
                });
        }
        catch (Exception ex)
        {
            FrontendRuntime.Status = "请求后端摘要失败：" + ex.Message;
            FrontendRuntime.Error(FrontendRuntime.Status, ex);
        }
    }

    private static SerializableModData Deserialize(int offset, object pool)
    {
        Type poolType = AccessTools.TypeByName("GameData.Utilities.RawDataPool");
        var method = AccessTools.Method(typeof(Serializer), "Deserialize",
            new[] { poolType ?? typeof(object), typeof(int), typeof(SerializableModData).MakeByRefType() });
        if (method == null) return null;
        object[] args = { pool, offset, null };
        method.Invoke(null, args);
        return args[2] as SerializableModData;
    }
}
