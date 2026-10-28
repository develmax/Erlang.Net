namespace Erlang.Tests;

internal static class NativeResourceTypeRetirement575Names
{
    internal const string RepeatedCase = "native-resource575/empty-committed-types-retire-before-vm-shutdown";
    internal const string PendingCase = "native-resource575/pending-rollback-and-new-owner-prevent-premature-detach";
    internal const string HeldCase = "native-resource575/payload-and-physical-invocation-custody-survive-detach";
    internal const string ResourceName = "empty_type575";
    internal const string Token = "Token";
    internal const string Gate = "gate";
    internal const string Types = "types";
    internal const string Handle = "handle";
    internal const string RegistryType = "Erlang.NativeResourceTypes527";
    internal const string Runtime = "Runtime";
    internal const string BindingsRetire = "RetireAsync";
    internal const string PhysicalPin = "RetainResourceImage524";
    internal const string Failure = "Native resource type retirement did not release its actual domain/registry/handle ownership.";
    internal const string CleanupFailure = "Resource-type retirement assertion or an independent cleanup phase failed.";
    internal const int Repetitions = 32;
    internal const int Create = 1;
    internal const int Takeover = 2;
}
