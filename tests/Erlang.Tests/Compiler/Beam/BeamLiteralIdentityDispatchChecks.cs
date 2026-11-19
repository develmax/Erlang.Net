using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamLiteralIdentityDispatchChecks
{
    internal const string TestName = "compiler/beam/selects-constant-branches-with-an-identity-fallback";
    private const string WrongDispatch = "Expected a value selection for distinct literal branches and an exact first match for duplicate keys.";
    private const string WrongResult = "The literal selection changed a matched value or the identity fallback.";
    private const string Source = """
        -module(beam_atom_literal_identity).
        -export([translate/1, guarded/1, duplicate/1, numbers/1]).

        translate(any) -> absent;
        translate(loopback) -> {local, <<>>};
        translate(IP) -> IP.

        guarded(any) -> absent;
        guarded(loopback) -> {local, <<>>};
        guarded(IP) when is_atom(IP) -> IP.

        duplicate(any) -> absent;
        duplicate(any) -> {local, <<>>};
        duplicate(IP) -> IP.

        numbers(1) -> absent;
        numbers(2) -> {local, <<>>};
        numbers(Number) -> Number.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_atom_literal_identity.erl");
        object code = Code(image);
        if (!HasOpcode(code, "translate", SelectValue) || !HasOpcode(code, "guarded", IsAtom)
            || HasOpcode(code, "duplicate", SelectValue) || !HasOpcode(code, "numbers", SelectValue))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        BeamModule beam = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term passthrough = Term.Tuple(Term.A("other"), Term.I(0));
            Term floatValue = new FloatTerm(1.0);
            if (!Term.A("absent").Equals(await beam.Invoke(context, "translate", Term.A("any")))
                || await beam.Invoke(context, "translate", Term.A("loopback")) is not
                    TupleTerm { Items: [Atom { Name: "local" }, BitString { BitLength: 0 }] }
                || !passthrough.Equals(await beam.Invoke(context, "translate", passthrough))
                || !Term.A("absent").Equals(await beam.Invoke(context, "duplicate", Term.A("any")))
                || await beam.Invoke(context, "numbers", floatValue) is not FloatTerm { Value: 1.0 })
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    private static bool HasOpcode(object code, string name, byte opcode)
    {
        object function = ((IEnumerable)Property(code, "Functions")).Cast<object>()
            .Single(item => (string)Property(item, "Name") == name);
        int first = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");

        return ((IEnumerable)Property(code, "Instructions")).Cast<object>().Skip(first).Take(end - first)
            .Any(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode") == opcode);
    }
}
