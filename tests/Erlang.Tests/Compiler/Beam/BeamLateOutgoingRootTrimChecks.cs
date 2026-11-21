using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamLateOutgoingRootTrimChecks
{
    internal const string TestName = "compiler/beam/trim-consumed-outgoing-root-before-terminal-call";
    private const string Kills = "ReadOutgoingStackValueKills";
    private const string Trim = "PlanCallFrameTrim";
    private const string PairShapeError = "A consumed binary-call list argument must allow the retained Y root to move before trimming.";
    private const string PairPreservationError = "An outgoing value used after the call must remain rooted.";
    private const int StackWidth = 7;
    private const int RetainedSlot = 5;
    private const int ArgumentSlot = 6;
    private const string ShapeError = "The consumed high Y slot did not receive the retained value before the call.";
    private const string Source = """
        -module(beam_late_outgoing_root_trim).
        -export([resolve/3]).
        -record(handle,{ref,type,state=[]}).

        resolve(Window = #handle{}, Name, Class) when is_list(Name), is_atom(Class) ->
            Identifier = service:identify(Name),
            Found = service:lookup(Window, Identifier),
            service:cast(Found, Class).
        """;

    internal static void Run()
    {
        string retainedWindow = Source.Replace("service:cast(Found, Class)", "service:cast(Found, Class, Window)", StringComparison.Ordinal);
        if (!HasConsumedRootTrim(Source) || HasConsumedRootTrim(retainedWindow))
        {
            throw new InvalidOperationException(ShapeError);
        }

        VerifyBinaryCallListArgument();
    }

    private static void VerifyBinaryCallListArgument()
    {
        var compiler = typeof(BeamModuleCompiler);
        var method = compiler.GetMethod(
            Kills,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
        )!;
        bool supportsBinaryList = method.GetParameters().Length == 6;

        var (instructions, body, previous, call, release) = DecodeBinaryCall(retainArgument: false);
        var outgoing = (int[])method.Invoke(
            null,
            supportsBinaryList ? [instructions, previous, call, release, body[call], true]
                : [instructions, previous, call, release, body[call]]
        )!;
        if (!outgoing.SequenceEqual([ArgumentSlot]))
        {
            throw new InvalidOperationException(PairShapeError);
        }

        int[] slots = Enumerable.Range(0, StackWidth).ToArray();
        int[] lastUse = [.. Enumerable.Repeat(previous + 1, RetainedSlot), call + 2, call - 1];
        bool[] initialized = Enumerable.Repeat(true, StackWidth).ToArray();
        if (Call(
            compiler,
            Trim,
            slots,
            lastUse,
            initialized,
            previous,
            call - 1,
            true,
            Array.Empty<int>()
        ) is not ValueTuple<int, int[], (int, int)[]> oldPlan
            || oldPlan.Item1 != StackWidth - 2
            || Call(
                compiler,
                Trim,
                slots,
                lastUse,
                initialized,
                previous,
                call,
                true,
                outgoing
            ) is not ValueTuple<int, int[], (int, int)[]> newPlan
            || newPlan.Item1 != StackWidth - 1
            || !newPlan.Item3.SequenceEqual([(RetainedSlot, ArgumentSlot)]))
        {
            throw new InvalidOperationException(PairShapeError);
        }

        var (liveInstructions, liveBody, livePrevious, liveCall, liveRelease) = DecodeBinaryCall(retainArgument: true);
        var needed = (int[])method.Invoke(
            null,
            [liveInstructions, livePrevious, liveCall, liveRelease, liveBody[liveCall], true]
        )!;
        if (needed.Length != 0)
        {
            throw new InvalidOperationException(PairPreservationError);
        }
    }

    private static (object Instructions, object[] Body, int Previous, int Call, int Release) DecodeBinaryCall(bool retainArgument)
    {
        byte[] entry = Start(
            1,
            2,
            1,
            1
        );
        byte[] later = retainArgument ? [Move, Y(ArgumentSlot), X(2)] : [];
        byte[] bytes =
        [
            .. entry, Allocate, U(StackWidth), U(1),
            InitYRegisters, ExtendedListOperand, U(StackWidth), .. Enumerable.Range(0, StackWidth).Select(Y),
            ExternalCall, U(0), U(0),
            TestHeap, U(8), U(1), PutTuple, X(1), ExtendedListOperand, U(5), Y(0), Y(1), Y(2), Y(3), Y(4),
            PutList, X(1), A(NilAtomIndex), X(1), Move, Y(ArgumentSlot), X(0),
            ExternalCall, U(2), U(1), .. later,
            TestHeap, U(3), U(1), PutTuple, X(0), ExtendedListOperand, U(2), Y(RetainedSlot), X(0),
            Deallocate, U(StackWidth), Return, EndCode
        ];
        ReadOnlyMemory<byte> code = bytes;
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            3,
            1,
            code
        );
        object instructions = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            2,
            0,
            null,
            null
        );
        object[] body = ((IEnumerable)instructions).Cast<object>().ToArray();
        int firstCall = Array.FindIndex(body, item => Opcode(item) == ExternalCall);
        int lastCall = Array.FindLastIndex(body, item => Opcode(item) == ExternalCall);
        int release = Array.FindLastIndex(body, item => Opcode(item) == Deallocate);

        return (instructions, body, firstCall, lastCall, release);
    }

    private static bool HasConsumedRootTrim(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_late_outgoing_root_trim.erl"));
        object[] body = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .SkipWhile(instruction => Opcode(instruction) != FunctionInfo).Skip(1)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int trim = Array.FindIndex(body, instruction => Opcode(instruction) == TrimFrame);
        if (trim < 1 || Opcode(body[trim - 1]) != Move)
        {
            return false;
        }

        object[] relocation = Operands(body[trim - 1]);
        object[] sizes = Operands(body[trim]);

        return (string)Property(relocation[0], "Kind") == "y" && Value(relocation[0]) == 0
            && (string)Property(relocation[1], "Kind") == "y" && Value(relocation[1]) == 1
            && Value(sizes[0]) == 1 && Value(sizes[1]) == 1
            && body.Skip(trim + 1).First(instruction => Opcode(instruction) != Line) is { } call
            && Opcode(call) == ExternalCall
            && body.Any(instruction => Opcode(instruction) == TailExternalCallWithFrame && Value(Operands(instruction)[2]) == 1);
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
