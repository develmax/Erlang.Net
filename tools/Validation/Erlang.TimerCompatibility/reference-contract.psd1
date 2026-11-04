@{
    ReleaseVersion = '29.1.1'
    SourceCommit = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
    MinimumClosureFiles = 15
    PrimarySource = 'tools/Validation/Erlang.TimerCompatibility/timer_port_cases.erl'
    SuiteSource = 'tools/Validation/Erlang.TimerCompatibility/Reference/timer_simple_SUITE.erl'
    SuiteObserverSource = 'tools/Validation/Erlang.TimerCompatibility/timer_suite_capture.erl'
    ProviderNotAdmitted = 'Timer requires the admitted official OTP 29.1.1 provider.'
    IncompleteProviderClosure = 'Incomplete timer oracle closure.'
    ProviderInputChanged = 'Timer oracle input changed.'
    OutputAlreadyExists = 'Choose a new output directory.'
    InvalidOutputPath = 'Unsupported output path.'
    PrimaryCaptureFailed = 'Timer oracle failed.'
    UpstreamCaptureFailed = 'Timer upstream capture failed.'
    ComparisonFailed = 'Timer {0} comparison failed.'
    RunnerRequired = 'Supply a built timer validator DLL or use CaptureOnly.'
}
