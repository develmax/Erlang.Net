using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOrderedPairDistanceSelectionChecks
{
    internal const string TestName = "compiler/beam/shares-closed-ordered-pair-distance-selection";
    private const string Fixture = "beam_ordered_pair_distance_probe.erl";
    private const string WrongCode = "The private ordered pair selection must keep its shared list entry and preserve live physical registers.";
    private const string WrongValue = "The ordered pair selection changed its recursive value or original failure.";

    private const string Source = """
        -module(beam_ordered_pair_distance_probe).
        -export([choose/4, open/5, sizes/0]).
        choose(Wanted, Max, Items, PreviousKey) ->
            walk(start, Wanted, Max+1, Items, {PreviousKey,old}).
        walk(_Token, _Wanted, Max, [{K,_Value}|_Rest], Previous) when K > Max ->
            Previous;
        walk(Token, Wanted, Max, [{K,_Value}=Current|Rest], Previous={OldKey,_OldValue}) ->
            if
                Wanted == K -> Current;
                Wanted > K -> walk(Token, Wanted, Max, Rest, Current);
                Wanted < K, (K-Wanted) < (Wanted-OldKey) -> Current;
                Wanted < K -> Previous
            end;
        walk(_Token, _Wanted, _Max, [], Previous) -> Previous.

        open(_Token, _Wanted, Max, [{K,_Value}|_Rest], Previous) when K > Max ->
            Previous;
        open(Token, Wanted, Max, [{K,Value}|Rest], {OldKey,OldValue}) ->
            if
                Wanted == K -> {K,Value};
                Wanted > K -> open(Token, Wanted, Max, Rest, {K,Value});
                Wanted < K, (K-Wanted) < (Wanted-OldKey) -> {K,Value};
                Wanted < K -> {OldKey,OldValue}
            end;
        open(_Token, _Wanted, _Max, [], Previous) -> Previous.

        sizes() -> [Key || {Key,_} <- [{2,old},{4,new}]].
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckCode(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term sizes = await module.Invoke(context, "sizes");
            if (!sizes.Equals(Term.List(Term.I(2), Term.I(4))))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term near = Term.Tuple(Term.I(12), Term.A("near"));
            Term far = Term.Tuple(Term.I(14), Term.A("far"));
            Term initial = Term.I(2);
            Term previous = Term.Tuple(initial, Term.A("old"));
            Term invalidKey = Term.A("invalid_key");
            Term groups = Term.List(near, far);
            await Check(
                Term.I(8),
                20,
                initial,
                near
            );
            await Check(
                Term.I(7),
                20,
                initial,
                previous
            );
            await Check(
                Term.I(12),
                20,
                initial,
                near
            );
            await Check(
                Term.I(12),
                6,
                initial,
                previous
            );
            await Check(
                Term.A("after_all_numbers"),
                20,
                initial,
                far
            );
            await Check(
                Term.I(8),
                20,
                invalidKey,
                Term.Tuple(invalidKey, Term.A("old"))
            );

            Term open = await module.Invoke(
                context,
                "open",
                Term.A("start"),
                Term.I(8),
                Term.I(6),
                groups,
                invalidKey
            );
            if (!open.Equals(invalidKey))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "choose",
                    Term.I(8),
                    Term.I(20),
                    Term.List(invalidKey),
                    initial
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongValue);

            async Task Check(
                Term wanted,
                int max,
                Term oldKey,
                Term expected
            )
            {
                Term result = await module.Invoke(
                    context,
                    "choose",
                    wanted,
                    Term.I(max),
                    groups,
                    oldKey
                );
                if (!result.Equals(expected))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }
        });
    }

    private static void CheckCode(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction =>
            Opcode(instruction) == FunctionInfo && Operands(instruction) is [_, var name, _] && atoms[Value(name)] == "walk"
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongCode);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        object[] numeric = body.Where(instruction => Opcode(instruction) == GcBifTwo).ToArray();
        if (body.Count(instruction => Opcode(instruction) == GetList) != 1
            || body.Count(instruction => Opcode(instruction) == IsTuple) != 1
            || body.Any(instruction => Opcode(instruction) is Allocate or AllocateHeap or IfEnd)
            || numeric.Length != 2 || numeric.Any(instruction => Value(Operands(instruction)[1]) != 6)
            || !body.Any(instruction => Opcode(instruction) == TailLocalCall))
        {
            throw new InvalidOperationException(WrongCode);
        }
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
