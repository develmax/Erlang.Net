using System.Reflection;

namespace Erlang.Tests;

internal static class UndefinedLambdaLifecycleHostNames
{
    internal const string Test = "host_undefined_lambda_pending_purge_and_selected_source_handler_lease";
    internal const string Broken = "Restored local function pending purge or selected source handler lease contract failed.";
    internal const string Target = "lambda_lifecycle_target";
    internal const string Handler = "lambda_lifecycle_handler";
    internal const string Bridge = "lambda_lifecycle_bridge";
    internal const string Held = "held";
    internal const string Named = "make_named";
    internal const string Anonymous = "make_anonymous";
    internal const string Undefined = "undefined_lambda";
    internal const string Flag = "error_handler";
    internal const string ProcessFlag = "process_flag";
    internal const string ProcessInfo = "process_info";
    internal const string Status = "status";
    internal const string ToBinary = "term_to_binary";
    internal const string FromBinary = "binary_to_term";
    internal const string Purge = "purge_module";
    internal const string Prepare = "PreparePurge";
    internal const string Finish = "FinishPurge";
    internal const string Image = "Image";
    internal const string RegisterIntrinsic = "RegisterIntrinsic";
    internal const string CodeGate = "codeGate";
    internal const string ActiveCode = "activeCode";
    internal const string Argument = "argument";
    internal const string Environment = "environment";
    internal const string OldBody = "old_body";
    internal const string Handled = "handled";
    internal const string Forwarded = "forwarded";
    internal const string Fault = "lambda_handler_fault";
    internal const string Kill = "kill";
    internal const string Killed = "killed";
    internal const int CallbackArity = 3;
    internal const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    internal const string TargetSource = """
        -module(lambda_lifecycle_target).
        -export([make_named/0,make_anonymous/1]).
        make_named()->fun hidden/1.
        hidden(X)->{old_body,X}.
        make_anonymous(E)->fun(X)->{old_body,E,X} end.
        """;
    internal const string ReplacementSource = """
        -module(lambda_lifecycle_target).
        -export([different/0]).
        different()->replacement.
        """;
    internal const string HandlerSource = """
        -module(lambda_lifecycle_handler).
        -export([undefined_lambda/3]).
        undefined_lambda(M,F,A)->R=lambda_lifecycle_bridge:held(M,F,A),{forwarded,R}.
        """;
}
