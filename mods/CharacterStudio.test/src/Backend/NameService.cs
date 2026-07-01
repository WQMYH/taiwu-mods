using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;

namespace CharacterStudio.Backend;

internal static class NameService
{
    internal static FullName CreateName(
        DataContext context,
        sbyte gender,
        string surname,
        string givenName)
    {
        FullName name = DomainManager.Character.GenerateRandomHanName(context, -1, -1, gender);
        if (!string.IsNullOrWhiteSpace(surname))
        {
            name.Type |= 1;
            name.Type |= 4;
            name.CustomSurnameId = DomainManager.World.RegisterCustomText(context, surname);
        }
        if (!string.IsNullOrWhiteSpace(givenName))
        {
            name.Type |= 1;
            name.Type |= 8;
            name.CustomGivenNameId = DomainManager.World.RegisterCustomText(context, givenName);
        }
        return name;
    }
}
