using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOrderedListPayloadChecks
{
    internal const string TestName = "compiler/beam/defers-checked-list-body-payload";
    private const string Module = "beam_ordered_list_payload_probe";
    private const string WrongSchedule = "The list arm must read its payload only after the closing tuple has matched.";
    private const string WrongResult = "The ordered list arm changed a result or the original retry input.";
    private const string Source = """
        -module(beam_ordered_list_payload_probe).
        %% options
        -export([scan/1]).
        scan([{open, Name}, {value, Payload}, {close, _} | Tail]) ->
            Constant = {constant, kind, vertex, {Payload}},
            [{vertex, Name, Constant} | scan(Tail)];
        scan([{single, Name, Payload} | Tail]) ->
            [{single, Name, Payload} | scan(Tail)];
        scan([Token | Tail]) ->
            [Token | scan(Tail)];
        scan([]) -> [].
        """;

    internal static async Task Run()
    {
        byte[] optimized = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        var unoptimized = new Parser(Source.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal)).ParseModule();
        var locations = Enumerable.Range(4, unoptimized.DeclaredFunctions.Count)
            .Select(line => new BeamSourceLocation(null, line)).ToArray();
        byte[] disabled = new BeamModuleCompiler(unoptimized.Name, sourceModule: unoptimized).Emit(
            unoptimized.DeclaredFunctions,
            locations,
            unoptimized.DeclaredExports.ToHashSet(),
            new BeamModuleMetadata(unoptimized.Attributes, null)
        );
        string guardSource = Source.Replace(
            "{close, _} | Tail]) ->",
            "{close, _} | Tail]) when Payload =:= value ->",
            StringComparison.Ordinal
        );
        byte[] guarded = CodeGeneration.CompileBeamModule(guardSource, Module + ".erl");
        if (!HasClosingPayload(optimized) || HasClosingPayload(disabled) || HasClosingPayload(guarded))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        Term name = Term.A("name");
        Term payload = Term.A("value");
        Term open = Term.Tuple(Term.A("open"), name);
        Term value = Term.Tuple(Term.A("value"), payload);
        Term close = Term.Tuple(Term.A("close"), Term.I(1));
        Term wrongTag = Term.Tuple(Term.A("other"), Term.I(1));
        Term wrongArity = Term.Tuple(Term.A("close"), Term.I(1), Term.I(2));
        Term single = Term.Tuple(Term.A("single"), name, payload);
        Term constant = Term.Tuple(
            Term.A("constant"),
            Term.A("kind"),
            Term.A("vertex"),
            Term.Tuple(payload)
        );
        Term expected = Term.List(Term.Tuple(Term.A("vertex"), name, constant), single);
        var module = BeamModule.Prepare(optimized);
        var baseline = BeamModule.Prepare(disabled);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term full = Term.List(
                open,
                value,
                close,
                single
            );
            Term mismatched = Term.List(
                open,
                value,
                wrongTag,
                single
            );
            Term malformed = Term.List(
                open,
                value,
                wrongArity,
                single
            );
            Term shortInput = Term.List(open, value);
            foreach (var (input, outcome) in new[]
            {
                (full, expected),
                (mismatched, mismatched),
                (malformed, malformed),
                (shortInput, shortInput)
            })
            {
                if (!(await module.Invoke(context, "scan", input)).Equals(outcome)
                    || !(await baseline.Invoke(context, "scan", input)).Equals(outcome))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }

            return Nil.Value;
        });
    }

    private static bool HasClosingPayload(byte[] beam)
    {
        object[] instructions = Items(Code(beam), "Instructions");
        for (int index = 1; index + 1 < instructions.Length; index++)
        {
            if (Opcode(instructions[index - 1]) != GetList
                || Opcode(instructions[index]) != IsTaggedTuple
                || Opcode(instructions[index + 1]) != GetTupleElement
                || Items(instructions[index - 1], "Operands") is not [_, var checkedHead, _]
                || Items(instructions[index], "Operands") is not [_, var tested, _, _]
                || Items(instructions[index + 1], "Operands") is not [var carrier, _, var result])
            {
                continue;
            }

            if (Value(checkedHead) == Value(tested) && Value(tested) == Value(result)
                && Value(carrier) != Value(result))
            {
                return true;
            }
        }

        return false;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
