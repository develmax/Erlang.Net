using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamZeroBifArgumentOrderChecks
{
    internal const string TestName = "compiler/beam/forward-zero-bif-argument-through-literal-moves";
    private const string KeptTemporary = "The zero-operand BIF did not write directly to the outgoing argument.";
    private const string MovedLiveResult = "A zero-operand BIF result still used in X0 was redirected.";
    private const string Source = """
        -module(beam_zero_bif_argument_order).
        -export([schedule/2, keep/2]).
        schedule(Device,Value) ->
            driver:set(Device,Value),
            erlang:send_after(300,self(),tick).
        keep(Device,Value) ->
            driver:set(Device,Value),
            Pid = self(),
            driver:result(Pid,Pid,tag).
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_zero_bif_argument_order.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>().ToArray();
        object[] suffix = FromBif("schedule");
        if (suffix is not [var bif, var last, var first, var tail]
            || Operands(bif)[^1] is not ("x", 1)
            || Opcode(last) != Move || Operands(last)[1] is not ("x", 2)
            || Opcode(first) != Move || Operands(first)[1] is not ("x", 0)
            || Opcode(tail) != TailExternalCallWithFrame)
        {
            throw new InvalidOperationException(KeptTemporary);
        }

        if (Operands(FromBif("keep")[0])[^1] is not ("x", 0))
        {
            throw new InvalidOperationException(MovedLiveResult);
        }

        object[] FromBif(string name)
        {
            object selected = functions.Single(candidate => (string)Property(candidate, "Name") == name);
            int start = (int)Property(selected, "FirstInstructionIndex");
            int end = (int)Property(selected, "EndInstructionIndex");

            return instructions[start..end].SkipWhile(instruction => Opcode(instruction) != BifZero)
                .Where(instruction => Opcode(instruction) != Line).ToArray();
        }
    }

    private static (string Kind, int Value)[] Operands(object instruction) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>()
            .Select(operand => ((string)Property(operand, "Kind"), (int)(BigInteger)Property(operand, "Value"))).ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
