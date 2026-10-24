namespace Erlang.Tests;

internal static class DistributionEntryCreationHostNames
{
    internal const string PrimitiveModule = "erts_internal";
    internal const string Getter = "get_creation";
    internal const string ErlangModule = "erlang";
    internal const string SetNode = "setnode";
    internal const string Kernel = "net_kernel";
    internal const string Local = "creation_correction402@local";
    internal const string Remote = "creation_correction402@remote";
    internal const string CurrentField = "currentDistributionEntry";
    internal const string RegistryField = "distributionEntryIdentities";
    internal const string FreshTest = "host/distribution-entry-creation-fresh-lazy-budget";
    internal const string UnsignedTest = "host/distribution-entry-creation-unsigned-started";
    internal const string RetainedTest = "host/distribution-entry-creation-retained-dormant-generation";
    internal const string ClosedTest = "host/distribution-entry-creation-closed-owner-releases-roots";
    internal const string Failure = "Distribution-entry creation correction control failed.";
    internal const uint RequestedCreation = 37;
    internal const uint RemoteCreation = 1;
    internal const uint NextCreation = 38;
    internal const int MaximumNodes = 1;
    internal const uint ConnectionIdMask = 0x00ffffff;
}
