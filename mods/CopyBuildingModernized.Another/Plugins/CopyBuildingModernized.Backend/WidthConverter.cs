using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameData.Domains.Building;

namespace CopyBuildingModernized.Backend
{
    /// <summary>
    /// 蓝图宽度转换工具
    /// 用于将不同宽度的村庄蓝图转换为指定宽度，解决导入时的 NullReferenceException 问题
    /// </summary>
    public static class WidthConverter
    {
        /// <summary>
        /// 将蓝图转换为指定宽度
        /// </summary>
        /// <param name="inputFile">输入 .bin 文件路径</param>
        /// <param name="outputFile">输出 .bin 文件路径</param>
        /// <param name="targetWidth">目标宽度（18~126）</param>
        /// <returns>是否成功</returns>
        public static bool ConvertWidth(string inputFile, string outputFile, sbyte targetWidth)
        {
            // 验证参数
            if (targetWidth < 18 || targetWidth >= 127)
            {
                BackendEntry.Logger.Error($"[WidthConverter] 错误：目标宽度 {targetWidth} 超出合法范围 [18, 126]");
                return false;
            }

            if (!File.Exists(inputFile))
            {
                BackendEntry.Logger.Error($"[WidthConverter] 错误：输入文件不存在: {inputFile}");
                return false;
            }

            try
            {
                // 1. 读取原始蓝图
                BackendEntry.Logger.Info($"[WidthConverter] [步骤1] 读取文件: {inputFile}");
                byte[] fileBytes = File.ReadAllBytes(inputFile);
                BackendEntry.Logger.Info($"[WidthConverter] [步骤1] 文件大小: {fileBytes.Length} 字节");
                
                var data = BuildingDataSerializer.DeserializeAll(fileBytes);
                BackendEntry.Logger.Info($"[WidthConverter] [步骤1] 反序列化成功，原始宽度: {data.Width}");
                
                sbyte oldWidth = data.Width;
                BackendEntry.Logger.Info($"[WidthConverter] 原始宽度: {oldWidth}, 目标宽度: {targetWidth}");

                // 2. 获取主 AreaId/BlockId（从输入数据中提取）
                BackendEntry.Logger.Info($"[WidthConverter] [步骤2] 提取区域信息...");
                short mainAreaId = 0;
                short mainBlockId = 0;
                if (data.Blocks.Count > 0)
                {
                    var firstKey = data.Blocks.Keys.First();
                    mainAreaId = firstKey.AreaId;
                    mainBlockId = firstKey.BlockId;
                    BackendEntry.Logger.Info($"[WidthConverter] 使用主区域信息: AreaId={mainAreaId}, BlockId={mainBlockId}");
                }
                else
                {
                    BackendEntry.Logger.Warn("[WidthConverter] 警告: Blocks为空，使用默认区域信息(0,0)");
                }

                // 3. 获取空地模板（带null防护）
                BuildingBlockData emptyTemplate = GetEmptyTemplate(data);
                if (emptyTemplate == null)
                {
                    BackendEntry.Logger.Warn("[WidthConverter] 警告：未找到空地模板，使用默认模板");
                    emptyTemplate = new BuildingBlockData { TemplateId = 0 };
                }

                // 4. 计算居中偏移量
                int offsetX = (targetWidth - oldWidth) / 2;
                int offsetY = (targetWidth - oldWidth) / 2;
                BackendEntry.Logger.Info($"[WidthConverter] 偏移量: X={offsetX}, Y={offsetY}");

                // 5. 重建完整网格（直接清空，不迁移附属数据）
                var newBlocks = new Dictionary<BuildingBlockKey, BuildingBlockData>();
                int totalCells = targetWidth * targetWidth;

                foreach (var kvp in data.Blocks)
                {
                    short oldIndex = kvp.Key.BuildingBlockIndex;
                    BuildingBlockData blockData = kvp.Value;

                    // Null防护
                    if (blockData == null)
                    {
                        BackendEntry.Logger.Warn($"[WidthConverter] 跳过null建筑: 索引={oldIndex}");
                        continue;
                    }

                    // 转换索引
                    short newIndex = ConvertGridIndex(oldIndex, oldWidth, targetWidth, offsetX, offsetY);

                    // 越界检查
                    if (newIndex < 0 || newIndex >= totalCells)
                    {
                        BackendEntry.Logger.Warn($"[WidthConverter] 丢弃越界建筑: 旧索引={oldIndex}, 新索引={newIndex}");
                        continue;
                    }

                    // 创建新的 Key（使用提取的 AreaId/BlockId）
                    BuildingBlockKey newKey = new BuildingBlockKey(mainAreaId, mainBlockId, newIndex);
                    
                    // 复制建筑数据并更新索引
                    BuildingBlockData newBlock = CloneBlockData(blockData);
                    newBlock.BlockIndex = newIndex;
                    if (newBlock.RootBlockIndex >= 0)
                    {
                        short newRootIndex = ConvertGridIndex(newBlock.RootBlockIndex, oldWidth, targetWidth, offsetX, offsetY);
                        newBlock.RootBlockIndex = newRootIndex;
                    }
                    newBlock.OperationProgress = 0;
                    newBlock.OperationType = -1;      // 清理操作类型
                    newBlock.OperationStopping = false; // 清理停止标志
                    newBlocks[newKey] = newBlock;
                }

                // 5. 填充空地（使用统一的 AreaId/BlockId）
                for (short i = 0; i < totalCells; i++)
                {
                    BuildingBlockKey key = new BuildingBlockKey(mainAreaId, mainBlockId, i);
                    if (!newBlocks.ContainsKey(key))
                    {
                        BuildingBlockData emptyBlock = CloneBlockData(emptyTemplate);
                        emptyBlock.BlockIndex = i;
                        emptyBlock.RootBlockIndex = -1;
                        emptyBlock.OperationProgress = 0;
                        emptyBlock.OperationType = -1;      // 空地无操作
                        emptyBlock.OperationStopping = false;
                        newBlocks[key] = emptyBlock;
                    }
                }

                // 6. 更新数据（直接清空所有运行时状态）
                data.Width = targetWidth;
                data.Blocks = newBlocks;
                
                // 清空所有附属数据和自动列表（干净蓝图策略）
                data.ArtisanOrders?.Clear();
                data.ResourceOutput?.Clear();
                data.CollectBuildingResourceType?.Clear();
                data.AutoWorkBlocks?.Clear();
                data.AutoSoldBlocks?.Clear();
                data.AutoCheckInResidence?.Clear();
                data.AutoCheckInComfortable?.Clear();

                BackendEntry.Logger.Info("[WidthConverter] ✅ 已清空所有运行时状态和附属数据");

                // 9. 序列化输出
                byte[] outputBytes = BuildingDataSerializer.SerializeAll(data);
                File.WriteAllBytes(outputFile, outputBytes);

                // 10. 校验输出
                bool isValid = ValidateOutput(outputFile, targetWidth);
                if (isValid)
                {
                    BackendEntry.Logger.Info($"[WidthConverter] ✅ 转换成功！输出文件: {outputFile}");
                    BackendEntry.Logger.Info($"[WidthConverter]    总格子数: {totalCells}");
                    BackendEntry.Logger.Info($"[WidthConverter]    建筑数量: {data.Blocks.Count(b => b.Value.TemplateId != 0)}");
                    BackendEntry.Logger.Info($"[WidthConverter]    空地数量: {data.Blocks.Count(b => b.Value.TemplateId == 0)}");
                }
                else
                {
                    BackendEntry.Logger.Error("[WidthConverter] ❌ 输出文件校验失败！");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                BackendEntry.Logger.Error(ex, $"[WidthConverter] 转换失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取空地模板（带null防护）
        /// </summary>
        private static BuildingBlockData GetEmptyTemplate(BuildingDataCollector.VillageBuildingData data)
        {
            // 优先使用 TemplateId == 0 的块
            foreach (var block in data.Blocks.Values)
            {
                if (block != null && block.TemplateId == 0)
                {
                    return block;
                }
            }
            return null;
        }

        /// <summary>
        /// 转换网格索引（带偏移）
        /// </summary>
        private static short ConvertGridIndex(short oldIndex, sbyte oldWidth, sbyte newWidth, int offsetX, int offsetY)
        {
            // 计算旧坐标
            int oldX = oldIndex % oldWidth;
            int oldY = oldIndex / oldWidth;

            // 应用偏移
            int newX = oldX + offsetX;
            int newY = oldY + offsetY;

            // 检查边界
            if (newX < 0 || newX >= newWidth || newY < 0 || newY >= newWidth)
            {
                return -1; // 越界
            }

            // 返回新索引
            return (short)(newY * newWidth + newX);
        }

        /// <summary>
        /// 克隆建筑数据（使用完整序列化方式深拷贝）
        /// </summary>
        private static BuildingBlockData CloneBlockData(BuildingBlockData source)
        {
            if (source == null) return null;

            // 通过序列化/反序列化实现完整深拷贝
            using var ms = new System.IO.MemoryStream();
            using var bw = new System.IO.BinaryWriter(ms);
            
            // 序列化
            int size = source.GetSerializedSize();
            byte[] buffer = new byte[size];
            unsafe { fixed (byte* p = buffer) { source.Serialize(p); } }
            bw.Write(size);
            bw.Write(buffer);
            
            // 反序列化
            ms.Position = 0;
            using var br = new System.IO.BinaryReader(ms);
            int readSize = br.ReadInt32();
            byte[] readBuffer = br.ReadBytes(readSize);
            
            BuildingBlockData result = new BuildingBlockData();
            unsafe { fixed (byte* p = readBuffer) { result.Deserialize(p); } }
            
            return result;
        }

        /// <summary>
        /// 校验输出文件
        /// </summary>
        private static bool ValidateOutput(string filePath, sbyte expectedWidth)
        {
            try
            {
                BackendEntry.Logger.Info($"[WidthConverter] [校验] 开始校验输出文件: {filePath}");
                byte[] bytes = File.ReadAllBytes(filePath);
                var data = BuildingDataSerializer.DeserializeAll(bytes);

                // 检查宽度
                if (data.Width != expectedWidth)
                {
                    BackendEntry.Logger.Error($"[WidthConverter] [校验] 失败: Width={data.Width}, 期望={expectedWidth}");
                    return false;
                }

                // 检查格子总数
                int expectedCells = expectedWidth * expectedWidth;
                if (data.Blocks.Count != expectedCells)
                {
                    BackendEntry.Logger.Error($"[WidthConverter] [校验] 失败: Blocks.Count={data.Blocks.Count}, 期望={expectedCells}");
                    return false;
                }

                // 检查无 null block
                foreach (var block in data.Blocks.Values)
                {
                    if (block == null)
                    {
                        BackendEntry.Logger.Error("[WidthConverter] [校验] 失败: 存在 null block");
                        return false;
                    }
                }

                // 检查 BlockIndex 一致性
                foreach (var kvp in data.Blocks)
                {
                    if (kvp.Key.BuildingBlockIndex != kvp.Value.BlockIndex)
                    {
                        BackendEntry.Logger.Error($"[WidthConverter] [校验] 失败: Key.BuilduildingBlockIndex={kvp.Key.BuildingBlockIndex}, Block.BlockIndex={kvp.Value.BlockIndex}");
                        return false;
                    }
                }

                // 检查最大索引
                short maxIndex = data.Blocks.Keys.Max(k => k.BuildingBlockIndex);
                if (maxIndex >= expectedCells)
                {
                    BackendEntry.Logger.Error($"[WidthConverter] [校验] 失败: MaxIndex={maxIndex}, 期望<{expectedCells}");
                    return false;
                }

                foreach (var kvp in data.Blocks)
                {
                    var block = kvp.Value;
                    if (block.TemplateId != -1)
                        continue;

                    if (block.RootBlockIndex < 0 || block.RootBlockIndex >= expectedCells)
                    {
                        BackendEntry.Logger.Error($"[WidthConverter] [校验] 失败: 从属格 Index={block.BlockIndex} RootBlockIndex={block.RootBlockIndex} 越界");
                        return false;
                    }

                    bool rootExists = data.Blocks.Values.Any(root =>
                        root != null &&
                        root.BlockIndex == block.RootBlockIndex &&
                        root.TemplateId > 0);
                    if (!rootExists)
                    {
                        BackendEntry.Logger.Error($"[WidthConverter] [校验] 失败: 从属格 Index={block.BlockIndex} 找不到有效根格 RootBlockIndex={block.RootBlockIndex}");
                        return false;
                    }
                }

                BackendEntry.Logger.Info($"[WidthConverter] [校验] ✅ 校验通过！");
                return true;
            }
            catch (Exception ex)
            {
                BackendEntry.Logger.Error(ex, $"[WidthConverter] [校验] 异常: {ex.Message}");
                return false;
            }
        }
    }
}
