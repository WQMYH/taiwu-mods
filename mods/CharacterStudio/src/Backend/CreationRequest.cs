using System;
using GameData.Domains.Mod;

namespace CharacterStudio.Backend;

internal readonly record struct CreationRequest(
    int Count,
    int Gender,
    int Age,
    int Attraction,
    string Surname,
    string GivenName)
{
    internal static CreationRequest From(SerializableModData? data, CharacterStudioSettings defaults)
    {
        int count = 1;
        int gender = defaults.DefaultGender;
        int age = defaults.DefaultAge;
        int attraction = defaults.DefaultAttraction;
        string surname = defaults.DefaultSurname;
        string givenName = defaults.DefaultGivenName;
        if (data != null)
        {
            data.Get("Count", out count);
            if (!data.Get("Gender", out gender)) gender = defaults.DefaultGender;
            if (!data.Get("Age", out age)) age = defaults.DefaultAge;
            if (!data.Get("Attraction", out attraction)) attraction = defaults.DefaultAttraction;
            if (!data.Get("Surname", out surname)) surname = defaults.DefaultSurname;
            if (!data.Get("GivenName", out givenName)) givenName = defaults.DefaultGivenName;
        }

        return new CreationRequest(
            Math.Clamp(count, 1, 100),
            gender is 0 or 1 ? gender : -1,
            Math.Clamp(age, 3, 100),
            Math.Clamp(attraction, 0, 999),
            surname?.Trim() ?? "",
            givenName?.Trim() ?? "");
    }
}
