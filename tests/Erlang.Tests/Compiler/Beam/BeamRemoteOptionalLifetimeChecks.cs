using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRemoteOptionalLifetimeChecks
{
    internal const string TestName = "compiler/beam/keeps-remote-optional-fields-through-required-decode";
    private const string Fixture = "beam_remote_optional_probe.erl";
    private const string Decode = "decode";
    private const string Remote = "beam_remote_field_fixture";
    private const string Read = "read";
    private const string IncorrectValue = "The static remote call changed the field, tail, or exceptional result.";
    private const string IncorrectLayout = "Only literal remote fields should use the compact optional-before-required frame.";

    private const string Source = """
        -module(beam_remote_optional_probe).
        -export([decode/2]).

        decode(Input, Tags) ->
            Fields = prepare(Input, Tags),
            {Opt, Pending} = case Fields of
                [{7, Value} | Rest] -> {beam_remote_field_fixture:read(Value, optional), Rest};
                _ -> {absent, Fields}
            end,
            [Required | Extra] = Pending,
            Last = beam_remote_field_fixture:read(Required, required),
            case Extra of
                [] -> true;
                _ -> exit({error, {asn1, {unexpected, Extra}}})
            end,
            Result = {observed, Opt, Last},
            Result.

        prepare(Input, _) -> Input.
        """;

    internal static async Task Run()
    {
        byte[] literal = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] runtimeArguments = CodeGeneration.CompileBeamModule(
            Source.Replace("Value, optional", "Value, Tags", StringComparison.Ordinal), Fixture
        );
        var dependencies = new BeamDependencySelection([Remote]);
        var literalModule = BeamModule.Prepare(literal, dependencies);
        var dynamicModule = BeamModule.Prepare(runtimeArguments, dependencies);
        Term input = Term.List(Term.Tuple(Term.I(7), Term.I(41)), Term.I(42));
        Term record = Term.A("observed");
        Term required = Term.Tuple(Term.I(42), Term.A("required"));
        Term known = Term.Tuple(record, Term.Tuple(Term.I(41), Term.A("optional")), required);
        Term runtimeTag = Term.A("runtime");
        Term unproved = Term.Tuple(record, Term.Tuple(Term.I(41), runtimeTag), required);
        Term absent = Term.Tuple(record, Term.A("absent"), required);
        await using var runtime = new ProcessRuntime();
        runtime.Modules.Register(
            Remote,
            Read,
            2,
            (_, arguments) => ValueTask.FromResult<Term>(Term.Tuple(arguments[0], arguments[1]))
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!known.Equals(await literalModule.Invoke(
                context,
                Decode,
                input,
                Nil.Value
            ))
                || !unproved.Equals(await dynamicModule.Invoke(
                    context,
                    Decode,
                    input,
                    runtimeTag
                ))
                || !absent.Equals(await literalModule.Invoke(
                    context,
                    Decode,
                    Term.List(Term.I(42)),
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            try
            {
                await literalModule.Invoke(
                    context,
                    Decode,
                    Nil.Value,
                    Nil.Value
                );
                throw new InvalidOperationException(IncorrectValue);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), Nil.Value)))
            {
            }

            return Nil.Value;
        });

        if (Frame(literal) != 2 || Frame(runtimeArguments) <= 2)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }
    }

    private static int Frame(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == Decode && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object frame = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Single(instruction => Opcode(instruction) == Allocate);

        return Value(Items(frame, "Operands")[0]);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
