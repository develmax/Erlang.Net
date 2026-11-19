using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOrdinaryScratchCopyChecks
{
    internal const string TestName = "compiler/beam/ordinary-entry-call-composes-scratch-copy";
    private const string ScheduleMismatch = "Expected direct local call arguments in pinned OTP move order.";
    private const string Source = """
        -module(ordinary_scratch_copies).
        -export([make/3, run/5, identity/5]).

        make(File, Opts, Dict) ->
            lists:foldl(fun(Mode, Acc) -> [render(File,Opts,Dict,Mode) | Acc] end, [], [source,header]).

        run(Mode, Acc, File, Opts, Dict) ->
            [render(File,Opts,Dict,Mode) | Acc].

        identity(Mode, Acc, File, Opts, Dict) ->
            [render(Mode,File,Opts,Dict) | Acc].

        render(File, Opts, Dict, Mode) -> {File, Opts, Dict, Mode}.
        """;

    internal static Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "ordinary_scratch_copies.erl");
        object[] operations = ((IEnumerable)Property(Code(image), "Instructions")).Cast<object>()
            .Where(instruction => Opcode(instruction) != Line).ToArray();
        int[] calls = Enumerable.Range(0, operations.Length)
            .Where(index => Opcode(operations[index]) == LocalCall).ToArray();

        (int From, int To)[] composed = [(3, 1), (0, 3), (2, 0), (4, 2)];
        (int From, int To)[] unchanged = [(2, 1), (3, 2), (4, 3)];
        if (calls.Length != 3)
        {
            throw new InvalidOperationException(ScheduleMismatch);
        }

        Check(calls[0], composed);
        Check(calls[1], unchanged);
        Check(calls[2], composed);

        return Task.CompletedTask;

        void Check(int call, (int From, int To)[] copies)
        {
            int first = call - copies.Length;
            if (Opcode(operations[first - 2]) != Allocate || Operand(operations[first - 2], 0) != 1
                || Opcode(operations[first - 1]) != Move || Operand(operations[first - 1], 0) != 1
                || Operand(operations[first - 1], 1) != 0)
            {
                throw new InvalidOperationException(ScheduleMismatch);
            }

            for (int index = 0; index < copies.Length; index++)
            {
                object move = operations[first + index];
                if (Opcode(move) != Move || Operand(move, 0) != copies[index].From || Operand(move, 1) != copies[index].To)
                {
                    throw new InvalidOperationException(ScheduleMismatch);
                }
            }
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Operand(object instruction, int index) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index), "Value");
}
