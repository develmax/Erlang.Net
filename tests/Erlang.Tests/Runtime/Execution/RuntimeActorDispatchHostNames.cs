namespace Erlang.Tests;

internal static class RuntimeActorDispatchHostNames
{
    internal const string Test = "runtime/owned-actor-dispatch-capacity-continuation-and-retirement";
    internal const string Failure = "Runtime actor dispatch contract failed.";
    internal const string DispatcherField = "actorDispatcher";
    internal const string CompletionProperty = "ActorDispatchCompletion";
    internal const string RetireMethod = "CompleteWhenIdle";
    internal const string SystemInfo = "system_info";
    internal const string Schedulers = "schedulers";
    internal const string Message = "dispatch_receive_marker";
    internal const string Module = "dispatch_initialized_module";
    internal const string PostRetirementModule = "dispatch_post_retirement_module";
    internal const string Function = "value";
    internal const string InitializedValue = "dispatch_initialized_value";
    internal const string Status = "Owned actor dispatch HOST contracts passed; physical BEAM scheduler parity unclaimed.";
}
