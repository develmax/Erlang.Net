using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMapIteratorHelperChecks
{
    internal const string TestName = "compiler/beam/lifts-lexically-owned-map-iterator-pairs";
    private const string ModuleName = "beam_map_iterator_pairs";
    private const string Maps = "maps";
    private const string WrongShape = "The map iterator helper did not retain the source-required roots across recursion.";
    private const string WrongValue = "Map iterator helper lowering changed filtering or original generator errors.";
    private const string Source = """
        -module(beam_map_iterator_pairs).
        -export([copy/1, choose/2, unrelated/1, callback/0]).

        copy(Source) ->
            #{Key => Value || Key := Value <- Source}.

        choose(scan, Source) ->
            #{Key => Value ||
                Key := Value <- Source,
                Value =/= reject};
        choose(Other, _) ->
            Other.

        unrelated(Source) ->
            #{Key => Value || Key := Value <- Source, Key =/= Value}.

        callback() -> fun(X) -> X end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, ModuleName + ".erl");
        object code = Code(image);
        RequireHelper(code, "-copy/1-lc$^0/1-0-", 1);
        RequireHelper(code, "-choose/2-lc$^0/1-0-", 2);
        if (Items(code, "Functions").Any(item => (string)Property(item, "Name") == "-unrelated/1-lc$^0/1-0-"))
        {
            throw new InvalidOperationException(WrongShape);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([Maps]));
        var selection = new RuntimeLibrarySelection(
        [
            RuntimeModuleSelection.Managed("erlang"),
            RuntimeModuleSelection.Managed(Maps)
        ]);
        await using var runtime = await ProcessRuntime.CreateAsync(selection);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term scan = Term.A("scan");
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term wanted = Term.I(7);
            Term rejected = Term.A("reject");
            Term none = Term.A("none");
            Term input = new MapTerm([new(first, wanted), new(second, rejected)]);
            Term expected = new MapTerm([new(first, wanted)]);
            Term used = Term.Tuple(first, wanted, Term.Tuple(second, rejected, none));
            Term ignored = Term.A("ignored");
            if (!input.Equals(await module.Invoke(context, "copy", input))
                || !input.Equals(await module.Invoke(context, "copy", used))
                || !expected.Equals(await module.Invoke(
                    context,
                    "choose",
                    scan,
                    input
                ))
                || !expected.Equals(await module.Invoke(
                    context,
                    "choose",
                    scan,
                    used
                ))
                || !new MapTerm([]).Equals(await module.Invoke(context, "copy", none))
                || !ignored.Equals(await module.Invoke(
                    context,
                    "choose",
                    ignored,
                    ignored
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            foreach (Term bad in new[] { ignored, Term.Tuple(first, wanted, ignored) })
            {
                try
                {
                    await module.Invoke(
                        context,
                        "choose",
                        scan,
                        bad
                    );
                    throw new InvalidOperationException(WrongValue);
                }
                catch (ErlangException error) when (error.Reason.Equals(
                    Term.Tuple(Term.A(ComprehensionErrorReasons.BadGenerator), bad)))
                {
                }
            }

            return Nil.Value;
        });
    }

    private static void RequireHelper(object code, string name, int frame)
    {
        object function = Items(code, "Functions").Single(item => (string)Property(item, "Name") == name);
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        int entry = (int)Property(function, "EntryLabel");
        object[] body = Items(code, "Instructions").Skip(start).Take(end - start).ToArray();
        object call = body.Single(instruction => Opcode(instruction) == LocalCall);
        int recurse = Array.IndexOf(body, call);
        if (Items(call, "Operands").Select(Value).ToArray() is not [1, var target] || target != entry
            || !body.Take(recurse).Any(instruction => Opcode(instruction) == Allocate
                && Value(Items(instruction, "Operands").First()) == frame)
            || body.Any(instruction => Opcode(instruction) == TailLocalCall) != (frame == 2)
            || !body.Skip(recurse + 1).Any(instruction => Opcode(instruction) == PutList))
        {
            throw new InvalidOperationException(WrongShape);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
