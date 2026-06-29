using System;

namespace CharacterStudio.Backend;

internal sealed class CharacterStudioSettings
{
    internal bool EnableMod = true;
    internal bool CustomizeProtagonist;
    internal bool CustomizeCloseFriend = true;
    internal int CloseFriendCount = 1;
    internal bool CustomizeCreatedVillagers = true;
    internal bool CustomizeInitialVillagers;
    internal bool ExpandVillageCapacity = true;
    internal int VillageCapacity = 500;
    internal int AttributeMode = 1;
    internal int MainAttributeValue = 100;
    internal int LifeQualificationValue = 100;
    internal int CombatQualificationValue = 100;
    internal int BaseHealth;
    internal int Morality = -1;
    internal string FeatureIds = "";
    internal string RemoveFeatureIds = "";
    internal bool Bisexual;
    internal int RelationType = 8192;
    internal int Favorability = 30000;
    internal int DefaultGender = -1;
    internal int DefaultAge = 18;
    internal int DefaultAttraction = 550;
    internal string DefaultSurname = "";
    internal string DefaultGivenName = "";
    internal int BodyTypeChoice;
    internal int BodyType => BodyTypeChoice - 1;
    internal int ClothingTemplateId;
    internal bool EnableDebugLog = true;

    internal void Normalize()
    {
        VillageCapacity = Math.Clamp(VillageCapacity, 0, 10000);
        CloseFriendCount = Math.Clamp(CloseFriendCount, 1, 10);
        AttributeMode = Math.Clamp(AttributeMode, 0, 2);
        MainAttributeValue = Math.Clamp(MainAttributeValue, 0, short.MaxValue);
        LifeQualificationValue = Math.Clamp(LifeQualificationValue, 0, short.MaxValue);
        CombatQualificationValue = Math.Clamp(CombatQualificationValue, 0, short.MaxValue);
        BaseHealth = Math.Clamp(BaseHealth, 0, short.MaxValue);
        Morality = Math.Clamp(Morality, -1, 500);
        RelationType = Math.Max(0, RelationType);
        Favorability = Math.Clamp(Favorability, -30000, 30000);
        DefaultGender = DefaultGender is 0 or 1 ? DefaultGender : -1;
        DefaultAge = Math.Clamp(DefaultAge, 3, 100);
        DefaultAttraction = Math.Clamp(DefaultAttraction, 0, 999);
        ClothingTemplateId = Math.Clamp(ClothingTemplateId, 0, short.MaxValue);
        BodyTypeChoice = Math.Clamp(BodyTypeChoice, 0, 3);
    }
}
