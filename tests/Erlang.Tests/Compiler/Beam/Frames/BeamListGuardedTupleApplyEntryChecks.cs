using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamListGuardedTupleApplyEntryChecks
{
    internal const string TestName = "compiler/beam/owns-apply-frame-after-list-and-tuple-entry";
    private const string WrongSchedule = "Expected an unframed list and pair tail and a separately owned fixed apply.";
    private const string WrongResult = "The list and tuple entry changed the selected result or failure arguments.";
    private const string Source = """
        -module(beam_list_tuple_apply_probe).
        -export([route/3, other_guard/3]).

        route(First, Second, Value) when is_list(Value) ->
            output(First, Second, Value, []);
        route(First, Second, {Value, Rest}) when erlang:is_list(Value) ->
            output(First, Second, Value, Rest);
        route(First, Second, {decode, Module, Argument}) ->
            output(First, Second, Module:length(Argument), []).

        other_guard(First, Second, Value) when is_list(First) ->
            output(First, Second, Value, []);
        other_guard(First, Second, {Value, Rest}) when is_list(Value) ->
            output(First, Second, Value, Rest);
        other_guard(First, Second, {decode, Module, Argument}) ->
            output(First, Second, Module:length(Argument), []).

        output(First, Second, Value, Rest) ->
            {First, Second, Value, Rest}.
        """;

    internal static async Task Run()
    {
        const string path = "beam_list_tuple_apply_probe.erl";
        byte[] beam = CodeGeneration.CompileBeamModule(Source, path);
        CheckSchedule(beam);
        var image = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term values = Term.List(first, second);
            Term data = Term.A("data");
            Term decode = Term.A("decode");
            Term erlang = Term.A("erlang");
            var cases = new (string Function, Term Input, Term Output)[]
            {
                ("route", Nil.Value, Term.Tuple(
                    first,
                    second,
                    Nil.Value,
                    Nil.Value
                )),
                ("route", values, Term.Tuple(
                    first,
                    second,
                    values,
                    Nil.Value
                )),
                ("route", Term.Tuple(values, data), Term.Tuple(
                    first,
                    second,
                    values,
                    data
                )),
                ("route", Term.Tuple(decode, erlang, values), Term.Tuple(
                    first,
                    second,
                    Term.I(2),
                    Nil.Value
                )),
                ("other_guard", Term.Tuple(values, data), Term.Tuple(
                    first,
                    second,
                    values,
                    data
                ))
            };
            foreach (var (function, input, expected) in cases)
            {
                if (!expected.Equals(await image.Invoke(
                    context,
                    function,
                    first,
                    second,
                    input
                )))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }

            await CheckFailure(Term.Tuple(data, values));
            await CheckFailure(Term.Tuple(data, erlang, values));
            await CheckFailure(Term.Tuple(values));
            try
            {
                _ = await image.Invoke(
                    context,
                    "route",
                    first,
                    second,
                    Term.Tuple(decode, erlang, data)
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);

            async Task CheckFailure(Term input)
            {
                try
                {
                    _ = await image.Invoke(
                        context,
                        "route",
                        first,
                        second,
                        input
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause))
                    && error.StackTraceTerm is Cons { Head: TupleTerm { Items: [_, Atom { Name: "route" }, var arguments, _] } }
                    && Cons.Items(arguments).SequenceEqual(new[] { first, second, input }))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });
    }

    private static void CheckSchedule(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] route = Body("route");
        object[] negative = Body("other_guard");
        byte[] expected =
        [
            IsList, Move, TailLocalCall, IsTuple, SelectTupleArity,
            GetTupleElement, ExactEqualTest, Allocate, Move, Move, GetTupleElement, GetTupleElement,
            Move, Swap, DynamicApply, Move, Move, Move, Move, TailLocalCallWithFrame,
            GetTupleElement, IsList, GetTupleElement, Swap, TailLocalCall
        ];
        if (!route.Select(Opcode).SequenceEqual(expected)
            || Operand(route[0], 1) != 2 || Operand(route[4], 0) != 2
            || Operand(route[7], 0) != 2 || Operand(route[7], 1) != 3
            || Operand(route[14], 0) != 1 || Operand(route[19], 2) != 2
            || negative.Any(instruction => Opcode(instruction) == SelectTupleArity))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        object[] Body(string function)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Operand(instruction, 1)] == function);
            if (start < 0)
            {
                throw new InvalidOperationException(WrongSchedule);
            }

            return instructions.Skip(start + 1)
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Where(instruction => Opcode(instruction) is not (Label or Line))
                .ToArray();
        }
    }

    private static int Operand(object instruction, int index) => (int)(BigInteger)Property(
        Items(instruction, "Operands")[index],
        "Value"
    );

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
