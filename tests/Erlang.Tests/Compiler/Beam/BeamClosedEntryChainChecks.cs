using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedEntryChainChecks
{
    internal const string TestName = "compiler/beam/groups-exclusive-terminal-entry-tests";
    private const string Fixture = "beam_closed_entry_chain_probe.erl";
    private const string WrongSelection = "A terminal entry lost its selector order or shared arm.";
    private const string WrongBehavior = "The terminal entry changed an effect or a fallback.";

    private const string Source = """
        -module(beam_closed_entry_chain_probe).
        -export([guard_choice/2, number/2, aliases/2, mixed/2, effect/0]).

        guard_choice(Kind, Context) ->
            if
                Kind =:= zulu -> {last, Context};
                Kind =:= alpha -> {first, Context};
                true -> {fallback, Kind, Context}
            end.

        number(Value, Context) ->
            case Value of
                16#FFFFFFFF -> 16#FFFFFFFF;
                0 -> 0;
                16#FFFFFFFE -> 16#FFFFFFFE;
                Other when is_integer(Other) -> {ordinary, Other, Context}
            end.

        aliases(alpha = _Selected, Context) -> touch(Context);
        aliases(zulu = _Selected, Context) -> touch(Context);
        aliases(Value, Context) when is_integer(Value) -> {ordinary, Value, Context}.

        touch(Context) ->
            erlang:put(closed_entry_effect, Context),
            {touched, Context}.

        effect() -> erlang:get(closed_entry_effect).

        mixed(First, Second) ->
            if
                First =:= alpha -> {first, First};
                Second =:= zulu -> {second, Second};
                true -> fallback
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        var guarded = Select("guard_choice");
        if (!AtomKeys(guarded).SequenceEqual(["alpha", "zulu"]) || Value(guarded[4]) == Value(guarded[6]))
        {
            throw new InvalidOperationException(WrongSelection);
        }

        var integers = Select("number");
        var integerKeys = Integers(integers);
        if (!integerKeys.SequenceEqual([BigInteger.Zero, new BigInteger(uint.MaxValue - 1), new BigInteger(uint.MaxValue)])
            || Value(integers[4]) != Value(integers[6]) || Value(integers[6]) != Value(integers[8]))
        {
            throw new InvalidOperationException(WrongSelection);
        }

        var shared = Select("aliases");
        if (!AtomKeys(shared).SequenceEqual(["alpha", "zulu"]) || Value(shared[4]) != Value(shared[6])
            || FunctionBody("mixed").Any(instruction => Opcode(instruction) == SelectValue))
        {
            throw new InvalidOperationException(WrongSelection);
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term alpha = Term.A("alpha");
            Term zulu = Term.A("zulu");
            Term unknown = Term.A("unknown");
            Term marker = Term.A("seen");
            Term normal = Term.I(3);
            Term upper = Term.I(uint.MaxValue);
            Term ordinary = Term.Tuple(Term.A("ordinary"), normal, marker);
            if (!Term.Tuple(Term.A("first"), marker).Equals(await module.Invoke(
                context,
                "guard_choice",
                alpha,
                marker
            ))
                || !Term.Tuple(Term.A("fallback"), unknown, marker).Equals(await module.Invoke(
                    context,
                    "guard_choice",
                    unknown,
                    marker
                ))
                || !upper.Equals(await module.Invoke(
                    context,
                    "number",
                    upper,
                    marker
                ))
                || !ordinary.Equals(await module.Invoke(
                    context,
                    "number",
                    normal,
                    marker
                ))
                || !ordinary.Equals(await module.Invoke(
                    context,
                    "aliases",
                    normal,
                    marker
                ))
                || !Term.A("undefined").Equals(await module.Invoke(context, "effect"))
                || !Term.Tuple(Term.A("touched"), marker).Equals(await module.Invoke(
                    context,
                    "aliases",
                    zulu,
                    marker
                ))
                || !marker.Equals(await module.Invoke(context, "effect"))
                || !Term.Tuple(Term.A("second"), zulu).Equals(await module.Invoke(
                    context,
                    "mixed",
                    unknown,
                    zulu
                )))
            {
                throw new InvalidOperationException(WrongBehavior);
            }

            try
            {
                await module.Invoke(
                    context,
                    "aliases",
                    unknown,
                    marker
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A("function_clause")))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongBehavior);
        });

        object[] Select(string name)
        {
            object? select = FunctionBody(name).SingleOrDefault(instruction => Opcode(instruction) == SelectValue);
            if (select is null)
            {
                throw new InvalidOperationException(WrongSelection);
            }

            return Operands(select);
        }

        string[] AtomKeys(object[] select) => Keys(select).Select(operand => atoms[Value(operand)]).ToArray();

        object[] FunctionBody(string name)
        {
            int start = Array.FindIndex(instructions, instruction =>
                Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == name);
            if (start < 0)
            {
                throw new InvalidOperationException(WrongSelection);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static IEnumerable<BigInteger> Integers(object[] select) => Keys(select)
        .Select(operand => (BigInteger)Property(operand, "Value"));

    private static IEnumerable<object> Keys(object[] operands) => operands.Skip(3).Where((_, index) => index % 2 == 0);

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
