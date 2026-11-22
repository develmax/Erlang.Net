using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOrderedPairUpdateChecks
{
    internal const string TestName = "compiler/beam/owns-ordered-pair-update-frames-by-branch";
    private const string Fixture = "beam_ordered_pair_update.erl";
    private const string BadLayout = "The ordered update did not share the entry shape or place its branch frames.";
    private const string BadValue = "The ordered update changed an alias, callback, binding or failure.";

    private const string Source = """
        -module(beam_ordered_pair_update).
        -export([store/3, append/3, append_many/3, counter/3, update/4, invoke/3, repeated/3]).

        store(Key, New, [{K,_}|_]=Dict) when Key < K -> [{Key,New}|Dict];
        store(Key, New, [{K,_}=Pair|Dict]) when Key > K -> [Pair|store(Key,New,Dict)];
        store(Key, New, [{_K,_Old}|Dict]) -> [{Key,New}|Dict];
        store(Key, New, []) -> [{Key,New}].

        append(Key, New, [{K,_}|_]=Dict) when Key < K -> [{Key,[New]}|Dict];
        append(Key, New, [{K,_}=Pair|Dict]) when Key > K -> [Pair|append(Key,New,Dict)];
        append(Key, New, [{_K,Old}|Dict]) -> [{Key,Old ++ [New]}|Dict];
        append(Key, New, []) -> [{Key,[New]}].

        append_many(Key, New, [{K,_}|_]=Dict) when Key < K -> [{Key,New}|Dict];
        append_many(Key, New, [{K,_}=Pair|Dict]) when Key > K -> [Pair|append_many(Key,New,Dict)];
        append_many(Key, New, [{_K,Old}|Dict]) -> [{Key,Old ++ New}|Dict];
        append_many(Key, New, []) -> [{Key,New}].

        counter(Key, New, [{K,_}|_]=Dict) when Key < K -> [{Key,New}|Dict];
        counter(Key, New, [{K,_}=Pair|Dict]) when Key > K -> [Pair|counter(Key,New,Dict)];
        counter(Key, New, [{_K,Old}|Dict]) -> [{Key,Old + New}|Dict];
        counter(Key, New, []) -> [{Key,New}].

        update(Key, _, Init, [{K,_}|_]=Dict) when Key < K -> [{Key,Init}|Dict];
        update(Key, Fun, Init, [{K,_}=Pair|Dict]) when Key > K -> [Pair|update(Key,Fun,Init,Dict)];
        update(Key, Fun, _Init, [{_K,Old}|Dict]) -> [{Key,Fun(Old)}|Dict];
        update(Key, _, Init, []) -> [{Key,Init}].

        repeated(Key, New, [{K,K}|_]=Dict) when Key < K -> [{Key,New}|Dict];
        repeated(Key, New, [{K,_}=Pair|Dict]) when Key > K -> [Pair|repeated(Key,New,Dict)];
        repeated(Key, New, [{_K,_Old}|Dict]) -> [{Key,New}|Dict];
        repeated(Key, New, []) -> [{Key,New}].

        invoke(Key, Init, Dict) -> update(Key,fun changed/1,Init,Dict).
        changed(Old) -> {changed,Old}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        foreach (var (name, arity, frames) in new[]
            {
                ("store", 3, 1), ("append", 3, 2), ("append_many", 3, 2),
                ("counter", 3, 1), ("update", 4, 2)
            })
        {
            object[] body = ReadBody(name, arity);
            int firstFrame = Array.FindIndex(body, instruction => Opcode(instruction) is Allocate or AllocateHeap);
            int firstComparison = Array.FindIndex(body, instruction => Opcode(instruction) == IsLess);
            if (body.Count(instruction => Opcode(instruction) == GetList) != 1
                || body.Count(instruction => Opcode(instruction) == IsTuple) != 1
                || body.Count(instruction => Opcode(instruction) is Allocate or AllocateHeap) != frames
                || firstComparison < 0 || firstFrame <= firstComparison)
            {
                throw new InvalidOperationException(BadLayout);
            }
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term a = Term.A("a");
            Term b = Term.A("b");
            Term c = Term.A("c");
            Term d = Term.A("d");
            Term old = Term.A("old");
            Term added = Term.A("added");
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term left = Term.Tuple(a, Term.List(old));
            Term right = Term.Tuple(c, Term.List(old));
            Term dictionary = Term.List(left, right);

            await Check(
                "store",
                b,
                added,
                dictionary,
                Term.List(left, Term.Tuple(b, added), right)
            );
            await Check(
                "store",
                d,
                added,
                dictionary,
                Term.List(left, right, Term.Tuple(d, added))
            );
            await Check(
                "append",
                c,
                added,
                dictionary,
                Term.List(left, Term.Tuple(c, Term.List(old, added)))
            );
            await Check(
                "append_many",
                a,
                Term.List(added),
                dictionary,
                Term.List(Term.Tuple(a, Term.List(old, added)), right)
            );
            await Check(
                "counter",
                a,
                two,
                Term.List(Term.Tuple(a, one)),
                Term.List(Term.Tuple(a, Term.I(3)))
            );
            await Check(
                "invoke",
                a,
                added,
                dictionary,
                Term.List(Term.Tuple(a, Term.Tuple(Term.A("changed"), Term.List(old))), right)
            );
            await Check(
                "invoke",
                b,
                added,
                dictionary,
                Term.List(left, Term.Tuple(b, added), right)
            );
            await Check(
                "repeated",
                a,
                added,
                Term.List(right),
                Term.List(Term.Tuple(a, added))
            );

            await Reject(
                "store",
                c,
                added,
                Term.List(Term.A("malformed")),
                ErlangErrorReasons.FunctionClause
            );
            await Reject(
                "store",
                c,
                added,
                new Cons(left, old),
                ErlangErrorReasons.FunctionClause
            );
            await Reject(
                "append",
                a,
                added,
                Term.List(Term.Tuple(a, old)),
                ErlangErrorReasons.BadArgument
            );
            await Reject(
                "counter",
                a,
                two,
                Term.List(Term.Tuple(a, old)),
                ErlangErrorReasons.BadArithmetic
            );

            return Nil.Value;

            async Task Check(
                string function,
                Term key,
                Term seed,
                Term input,
                Term expected
            )
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    function,
                    key,
                    seed,
                    input
                )))
                {
                    throw new InvalidOperationException(BadValue);
                }
            }

            async Task Reject(
                string function,
                Term key,
                Term seed,
                Term input,
                string reason
            )
            {
                try
                {
                    await module.Invoke(
                        context,
                        function,
                        key,
                        seed,
                        input
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(reason)))
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

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
