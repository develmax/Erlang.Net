using System.Reflection;

namespace Erlang.Tests;

internal static class ProcessErrorHandlerHostNames
{
    internal const string Test = "host_process_error_handler_dispatch_frames_source_lease_and_isolation";
    internal const string Failure = "Process error handler assertion failed";
    internal const string Flag = "error_handler";
    internal const string Default = "error_handler";
    internal const string Handler = "host_error_handler";
    internal const string OtherHandler = "host_other_error_handler";
    internal const string OuterHandler = "host_outer_error_handler";
    internal const string InnerHandler = "host_inner_error_handler";
    internal const string Undefined = "undefined_function";
    internal const string MissingModule = "missing_handler_target";
    internal const string MissingFunction = "missing_handler_function";
    internal const string AbsentHandler = "missing_handler_export";
    internal const string Payload = "handler_payload";
    internal const string Invalid = "invalid_handler_value";
    internal const string Fault = "handler_fault";
    internal const string Bridge = "handler_host_bridge";
    internal const string Held = "held";
    internal const string NonTail = "non_tail";
    internal const string Apply = "apply_call";
    internal const string Tail = "tail_call";
    internal const string ActiveCode = "activeCode";
    internal const string RegisterIntrinsic = "RegisterIntrinsic";
    internal const string Forwarded = "forwarded";
    internal const string Kill = "kill";
    internal const string Killed = "killed";
    internal const string ProcessFlag = "process_flag";
    internal const string ProcessInfo = "process_info";
    internal const string Source = """
        -module(source_error_handler).
        -export([undefined_function/3]).
        undefined_function(M,F,A)->R=handler_host_bridge:held(M,F,A),{forwarded,R}.
        """;
    internal const string CallerSource = """
        -module(error_handler_caller).
        -export([non_tail/0,tail_call/0,apply_call/0]).
        non_tail()->R=missing_handler_target:missing_handler_function(handler_payload),{forwarded,R}.
        tail_call()->missing_handler_target:missing_handler_function(handler_payload).
        apply_call()->apply(missing_handler_target,missing_handler_function,[handler_payload]).
        """;
    internal const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
