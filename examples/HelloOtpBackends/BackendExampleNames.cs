namespace HelloOtpBackends;

internal static class BackendExampleNames
{
    internal const string Observer = "backend_project389";
    internal const string Runtime = "erlang";
    internal const string ObserverFile = "Modules/backend_project389.beam";
    internal const string ObserverSha256 = "796963F435BC84B859707C7BFFEE946D35F9BECBAF6A5F2694F055A8D48541EC";
    internal const string Run = "run";
    internal const string Stacks = "run_stacks";
    internal const string ValuesFile = "values.etf";
    internal const string StacksFile = "stacks.etf";
    internal const string ReportFile = "backends.json";
    internal const string ObserveOption = "--observations";
    internal const string Usage = "Usage: HelloOtpBackends [--observations <new-output-directory>]";
    internal const string Failure = "Selected library backend or execution ownership differs";
    internal const string Normal = "normal";
    internal const string Modules = "Modules";
    internal const int EtfMinorVersion = 2;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(120);
}
