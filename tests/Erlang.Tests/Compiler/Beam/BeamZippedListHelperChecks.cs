using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamZippedListHelperChecks
{
    internal const string TestName = "compiler/beam/lifts-a-terminal-two-list-zip-with-an-ordered-mapper";
    private const string Module = "beam_zipped_list_helper";
    private const string Helper = "-zip/2-zlc$^0/2-0-";
    private const string FilteredHelper = "-filtered/2-zlc$^0/2-0-";
    private const string WrongLayout = "Expected the zipped helper to retain only the mapped head across recursion.";
    private const string WrongValue = "The zipped helper changed the result, mapper order, or failing suffixes.";
    private const string Source = """
        -module(beam_zipped_list_helper).
        -export([zip/2, filtered/2]).
        zip(Left, Right) ->
            [map_pair(A, B) || A <- Left && B <- Right].
        filtered(Left, Right) ->
            [map_pair(A, B) || A <- Left && B <- Right, A =/= B].
        map_pair(fail, Value) ->
            erlang:error({mapper, Value});
        map_pair(Left, Right) ->
            {Left, Right}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        BeamModule module = BeamModule.Prepare(image);
        RequireLayout(Code(image));

        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term three = Term.I(3);
            Term four = Term.I(4);
            Term fail = Term.A("fail");
            Term improper = Term.A("improper");
            if (!Term.List(Term.Tuple(one, three), Term.Tuple(two, four)).Equals(
                    await module.Invoke(
                        context,
                        "zip",
                        Term.List(one, two),
                        Term.List(three, four)
                    ))
                || !Nil.Value.Equals(await module.Invoke(
                    context,
                    "zip",
                    Nil.Value,
                    Nil.Value
                ))
                || !Term.List(Term.Tuple(two, three)).Equals(
                    await module.Invoke(
                        context,
                        "filtered",
                        Term.List(one, two),
                        Term.List(one, three)
                    )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await RequireFailure(Term.Tuple(Term.A("mapper"), three), Term.List(fail, two), Term.List(three));
            await RequireFailure(
                Term.Tuple(Term.A(ComprehensionErrorReasons.BadGenerators), Term.Tuple(improper, Term.List(four))),
                new Cons(one, improper),
                Term.List(three, four)
            );
            await RequireFailure(
                Term.Tuple(Term.A(ComprehensionErrorReasons.BadGenerators), Term.Tuple(Term.List(two), Nil.Value)),
                Term.List(one, two),
                Term.List(three)
            );

            return Nil.Value;

            async Task RequireFailure(Term reason, Term left, Term right)
            {
                try
                {
                    await module.Invoke(
                        context,
                        "zip",
                        left,
                        right
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }
        });
    }

    private static void RequireLayout(object code)
    {
        object[] functions = Items(code, "Functions").ToArray();
        object helper = functions.Single(function => (string)Property(function, "Name") == Helper);
        object owner = functions.Single(function => (string)Property(function, "Name") == "zip");
        object mapper = functions.Single(function => (string)Property(function, "Name") == "map_pair");
        int entry = (int)Property(helper, "EntryLabel");
        int mapperEntry = (int)Property(mapper, "EntryLabel");
        object[] body = Body(helper);
        object[] ownerBody = Body(owner);

        if (functions.Any(function => (string)Property(function, "Name") == FilteredHelper)
            || ownerBody.Select(Opcode).ToArray() is not [TailLocalCall]
            || Values(ownerBody[0]) is not [2, var jump] || jump != entry
            || body.Select(Opcode).ToArray() is not [
                IsNonemptyList, GetList, IsNonemptyList, Allocate, Move, GetList, Move,
                LocalCall, Swap, Move, TrimFrame, LocalCall, TestHeap, PutList, Deallocate, Return,
                IsNil, IsNil, Move, Return, TestHeap, PutTuple, PutTuple, TailExternalCall
            ]
            || Values(body[3]) is not [2, 4]
            || Values(body[7]) is not [2, var map] || map != mapperEntry
            || Values(body[10]) is not [1, 1]
            || Values(body[11]) is not [2, var recur] || recur != entry
            || Values(body[12]) is not [2, 1]
            || Values(body[20]) is not [6, 2])
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] Body(object function)
        {
            int first = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");

            return Items(code, "Instructions").Skip(first).Take(end - first)
                .Where(instruction => Opcode(instruction) is not (Label or Line or FunctionInfo))
                .ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int[] Values(object instruction) =>
        Items(instruction, "Operands").Select(operand => (int)(BigInteger)Property(operand, "Value")).ToArray();

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
