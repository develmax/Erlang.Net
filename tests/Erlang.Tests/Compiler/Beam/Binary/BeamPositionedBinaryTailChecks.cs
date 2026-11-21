using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPositionedBinaryTailChecks
{
    internal const string TestName = "compiler/beam/positioned-tail-size-delays-materialization";
    private const string ScheduleMismatch = "Expected the original match context to remain live only until a terminal binary constructor.";
    private const string Source = """
        -module(context_tail_probe).
        -export([build/1, build_bits/1, consume/1, reuse/1, size_only/1]).

        build(Bin) ->
            <<H,T/binary>> = Bin,
            case H band 128 of
                128 -> <<(byte_size(T)+2):32,0,H,T/binary>>;
                _ -> <<(byte_size(T)+1):32,H,T/binary>>
            end.

        build_bits(Bin) ->
            <<H:3,T/bitstring>> = Bin,
            case H band 4 of
                4 -> <<(bit_size(T)+2):32,0,H:3,T/bitstring>>;
                _ -> <<(bit_size(T)+1):32,H:3,T/bitstring>>
            end.

        consume(Bin) ->
            <<H,T/binary>> = Bin,
            {H, context_tail_peer:opaque(T), byte_size(T)}.

        reuse(Bin) ->
            <<H,T/binary>> = Bin,
            <<H2,T2/binary>> = T,
            {H,H2,byte_size(T),T2}.

        size_only(Bin) ->
            <<H,T/binary>> = Bin,
            {H,byte_size(T)}.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "context_tail_probe.erl");
        object[] operations = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>().ToArray();
        int[] starts = Enumerable.Range(0, operations.Length)
            .Where(index => Opcode(operations[index]) == FunctionInfo).ToArray();
        for (int function = 0; function < 5; function++)
        {
            object[] body = operations[starts[function]..starts[function + 1]];
            int[] sizes = Enumerable.Range(0, body.Length).Where(index => Opcode(body[index]) == GcBifOne).ToArray();
            if (function >= 2)
            {
                if (body.Any(instruction => Opcode(instruction) == GetBinaryTail))
                {
                    throw new InvalidOperationException(ScheduleMismatch);
                }

                continue;
            }

            if (sizes.Length != 2 || !body.Any(instruction => Opcode(instruction) == BadMatch))
            {
                throw new InvalidOperationException(ScheduleMismatch);
            }

            foreach (int size in sizes)
            {
                if (Value(body[size], 1) != 2 || Value(body[size], 3) != 1 || Value(body[size], 4) != 2
                    || Opcode(body[size + 2]) != GetBinaryTail || Value(body[size + 2], 2) != 3
                    || Opcode(body[size + 3]) != CreateBinary || Value(body[size + 3], 2) != 3)
                {
                    throw new InvalidOperationException(ScheduleMismatch);
                }
            }
        }

        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term packet = new BitString(new byte[] { 128, 255 });
            Term expected = new BitString(new byte[] { 0, 0, 0, 3, 0, 128, 255 });
            if (!expected.Equals(await image.Invoke(context, "build", packet)))
            {
                throw new InvalidOperationException(ScheduleMismatch);
            }

            Term unaligned = new BitString(new byte[] { 224 }, 3);
            try
            {
                await image.Invoke(context, "build", unaligned);
                throw new InvalidOperationException(ScheduleMismatch);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), unaligned)))
            {
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object instruction, int index) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index), "Value");
}
