-- AutoMonthlyEvent config.
return {
    -- Keep this false during ID discovery.
    EnableAutoProcess = false,

    -- Read-only discovery mode. Dumps IDs and options; does not process events.
    DiscoveryMode = true,

    -- Write discovery output as JSON/JSONL.
    DumpToJson = true,

    -- Output directory relative to the game root.
    DumpDirectory = "Dump_out",

    -- Reserved for the future auto-processing phase.
    ShowConfirmation = false,
    SkipSpecialEvents = true,

    -- Verbose log output.
    LogVerbose = false
}
