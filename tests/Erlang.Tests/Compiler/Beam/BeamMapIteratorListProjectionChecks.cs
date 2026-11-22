using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMapIteratorListProjectionChecks
{
    internal const string TestName = "compiler/beam/lifts-map-generators-that-project-list-values";
    private const string Source = """
        -module(beam_map_iterator_list_probe).
        -export([select/1, fields/1, captured/2]).

        select(Source) ->
            [Key || Key := included <- Source].
        fields(Source) ->
            [{Key, Field} || Key := {_Tag, Field} <- Source].
        captured(Source, Outer) ->
            [{Key, Outer} || Key := included <- Source].
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_map_iterator_list_probe.erl");
        object code = Code(image);
        RequireHelper("select", 1);
        RequireHelper("fields", 2);
        if (Functions().Any(item => (string)Property(item, "Name") == "-captured/2-lc$^0/1-0-"))
        {
            throw new InvalidOperationException("A captured result needs a separate iterator layout.");
        }

        var selection = new RuntimeLibrarySelection([RuntimeModuleSelection.Managed(ErlangModuleNames.Module)]);
        await using var runtime = await ProcessRuntime.CreateAsync(selection);
        var module = BeamModule.Prepare(image);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term alpha = Term.A("alpha");
            Term beta = Term.A("beta");
            Term gamma = Term.A("gamma");
            Term included = Term.A("included");
            Term other = Term.A("other");
            Term none = Term.A("none");
            Term tag = Term.A("tag");
            Term invalid = Term.Tuple(Term.A("invalid_source"), alpha);
            Term wanted = Term.I(37);
            Term pair = Term.Tuple(tag, wanted);
            Term iterator = Term.Tuple(alpha, included, Term.Tuple(beta, other, Term.Tuple(gamma, included, none)));
            Term nested = Term.Tuple(alpha, pair, Term.Tuple(beta, other, Term.Tuple(gamma, pair, none)));
            Term selected = Term.List(alpha, gamma);
            Term projected = Term.List(Term.Tuple(alpha, wanted), Term.Tuple(gamma, wanted));
            Term simpleMap = new MapTerm([new(alpha, included)]);
            if (!selected.Equals(await module.Invoke(context, "select", iterator))
                || !projected.Equals(await module.Invoke(context, "fields", nested))
                || !Term.List(alpha).Equals(await module.Invoke(context, "select", simpleMap))
                || !Nil.Value.Equals(await module.Invoke(context, "fields", new MapTerm([])))
                || !Term.List(Term.Tuple(alpha, wanted)).Equals(await module.Invoke(
                    context,
                    "captured",
                    simpleMap,
                    wanted
                )))
            {
                throw new InvalidOperationException("The iterator helper changed projection, order or skipped pairs.");
            }

            try
            {
                await module.Invoke(context, "select", invalid);
            }
            catch (ErlangException error) when (error.Reason.Equals(
                Term.Tuple(Term.A(ComprehensionErrorReasons.BadGenerator), invalid)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException("An invalid map generator must carry the original source term.");
        });

        IEnumerable<object> Functions() => Items(code, "Functions");

        void RequireHelper(string name, int width)
        {
            string helper = $"-{name}/1-lc$^0/1-0-";
            object function = Functions().Single(item => (string)Property(item, "Name") == helper
                && (int)Property(item, "Arity") == 1);
            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");
            object[] body = Items(code, "Instructions").Skip(start).Take(end - start).ToArray();
            if (!body.Any(instruction => Opcode(instruction) == Allocate
                    && (int)(BigInteger)Property(Items(instruction, "Operands").First(), "Value") == width)
                || !body.Any(instruction => Opcode(instruction) == TailLocalCallWithFrame)
                || !body.Any(instruction => Opcode(instruction) == TailLocalCall)
                || !body.Any(instruction => Opcode(instruction) == PutList))
            {
                throw new InvalidOperationException("The map iterator must retain the checked fields across its recursive call.");
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
