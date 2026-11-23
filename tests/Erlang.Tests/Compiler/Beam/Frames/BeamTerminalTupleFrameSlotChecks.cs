using System.Collections;
using System.Numerics;
using System.Reflection;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalTupleFrameSlotChecks
{
    internal const string TestName = "compiler/beam/promotes-terminal-tuple-result-scratch";
    private const string Fixture = "beam_terminal_tuple_frame.erl";
    private const string IncorrectWidth = "Only the post-call tuple projection may leave the frame.";
    private const string InvalidPrefix = "An uninitialized X prefix cannot be extended through a heap test.";
    private const string IncorrectRoots = "Each terminal record result must retain the roots used by that branch.";
    private const string Source = """
        -module(beam_terminal_tuple_frame).
        -export([read/1, negative/1, get/2, touch/1]).
        -record(state, {name, handle, cont}).

        read(S) ->
            Fd = S#state.handle,
            case ?MODULE:get(Fd, S#state.cont) of
                {error, Reason} -> {error, {truncated, Reason}};
                eof -> {ok, S, []};
                {Next, []} -> read(S#state{cont = Next});
                {Next, Items, _} -> {ok, S#state{cont = Next}, Items};
                {Next, Items} -> {ok, S#state{cont = Next}, Items}
            end.

        negative(S) ->
            Fd = S#state.handle,
            case ?MODULE:get(Fd, S#state.cont) of
                {Next, Items, _} -> ?MODULE:touch(Next), {ok, S#state{cont = Next}, Items};
                {Next, Items} -> {ok, S#state{cont = Next}, Items};
                eof -> {ok, S, []}
            end.

        get(_, {return, Value}) -> Value;
        get(_, {retry, Value}) -> {{return, Value}, []}.

        touch(Value) -> Value.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>().ToArray();
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        int readStart = FindStart("read");
        int negativeStart = FindStart("negative");
        var read = instructions[readStart..negativeStart];
        var negative = instructions[negativeStart..FindStart("get")];
        if (Allocation(read) != 1 || Allocation(negative) != 3)
        {
            throw new InvalidOperationException(IncorrectWidth);
        }

        var heaps = read.Where(instruction => Opcode(instruction) == TestHeap)
            .Select(instruction => ((int)Operand(instruction, 0), (int)Operand(instruction, 1)));
        if (!heaps.SequenceEqual([(9, 1), (6, 1), (5, 2), (9, 2), (4, 0)]))
        {
            throw new InvalidOperationException(IncorrectRoots);
        }

        int firstHeap = Array.FindIndex(read, instruction => Opcode(instruction) == TestHeap);
        if (Opcode(read[firstHeap + 1]) != GetTupleElement || Operand(read[firstHeap + 1], 2) != 1
            || Opcode(read[firstHeap + 2]) != UpdateRecord || Operand(read[firstHeap + 2], 3) != 1
            || Opcode(read[firstHeap + 3]) != GetTupleElement || Operand(read[firstHeap + 3], 2) != 0
            || Opcode(read[firstHeap + 4]) != PutTuple)
        {
            throw new InvalidOperationException(IncorrectRoots);
        }

        var nil = read.Single(instruction => Opcode(instruction) == IsNil);
        if (Operand(nil, 1) != 0)
        {
            throw new InvalidOperationException(IncorrectRoots);
        }

        if (!CanExtendRootPrefix(initializeX1: true)
            || CanExtendRootPrefix(initializeX1: false)
            || CanExtendRootPrefix(initializeX1: true, dropRoots: true))
        {
            throw new InvalidOperationException(InvalidPrefix);
        }

        return Task.CompletedTask;

        int FindStart(string name) =>
            (int)Property(functions.Single(function => (string)Property(function, "Name") == name), "FirstInstructionIndex");
    }

    private static bool CanExtendRootPrefix(bool initializeX1, bool dropRoots = false)
    {
        byte[] prefix = initializeX1 ? [GetTupleElement, X(0), U(1), X(1)] : [];
        byte[] drop = dropRoots ? [TestHeap, U(0), U(0)] : [];
        byte[] entry = Start(
            1,
            2,
            1,
            1
        );
        byte[] program =
        [
            .. entry, GetTupleElement, X(0), U(0), Y(0), .. prefix, .. drop,
            TestHeap, U(2), U(0), PutList, Y(0), A(NilAtomIndex), X(0), Return, EndCode
        ];
        ReadOnlyMemory<byte> code = program;
        object header = New(
            "BeamCodeHeader",
            0,
            (int)InitYRegisters,
            3,
            1,
            code
        );
        object decoded = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            null
        );
        object[] instructions = ((IEnumerable)decoded).Cast<object>().ToArray();
        System.Type type = typeof(BeamModuleCompiler).GetNestedType("BeamProjectionFlow", BindingFlags.NonPublic)!;
        object flow = Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [decoded],
            culture: null
        )!;
        int first = Array.FindIndex(instructions, instruction => Opcode(instruction) == GetTupleElement);
        int last = Array.FindIndex(instructions, instruction => Opcode(instruction) == PutList);
        var heaps = new HashSet<int> { last - 1 };

        return (bool)Call(
            flow,
            "HasInitializedTerminalTupleRoots",
            first,
            last,
            2,
            heaps
        );
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Allocation(IEnumerable<object> instructions) =>
        Operand(instructions.Single(instruction => Opcode(instruction) is Allocate or AllocateHeap), 0);

    private static BigInteger Operand(object instruction, int index) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index), "Value");
}
