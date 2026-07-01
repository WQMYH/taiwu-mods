using Game.Views.NewGame;
using HarmonyLib;

namespace CharacterStudio.Frontend;

[HarmonyPatch(typeof(NewGameSubPageCustomPresetAttribute), "Awake")]
internal static class AttributePageAwakePatch
{
    private static void Postfix() => FrontendEntry.ApplyPointLimits();
}

[HarmonyPatch(typeof(NewGameSubPageCustomPresetQualification), "Awake")]
internal static class QualificationPageAwakePatch
{
    private static void Postfix() => FrontendEntry.ApplyPointLimits();
}

[HarmonyPatch(typeof(NewGameSubPageFeature), "Awake")]
internal static class FeaturePageAwakePatch
{
    private static void Postfix() => FrontendEntry.ApplyPointLimits();
}
