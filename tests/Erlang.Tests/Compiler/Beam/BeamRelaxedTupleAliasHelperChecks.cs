using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRelaxedTupleAliasHelperChecks
{
    internal const string TestName = "compiler/beam/lifts-relaxed-tuple-generator-aliases";
    private const string Fixture = "beam_relaxed_tuple_alias_helpers.erl";
    private const string GuardedHelper = "-guarded/2-lc$^0/1-0-";
    private const string ChooseHelper = "-choose/1-lc$^0/1-0-";
    private const string XRegisterKind = "x";
    private const string WrongSchedule = "The relaxed alias helper saved its frame before accepting the head.";
    private const string WrongHelpers = "Relaxed tuple alias helper names or arities differed.";
    private const string WrongResult = "Relaxed tuple alias binding, guard, or generator failure differed.";

    private const string Source = """
        -module(beam_relaxed_tuple_alias_helpers).
        -export([guarded/2, choose/1, project/1, recorded/1, strict/1]).
        -record(entry, {id, state}).
        guarded(Floor, Rows) ->
            [Whole || Whole = {tag, V} <- Rows, is_integer(V) andalso V > Floor].
        choose(Rows) ->
            [Whole || {{key, K}, 0} = Whole <- Rows, K =:= hot orelse K =:= cold orelse K =:= warm].
        project(Rows) ->
            [{Id, Whole} || {_, Id, _} = Whole <- Rows].
        recorded(Rows) ->
            [{Id, Whole} || #entry{id=Id} = Whole <- Rows].
        strict(Rows) ->
            [Whole || Whole = {tag, _} <:- Rows].
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(beam);
        object[] functions = Items(code, "Functions");
        var helpers = functions.Select(function => ((string)Property(function, "Name"), (int)Property(function, "Arity")))
            .ToHashSet();
        if (!helpers.Contains((GuardedHelper, 2)) || !helpers.Contains((ChooseHelper, 1))
            || !helpers.Contains(("-project/1-lc$^0/1-0-", 1))
            || !helpers.Contains(("-recorded/1-lc$^0/1-0-", 1)))
        {
            throw new InvalidOperationException(WrongHelpers);
        }

        CheckSchedule(Body(code, functions, GuardedHelper), 2, BeamEmissionFormat.IsInteger);
        CheckSchedule(Body(code, functions, ChooseHelper), 1, BeamEmissionFormat.SelectValue);

        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tag = Term.A("tag");
            Term bad = Term.A("wrong");
            Term first = Term.I(1);
            Term second = Term.I(2);
            Term selected = Term.Tuple(tag, second);
            Term row = Term.Tuple(tag, second, bad);
            Term record = Term.Tuple(Term.A("entry"), second, bad);
            Term invalidTail = Term.A("invalid_tail");

            RequireEqual(
                Term.List(selected),
                await module.Invoke(
                    context,
                    "guarded",
                    first,
                    Term.List(
                        bad,
                        Term.Tuple(tag, bad),
                        Term.Tuple(bad, second),
                        Term.Tuple(tag, first),
                        selected
                    )
                )
            );
            Term key = Term.A("key");
            Term zero = Term.I(0);
            Term hot = Term.Tuple(Term.Tuple(key, Term.A("hot")), zero);
            Term warm = Term.Tuple(Term.Tuple(key, Term.A("warm")), zero);
            Term cool = Term.Tuple(Term.Tuple(key, Term.A("cool")), zero);
            Term wrongValue = Term.Tuple(Term.Tuple(key, Term.A("hot")), first);
            Term wrongNested = Term.Tuple(Term.Tuple(bad, Term.A("hot")), zero);
            RequireEqual(
                Term.List(hot, warm),
                await module.Invoke(context, "choose", Term.List(
                    wrongNested,
                    bad,
                    hot,
                    wrongValue,
                    cool,
                    warm
                ))
            );
            RequireEqual(Nil.Value, await module.Invoke(context, "choose", Nil.Value));
            await CheckFailure(
                "choose",
                new Cons(hot, invalidTail),
                ErlangErrorReasons.BadGenerator,
                invalidTail
            );

            RequireEqual(Term.List(Term.Tuple(second, row)), await module.Invoke(context, "project", Term.List(bad, row)));
            RequireEqual(Term.List(Term.Tuple(second, record)), await module.Invoke(context, "recorded", Term.List(row, record)));
            RequireEqual(Nil.Value, await module.Invoke(context, "project", Nil.Value));

            await CheckFailure(
                "project",
                new Cons(row, invalidTail),
                ErlangErrorReasons.BadGenerator,
                invalidTail
            );
            await CheckFailure(
                "strict",
                Term.List(bad),
                ErlangErrorReasons.BadMatch,
                bad
            );

            return Nil.Value;

            async ValueTask CheckFailure(
                string function,
                Term input,
                string reason,
                Term value
            )
            {
                try
                {
                    await module.Invoke(context, function, input);
                }
                catch (ErlangException error) when (Term.Tuple(Term.A(reason), value).Equals(error.Reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult + " " + function);
            }
        });
    }

    private static void CheckSchedule(object[] instructions, int arity, byte guardOpcode)
    {
        int listTest = Array.FindIndex(instructions, instruction => Opcode(instruction) == BeamEmissionFormat.IsNonemptyList);
        int split = Array.FindIndex(instructions, instruction => Opcode(instruction) == BeamEmissionFormat.GetList);
        int guard = Array.FindIndex(instructions, instruction => Opcode(instruction) == guardOpcode);
        int allocation = Array.FindIndex(instructions, instruction =>
            Opcode(instruction) is BeamEmissionFormat.Allocate or BeamEmissionFormat.AllocateHeap);
        if (listTest < 0 || listTest >= split || split >= guard || guard >= allocation
            || Opcode(instructions[allocation]) != BeamEmissionFormat.Allocate
            || Value(Items(instructions[allocation], "Operands")[0]) != 1
            || Value(Items(instructions[allocation], "Operands")[1]) != arity + 1)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        object[] registers = Items(instructions[split], "Operands");
        if (!IsX(registers[0], 0) || !IsX(registers[1], arity) || !IsX(registers[2], 0)
            || !instructions.Skip(allocation + 1).Any(instruction => Opcode(instruction) == BeamEmissionFormat.TailLocalCall))
        {
            throw new InvalidOperationException(WrongSchedule);
        }
    }

    private static object[] Body(object code, object[] functions, string name)
    {
        object function = functions.Single(candidate => (string)Property(candidate, "Name") == name);
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");

        return Items(code, "Instructions")[start..end];
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();

    private static bool IsX(object operand, int register) =>
        (string)Property(operand, "Kind") == XRegisterKind && Value(operand) == register;

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static void RequireEqual(Term expected, Term actual)
    {
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException(WrongResult + " " + actual);
        }
    }
}
