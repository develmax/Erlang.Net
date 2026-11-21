using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamResolvedTransformReachabilityChecks
{
    internal const string TestName = "compiler/beam/retains-reachable-entries-after-resolved-transforms";
    private const string Fixture = "beam_resolved_transform_reachability_probe.erl";
    private const string Registration = "-export([direct/1, make/0, make_anon/0, observed/0]).";
    private const string UnexpectedReachability = "Expected resolved-transform code to retain only provable callable entries.";

    internal const string Source = """
        -module(beam_resolved_transform_reachability_probe).
        -compile({parse_transform,diameter_exprecs}).
        -compile(nowarn_unused_function).
        -export_records([]).
        -on_load(mark/0).
        -export([direct/1, make/0, make_anon/0, observed/0]).

        direct(Value) -> local(Value).
        make() -> fun held/1.
        make_anon() -> fun(Value) -> captured(Value) end.
        observed() -> persistent_term:get({?MODULE,loaded}).
        mark() -> load_state().
        load_state() -> persistent_term:put({?MODULE,loaded},true).
        local(Value) -> {local,Value}.
        held(Value) -> {held,Value}.
        captured(Value) -> {captured,Value}.
        unused(Value) -> unused_cycle(Value).
        unused_cycle(Value) -> unused(Value).
        unused_closure() -> fun never_called/0.
        never_called() -> unreachable.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static void Run()
    {
        var reachable = FunctionNames(CompileFixture());
        string opaque = Source.Replace(
            Registration,
            "-export([direct/1, make/0, make_anon/0, observed/0, opaque/1]).",
            StringComparison.Ordinal
        ) + "\nopaque(Name) -> ?MODULE:Name().\n";
        var opaqueEntries = FunctionNames(CodeGeneration.CompileBeamModule(opaque, Fixture));
        string declaredSource = Source.Replace("-on_load(mark/0).", "", StringComparison.Ordinal)
            .Replace("?MODULE", "beam_resolved_transform_reachability_probe", StringComparison.Ordinal);
        var declared = new Parser(declaredSource).ParseModule().DeclaredFunctions;
        string[] dead = ["unused", "unused_cycle", "unused_closure", "never_called"];
        string[] live = ["local", "held", "captured", "mark", "load_state"];
        if (dead.Any(reachable.Contains) || live.Any(name => !reachable.Contains(name))
            || dead.Any(name => !opaqueEntries.Contains(name))
            || dead.Any(name => !declared.Any(function => function.Name == name)))
        {
            throw new InvalidOperationException(UnexpectedReachability);
        }
    }

    private static HashSet<string> FunctionNames(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();

        return Items(code, "Instructions").Where(instruction => Opcode(instruction) == FunctionInfo)
            .Select(instruction => atoms[Value(Items(instruction, "Operands").ElementAt(1))])
            .ToHashSet(StringComparer.Ordinal);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
