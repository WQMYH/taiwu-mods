using System;

namespace CharacterStudio.Backend;

internal enum CreationSource
{
    InitialVillage,
    ManualCreate,
    RecruitCreated,
    JoinedVillage,
    ExistingVillagerMonthly
}

internal static class CreationSourceContext
{
    [ThreadStatic]
    private static CreationSource? _source;
    [ThreadStatic]
    private static string? _profileId;

    internal static CreationSource Source => _source ?? CreationSource.RecruitCreated;
    internal static string? ProfileId => _profileId;

    internal static IDisposable Push(CreationSource source, string? profileId)
    {
        CreationSource? previousSource = _source;
        string? previousProfile = _profileId;
        _source = source;
        _profileId = profileId;
        return new Scope(() =>
        {
            _source = previousSource;
            _profileId = previousProfile;
        });
    }

    private sealed class Scope(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
