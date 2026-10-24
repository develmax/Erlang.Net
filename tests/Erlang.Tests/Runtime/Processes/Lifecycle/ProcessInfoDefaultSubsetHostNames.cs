namespace Erlang.Tests;

internal static class ProcessInfoDefaultSubsetHostNames
{
    internal const string Test = "runtime/process-info-default-supported-subset-public-registration";
    internal const string Registration = "process_info_default_subset_owner";
    internal const string Failure = "Public process_info/1 subset contract failed.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
}
