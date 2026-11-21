using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamStackTupleHeapChecks
{
    internal const string TestName = "compiler/beam/reserves-heap-before-projecting-a-stack-rooted-tuple";
    private const string IncorrectRoots = "The heap check must retain X sources and may reserve before reading a tuple rooted in Y.";
    private const string Source = """
        -module(y_tuple_heap_probe).
        -export([nonterminal/1, x_root/1]).
        -record(payload,{marker,data}).
        nonterminal(Row=#payload{}) ->
            remember(Row#payload.marker),
            Result = {wrapped,Row#payload.data},
            remember(Result),
            Result.
        x_root(Row=#payload{}) -> {wrapped,Row#payload.data}.
        remember(Value) -> erlang:put(last_tuple_heap,Value).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "y_tuple_heap_probe.erl"));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        Verify("nonterminal", "y", 0);
        Verify("x_root", "x", 1);
        VerifyCheckedXProjectionSlots();

        return Task.CompletedTask;

        void Verify(string name, string source, int count)
        {
            int info = Array.FindIndex(
                instructions,
                i => Opcode(i) == FunctionInfo && atoms[(int)Number(i, 1)] == name
            );
            object[] body = instructions.Skip(info + 1).TakeWhile(i => Opcode(i) != FunctionInfo).ToArray();
            int check = Array.FindIndex(body, i => Opcode(i) == TestHeap);
            if (info < 0 || check < 0 || check + 1 >= body.Length
                || Number(body[check], 1) != count || Opcode(body[check + 1]) != GetTupleElement
                || (string)Property(Items(body[check + 1], "Operands").First(), "Kind") != source)
            {
                throw new InvalidOperationException(IncorrectRoots);
            }
        }
    }

    private static void VerifyCheckedXProjectionSlots()
    {
        byte[] entry =
        [
            .. Start(
                1,
                2,
                2,
                3
            ),
            Allocate, U(1), U(3)
        ];
        byte[] initialized = [InitYRegisters, ExtendedListOperand, U(1), Y(0)];
        byte[] tag = [IsTaggedTuple, F(3), X(0), U(2), A(2)];
        byte[] projection = [GetTupleElement, X(0), U(1), Y(0)];
        byte[] heap = [TestHeap, U(2), U(0)];
        byte[] consume = [PutList, ExtendedTypedRegisterOperand, Y(0), U(1), A(0), X(0)];
        byte[] finish = [Deallocate, U(1), Return, Label, U(3), BadRecord, X(0), EndCode];
        byte[] source = [.. entry, .. initialized, .. tag, .. projection, .. heap, .. consume, .. finish];
        byte[] expected =
        [
            .. Start(
                1,
                2,
                2,
                3
            ),
            Allocate, U(0), U(3), .. tag, TestHeap, U(2), U(1),
            GetTupleElement, X(0), U(1), X(0),
            PutList, ExtendedTypedRegisterOperand, X(0), U(1), A(0), X(0),
            Deallocate, U(0), Return, Label, U(3), BadRecord, X(0), EndCode
        ];
        byte[] replacement = [Move, A(3), X(0)];
        byte[] scratch = [.. entry, .. initialized, .. tag, .. projection, .. heap, .. replacement, .. consume, .. finish];
        byte[] scratchExpected = expected.ToArray();
        int project = Array.IndexOf(scratchExpected, GetTupleElement);
        scratchExpected[project + 3] = X(1);
        int consumer = Array.IndexOf(scratchExpected, PutList);
        scratchExpected[consumer + 2] = X(1);
        scratchExpected = [.. scratchExpected[..consumer], .. replacement, .. scratchExpected[consumer..]];
        if (Rewrite(source) is not { } rewritten || !rewritten.AsSpan().SequenceEqual(expected)
            || Rewrite(scratch) is not { } alternative || !alternative.AsSpan().SequenceEqual(scratchExpected)
            || Rewrite(expected) is not null
            || Rewrite([.. entry, .. initialized, IsTaggedTuple, F(3), X(2), U(2), A(2),
                    GetTupleElement, X(2), U(1), Y(0), .. heap, .. consume, .. finish]) is not null
            || Rewrite([.. entry, .. initialized, IsTaggedTuple, F(3), X(1), U(2), A(2),
                    .. projection, .. heap, .. consume, .. finish]) is not null
            || Rewrite([.. entry, .. initialized, .. tag, .. projection, .. heap, .. consume,
                    Deallocate, U(1), Return, Label, U(3), BadRecord, Y(0), EndCode]) is not null
            || Rewrite([.. entry, .. initialized, .. tag, .. projection, .. heap,
                    PutList, Y(0), Y(0), X(0), .. finish]) is not null)
        {
            throw new InvalidOperationException(IncorrectRoots);
        }

        static byte[]? Rewrite(byte[] source)
        {
            var (body, slots) = Read(source);
            if (Call(
                typeof(BeamModuleCompiler),
                "RewriteCheckedHeapProjectionSlots",
                new ReadOnlyMemory<byte>(source),
                body,
                slots
            ) is not ValueTuple<byte[], HashSet<int>> changed)
            {
                return null;
            }

            var (updated, rewrittenSlots) = Read(changed.Item1);

            return (byte[]?)Call(
                typeof(BeamModuleCompiler),
                "RewriteDisjointFrames",
                new ReadOnlyMemory<byte>(changed.Item1),
                updated,
                rewrittenSlots,
                false,
                false,
                changed.Item2
            );
        }

        static (object Body, List<(int Start, int End, int Slot)> Slots) Read(byte[] source)
        {
            object header = New(
                "BeamCodeHeader",
                0,
                (int)DebugLine,
                4,
                1,
                new ReadOnlyMemory<byte>(source)
            );
            var slots = new List<(int Start, int End, int Slot)>();
            Action<int, int, int> record = (start, end, slot) => slots.Add((start, end, slot));
            object body = Call(
                Type("BeamInstructionReader"),
                "Read",
                header,
                5,
                0,
                null,
                record
            );

            return (body, slots);
        }
    }

    private static BigInteger Number(object instruction, int index) =>
        (BigInteger)Property(Items(instruction, "Operands").ElementAt(index), "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object item, string name) => ((IEnumerable)Property(item, name)).Cast<object>();
}
