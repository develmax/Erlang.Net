using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamLocalTaggedTuplePayloadChecks
{
    internal const string TestName = "compiler/beam/proves-matching-local-tagged-payload-shapes";
    private const string ShapeError = "Local tagged result proof did not account for every selected return.";

    internal static void Run()
    {
        const string Source = """
            -module(local_payload_fixture).
            -export([entry/1]).
            entry(Input) -> tagged(Input).
            tagged([_ | Rest]) -> tagged(Rest);
            tagged([]) -> {ok,pair({first,second})};
            tagged(_) -> {error,no_value}.
            pair({A,B}=Both) -> if A =:= B -> {A,B}; true -> Both end.
            """;
        Check(Source, true);
        Check(Source.Replace("{error,no_value}", "{ok,no_value}", StringComparison.Ordinal), false);
        Check(Source.Replace("true -> Both", "true -> external_probe:untyped()", StringComparison.Ordinal), false);
        Check(Source, true, tag: "ignored");

        static void Check(string source, bool expected, string tag = "ok")
        {
            ModuleDefinition module = new Parser(source).ParseModule();
            var compiler = new BeamModuleCompiler(module.Name, sourceModule: module);
            FunctionDefinition terminal = module.DeclaredFunctions.Single(function => function.Name == "tagged");
            bool valid = (bool)Call(
                compiler,
                "HasProvenLocalTaggedTuplePayload",
                terminal,
                tag,
                2,
                module.DeclaredFunctions
            );
            if (valid != expected)
            {
                throw new InvalidOperationException(ShapeError);
            }
        }
    }
}
