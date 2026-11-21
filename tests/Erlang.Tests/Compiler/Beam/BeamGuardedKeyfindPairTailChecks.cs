using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedKeyfindPairTailChecks
{
    internal const string TestName = "compiler/beam/trims-keyfind-frames-after-a-pair-match";
    private const string Fixture = "beam_guarded_keyfind_pair_tail_probe.erl";
    private const string IncorrectLayout = "The successful pair lookup did not release the saved key before the next call.";
    private const string IncorrectOutcome = "Key lookup, direct forwarding or the missing-key exit changed.";

    private const string Source = """
        -module(beam_guarded_keyfind_pair_tail_probe).
        -export([encode/3]).
        encode(Value, Named, Tag) when is_atom(Value) ->
            case lists:keyfind(Value, 1, Named) of
                {_, Result} ->
                    finish(Tag, read(Result));
                _ ->
                    exit({error, {lookup, {unnamed, Value}}})
            end;
        encode(Value, _Named, Tag) ->
            finish(Tag, read(Value)).
        read(Value) -> {converted, Value}.
        finish(Tag, Value) -> {Tag, Value}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                && atoms[Value(atom)] == "encode" && Value(arity) == 3
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        object[] trims = body.Where(instruction => Opcode(instruction) == TrimFrame).ToArray();
        byte[] setup = body.Select(Opcode).Where(opcode => opcode is IsAtom or Allocate or InitYRegisters).ToArray();
        if (trims is not [var trim] || Items(trim, "Operands").Select(Value).ToArray() is not [1, 1]
            || setup is not [IsAtom, Allocate, Allocate])
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term tag = Term.A("tag");
            Term name = Term.A("name");
            Term value = Term.I(5);
            Term expected = Term.Tuple(tag, Term.Tuple(Term.A("converted"), value));
            if (!expected.Equals(await module.Invoke(
                context,
                "encode",
                name,
                Term.List(Term.Tuple(name, value)),
                tag
            ))
                || !expected.Equals(await module.Invoke(
                    context,
                    "encode",
                    value,
                    Term.A("unused"),
                    tag
                )))
            {
                throw new InvalidOperationException(IncorrectOutcome);
            }

            await CheckMissing(Nil.Value);
            await CheckMissing(Term.List(Term.Tuple(name, value, Term.A("extra"))));

            return Nil.Value;

            async Task CheckMissing(Term entries)
            {
                Term reason = Term.Tuple(Term.A("error"), Term.Tuple(Term.A("lookup"), Term.Tuple(Term.A("unnamed"), name)));
                try
                {
                    _ = await module.Invoke(
                        context,
                        "encode",
                        name,
                        entries,
                        tag
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Exit && error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(IncorrectOutcome);
            }
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
