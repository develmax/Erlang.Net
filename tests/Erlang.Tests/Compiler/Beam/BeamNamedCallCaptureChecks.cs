using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNamedCallCaptureChecks
{
    internal const string TestName = "compiler/beam/orders-straight-call-closure-captures-by-core-name";
    private const string Fixture = "beam_named_call_capture.erl";
    private const string IncorrectLayout = "The lifted function must keep entry captures in order and sort proven named call results.";
    private const string IncorrectResult = "Closure capture ordering changed a captured value.";

    private const string Source = """
        -module(beam_named_call_capture).
        -export([run/4, unknown/4]).

        run(Input, Rows, ZedCarry, AlphaCarry) ->
            Zulu = lists:reverse(Input),
            Alpha = lists:sort(Input),
            F = fun(Item) -> pack(Item, ZedCarry, AlphaCarry, Zulu, Alpha) end,
            lists:map(F, Rows).

        unknown(Input, Rows, ZedCarry, AlphaCarry) ->
            Zulu = lists:reverse(Input),
            Alpha = identity(Input),
            F = fun(Item) -> pack(Item, ZedCarry, AlphaCarry, Zulu, Alpha) end,
            lists:map(F, Rows).

        identity(Value) -> Value.
        pack(Item, ZedCarry, AlphaCarry, Zulu, Alpha) ->
            {Item, ZedCarry, AlphaCarry, Zulu, Alpha}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckLiftedOrder(image, "-run/4-fun-0-", sorted: true);
        CheckLiftedOrder(image, "-unknown/4-fun-0-", sorted: false);

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        var selection = new RuntimeLibrarySelection(
            [RuntimeModuleSelection.Managed("erlang"), RuntimeModuleSelection.Managed("lists")]
        );
        await using var runtime = await ProcessRuntime.CreateAsync(
            selection,
            RuntimeNativePrimitives.InternalScalars413,
            CancellationToken.None
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term items = Term.List(Term.I(2), Term.I(3), Term.I(1));
            Term reversed = Term.List(Term.I(1), Term.I(3), Term.I(2));
            Term sorted = Term.List(Term.I(1), Term.I(2), Term.I(3));
            Term marker = Term.A("marker");
            Term first = Term.A("first");
            Term second = Term.A("second");
            await Check("run", sorted);
            await Check("unknown", items);

            return Nil.Value;

            async Task Check(string function, Term expectedInput)
            {
                Term observed = await module.Invoke(
                    context,
                    function,
                    items,
                    Term.List(marker),
                    first,
                    second
                );
                if (!observed.Equals(Term.List(Term.Tuple(
                    marker,
                    first,
                    second,
                    reversed,
                    expectedInput
                ))))
                {
                    throw new InvalidOperationException(IncorrectResult);
                }
            }
        });
    }

    private static void CheckLiftedOrder(byte[] image, string name, bool sorted)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int info = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var function, var arity]
                && atoms[Value(function)] == name && Value(arity) == 5
        );
        if (info < 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] body = instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Where(instruction => Opcode(instruction) is Move or Swap).ToArray();
        if (sorted ? body is not [var swap] || !ReordersNamedResults(swap) : body.Length != 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }
    }

    private static bool ReordersNamedResults(object instruction) =>
        Opcode(instruction) == Swap && Items(instruction, "Operands") is [var first, var second]
        && (Value(first), Value(second)) is (3, 4) or (4, 3);

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
