using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOrderedPairMergeChecks
{
    internal const string TestName = "compiler/beam/shares-ordered-merge-pair-prefixes";
    private const string Fixture = "beam_ordered_pair_merge.erl";
    private const string BadLayout = "Ordered merge did not share both checked pair heads or preserve its guarded exits.";
    private const string BadValue = "Ordered merge changed the original list, callback arguments or rejection.";

    private const string Source = """
        -module(beam_ordered_pair_merge).
        -export([merge/3, run/2, repeat_run/2]).

        merge(F, [{K1,_}=P1|D1], [{K2,_}=P2|D2]) when K1 < K2 ->
            [P1|merge(F,D1,[P2|D2])];
        merge(F, [{K1,_}=P1|D1], [{K2,_}=P2|D2]) when K1 > K2 ->
            [P2|merge(F,[P1|D1],D2)];
        merge(F, [{K1,V1}|D1], [{_K2,V2}|D2]) ->
            [{K1,F(K1,V1,V2)}|merge(F,D1,D2)];
        merge(F, [], D2) when is_function(F, 3) -> D2;
        merge(F, D1, []) when is_function(F, 3) -> D1.

        repeated(F, [{K1,K1}=P1|D1], [{K2,_}=P2|D2]) when K1 < K2 ->
            [P1|repeated(F,D1,[P2|D2])];
        repeated(F, [{K1,_}=P1|D1], [{K2,_}=P2|D2]) when K1 > K2 ->
            [P2|repeated(F,[P1|D1],D2)];
        repeated(F, [{K1,V1}|D1], [{_K2,V2}|D2]) ->
            [{K1,F(K1,V1,V2)}|repeated(F,D1,D2)];
        repeated(F, [], D2) when is_function(F, 3) -> D2;
        repeated(F, D1, []) when is_function(F, 3) -> D1.

        run(Left, Right) -> merge(fun joined/3, Left, Right).
        repeat_run(Left, Right) -> repeated(fun joined/3, Left, Right).
        joined(Key, Left, Right) -> {Key,Left,Right}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == "merge" && Value(arity) == 3
        );
        if (start < 0)
        {
            throw new InvalidOperationException(BadLayout);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int[] frames = body.Where(instruction => Opcode(instruction) == Allocate)
            .Select(instruction => Value(Items(instruction, "Operands")[0])).ToArray();
        if (!frames.SequenceEqual([1, 1, 4])
            || body.TakeWhile(instruction => Opcode(instruction) != Allocate).Count(instruction => Opcode(instruction) == GetList) != 2
            || body.Count(instruction => Opcode(instruction) == IsTuple) != 2
            || body.Count(instruction => Opcode(instruction) == IsFunctionWithArity) != 2)
        {
            throw new InvalidOperationException(BadLayout);
        }

        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term three = Term.I(3);
            Term left = Term.A("left");
            Term right = Term.A("right");
            Term malformed = Term.A("malformed");
            Term first = Term.Tuple(one, left);
            Term second = Term.Tuple(two, right);
            Term sameLeft = Term.Tuple(three, left);
            Term sameRight = Term.Tuple(new FloatTerm(3.0), right);
            await Check(
                "run",
                Term.List(first, sameLeft),
                Term.List(second, sameRight),
                Term.List(first, second, Term.Tuple(three, Term.Tuple(three, left, right)))
            );
            await Check(
                "run",
                malformed,
                Nil.Value,
                malformed
            );
            await Check(
                "run",
                Nil.Value,
                malformed,
                malformed
            );
            await Check(
                "repeat_run",
                Term.List(first),
                Term.List(second),
                Term.List(Term.Tuple(one, Term.Tuple(one, left, right)))
            );

            try
            {
                await module.Invoke(
                    context,
                    "run",
                    Term.List(first),
                    Term.List(malformed)
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(BadValue);

            async Task Check(
                string function,
                Term inputLeft,
                Term inputRight,
                Term expected
            )
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    function,
                    inputLeft,
                    inputRight
                )))
                {
                    throw new InvalidOperationException(BadValue);
                }
            }
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
