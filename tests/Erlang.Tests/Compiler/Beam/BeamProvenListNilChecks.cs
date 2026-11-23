using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProvenListNilChecks
{
    internal const string TestName = "compiler/beam/types-nil-tests-on-proven-list-operands";
    private const string Fixture = "beam_proven_list_nil_probe.erl";
    private const string WrongType = "The nil test should have a typed operand only where a proper list is proven.";
    private const string WrongResult = "The nil test changed the accepted value or the rejected source.";
    private const string Source = """
        -module(beam_proven_list_nil_probe).
        -export([run/1, open/1, open_known/1, after_call/1, after_unknown/1]).

        run(Choice) -> closed(make(Choice), marker).
        open(Value) -> open_entry(Value, marker).
        open_known(Choice) -> open_entry(make(Choice), marker).

        closed([], Result) -> Result;
        closed(_, _) -> present.
        open_entry([], Result) -> Result;
        open_entry(_, _) -> present.

        after_call(Choice) ->
            case make(Choice) of
                [] -> missing;
                [_|_] -> present
            end.
        after_unknown(Choice) ->
            case maybe_list(Choice) of
                [] -> missing;
                [_|_] -> present
            end.

        make(empty) -> [];
        make(nonempty) -> [value].
        maybe_list(empty) -> [];
        maybe_list(other) -> invalid.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!TypedNil("closed") || !TypedNil("after_call") || TypedNil("open_entry") || TypedNil("after_unknown"))
        {
            throw new InvalidOperationException(WrongType);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term empty = Term.A("empty");
            Term nonempty = Term.A("nonempty");
            Term invalid = Term.A("invalid");
            Term marker = Term.A("marker");
            Term present = Term.A("present");
            if (!marker.Equals(await module.Invoke(context, "run", empty))
                || !present.Equals(await module.Invoke(context, "run", nonempty))
                || !marker.Equals(await module.Invoke(context, "open_known", empty))
                || !present.Equals(await module.Invoke(context, "open", invalid))
                || !Term.A("missing").Equals(await module.Invoke(context, "after_call", empty))
                || !present.Equals(await module.Invoke(context, "after_call", nonempty)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await module.Invoke(context, "after_unknown", Term.A("other"));
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("case_clause"), invalid)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });

        bool TypedNil(string function)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == function
            );
            if (entry < 0)
            {
                throw new InvalidOperationException(WrongType);
            }

            object instruction = instructions.Skip(entry + 1)
                .TakeWhile(operation => Opcode(operation) != FunctionInfo)
                .First(operation => Opcode(operation) == IsNil);
            object tested = Items(instruction, "Operands").ElementAt(1);

            return tested.GetType().GetProperty("TypeIndex")!.GetValue(tested) is int;
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
