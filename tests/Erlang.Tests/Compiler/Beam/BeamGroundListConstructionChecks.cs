using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGroundListConstructionChecks
{
    internal const string TestName = "compiler/beam/schedules-ground-list-call-choices-and-retains-the-owner";
    private const string Fixture = "beam_ground_list_construction.erl";
    private const string IncorrectChoice = "The integer-list decision did not preserve its default and retained owner.";
    private const string IncorrectRetainedRecipe = "The retained recipe did not release unused owners before its final call.";
    private const string IncorrectEvent = "The event did not validate both record shapes before allocating the callback frame.";
    private const string IncorrectBinding = "The callback skipped a repeated event field match.";

    private const string Source = """
        -module(beam_ground_list_construction).
        -export([choice/2,dispatch/2,recipe/1]).
        -record(envelope,{target,event}).
        -record(command,{type,payload,extra}).
        -record(state,{owner,config}).

        choice(Parent,Key) ->
            Owner = sizer_effects:create(Parent,Key),
            sizer_effects:configure(Owner),
            Container = sizer_effects:create_child(Owner,[]),
            Child = case Key of
                [65,108] -> first(Container);
                "Ba" -> second(Container);
                "Bb" -> third(Container);
                "X" -> fourth(Container)
            end,
            sizer_effects:attach(Container,Child),
            sizer_effects:fit(Child,Container),
            sizer_effects:start(Owner),
            sizer_effects:label(Owner,"done"),
            sizer_effects:finish(Owner).

        dispatch(#envelope{event=#command{type=activate,payload=Key}}, State=#state{}) ->
            choice(State#state.owner,Key),
            {noreply,State};
        dispatch(Event=#envelope{}, State=#state{}) ->
            demo:format(State#state.config, "event: ~p~n", [Event]),
            {noreply,State}.

        recipe(Parent) ->
            Owner = sizer_effects:new(20),
            Item = first(Parent),
            sizer_effects:resize(Item,80,81),
            sizer_effects:attach(Owner,Item,[{flags,6}]),
            Owner.

        first(Container) -> sizer_effects:make(Container,first).
        second(Container) -> sizer_effects:make(Container,second).
        third(Container) -> sizer_effects:make(Container,third).
        fourth(Container) -> sizer_effects:make(Container,fourth).
        """;

    internal static Task Run()
    {
        var functions = ReadFunctions(Source);
        var choice = functions("choice", 2);
        if (choice.Count(instruction => Opcode(instruction) == SelectValue) != 2
            || choice.Count(instruction => Opcode(instruction) == CaseEnd) != 1
            || choice.Count(instruction => Opcode(instruction) == TailExternalCallWithFrame) != 1
            || choice.Count(instruction => Opcode(instruction) == Move
                && IsRegister(Operands(instruction)[0], "y", 0)
                && IsRegister(Operands(instruction)[1], "x", 0)) < 8)
        {
            throw new InvalidOperationException(IncorrectChoice);
        }

        var recipe = functions("recipe", 1);
        if (recipe.Count(instruction => Opcode(instruction) == Swap) != 1
            || recipe.Count(instruction => Opcode(instruction) == TrimFrame) != 1)
        {
            throw new InvalidOperationException(IncorrectRetainedRecipe);
        }

        var dispatch = functions("dispatch", 2);
        int frame = Array.FindIndex(dispatch, instruction => Opcode(instruction) == Allocate);
        if (frame < 0 || Value(Operands(dispatch[frame])[1]) != 3
            || dispatch.Take(frame).Count(instruction => Opcode(instruction) == IsTaggedTuple) != 3)
        {
            throw new InvalidOperationException(IncorrectEvent);
        }

        string repeated = Source.Replace("type=activate,payload=Key", "type=activate,payload=Key,extra=Key", StringComparison.Ordinal);
        if (!ReadFunctions(repeated)("dispatch", 2).Where(instruction => Opcode(instruction) == ExactEqualTest)
            .Select(Operands).Any(operands => operands.Skip(1).All(operand => (string)Property(operand, "Kind") is "x" or "y")))
        {
            throw new InvalidOperationException(IncorrectBinding);
        }

        return Task.CompletedTask;
    }

    private static Func<string, int, object[]> ReadFunctions(string source)
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
            if (entry < 0)
            {
                throw new InvalidOperationException(Fixture);
            }

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        };
    }

    private static bool IsRegister(object operand, string kind, int index) =>
        (string)Property(operand, "Kind") == kind && Value(operand) == index;

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
