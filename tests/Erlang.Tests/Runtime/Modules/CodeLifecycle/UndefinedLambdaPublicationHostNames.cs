using System.Reflection;

namespace Erlang.Tests;

internal static class UndefinedLambdaPublicationHostNames
{
    internal const string Test = "host_restored_local_fun_publication_and_lambda_dispatch";
    internal const string Broken = "Restored local function publication or original-wrapper contract failed.";
    internal const string Module = "lambda_publication328";
    internal const string LegacyModule = "lambda_legacy_host328";
    internal const int LegacyIndex = 1;
    internal const int LegacyUniqueBytes = 16;
    internal const string CallbackModule = "lambda_publication328_callback";
    internal const string Handler = "lambda_publication328_handler";
    internal const string UndefinedLambda = "undefined_lambda";
    internal const string Body = "body";
    internal const string Named = "make_named";
    internal const string Anonymous = "make_anonymous";
    internal const string Other = "make_other";
    internal const string Flag = "error_handler";
    internal const string ProcessFlag = "process_flag";
    internal const string ToBinary = "term_to_binary";
    internal const string FromBinary = "binary_to_term";
    internal const string Purge = "purge_module";
    internal const string Argument = "argument";
    internal const string Environment = "environment";
    internal const string Handled = "handled";
    internal const string Capture = "Capture";
    internal const string Publication = "Publication";
    internal const string Factory = "Factory";
    internal const string RegisterSourceFactory = "RegisterSourceFactory";
    internal const string Gate = "gate";
    internal const string InvokeFactory = nameof(UndefinedLambdaPublicationHostBarrier.Construct);
    internal const int LambdaArity = 1;
    internal const int CallbackArity = 3;
    internal const int RetryObservationCount = 8;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string Source = """
        -module(lambda_publication328).
        -export([make_named/0,make_anonymous/1,make_other/0]).
        make_named()->fun hidden/1.
        make_anonymous(E)->fun(X)->lambda_publication328_callback:body(),{E,X} end.
        make_other()->fun other/1.
        hidden(X)->lambda_publication328_callback:body(),X.
        other(X)->X.
        """;
    internal const string Interpreted = "fun(X)->lambda_publication328_callback:body(),X end";
}
