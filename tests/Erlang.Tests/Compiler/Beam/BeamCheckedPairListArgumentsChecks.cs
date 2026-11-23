using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedPairListArgumentsChecks
{
    internal const string TestName = "compiler/beam/schedules-live-checked-pair-list-arguments";
    private const string Module = "beam_nested_pair_list_probe";
    private const string WrongLayout = "Checked pair projections may move past the heap test only for fields owned by the outgoing list.";
    private const string Source = """
        -module(beam_nested_pair_list_probe).
        -export([run/1, retained/1, report/2]).
        run(Key) ->
            Status = erlang:get(Key),
            ?MODULE:report(before, []),
            case Status of
                ok -> ok;
                {error,{Kind,Detail}} ->
                    ?MODULE:report(status, [Kind, Detail]),
                    Status
            end.
        retained(Key) ->
            Status = erlang:get(Key),
            ?MODULE:report(before, []),
            case Status of
                ok -> ok;
                {error,{Kind,Detail}} ->
                    ?MODULE:report(status, [Kind, Detail]),
                    {Status, Kind}
            end.
        report(Key, Values) -> erlang:put(Key, Values).
        """;

    internal static Task Run()
    {
        VerifyTrimmedCall();
        object image = Code(CodeGeneration.CompileBeamModule(Source, Module + ".erl"));
        object[] instructions = Items(image, "Instructions").ToArray();
        string[] atoms = Items(Property(image, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] reused = Body("run");
        int heap = Array.FindIndex(reused, instruction => Opcode(instruction) == TestHeap);
        if (heap < 3 || heap + 2 >= reused.Length || Opcode(reused[heap - 1]) != TestArity
            || Operands(reused[heap]) is not [var words, var live] || Value(words) != 4 || Value(live) != 1
            || !Projects(reused[heap + 1], 0, 1) || !Projects(reused[heap + 2], 1, 0))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] retained = Body("retained");
        int retainedHeap = Array.FindIndex(retained, instruction => Opcode(instruction) == TestHeap);
        if (retainedHeap < 2 || Opcode(retained[retainedHeap - 1]) != GetTupleElement
            || Operands(retained[retainedHeap - 1]) is not [_, _, var saved] || !Equals(Property(saved, "Kind"), "y"))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == name
            );

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static void VerifyTrimmedCall()
    {
        var compiler = new BeamModuleCompiler(Module);
        _ = Call(compiler, "Intern", "run");
        _ = Call(compiler, "Intern", "error");
        if (Rewrite(TrimmedCall()) is not { } packed || !packed.SequenceEqual(TrimmedCall(inRegisters: true))
            || Rewrite(packed) is not null || Rewrite(TrimmedCall(retainField: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        byte[]? Rewrite(ReadOnlyMemory<byte> instructions) => (byte[]?)Call(
            compiler,
            "ScheduleCheckedPairListArguments",
            instructions,
            4
        );
    }

    private static byte[] TrimmedCall(bool inRegisters = false, bool retainField = false)
    {
        byte pair = X(inRegisters ? 0 : 1);
        byte[] arguments = inRegisters
            ? [
                TestHeap, U(4), U(1), GetTupleElement, pair, U(0), X(1), GetTupleElement, pair, U(1), X(0),
                PutList, X(0), A(0), X(0), PutList, X(1), X(0), X(1)
            ]
            : [
                GetTupleElement, pair, U(0), Y(1), GetTupleElement, pair, U(1), Y(2), TestHeap, U(4), U(0),
                PutList, Y(2), A(0), X(0), PutList, Y(1), X(0), X(1)
            ];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            Allocate, U(3), U(1), Move, X(0), Y(0), InitYRegisters, ExtendedListOperand, U(2), Y(1), Y(2),
            IsTaggedTuple, F(3), Y(0), U(2), A(3), GetTupleElement, Y(0), U(1), pair,
            IsTuple, F(3), pair, TestArity, F(3), pair, U(2), .. arguments,
            Move, A(3), X(0), Move, Y(retainField ? 1 : 0), Y(2), TrimFrame, U(2), U(1),
            ExternalCall, U(2), U(0), Move, Y(0), X(0), Deallocate, U(1), Return,
            Label, U(3), Move, Y(0), X(0), CaseEnd, X(0), EndCode
        ];
    }

    private static bool Projects(object instruction, int field, int destination) =>
        Opcode(instruction) == GetTupleElement && Operands(instruction) is [var source, var index, var target]
        && IsX(source, 0) && Value(index) == field && IsX(target, destination);

    private static bool IsX(object operand, int register) => Equals(Property(operand, "Kind"), "x") && Value(operand) == register;

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
