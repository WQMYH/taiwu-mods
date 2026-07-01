using System;
using Config;
using GongfaFramework.Test.Contracts;
using Newtonsoft.Json;

namespace GongfaFramework.Test.Runtime;

internal static class SnapshotService
{
    internal static GongfaSnapshot Capture(string side)
    {
        var result = new GongfaSnapshot { Side = side };
        foreach (short id in CombatSkill.Instance.GetAllKeys())
        {
            try
            {
                CombatSkillItem skill = CombatSkill.Instance.GetItem(id);
                if (skill == null) continue;
                SkillBookItem book = skill.BookId >= 0 ? SkillBook.Instance.GetItem(skill.BookId) : null;
                SpecialEffectItem direct = skill.DirectEffectID >= 0
                    ? SpecialEffect.Instance.GetItem((short)skill.DirectEffectID) : null;
                SpecialEffectItem reverse = skill.ReverseEffectID >= 0
                    ? SpecialEffect.Instance.GetItem((short)skill.ReverseEffectID) : null;
                result.Records.Add(new GongfaRecord
                {
                    Id = id,
                    Name = skill.Name ?? "",
                    Description = skill.Desc ?? "",
                    SectId = skill.SectId,
                    Type = skill.Type,
                    Grade = skill.Grade,
                    EquipType = skill.EquipType,
                    OrderIdInSect = skill.OrderIdInSect,
                    BookId = skill.BookId,
                    DirectEffectId = skill.DirectEffectID,
                    ReverseEffectId = skill.ReverseEffectID,
                    BookName = book?.Name ?? "",
                    BookDescription = book?.Desc ?? "",
                    DirectEffectName = direct?.Name ?? "",
                    ReverseEffectName = reverse?.Name ?? "",
                    CombatSkillJson = JsonConvert.SerializeObject(skill, Formatting.Indented),
                    SkillBookJson = book == null ? "" : JsonConvert.SerializeObject(book, Formatting.Indented),
                    DirectEffectJson = direct == null ? "" : JsonConvert.SerializeObject(direct, Formatting.Indented),
                    ReverseEffectJson = reverse == null ? "" : JsonConvert.SerializeObject(reverse, Formatting.Indented)
                });
            }
            catch (Exception ex)
            {
                result.Errors.Add($"功法 {id} 读取失败：{ex.Message}");
            }
        }
        result.RecalculateHash();
        return result;
    }
}
