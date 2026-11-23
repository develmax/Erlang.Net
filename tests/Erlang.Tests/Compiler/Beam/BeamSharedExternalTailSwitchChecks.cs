using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSharedExternalTailSwitchChecks
{
    internal const string TestName = "compiler/beam/tuple-tail-switch-shares-identical-external-calls";
    private const string Module = "beam_shared_external_tail_switch";
    private const string TargetsMismatch = "Expected just the equivalent projected calls to have the same switch target.";
    private const string LocationMismatch = "Expected the first equivalent selector arm to retain its own call location.";
    private const string RemainderMismatch = "Expected the guard, different tuple arity and catch-all clauses to retain the input.";

    private const string Source = """
        -module(beam_shared_external_tail_switch).
        -export([run/1]).
        run({integer, _, N}) -> io_lib:write(N);
        run({float, _, F}) -> io_lib:write(F);
        run({reserved_symbol, _, A}) -> io_lib:write(A);
        run({char, C, _}) -> io_lib:write(C);
        run({atom, _, A}) -> io_lib:write_atom(A);
        run({var, _, V}) -> io_lib:format("~s", [V]);
        run({guarded, _, V}) when is_atom(V) -> V;
        run({atom, V}) -> V;
        run(X) -> X.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        VerifyInstructions(Code(beam));

        // io_lib is exercised by the pinned ERTS fixture. Registered
        // external functions exercise the same lowering on the managed host.
        string runtimeSource = Source.Replace("io_lib:write(", "maps:keys(", StringComparison.Ordinal)
            .Replace("io_lib:write_atom(A)", "maps:values(A)", StringComparison.Ordinal)
            .Replace("io_lib:format(\"~s\", [V])", "lists:member(marker, [V])", StringComparison.Ordinal);
        byte[] runtimeBeam = CodeGeneration.CompileBeamModule(runtimeSource, Module + ".erl");
        VerifyInstructions(Code(runtimeBeam));
        BeamModule image = BeamModule.Prepare(runtimeBeam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term result = Term.A("result");
            Term guarded = await image.Invoke(
                context,
                "run",
                Term.Tuple(Term.A("guarded"), Term.A("ignored"), result)
            );
            Term shortTuple = await image.Invoke(context, "run", Term.Tuple(Term.A("atom"), result));
            Term other = await image.Invoke(context, "run", result);
            Term key = Term.A("key");
            var map = new MapTerm([new(key, Term.I(1))]);
            Term keys = await image.Invoke(
                context,
                "run",
                Term.Tuple(Term.A("integer"), Term.A("ignored"), map)
            );
            Term member = await image.Invoke(
                context,
                "run",
                Term.Tuple(Term.A("var"), Term.A("ignored"), Term.A("marker"))
            );
            if (!result.Equals(guarded) || !result.Equals(shortTuple) || !result.Equals(other)
                || !keys.Equals(Term.List(key))
                || !member.Equals(Term.A("true")))
            {
                throw new InvalidOperationException(RemainderMismatch);
            }

            return Nil.Value;
        });
    }

    private static void VerifyInstructions(object code)
    {
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        object selector = instructions.First(instruction => Opcode(instruction) == BeamEmissionFormat.SelectValue);
        object structure = Property(code, "Structure");
        var atoms = (IReadOnlyList<string?>)Property(structure, "Atoms");
        var labels = (IReadOnlyDictionary<int, int>)Property(code, "Labels");
        var locations = (IReadOnlyList<Term>)Property(code, "SourceLocations");

        var operands = Operands(selector).Skip(3).ToArray();
        var targets = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int index = 0; index < operands.Length; index += 2)
        {
            targets.Add(atoms[Value(operands[index])]!, Value(operands[index + 1]));
        }

        if (targets.Count != 6 || !targets.TryGetValue("float", out int common)
            || targets["integer"] != common || targets["reserved_symbol"] != common
            || new[] { common, targets["char"], targets["atom"], targets["var"] }.Distinct().Count() != 4)
        {
            throw new InvalidOperationException(TargetsMismatch);
        }

        CheckLine(common, 4);
        CheckLine(targets["char"], 6);
        CheckLine(targets["atom"], 7);
        CheckLine(targets["var"], 8);

        void CheckLine(int label, int line)
        {
            var block = instructions.Skip(labels[label] + 1)
                .TakeWhile(instruction => Opcode(instruction) != BeamEmissionFormat.Label).ToArray();
            Term location = block is [.., var lineInstruction, var tail]
                && Opcode(tail) == BeamEmissionFormat.TailExternalCall
                && Opcode(lineInstruction) == BeamEmissionFormat.Line
                ? locations[Value(Operands(lineInstruction).Single())]
                : Nil.Value;
            if (!Cons.Items(location).Contains(Term.Tuple(Term.A("line"), Term.I(line))))
            {
                throw new InvalidOperationException(LocationMismatch);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Operands(object instruction) => ((IEnumerable)Property(instruction, "Operands")).Cast<object>();

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
