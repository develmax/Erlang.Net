using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOpaqueSsaClosureIdentityChecks
{
    internal const string TestName = "compiler/beam/names-opaque-inline-callback-after-core";
    private const string Module = "beam_opaque_inlined_closure_order";
    private const string Inlining = "-compile(inline).";
    private const string WrongName = "The only opaque callback did not receive the name selected by the active Core pass.";
    private const string Source = """
        -module(beam_opaque_inlined_closure_order).
        -compile(inline).
        -export([pick/1]).
        pick([{tag, _} | Rest]) -> pick(Rest);
        pick([_ | Rest]) -> pick(Rest);
        pick([]) -> capture_sink:take(fun(X) -> X end).
        """;

    private const string ProtectedSource = """
        -module(beam_opaque_inlined_closure_order).
        -compile(inline).
        -export([pick/1]).

        pick([Value]) ->
            Result = try
                F = fun(X) -> {Value, X} end,
                lists:map(F, [Value])
            catch
                throw:Reason -> {caught, Reason}
            end,
            {ok, Result};
        pick([]) ->
            empty.
        """;

    internal static Task Run()
    {
        if (ClosureName(Source) != "-pick/1-anonymous-0-"
            || ClosureName(Source.Replace(Inlining, string.Empty, StringComparison.Ordinal)) != "-pick/1-fun-0-"
            || ClosureName(ProtectedSource) != "-pick/1-anonymous-0-"
            || ClosureName(ProtectedSource.Replace(Inlining, "-compile(no_inline).", StringComparison.Ordinal))
                != "-pick/1-fun-0-")
        {
            throw new InvalidOperationException(WrongName);
        }

        return Task.CompletedTask;
    }

    private static string ClosureName(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Module + ".erl"));
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>();

        return functions.Select(function => (string)Property(function, "Name"))
            .Single(function => function.StartsWith("-pick/1-", StringComparison.Ordinal));
    }
}
