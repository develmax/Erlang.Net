using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamZippedMapPairChecks
{
    internal const string TestName = "compiler/beam/lifts-dependent-variable-pairs-from-a-map-zip";
    private const string Module = "beam_zipped_map_pairs";
    private const string Maps = "maps";
    private const string WrongShape = "Expected a recursive pair-list helper with both projected inputs retained across recursion.";
    private const string WrongValue = "Map ZIP lowering changed key order, duplicate handling, or failed cursor payloads.";
    private const string Source = """
        -module(beam_zipped_map_pairs).
        -export([index/2, reverse_index/2, filtered/2]).
        index(Keys, Values) -> #{K => V || K <- Keys && V <- Values}.
        reverse_index(Values, Keys) -> #{K => V || V <- Values && K <- Keys}.
        filtered(Keys, Values) -> #{K => V || K <- Keys && V <- Values, K =/= V}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([Maps]));
        string[] names = module.Functions.Select(function => function.Name).ToArray();
        if (names.Contains("-filtered/2-zlc$^0/2-0-"))
        {
            throw new InvalidOperationException(WrongShape);
        }

        RequireHelper(Code(image), "-index/2-zlc$^0/2-0-", reversed: false);
        RequireHelper(Code(image), "-reverse_index/2-zlc$^0/2-0-", reversed: true);

        var selection = new RuntimeLibrarySelection(
        [
            RuntimeModuleSelection.Managed("erlang"),
            RuntimeModuleSelection.Managed(Maps)
        ]);
        await using var runtime = await ProcessRuntime.CreateAsync(selection);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("key");
            Term first = Term.I(1);
            Term last = Term.I(2);
            Term keys = Term.List(key, key);
            Term values = Term.List(first, last);
            Term expected = new MapTerm([new(key, last)]);
            if (!expected.Equals(await module.Invoke(
                context,
                "index",
                keys,
                values
            ))
                || !expected.Equals(await module.Invoke(
                    context,
                    "reverse_index",
                    values,
                    keys
                ))
                || !new MapTerm([]).Equals(await module.Invoke(
                    context,
                    "index",
                    Nil.Value,
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term improper = Term.A("improper");
            Term unresolvedValues = Term.List(last);
            Term reason = Term.Tuple(
                Term.A(ComprehensionErrorReasons.BadGenerators),
                Term.Tuple(improper, unresolvedValues)
            );

            try
            {
                await module.Invoke(
                    context,
                    "index",
                    new Cons(key, improper),
                    values
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(reason))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongValue);
        });
    }

    private static void RequireHelper(object code, string name, bool reversed)
    {
        object function = Items(code, "Functions").Single(item => (string)Property(item, "Name") == name);
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        int entry = (int)Property(function, "EntryLabel");
        object[] body = Items(code, "Instructions").Skip(start).Take(end - start).ToArray();
        object call = body.Single(instruction => Opcode(instruction) == LocalCall);
        int recurse = Array.IndexOf(body, call);
        if (Items(call, "Operands").Select(Value).ToArray() is not [2, var target] || target != entry
            || body.Any(instruction => Opcode(instruction) == TrimFrame)
            || Items(body[recurse + 1], "Operands").Select(Value).ToArray() is not [5, 1]
            || Opcode(body[recurse + 1]) != TestHeap || Opcode(body[recurse + 2]) != PutTuple
            || Items(body[recurse + 2], "Operands").Select(Value).TakeLast(2).ToArray()
                is not [var key, var value] || key != (reversed ? 0 : 1) || value != (reversed ? 1 : 0)
            || Opcode(body[recurse + 3]) != PutList)
        {
            throw new InvalidOperationException(WrongShape);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
