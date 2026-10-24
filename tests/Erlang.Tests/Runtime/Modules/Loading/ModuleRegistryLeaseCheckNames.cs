namespace Erlang.Tests;

internal static class ModuleRegistryLeaseCheckNames
{
    internal const string Test = "host/module-registry-image-lease-cleanup";
    internal const string Module = "registry_lease_cleanup";
    internal const string Function = "run";
    internal const string Gate = "gate";
    internal const string Executing = "IsExecutingCode";
    internal const string MetadataMissing = "Registry lease check metadata is missing";
    internal const string LeaseMissing = "Call did not acquire an actual image lease";
    internal const string LeaseLeaked = "Call tracing failure leaked the actual image lease";
    internal const string FlowLeaked = "Call failure retained its source image in the calling flow";
    internal const string BodyEntered = "Halted tracing unexpectedly invoked the image body";
    internal const string WrongHalt = "Call did not propagate its same-runtime halt";
    internal const string BodyFailure = "Actual registry image body failure";
    internal const string InBodyLeaseMissing = "Image body executed without its lease or attached flow";
    internal const string UnexpectedSuccess = "Failing image body completed successfully";
    internal const int DeadlineSeconds = 5;
    internal const int HaltStatus = 0;
    private const string Success = "ok";
    internal static readonly Erlang.Atom Result = Erlang.Term.A(Success);
}
