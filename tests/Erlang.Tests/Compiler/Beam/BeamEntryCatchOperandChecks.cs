using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEntryCatchOperandChecks
{
    internal const string TestName = "compiler/beam/uses-incoming-registers-for-effect-catch-arguments";
    private const string Fixture = "beam_entry_catch_operands.erl";
    private const string WrongFrame = "The entry catch did not keep call-only operands in X or preserve the live continuation value.";

    private const string Source = """
        -module(beam_entry_catch_operands).
        -export([translate/1, retains/1, bi/2]).

        translate(Input) ->
            case catch work(active, Input) of
                {'EXIT',Reason} -> {error,{translate,Reason}};
                Result -> Result
            end.

        retains(Input) ->
            case catch work(active, Input) of
                {'EXIT',_} -> {failure,Input};
                Result -> {Result,Input}
            end.

        bi(Group, Input) ->
            case catch work(Group, Input) of
                {'EXIT',Reason} -> {error,{two,Reason}};
                Result -> Result
            end.

        work(_, error_input) -> erlang:error(work_error);
        work(_, exit_input) -> erlang:exit(work_exit);
        work(_, throw_input) -> throw(work_throw);
        work(Mode, Input) ->
            erlang:put(work_input, {Mode,Input}),
            {reply,Input}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();

        if (!UsesCatchFrame(
            "translate",
            1,
            1,
            token: 0
        )
            || !UsesCatchFrame(
                "bi",
                1,
                2,
                token: 0
            )
            || !UsesCatchFrame(
                "retains",
                2,
                1,
                token: 1
            ))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        return Task.CompletedTask;

        bool UsesCatchFrame(
            string name,
            int slots,
            int live,
            int token
        )
        {
            int entry = Array.FindIndex(instructions, index =>
                Opcode(index) == FunctionInfo && atoms[Value(Operands(index)[1])] == name);
            if (entry < 0)
            {
                return false;
            }

            var operations = instructions.Skip(entry + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
            object? allocate = operations.FirstOrDefault(item => Opcode(item) == Allocate);
            object? begin = operations.FirstOrDefault(item => Opcode(item) == CatchEnter);
            object? end = operations.FirstOrDefault(item => Opcode(item) == CatchEnd);

            return allocate is not null && Value(Operands(allocate)[0]) == slots && Value(Operands(allocate)[1]) == live
                && begin is not null && Value(Operands(begin)[0]) == token
                && end is not null && Value(Operands(end)[0]) == token;
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
