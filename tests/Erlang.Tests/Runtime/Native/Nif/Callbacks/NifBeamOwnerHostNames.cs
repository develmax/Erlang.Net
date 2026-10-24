using System.Reflection;

namespace Erlang.Tests;

internal static class NifBeamOwnerHostNames
{
    internal const BindingFlags PropertyMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string Passed = "PASS prepared BEAM image owner ";
    internal const string Test = "runtime/nif-prepared-image-owner-without-global-exports";
    internal const string Broken = "Prepared BEAM image owner invariant failed";
    internal const string RegisterIntrinsic = "RegisterIntrinsic";
    internal const string CaptureCaller = "CaptureExecutingCode";
    internal const string Execution = "ExecutionIdentity";
    internal const string Stage = "StageLoaderOnLoad";
    internal const string LoaderPending = "loaderPendingImages";
    internal const string OwnerMismatch = "OwnerMismatch";
    internal const string UnknownImage = "UnknownImage";
    internal const string FallbackResult = "new_body";
    internal const string NativeResult = "managed_owner_override";
    internal const string RollbackFunction = "temporary_owner_probe";
    internal const string RollbackFailure = "Deliberate image-owner rollback control";
    internal const string RetireImage = "RetireNifImageAsync";
    internal const string OwnerProperty = "ExecutionOwner";
}
