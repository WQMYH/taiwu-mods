using System;
using System.Collections.Generic;
using System.Reflection;
using Config;
using Config.ConfigCells.Character;
using HarmonyLib;

namespace RespectTheStrongBackend;

internal static class OrganizationEquipmentOverride
{
    private static readonly Dictionary<short, PresetEquipmentItemWithProb[]> Originals = new();
    private static readonly FieldInfo EquipmentField =
        AccessTools.Field(typeof(OrganizationMemberItem), "Equipment");

    internal static void Refresh()
    {
        if (!ModSettings.ImproveOrganizationEquipment)
        {
            Restore();
            return;
        }
        if (EquipmentField == null)
        {
            ModLog.Warn("OrganizationEquipment", "Disabled: Equipment field missing");
            return;
        }

        for (short id = 46; id <= 54; id++)
        {
            OrganizationMemberItem item = OrganizationMember.Instance.GetItem(id);
            if (item == null)
                continue;
            if (!Originals.ContainsKey(id))
                Originals[id] = item.Equipment == null
                    ? Array.Empty<PresetEquipmentItemWithProb>()
                    : (PresetEquipmentItemWithProb[])item.Equipment.Clone();
            EquipmentField.SetValue(item, CreateEquipment());
        }
        ModLog.Info("OrganizationEquipment", $"Installed: {Originals.Count} member records");
    }

    internal static void Restore()
    {
        if (EquipmentField == null || Originals.Count == 0)
            return;
        foreach ((short id, PresetEquipmentItemWithProb[] equipment) in Originals)
        {
            OrganizationMemberItem item = OrganizationMember.Instance.GetItem(id);
            if (item != null)
                EquipmentField.SetValue(item, equipment);
        }
        Originals.Clear();
        ModLog.Info("OrganizationEquipment", "original member equipment restored");
    }

    private static PresetEquipmentItemWithProb[] CreateEquipment() =>
        new[]
        {
            new PresetEquipmentItemWithProb("Armor", 9, 100),
            new PresetEquipmentItemWithProb("Armor", 360, 100),
            new PresetEquipmentItemWithProb("Armor", 405, 100),
            new PresetEquipmentItemWithProb("Armor", 216, 100),
            new PresetEquipmentItemWithProb("Accessory", 144, 100),
            new PresetEquipmentItemWithProb("Accessory", 180, 60),
            new PresetEquipmentItemWithProb("Accessory", 72, 60),
            new PresetEquipmentItemWithProb("Carrier", 0, 100),
        };
}
