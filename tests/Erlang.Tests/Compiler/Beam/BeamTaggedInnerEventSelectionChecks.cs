using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTaggedInnerEventSelectionChecks
{
    internal const string TestName = "compiler/beam/selects-nested-tagged-events-by-width-before-saving-state";
    private const string Fixture = "beam_popup_record_dispatch.erl";
    private const string WrongChoice = "The event selector did not refine the inner tuple before saving state.";
    private const string WrongBinding = "The lowered selection lost a required repeated field binding.";
    private const string WrongConstruction = "A retained call result did not remain live during independent argument construction.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_popup_record_dispatch).
        -export([event/2, configure/0]).
        -record(event,{id,object,user,data}).
        -record(open,{kind,x,y}).
        -record(selected,{kind,value}).
        -record(state,{owner,config,menu}).

        event(#event{object=Object,data=#open{kind=click}}, State=#state{menu=Menu}) ->
            wxWindow:popupMenu(Object,Menu),
            {continued,State};
        event(#event{object=Object,id=Id,data=#selected{kind=pick}}, State=#state{}) ->
            Label = wxMenu:getLabel(Object,Id),
            demo:format(State#state.config,"item ~p",[Label]),
            {continued,State};
        event(Event,State) ->
            demo:format(State#state.config,"unknown ~p",[Event]),
            {continued,State}.

        configure() ->
            Parent = wxMenu:new([]),
            Child = wxMenu:new([]),
            wxMenu:append(Parent,42,"child",Child,[]),
            wxMenuItem:enable(wxMenu:append(Child,6,"entry",[]),[{enable,false}]),
            Bitmap = wxArtProvider:getBitmap("icon"),
            Item = wxMenuItem:new([{parentMenu,Parent},{subMenu,Child}]),
            wxMenuItem:setBitmap(Item,Bitmap),
            wxMenu:append(Parent,Item),
            wxMenu:connect(Parent,pick),
            wxMenu:connect(Child,pick),
            Parent.
        """;

    internal static Task Run()
    {
        var bodies = ReadFunctions(Source);
        object[] selector = bodies("event", 2).Body;
        int projection = Array.FindIndex(selector, instruction => Opcode(instruction) == GetTupleElement);
        int dispatch = Array.FindIndex(selector, instruction => Opcode(instruction) == SelectTupleArity);
        int frame = Array.FindIndex(selector, instruction => Opcode(instruction) == Allocate);
        if (projection < 0 || dispatch <= projection || frame <= dispatch
            || selector.Count(instruction => Opcode(instruction) == BadRecord) != 1)
        {
            throw new InvalidOperationException(WrongChoice);
        }

        string repeated = Source.Replace(
            "event(#event{object=Object,id=Id,data=#selected{kind=pick}}, State=#state{})",
            "event(#event{object=Object,id=Object,data=#selected{kind=pick}}, State=#state{})",
            StringComparison.Ordinal
        ).Replace("wxMenu:getLabel(Object,Id)", "wxMenu:getLabel(Object,Object)", StringComparison.Ordinal);
        object[] exact = ReadFunctions(repeated)("event", 2).Body;
        if (exact.Any(instruction => Opcode(instruction) == SelectTupleArity)
            || !exact.Where(instruction => Opcode(instruction) == ExactEqualTest)
                .Select(Operands)
                .Any(operands => operands.Skip(1).All(value => (string)Property(value, "Kind") is "x" or "y")))
        {
            throw new InvalidOperationException(WrongBinding);
        }

        object[] construction = bodies("configure", 0).Body;
        if (!construction.Any(instruction => Opcode(instruction) == TestHeap
            && Value(Operands(instruction)[0]) == 10 && Value(Operands(instruction)[1]) == 1))
        {
            throw new InvalidOperationException(WrongConstruction);
        }

        return Task.CompletedTask;
    }

    private static Func<string, int, (object[] Body, int Failure)> ReadFunctions(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));
        var instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();

        return (name, arity) =>
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name && Value(Operands(instruction)[2]) == arity
            );
            if (entry < 2)
            {
                throw new InvalidOperationException(Missing);
            }

            return (instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray(),
                Value(Operands(instructions[entry - 2])[0]));
        };
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
