using System;
using System.Collections.Generic;
using System.IO;
using GameData.Domains.Building;
using GameData.Serializer;

namespace CopyBuildingModernized.Backend
{
    public static class BuildingDataSerializer
    {
        public static byte[] SerializeAll(BuildingDataCollector.VillageBuildingData data)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(data.Width);

            WriteDict(bw, data.Blocks, WriteKey, WriteSerializableClass);
            WriteDict(bw, data.ArtisanOrders, WriteKey, WriteSerializableClass);
            WriteDict(bw, data.ResourceOutput, WriteKey, WriteSerializableClass);
            WriteDict(bw, data.CollectBuildingResourceType, WriteKey, (w, v) => w.Write(v));

            WriteShortList(bw, data.AutoWorkBlocks);
            WriteShortList(bw, data.AutoSoldBlocks);
            WriteShortList(bw, data.AutoCheckInResidence);
            WriteShortList(bw, data.AutoCheckInComfortable);

            return ms.ToArray();
        }

        public static BuildingDataCollector.VillageBuildingData DeserializeAll(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            using var br = new BinaryReader(ms);

            var data = new BuildingDataCollector.VillageBuildingData
            {
                Blocks = new Dictionary<BuildingBlockKey, BuildingBlockData>(),
                ArtisanOrders = new Dictionary<BuildingBlockKey, ArtisanOrder>(),
                ResourceOutput = new Dictionary<BuildingBlockKey, BuildingResourceOutputSetting>(),
                CollectBuildingResourceType = new Dictionary<BuildingBlockKey, sbyte>(),
                AutoWorkBlocks = new List<short>(),
                AutoSoldBlocks = new List<short>(),
                AutoCheckInResidence = new List<short>(),
                AutoCheckInComfortable = new List<short>()
            };

            data.Width = br.ReadSByte();

            ReadDict(br, data.Blocks, ReadKey, ReadSerializableClass<BuildingBlockData>);
            ReadDict(br, data.ArtisanOrders, ReadKey, ReadSerializableClass<ArtisanOrder>);
            ReadDict(br, data.ResourceOutput, ReadKey, ReadSerializableClass<BuildingResourceOutputSetting>);
            ReadDict(br, data.CollectBuildingResourceType, ReadKey, r => r.ReadSByte());

            data.AutoWorkBlocks = ReadShortList(br);
            data.AutoSoldBlocks = ReadShortList(br);
            data.AutoCheckInResidence = ReadShortList(br);
            data.AutoCheckInComfortable = ReadShortList(br);

            return data;
        }

        // ---- 键值辅助 ----
        private static void WriteKey(BinaryWriter bw, BuildingBlockKey key) => WriteSerializable(bw, key);
        private static BuildingBlockKey ReadKey(BinaryReader br) => ReadSerializable<BuildingBlockKey>(br);

        // ---- 泛型字典读写 ----
        private static void WriteDict<TKey, TValue>(BinaryWriter bw, Dictionary<TKey, TValue> dict,
            Action<BinaryWriter, TKey> writeKey, Action<BinaryWriter, TValue> writeVal)
        {
            bw.Write(dict.Count);
            foreach (var kv in dict)
            {
                writeKey(bw, kv.Key);
                writeVal(bw, kv.Value);
            }
        }

        private static void ReadDict<TKey, TValue>(BinaryReader br, Dictionary<TKey, TValue> dict,
            Func<BinaryReader, TKey> readKey, Func<BinaryReader, TValue> readVal)
        {
            int count = br.ReadInt32();
            for (int i = 0; i < count; i++)
                dict[readKey(br)] = readVal(br);
        }

        // ---- struct 序列化 ----
        private static void WriteSerializable<T>(BinaryWriter bw, T value) where T : struct, ISerializableGameData
        {
            int size = value.GetSerializedSize();
            byte[] buffer = new byte[size];
            unsafe { fixed (byte* p = buffer) { value.Serialize(p); } }
            bw.Write(size);
            bw.Write(buffer);
        }

        private static T ReadSerializable<T>(BinaryReader br) where T : struct, ISerializableGameData
        {
            int size = br.ReadInt32();
            byte[] buffer = br.ReadBytes(size);
            T result = default;
            unsafe { fixed (byte* p = buffer) { result.Deserialize(p); } }
            return result;
        }

        // ---- class 序列化 ----
        private static void WriteSerializableClass<T>(BinaryWriter bw, T value) where T : class, ISerializableGameData
        {
            if (value == null) { bw.Write(0); return; }
            int size = value.GetSerializedSize();
            byte[] buffer = new byte[size];
            unsafe { fixed (byte* p = buffer) { value.Serialize(p); } }
            bw.Write(size);
            bw.Write(buffer);
        }

        private static T ReadSerializableClass<T>(BinaryReader br) where T : class, ISerializableGameData, new()
        {
            int size = br.ReadInt32();
            if (size == 0) return null;
            byte[] buffer = br.ReadBytes(size);
            T result = new T();
            unsafe { fixed (byte* p = buffer) { result.Deserialize(p); } }
            return result;
        }

        // ---- List<short> 读写 ----
        private static void WriteShortList(BinaryWriter bw, List<short> list)
        {
            bw.Write(list?.Count ?? 0);
            if (list != null)
                foreach (var v in list) bw.Write(v);
        }

        private static List<short> ReadShortList(BinaryReader br)
        {
            int count = br.ReadInt32();
            var list = new List<short>(count);
            for (int i = 0; i < count; i++) list.Add(br.ReadInt16());
            return list;
        }
    }
}
