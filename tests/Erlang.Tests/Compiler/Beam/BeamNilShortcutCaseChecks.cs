using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNilShortcutCaseChecks
{
    internal const string TestName = "compiler/beam/branches-on-shortcut-nil-comparisons";
    private const string Module = "beam_nil_shortcut_case_probe";
    private const string WrongLayout = "A Boolean case must branch directly from the two empty-list comparisons.";
    private const string WrongBehavior = "The branch test changed operand evaluation or returned the wrong case result.";
    private const string Source = """
        -module(beam_nil_shortcut_case_probe).
        -export([run/2, numeric/2, sequence/2]).
        run(Left, Right) ->
            {case (probe(left, Left) /= []) orelse (probe(right, Right) /= []) of
                 true -> hit;
                 false -> miss
             end, erlang:get(events)}.
        numeric(Left, Right) ->
            {case (probe(left, Left) /= 0) orelse (probe(right, Right) /= 0) of
                 true -> hit;
                 false -> miss
             end, erlang:get(events)}.
        sequence(Left, Right) ->
            Selected = {tag, Left},
            Choice = case (probe(left, Left) /= []) orelse (probe(right, Right) /= []) of
                true -> Selected;
                false -> miss
            end,
            {Choice, erlang:get(events)}.
        probe(Tag, Value) ->
            erlang:put(events, [Tag | erlang:get(events)]),
            case Value of
                fault -> erlang:error(probe_failed);
                Other -> Other
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        CheckLayout(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term events = Term.A("events");
            Term left = Term.A("left");
            Term right = Term.A("right");
            Term fault = Term.A("fault");
            Term hit = Term.A("hit");
            Term miss = Term.A("miss");
            Term nil = Nil.Value;
            var selections = new (string Function, Term Left, Term Right, Term Expected, Term Events)[]
            {
                ("run", Term.I(0), fault, hit, Term.List(left)),
                ("run", nil, Term.I(0), hit, Term.List(right, left)),
                ("run", nil, nil, miss, Term.List(right, left)),
                ("sequence", Term.I(0), fault, Term.Tuple(Term.A("tag"), Term.I(0)), Term.List(left)),
                ("numeric", new FloatTerm(0.0), Term.I(0), miss, Term.List(right, left))
            };
            foreach (var selection in selections)
            {
                context.Dictionary[events] = nil;
                Term expected = Term.Tuple(selection.Expected, selection.Events);
                Term actual = await module.Invoke(
                    context,
                    selection.Function,
                    selection.Left,
                    selection.Right
                );
                if (!expected.Equals(actual))
                {
                    throw new InvalidOperationException(WrongBehavior);
                }
            }

            foreach (var (first, second, visited) in new[] { (fault, nil, Term.List(left)), (nil, fault, Term.List(right, left)) })
            {
                context.Dictionary[events] = nil;
                try
                {
                    _ = await module.Invoke(
                        context,
                        "run",
                        first,
                        second
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A("probe_failed"))
                    && context.Dictionary[events].Equals(visited))
                {
                    continue;
                }

                throw new InvalidOperationException(WrongBehavior);
            }

            return nil;
        });
    }

    private static void CheckLayout(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!HasDirectNilBranches(Body("run")) || !HasDirectNilBranches(Body("sequence"))
            || Body("numeric").Any(instruction => Opcode(instruction) == IsNil))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] Body(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == name
            );

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool HasDirectNilBranches(object[] body)
    {
        object[] tests = body.Where(instruction => Opcode(instruction) == IsNil).ToArray();

        return tests is [var left, var right] && Value(Operands(left)[0]) == Value(Operands(right)[0])
            && !body.Any(instruction => Opcode(instruction) == SelectValue);
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
