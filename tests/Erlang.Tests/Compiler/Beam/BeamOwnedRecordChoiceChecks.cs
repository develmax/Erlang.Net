using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOwnedRecordChoiceChecks
{
    internal const string TestName = "compiler/beam/keeps-private-record-choice-unframed-until-the-checked-branch";
    private const string Fixture = "beam_owned_record_choice.erl";
    private const string WrongChoice = "The private record branch did not dispatch before allocating the checked frame.";
    private const string WrongFallback = "The public record branch lost its record failure check.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_owned_record_choice).
        -export([run/2, open/2, callback/3, read/1]).
        -record(state, {file, mod, fun_name, initial, current}).
        -record(payload, {amount}).

        run(Event, Initial) ->
            choose(Event, #state{mod=?MODULE, fun_name=callback, initial=Initial, current=Initial}).

        choose({checked, Name}, State) ->
            {accepted, Data} = ?MODULE:read(Name),
            choose({count, Data#payload.amount}, State#state{file=Name});
        choose({known, Name}, State) ->
            choose({count, cached}, State#state{file=Name});
        choose(Event, #state{file=Name, mod=M, fun_name=F, current=Old, initial=Initial}=State) ->
            Current = M:F(Old, Name, Event),
            case Event of
                {done, 0} -> State#state{file=invalid, current=Initial};
                _ -> State#state{current=Current}
            end.

        open({checked, Name}, State) ->
            {accepted, Data} = ?MODULE:read(Name),
            open({count, Data#payload.amount}, State#state{file=Name});
        open({known, Name}, State) ->
            open({count, cached}, State#state{file=Name});
        open(Event, #state{file=Name, mod=M, fun_name=F, current=Old, initial=Initial}=State) ->
            Current = M:F(Old, Name, Event),
            case Event of
                {done, 0} -> State#state{file=invalid, current=Initial};
                _ -> State#state{current=Current}
            end.

        read(ok) -> {accepted, #payload{amount=5}};
        read(wrong_record) -> {accepted, unexpected};
        read(failed) -> {rejected, denied}.

        callback(Value, Name, Event) -> {Value, Name, Event}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] closed = Operations("choose");
        int select = Array.FindIndex(closed, instruction => Opcode(instruction) == SelectValue);
        int firstFrame = Array.FindIndex(closed, instruction => Opcode(instruction) == Allocate);
        int firstUpdate = Array.FindIndex(closed, instruction => Opcode(instruction) == UpdateRecord);
        if (select < 0 || firstUpdate <= select || firstFrame <= firstUpdate
            || closed.Count(instruction => Opcode(instruction) == UpdateRecord) != 4
            || closed.Count(instruction => Opcode(instruction) == IsTaggedTuple) != 2
            || closed.Count(instruction => Opcode(instruction) == DynamicApply) != 1
            || !closed.Any(instruction => Opcode(instruction) == BadRecord))
        {
            throw new InvalidOperationException(WrongChoice);
        }

        object[] opened = Operations("open");
        if (opened.Count(instruction => Opcode(instruction) == BadRecord) <= closed.Count(instruction => Opcode(instruction) == BadRecord))
        {
            throw new InvalidOperationException(WrongFallback);
        }

        return Task.CompletedTask;

        object[] Operations(string name)
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name);

            return entry >= 0
                ? instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray()
                : throw new InvalidOperationException(Missing);
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
