using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPairedRecordGeometryChecks
{
    internal const string TestName = "compiler/beam/schedules-paired-record-geometry-and-delayed-arguments";
    private const string Fixture = "beam_paired_record_geometry.erl";
    private const string WrongSelection = "The tagged geometry branches lost their tuple selection or the first checked projection.";
    private const string WrongFrames = "The geometry frame did not preserve the arithmetic and terminal lifetimes.";
    private const string WrongArguments = "A delayed return copied the literal before its independent stack argument.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_paired_record_geometry).
        -export([handle/2,query/1,location/1,minimum/2,place/2,refresh/1,notify/3,setup/1,make/2,reserve/1]).

        -record(envelope, {metadata,payload,unused,other}).
        -record(drag, {key,rect,pad,extra}).
        -record(resize, {info,size,note}).
        -record(room, {config,bottom,pad,parent,top}).

        handle(#envelope{payload=#drag{rect={_Before,Y,_Ignored,H}}},
               Room=#room{top=Top,bottom=Bottom}) ->
            {Width,Height} = ?MODULE:query(Room#room.parent),
            Difference = Height-H,
            {OldX,_} = ?MODULE:location(Bottom),
            ?MODULE:minimum(Bottom,{Width,Difference}),
            ?MODULE:minimum(Top,{Width,H}),
            ?MODULE:place(Bottom,{OldX,Y,Width,Difference}),
            ?MODULE:place(Top,{Width,H}),
            ?MODULE:refresh(Room#room.parent),
            {adjusted,Room};
        handle(#envelope{payload=#resize{size={Width,Height}}},
               Room=#room{top=Top,bottom=Bottom}) ->
            ?MODULE:minimum(Top,{Width,Height div 3}),
            ?MODULE:minimum(Bottom,{Width,Height div 3}),
            ?MODULE:place(Top,{Width,Height div 3}),
            ?MODULE:place(Bottom,{11,Height div 3,Width,Height div 3}),
            ?MODULE:refresh(Room#room.parent),
            {adjusted,Room};
        handle(Message=#envelope{},Room=#room{}) ->
            ?MODULE:notify(Room#room.config,"fixture",[Message]),
            {adjusted,Room}.

        query(Panel) -> observe({query,Panel}), erlang:get(dimensions).
        location(Window) -> observe({location,Window}), erlang:get(origin).
        minimum(Window, Size) -> observe({minimum,Window,Size}).
        place(Window, Rect) -> observe({place,Window,Rect}).
        refresh(Window) -> observe({refresh,Window}).
        notify(Config, Form, Message) -> observe({notify,Config,Form,Message}).

        observe(Event) ->
            Previous = case erlang:get(order) of undefined -> []; Existing -> Existing end,
            erlang:put(order,[Event|Previous]),ok.

        setup(Seed) ->
            Parent = ?MODULE:make(Seed, []),
            First = ?MODULE:reserve(first),
            Second = ?MODULE:reserve(second),
            Child = ?MODULE:make(Parent, [{role,child}]),
            {Parent,First,Second,Child}.

        make(Seed, Options) -> {created,Seed,Options}.
        reserve(Tag) -> {reservation,Tag}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] events = Operations("handle", 2);
        int select = Array.FindIndex(events, instruction => Opcode(instruction) == SelectTupleArity);
        int projection = Array.FindIndex(
            events,
            instruction => Opcode(instruction) == GetTupleElement
            && Operands(instruction) is [var input, var field, var output]
            && Value(input) == 2 && Value(field) == 1 && Value(output) == 3
        );
        if (select < 0 || projection <= select || Opcode(events[projection + 1]) != IsTaggedTuple)
        {
            throw new InvalidOperationException(WrongSelection);
        }

        if (events.Count(instruction => Opcode(instruction) == TrimFrame) != 4
            || events.Count(instruction => Opcode(instruction) == BadMatch) != 2
            || !events.Any(instruction => Opcode(instruction) == AllocateHeap))
        {
            throw new InvalidOperationException(WrongFrames);
        }

        object[] setup = Operations("setup", 1);
        int lastCall = Array.FindLastIndex(setup, instruction => Opcode(instruction) == ExternalCall);
        object[] arguments = setup.Take(lastCall).Reverse()
            .SkipWhile(instruction => Opcode(instruction) == Line)
            .TakeWhile(instruction => Opcode(instruction) == Move).Reverse().ToArray();
        if (arguments.Length < 2 || !Property(Operands(arguments[^2])[0], "Kind").Equals("y")
            || Value(Operands(arguments[^2])[1]) != 0 || Value(Operands(arguments[^1])[1]) != 1)
        {
            throw new InvalidOperationException(WrongArguments);
        }

        return Task.CompletedTask;

        object[] Operations(string name, int arity)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name && Value(Operands(instruction)[2]) == arity
            );

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
