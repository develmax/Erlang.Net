using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCaughtBranchFrameChecks
{
    internal const string TestName = "compiler/beam/allocates-catch-frame-only-on-protected-branch";
    private const string Fixture = "beam_caught_branch_frame.erl";
    private const string WrongTail = "The direct clause must tail call before allocating the catch frame.";
    private const string WrongFrame = "The caught branch must allocate one handler slot for the ordinary call.";
    private const string MissingFunction = "A caught branch fixture function is missing.";
    private const string WrongEquality = "A repeated parameter in the literal clause must still be matched.";

    internal const string Source = """
        -module(beam_caught_branch_frame).
        -export([dispatch/3,other_slot/3,repeated/3]).

        dispatch(now, Input, _Unused) -> beam_caught_branch_target:invoke(Input);
        dispatch(_, Input, _) -> catch beam_caught_branch_target:invoke(Input).

        other_slot(Input, now, _Unused) -> beam_caught_branch_target:invoke(Input);
        other_slot(Input, _, _) -> catch beam_caught_branch_target:invoke(Input).

        repeated(now, Input, Input) -> beam_caught_branch_target:invoke(Input);
        repeated(_, Input, _) -> catch beam_caught_branch_target:invoke(Input).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions");
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        Check("dispatch", 0);
        Check("other_slot", 1);

        object[] repeated = Function("repeated");
        if (!repeated.Where(instruction => Opcode(instruction) == ExactEqualTest).Select(Operands)
            .Any(operands => operands.Skip(1).All(operand => Property(operand, "Kind") is "x" or "y")))
        {
            throw new InvalidOperationException(WrongEquality);
        }

        return Task.CompletedTask;

        void Check(string function, int tested)
        {
            object[] operations = Function(function);
            int allocation = Array.FindIndex(operations, instruction => Opcode(instruction) == Allocate);
            int tail = Array.FindIndex(operations, instruction => Opcode(instruction) == TailExternalCall);
            int selector = Array.FindIndex(operations, instruction => Opcode(instruction) == ExactEqualTest);
            if (selector < 0 || Value(Operands(operations[selector])[1]) != tested || tail <= selector
                || allocation <= tail)
            {
                throw new InvalidOperationException(WrongTail);
            }

            object[] allocationArguments = Operands(operations[allocation]);
            if (Value(allocationArguments[0]) != 1 || Opcode(operations[allocation + 1]) != CatchEnter
                || operations.Skip(allocation + 2).Count(instruction => Opcode(instruction) == CatchEnd) != 1)
            {
                throw new InvalidOperationException(WrongFrame);
            }
        }

        object[] Function(string name)
        {
            int index = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name && Value(Operands(instruction)[2]) == 3
            );
            if (index < 0)
            {
                throw new InvalidOperationException(MissingFunction);
            }

            return instructions.Skip(index + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
