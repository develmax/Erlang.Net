namespace BeamFixtureExport;

internal static class OnLoadObserverNames
{
    internal const string CallbackSuccessResult = "ok";
    internal const string CallbackFailureResult = "not_ok";
    internal const string Directory = "on-load-observers";
    internal const string Manifest = "on-load-observer-manifest.json";
    internal const string Observer = "beam_on_load_observer";
    internal const string NewVersion = "observer-new.beam";
    internal const string OldVersion = "observer-old.beam";
    internal const int WaitMilliseconds = 10000;
    internal const string InvalidEtf = "Original on_load observer summary differs from its independent contract";
    internal static readonly OnLoadObserverScenario[] Scenarios =
    [
        new(
            "fresh-success",
            "beam_on_load_fresh_success",
            true,
            false
        ),
        new(
            "fresh-failure",
            "beam_on_load_fresh_failure",
            false,
            false
        ),
        new(
            "replacement-success",
            "beam_on_load_replace_success",
            true,
            true
        ),
        new(
            "replacement-failure",
            "beam_on_load_replace_failure",
            false,
            true
        )
    ];
}
