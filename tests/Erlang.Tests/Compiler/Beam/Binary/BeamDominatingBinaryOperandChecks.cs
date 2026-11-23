using System.Collections;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamCallConstructionProbe;

namespace Erlang.Tests;

internal static class BeamDominatingBinaryOperandChecks
{
    internal const string TestName = "compiler/beam/uses-dominating-literal-bit-operands";
    private const string IncorrectConstruction = "Dominating binary operands changed the string run, bound match, or lexical scope.";
    private const string Source = """
        -module(beam_dominating_binary_operands).
        -export([pack/1, repeat/2, shadow/1, effect/0]).

        pack(X) ->
            Header = 5,
            Width = 5,
            erlang:put(binary_effect, saved),
            <<Header:3, 19:Width, X:8>>.

        repeat(X, Header) ->
            Header = 5,
            <<Header:3, X:5>>.

        shadow(X) ->
            Header = 5,
            Fun = fun(Header) -> <<Header:3, X:5>> end,
            {<<Header:3, X:5>>, Fun(2)}.

        effect() -> erlang:get(binary_effect).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_dominating_binary_operands.erl");
        var probe = new BeamCallConstructionProbe(image);
        var pack = probe.Body("pack").Single(operation => Opcode(operation) == CreateBinary);
        object code = Code(image);
        object structure = Property(code, "Structure");
        object[] chunks = ((IEnumerable)Property(structure, "Chunks")).Cast<object>().ToArray();
        object poolChunk = chunks.Single(chunk => (string)Property(chunk, "Id") == BeamBinaryHostNames.Strings);
        var pool = (ReadOnlyMemory<byte>)Property(poolChunk, "Payload");
        const int firstSegmentValue = 10;
        if (pool.Span[Value(Operand(pack, firstSegmentValue))] != 179
            || Value(Operand(pack, firstSegmentValue + 1)) != 1)
        {
            throw new InvalidOperationException(IncorrectConstruction);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term x = Term.I(7);
            if (!new BitString([179, 7]).Equals(await module.Invoke(context, "pack", x))
                || !Term.A("saved").Equals(await module.Invoke(context, "effect"))
                || !new BitString([167]).Equals(await module.Invoke(
                    context,
                    "repeat",
                    x,
                    Term.I(5)
                ))
                || !Term.Tuple(new BitString([167]), new BitString([71])).Equals(
                    await module.Invoke(context, "shadow", x)
                ))
            {
                throw new InvalidOperationException(IncorrectConstruction);
            }

            try
            {
                await module.Invoke(
                    context,
                    "repeat",
                    x,
                    Term.I(4)
                );
                throw new InvalidOperationException(IncorrectConstruction);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), Term.I(5))))
            {
            }

            return Nil.Value;
        });
    }
}
