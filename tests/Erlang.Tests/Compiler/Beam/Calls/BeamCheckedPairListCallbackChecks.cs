using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedPairListCallbackChecks
{
    internal const string TestName = "compiler/beam/keeps-checked-pair-callback-roots-per-list-result";
    private const string Fixture = "beam_checked_pair_list_callbacks.erl";
    private const string BadLayout = "A checked pair callback misplaced its projections, roots or guarded nil branch.";
    private const string BadValue = "A checked pair callback changed order, filtering, or the original failure.";

    private const string Source = """
        -module(beam_checked_pair_list_callbacks).
        -export([fold/3, map/2, filter/2, swapped/2, fold_with/1, map_with/1, filter_with/1, swapped_with/1]).

        fold(F, Acc, [{Key,Val}|Rest]) ->
            fold(F, F(Key,Val,Acc), Rest);
        fold(F, Acc, []) when is_function(F, 3) -> Acc.

        map(F, [{Key,Val}|Rest]) ->
            [{Key,F(Key,Val)}|map(F,Rest)];
        map(F, []) when is_function(F, 2) -> [].

        filter(F, [{Key,Val}=Pair|Rest]) ->
            case F(Key,Val) of
                true -> [Pair|filter(F,Rest)];
                false -> filter(F,Rest)
            end;
        filter(F, []) when is_function(F, 2) -> [].

        swapped(F, [{Key,Val}|Rest]) ->
            [{Key,F(Val,Key)}|swapped(F,Rest)];
        swapped(F, []) when is_function(F, 2) -> [].

        fold_with(Rows) -> fold(fun add/3, [], Rows).
        map_with(Rows) -> map(fun item/2, Rows).
        filter_with(Rows) -> filter(fun keep/2, Rows).
        swapped_with(Rows) -> swapped(fun item/2, Rows).

        add(Key, Val, Acc) -> [{Key,Val}|Acc].
        item(Key, Val) -> {Key,Val}.
        keep(_, skip) -> false;
        keep(_, invalid) -> other;
        keep(_, _) -> true.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!HasCheckedCallback(
            ReadBody("fold", 3),
            2,
            5,
            trim: false
        )
            || !HasCheckedCallback(
                ReadBody("map", 2),
                3,
                4,
                trim: true
            )
            || !HasCheckedCallback(
                ReadBody("filter", 2),
                3,
                4,
                trim: true
            ))
        {
            throw new InvalidOperationException(BadLayout);
        }

        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term a = Term.A("a");
            Term b = Term.A("b");
            Term value = Term.A("value");
            Term skip = Term.A("skip");
            Term bad = Term.A("invalid");
            Term first = Term.Tuple(a, value);
            Term second = Term.Tuple(b, skip);
            Term rows = Term.List(first, second);

            await Check("fold_with", rows, Term.List(second, first));
            await Check("map_with", rows, Term.List(Term.Tuple(a, first), Term.Tuple(b, second)));
            await Check("filter_with", rows, Term.List(first));
            await Check("swapped_with", rows, Term.List(Term.Tuple(a, Term.Tuple(value, a)), Term.Tuple(b, Term.Tuple(skip, b))));
            await Check("filter_with", Nil.Value, Nil.Value);
            await Reject("fold_with", Term.List(bad), Term.A(ErlangErrorReasons.FunctionClause));
            await Reject("map_with", new Cons(first, bad), Term.A(ErlangErrorReasons.FunctionClause));
            await Reject("filter_with", Term.List(Term.Tuple(a, bad)), Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), Term.A("other")));

            try
            {
                await module.Invoke(
                    context,
                    "map",
                    value,
                    Nil.Value
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(BadValue);

            async Task Check(string function, Term input, Term expected)
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    function,
                    input
                )))
                {
                    throw new InvalidOperationException(BadValue);
                }
            }

            async Task Reject(string function, Term input, Term reason)
            {
                try
                {
                    await module.Invoke(
                        context,
                        function,
                        input
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(BadValue);
            }
        });

        object[] ReadBody(string function, int arity)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands") is [_, var name, var count]
                    && atoms[Value(name)] == function && Value(count) == arity
            );
            if (start < 0)
            {
                throw new InvalidOperationException(BadLayout);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool HasCheckedCallback(
        object[] body,
        int frame,
        int live,
        bool trim
    )
    {
        int allocate = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
        int projection = Array.FindIndex(body, instruction => Opcode(instruction) == GetTupleElement);

        return allocate >= 0 && allocate < projection
            && Items(body[allocate], "Operands") is [var actualFrame, var actualLive]
            && Value(actualFrame) == frame && Value(actualLive) == live
            && body.Any(instruction => Opcode(instruction) == TrimFrame) == trim
            && body.Count(instruction => Opcode(instruction) == IsFunctionWithArity) == 1;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
