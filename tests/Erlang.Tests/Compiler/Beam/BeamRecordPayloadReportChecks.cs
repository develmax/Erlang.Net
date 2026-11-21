using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRecordPayloadReportChecks
{
    internal const string TestName = "compiler/beam/groups-record-report-dispatch-and-empty-option-arguments";
    private const string Fixture = "beam_record_payload_reports.erl";
    private const string WrongDispatch = "Nested event dispatch did not use the arity switch or the function clause failure.";
    private const string WrongProjection = "The common event payload was not projected before the tag selection.";
    private const string WrongOptions = "The empty options list did not follow the independent delayed receiver copy.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_record_payload_reports).
        -export([choose/2,pick/2,read_widget/1,report/3,setup/1,new/3,reserve/1]).

        -record(envelope,{object,nested,payload,flag}).
        -record(room,{session,config,extra}).
        -record(option,{hint,kind,data,a,b}).
        -record(level,{data,kind}).
        -record(open,{kind,data}).
        -record(save,{kind,data}).
        -record(cancel,{kind,data}).

        choose(#envelope{object=Widget,nested=#option{kind=z_read}}, Room=#room{}) ->
            Data = ?MODULE:read_widget(Widget),
            ?MODULE:report(Room#room.config,"widget",[Data]),
            {handled,Room};
        choose(#envelope{nested=#option{kind=a_inline,data=Data}}, Room=#room{}) ->
            ?MODULE:report(Room#room.config,"inline",[Data]),
            {handled,Room};
        choose(#envelope{nested=#level{kind=number,data=Number}}, Room=#room{}) ->
            ?MODULE:report(Room#room.config,"number",[Number]),
            {handled,Room};
        choose(Event=#envelope{}, Room=#room{}) ->
            ?MODULE:report(Room#room.config,"fallback",[Event]),
            {handled,Room}.

        pick(#envelope{nested=#save{kind=z_text,data=Text}}, Room=#room{}) ->
            ?MODULE:report(Room#room.config,"text",[Text]),
            {handled,Room};
        pick(#envelope{nested=#open{data=File}}, Room=#room{}) ->
            ?MODULE:report(Room#room.config,"open",[File]),
            {handled,Room};
        pick(#envelope{nested=#save{kind=a_file,data=Name}}, Room=#room{}) ->
            ?MODULE:report(Room#room.config,"file",[Name]),
            {handled,Room};
        pick(#envelope{nested=#cancel{data=Reason}}, Room=#room{}) ->
            ?MODULE:report(Room#room.config,"cancel",[Reason]),
            {handled,Room};
        pick(Event=#envelope{}, Room=#room{}) ->
            ?MODULE:report(Room#room.config,"fallback",[Event]),
            {handled,Room}.

        setup(Seed) ->
            Owner = ?MODULE:new(Seed,0,[root]),
            First = ?MODULE:reserve(first),
            Second = ?MODULE:reserve(second),
            Child = ?MODULE:new(Owner,9,[]),
            {Owner,First,Second,Child}.

        read_widget(Widget) ->
            observe({widget,Widget}),
            case Widget of bad -> erlang:error(unavailable); _ -> {read,Widget} end.
        report(Config,Format,Args) -> observe({report,Config,Format,Args}).
        new(Owner,Id,Options) -> observe({new,Owner,Id,Options}), {made,Owner,Id,Options}.
        reserve(Tag) -> observe({reserve,Tag}), {reserved,Tag}.
        observe(Item) ->
            Previous=case erlang:get(observations) of undefined -> []; Prior -> Prior end,
            erlang:put(observations,[Item|Previous]),ok.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int entry = Entry("choose", 2);
        int functionClause = Value(Operands(instructions[entry - 2])[0]);
        object[] choice = Body(entry);
        if (choice.Count(instruction => Opcode(instruction) == SelectTupleArity) != 1
            || choice.Where(instruction => Opcode(instruction) == IsTaggedTuple)
                .Any(instruction => Value(Operands(instruction)[0]) != functionClause))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        object[] picker = Body(Entry("pick", 2));
        int selection = Array.FindIndex(picker, instruction => Opcode(instruction) == SelectValue);
        int payload = Array.FindIndex(
            picker,
            instruction => Opcode(instruction) == GetTupleElement
            && Operands(instruction) is [var input, var field, var output]
            && Value(input) == 2 && Value(field) == 2 && Value(output) == 4
        );
        if (payload < 0 || selection <= payload)
        {
            throw new InvalidOperationException(WrongProjection);
        }

        object[] setup = Body(Entry("setup", 1));
        int lastCall = Array.FindLastIndex(setup, instruction => Opcode(instruction) == ExternalCall);
        object[] moves = setup.Take(lastCall).Reverse()
            .SkipWhile(instruction => Opcode(instruction) == Line)
            .TakeWhile(instruction => Opcode(instruction) == Move).Reverse().ToArray();
        if (moves.Length < 4 || Property(Operands(moves[^4])[0], "Kind") is not "y"
            || Value(Operands(moves[^4])[1]) != 1 || Value(Operands(moves[^3])[1]) != 2
            || Value(Operands(moves[^2])[1]) != 0 || Value(Operands(moves[^1])[1]) != 1)
        {
            throw new InvalidOperationException(WrongOptions);
        }

        return Task.CompletedTask;

        int Entry(string name, int arity)
        {
            int found = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name && Value(Operands(instruction)[2]) == arity
            );

            return found >= 2 ? found : throw new InvalidOperationException(Missing);
        }

        object[] Body(int entry) =>
            instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
