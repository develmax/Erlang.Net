using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOrderedPairRemovalUpdateChecks
{
    internal const string TestName = "compiler/beam/shares-ordered-pair-removal-and-partial-update";
    private const string Fixture = "beam_ordered_pair_removal_update.erl";
    private const string BadLayout = "Ordered removal or partial update repeated the checked prefix or misplaced a frame.";
    private const string BadValue = "Ordered removal or partial update changed a preserved pair or rejection.";

    private const string Source = """
        -module(beam_ordered_pair_removal_update).
        -export([remove/2, update/3, invoke/2, repeated/2]).

        remove(Key, [{K,_}=Pair|Rest]) when Key < K -> [Pair|Rest];
        remove(Key, [{K,_}=Pair|Rest]) when Key > K -> [Pair|remove(Key,Rest)];
        remove(_Key, [{_K,_Old}|Rest]) -> Rest;
        remove(_, []) -> [].

        update(Key, Fun, [{K,_}=Pair|Rest]) when Key > K -> [Pair|update(Key,Fun,Rest)];
        update(Key, Fun, [{K,Value}|Rest]) when Key == K -> [{Key,Fun(Value)}|Rest].

        invoke(Key, Dict) -> update(Key,fun changed/1,Dict).
        changed(Old) -> {changed,Old}.

        repeated(Key, [{K,K}=Pair|Rest]) when Key < K -> [Pair|Rest];
        repeated(Key, [{K,_}=Pair|Rest]) when Key > K -> [Pair|repeated(Key,Rest)];
        repeated(_Key, [{_K,_Old}|Rest]) -> Rest;
        repeated(_, []) -> [].
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] removal = ReadBody("remove", 2);
        object[] update = ReadBody("update", 3);
        if (!HasSharedPrefix(removal, 1) || !HasSharedPrefix(update, 2)
            || update.Count(instruction => Opcode(instruction) == NumericEqualTest) != 1
            || !update.Any(instruction => Opcode(instruction) == CallFun))
        {
            throw new InvalidOperationException(BadLayout);
        }

        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term value = Term.A("value");
            Term before = Term.Tuple(Term.I(1), Term.A("first"));
            Term hit = Term.Tuple(new FloatTerm(2.0), value);
            Term after = Term.Tuple(Term.I(4), Term.A("after"));
            Term rows = Term.List(before, hit, after);
            Term two = Term.I(2);
            Term zero = Term.I(0);
            await Check(
                "remove",
                two,
                rows,
                Term.List(before, after)
            );
            await Check(
                "remove",
                zero,
                rows,
                rows
            );
            await Check(
                "remove",
                zero,
                Nil.Value,
                Nil.Value
            );
            await Check(
                "remove",
                two,
                new Cons(hit, value),
                value
            );
            await Check(
                "invoke",
                two,
                rows,
                Term.List(before, Term.Tuple(two, Term.Tuple(Term.A("changed"), value)), after)
            );
            await Check(
                "repeated",
                zero,
                Term.List(hit),
                Nil.Value
            );
            await Reject("remove", two, Term.List(value));
            await Reject("remove", two, new Cons(before, value));
            await Reject("invoke", zero, rows);
            await Reject("invoke", zero, Nil.Value);

            return Nil.Value;

            async Task Check(
                string function,
                Term key,
                Term input,
                Term expected
            )
            {
                if (!expected.Equals(await module.Invoke(
                    context,
                    function,
                    key,
                    input
                )))
                {
                    throw new InvalidOperationException(BadValue);
                }
            }

            async Task Reject(string function, Term key, Term input)
            {
                try
                {
                    await module.Invoke(
                        context,
                        function,
                        key,
                        input
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
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

    private static bool HasSharedPrefix(object[] body, int frames)
    {
        int firstFrame = Array.FindIndex(body, instruction => Opcode(instruction) is Allocate or AllocateHeap);
        int firstCompare = Array.FindIndex(body, instruction => Opcode(instruction) == IsLess);

        return body.Count(instruction => Opcode(instruction) == GetList) == 1
            && body.Count(instruction => Opcode(instruction) == IsTuple) == 1
            && body.Count(instruction => Opcode(instruction) is Allocate or AllocateHeap) == frames
            && firstCompare >= 0 && firstFrame > firstCompare;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
