using System;
using GameData.Domains.Mod;

namespace CharacterStudio.Backend;

internal readonly record struct CreationRequest(
    int Count,
    string ProfileId,
    int GenderOverride,
    int AgeOverride,
    int AttractionOverride,
    string Surname,
    string GivenName)
{
    internal static CreationRequest From(SerializableModData? data, CharacterStudioSettings defaults)
    {
        int count = 1;
        int gender = -1;
        int age = -1;
        int attraction = -1;
        string profileId = defaults.ManualCreateProfile;
        string surname = "";
        string givenName = "";
        if (data != null)
        {
            data.Get("Count", out count);
            data.Get("ProfileId", out profileId);
            data.Get("GenderOverride", out gender);
            data.Get("AgeOverride", out age);
            data.Get("AttractionOverride", out attraction);
            data.Get("Surname", out surname);
            data.Get("GivenName", out givenName);
        }

        return new CreationRequest(
            Math.Clamp(count, 1, 100),
            string.IsNullOrWhiteSpace(profileId) ? defaults.ManualCreateProfile : profileId.Trim(),
            gender is 0 or 1 ? gender : -1,
            age < 0 ? -1 : Math.Clamp(age, 3, 100),
            attraction < 0 ? -1 : Math.Clamp(attraction, 0, 999),
            surname?.Trim() ?? "",
            givenName?.Trim() ?? "");
    }
}
