using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSavedResultReceiverShuffleChecks
{
    internal const string TestName = "compiler/beam/stage-binary-call-receiver-while-saving-result";
    private const string MissingShuffle = "The two-argument call did not stage the receiver before the result spill.";
    private const string ReusedInput = "The outgoing receiver was staged over a still-needed call result.";
    private const string Source = """
        -module(beam_saved_result_receiver_shuffle).
        -export([create/1, consume/1]).
        create(Parent) ->
            Layout = service:layout(vertical,Parent,[]),
            First = service:create(Parent,1,"First",[]),
            Second = service:create(Parent,2,"Second",[]),
            Third = service:create(Parent,3,[]),
            service:add(Layout,First),
            service:add(Layout,Second),
            service:add(Layout,Third),
            service:connect(Parent,notify),
            {Layout,First,Second}.
        consume(Parent) ->
            Layout = service:layout(vertical,Parent,[]),
            First = service:create(Parent,1,"First",[]),
            Second = service:create(Parent,2,"Second",[]),
            Third = service:create(Parent,3,[]),
            service:add(Layout,Third),
            service:add(Layout,Second),
            service:add(Layout,First),
            service:connect(Parent,notify),
            {Layout,First,Second}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_saved_result_receiver_shuffle.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>().ToArray();
        if (!HasShuffle("create"))
        {
            throw new InvalidOperationException(MissingShuffle);
        }

        if (HasShuffle("consume"))
        {
            throw new InvalidOperationException(ReusedInput);
        }

        bool HasShuffle(string name)
        {
            var function = functions.Single(candidate => (string)Property(candidate, "Name") == name);
            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");
            object[] body = instructions[start..end].Where(instruction => Opcode(instruction) != Line).ToArray();
            for (int index = 1; index + 4 < body.Length; index++)
            {
                if (Opcode(body[index - 1]) != ExternalCall || Opcode(body[index + 4]) != ExternalCall
                    || body[index..(index + 4)].Any(instruction => Opcode(instruction) != Move))
                {
                    continue;
                }

                if (Operands(body[index]) is [("y", _), ("x", 2)]
                    && Operands(body[index + 1]) is [("x", 0), ("y", _)]
                    && Operands(body[index + 2]) is [("y", _), ("x", 1)]
                    && Operands(body[index + 3]) is [("x", 2), ("x", 0)]
                    && Operands(body[index + 4])[0].Value == 2)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private static (string Kind, int Value)[] Operands(object instruction) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>()
            .Select(operand => ((string)Property(operand, "Kind"), (int)(BigInteger)Property(operand, "Value"))).ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
