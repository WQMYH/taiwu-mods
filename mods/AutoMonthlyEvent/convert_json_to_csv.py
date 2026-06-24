#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
JSON to CSV 转换器
将 AutoMonthlyEvent MOD 导出的 JSON/JSONL 文件转换为 CSV 格式
"""

import json
import csv
import os
from pathlib import Path

# 源文件路径
SOURCE_DIR = r"a:\SteamLibrary\steamapps\common\The Scroll Of Taiwu\Dump_out"

# 目标目录（项目目录下的 /data）
TARGET_DIR = r"e:\Programming\Mods\Taiwu\AutoMonthlyEvent\data"

def ensure_directory(path):
    """确保目录存在"""
    os.makedirs(path, exist_ok=True)

def convert_monthly_events_catalog():
    """转换月度事件目录"""
    source_file = os.path.join(SOURCE_DIR, "monthly_events_catalog.json")
    target_file = os.path.join(TARGET_DIR, "monthly_events_catalog.csv")
    
    print(f"处理: {source_file}")
    
    with open(source_file, 'r', encoding='utf-8-sig') as f:
        data = json.load(f)
    
    items = data.get('items', [])
    generated_at = data.get('generatedAt', '')
    
    # CSV 字段
    fieldnames = [
        'templateId',
        'name',
        'type',
        'eventGuid',
        'desc',
        'parameters',
        'score',
        'node'
    ]
    
    with open(target_file, 'w', encoding='utf-8-sig', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        
        for item in items:
            # 将 parameters 列表转换为字符串
            params_str = '|'.join(item.get('parameters', []))
            
            row = {
                'templateId': item.get('templateId', ''),
                'name': item.get('name', ''),
                'type': item.get('type', ''),
                'eventGuid': item.get('eventGuid', '') or '',
                'desc': item.get('desc', ''),
                'parameters': params_str,
                'score': item.get('score', ''),
                'node': item.get('node', '')
            }
            writer.writerow(row)
    
    print(f"  ✓ 已导出 {len(items)} 条记录到: {target_file}")
    return len(items)

def convert_interaction_options_catalog():
    """转换交互选项目录"""
    source_file = os.path.join(SOURCE_DIR, "interaction_options_catalog.json")
    target_file = os.path.join(TARGET_DIR, "interaction_options_catalog.csv")
    
    print(f"处理: {source_file}")
    
    with open(source_file, 'r', encoding='utf-8-sig') as f:
        data = json.load(f)
    
    items = data.get('items', [])
    
    # CSV 字段
    fieldnames = [
        'templateId',
        'optionGuid',
        'name',
        'interactionType',
        'identityAbility',
        'oncePerMonth',
        'actionPointCost'
    ]
    
    with open(target_file, 'w', encoding='utf-8-sig', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        
        for item in items:
            row = {
                'templateId': item.get('templateId', ''),
                'optionGuid': item.get('optionGuid', ''),
                'name': item.get('name', ''),
                'interactionType': item.get('interactionType', ''),
                'identityAbility': item.get('identityAbility', ''),
                'oncePerMonth': item.get('oncePerMonth', ''),
                'actionPointCost': item.get('actionPointCost', '')
            }
            writer.writerow(row)
    
    print(f"  ✓ 已导出 {len(items)} 条记录到: {target_file}")
    return len(items)

def convert_runtime_event_options():
    """转换运行时事件选项（JSONL 格式）"""
    source_file = os.path.join(SOURCE_DIR, "runtime_event_options.jsonl")
    target_file = os.path.join(TARGET_DIR, "runtime_event_options.csv")
    
    print(f"处理: {source_file}")
    
    if not os.path.exists(source_file):
        print(f"  ⚠ 文件不存在: {source_file}")
        return 0
    
    # CSV 字段
    fieldnames = [
        'timestamp',
        'eventGuid',
        'eventContent',
        'mainCharacterId',
        'targetCharacterId',
        'optionsCount',
        'optionKeys',
        'optionContents',
        'optionTypes',
        'optionStates',
        'behaviors'
    ]
    
    records = []
    
    with open(source_file, 'r', encoding='utf-8-sig') as f:
        for line_num, line in enumerate(f, 1):
            line = line.strip()
            if not line:
                continue
            
            try:
                data = json.loads(line)
                
                # 提取选项信息
                options = data.get('options', [])
                option_keys = '|'.join([opt.get('optionKey', '') for opt in options])
                option_contents = '|'.join([opt.get('optionContent', '').replace('\n', '\\n') for opt in options])
                option_types = '|'.join([str(opt.get('optionType', '')) for opt in options])
                option_states = '|'.join([str(opt.get('optionState', '')) for opt in options])
                behaviors = '|'.join([str(opt.get('behavior', '')) for opt in options])
                
                row = {
                    'timestamp': data.get('timestamp', ''),
                    'eventGuid': data.get('eventGuid', ''),
                    'eventContent': data.get('eventContent', '').replace('\n', '\\n'),
                    'mainCharacterId': data.get('mainCharacterId', ''),
                    'targetCharacterId': data.get('targetCharacterId', ''),
                    'optionsCount': len(options),
                    'optionKeys': option_keys,
                    'optionContents': option_contents,
                    'optionTypes': option_types,
                    'optionStates': option_states,
                    'behaviors': behaviors
                }
                records.append(row)
                
            except json.JSONDecodeError as e:
                print(f"  ⚠ 第 {line_num} 行解析失败: {e}")
                continue
    
    with open(target_file, 'w', encoding='utf-8-sig', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(records)
    
    print(f"  ✓ 已导出 {len(records)} 条记录到: {target_file}")
    return len(records)

def main():
    """主函数"""
    print("=" * 60)
    print("AutoMonthlyEvent MOD - JSON to CSV 转换器")
    print("=" * 60)
    print()
    
    # 确保目标目录存在
    ensure_directory(TARGET_DIR)
    print(f"目标目录: {TARGET_DIR}")
    print()
    
    # 转换各个文件
    total_records = 0
    
    print("[1/3] 转换月度事件目录...")
    count1 = convert_monthly_events_catalog()
    total_records += count1
    print()
    
    print("[2/3] 转换交互选项目录...")
    count2 = convert_interaction_options_catalog()
    total_records += count2
    print()
    
    print("[3/3] 转换运行时事件选项...")
    count3 = convert_runtime_event_options()
    total_records += count3
    print()
    
    # 汇总
    print("=" * 60)
    print(f"转换完成！共导出 {total_records} 条记录")
    print("=" * 60)
    print()
    print("输出文件:")
    print(f"  1. monthly_events_catalog.csv ({count1} 条)")
    print(f"  2. interaction_options_catalog.csv ({count2} 条)")
    print(f"  3. runtime_event_options.csv ({count3} 条)")
    print()
    print(f"文件位置: {TARGET_DIR}")

if __name__ == "__main__":
    main()
