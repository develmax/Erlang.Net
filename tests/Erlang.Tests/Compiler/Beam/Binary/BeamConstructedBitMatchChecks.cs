using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamConstructedBitMatchChecks
{
    internal const string TestName = "compiler/beam/constructed-bitstring-starts-dynamic-match-without-type-failure";
    private const string ScheduleMismatch = "The constructed bitstring match must retain the extraction and exhaustion failure edges.";
    private const string ResultMismatch = "The constructed bitstring match returned an unexpected value or error.";
    private const string Source = """
        -module(constructed_match_probe).
        -export([dynamic/2, unknown/2, widen/2, fail/3]).

        dynamic(N,V) ->
            <<W:N>> = <<V:N>>,
            W.

        unknown(N,B) ->
            <<W:N>> = B,
            W.

        widen(N,V) ->
            Size = N+8,
            <<W:Size>> = <<V,0:N>>,
            W.

        fail(N,V,M) ->
            <<W:M>> = <<V:N>>,
            W.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "constructed_match_probe.erl");
        object[] operations = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>().ToArray();
        int[] starts = Enumerable.Range(0, operations.Length)
            .Where(index => Opcode(operations[index]) is StartBinaryMatchKnown or StartBinaryMatch).ToArray();
        if (starts.Length != 4 || Opcode(operations[starts[1]]) != StartBinaryMatch
            || starts.Where((_, index) => index != 1)
                .Any(start => Opcode(operations[start]) != StartBinaryMatchKnown))
        {
            throw new InvalidOperationException(ScheduleMismatch);
        }

        foreach (int start in starts)
        {
            object[] body = operations.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            object extraction = body.Single(instruction => Opcode(instruction) == GetBinaryInteger);
            object exhaustion = body.Single(instruction => Opcode(instruction) == BinaryMatch);
            if (Operand(extraction, 0) == 0 || Operand(extraction, 0) != Operand(exhaustion, 0)
                || !body.Any(instruction => Opcode(instruction) == BadMatch))
            {
                throw new InvalidOperationException(ScheduleMismatch);
            }
        }

        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            var original = new BitString(new byte[] { 160 }, 3);
            if (!(await image.Invoke(
                context,
                "dynamic",
                Term.I(3),
                Term.I(5)
            )).Equals(Term.I(5))
                || !(await image.Invoke(
                    context,
                    "dynamic",
                    Term.I(0),
                    Term.I(5)
                )).Equals(Term.I(0))
                || !(await image.Invoke(
                    context,
                    "widen",
                    Term.I(3),
                    Term.I(7)
                )).Equals(Term.I(56))
                || !(await image.Invoke(
                    context,
                    "unknown",
                    Term.I(3),
                    original
                )).Equals(Term.I(5)))
            {
                throw new InvalidOperationException(ResultMismatch);
            }

            try
            {
                await image.Invoke(
                    context,
                    "fail",
                    Term.I(3),
                    Term.I(5),
                    Term.I(2)
                );
                throw new InvalidOperationException(ResultMismatch);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), original)))
            {
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Operand(object instruction, int index) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index), "Value");
}
