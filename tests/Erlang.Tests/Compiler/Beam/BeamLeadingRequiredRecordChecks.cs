using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamLeadingRequiredRecordChecks
{
    internal const string TestName = "compiler/beam/keeps-a-required-record-field-across-optional-joins";
    private const string Fixture = "beam_leading_required_record_probe.erl";
    private const string Decode = "decode";
    private const string InferredBody = "<<Number:8/signed>> = Data, Number";
    private const string IncorrectValue = "A bounded optional field changed a result, default or exit reason.";
    private const string IncorrectLayout = "One required field and proved optional fields should use the compact join.";

    private const string Source = """
        -module(beam_leading_required_record_probe).
        -export([decode/2]).

        decode(Input, Tags) ->
            Parsed = prepare(Input, Tags),
            [Head | Fields] = Parsed,
            Required = read_flag(Head, []),
            {Flag, Rest} = case Fields of
                [{1, FlagValue} | Tail1] -> {read_flag(FlagValue, []), Tail1};
                _ -> {false, Fields}
            end,
            {Limit, Extra} = case Rest of
                [{2, LimitValue} | Tail2] ->
                    {begin
                        Value = read_limit(LimitValue, []),
                        if
                            0 =< Value, Value =< 'MAX' -> Value;
                            true -> exit(bad_range)
                        end
                    end, Tail2};
                _ -> {missing, Rest}
            end,
            case Extra of
                [] -> true;
                _ -> exit({error, {asn1, {unexpected, Extra}}})
            end,
            Result = {bounds, Required, Flag, Limit},
            Result.

        prepare(Input, []) -> Input.
        read_flag(Value, []) -> Value.
        read_limit(Data, []) -> <<Number:8/signed>> = Data, Number.
        """;

    internal static async Task Run()
    {
        byte[] known = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] unknown = CodeGeneration.CompileBeamModule(
            Source.Replace(InferredBody, "Data", StringComparison.Ordinal), Fixture
        );
        var knownModule = BeamModule.Prepare(known);
        var unknownModule = BeamModule.Prepare(unknown);
        var required = Term.A("required");
        Term positive = Term.List(
            required,
            Term.Tuple(Term.I(1), Term.A("true")),
            Term.Tuple(Term.I(2), new BitString([4]))
        );
        Term expected = Term.Tuple(
            Term.A("bounds"),
            required,
            Term.A("true"),
            Term.I(4)
        );
        Term defaults = Term.Tuple(
            Term.A("bounds"),
            required,
            Term.A("false"),
            Term.A("missing")
        );
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!expected.Equals(await knownModule.Invoke(
                context,
                Decode,
                positive,
                Nil.Value
            ))
                || !defaults.Equals(await knownModule.Invoke(
                    context,
                    Decode,
                    Term.List(required),
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            try
            {
                await knownModule.Invoke(
                    context,
                    Decode,
                    Term.List(required, Term.Tuple(Term.I(2), new BitString([255]))),
                    Nil.Value
                );
                throw new InvalidOperationException(IncorrectValue);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("bad_range")))
            {
            }

            try
            {
                await unknownModule.Invoke(
                    context,
                    Decode,
                    Term.List(required, Term.Tuple(Term.I(2), Term.A("outside"))),
                    Nil.Value
                );
                throw new InvalidOperationException(IncorrectValue);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("bad_range")))
            {
            }

            try
            {
                await knownModule.Invoke(
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

        if (FrameSize(known) != 4 || FrameSize(unknown) <= 4)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }
    }

    private static int FrameSize(byte[] image)
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

        object allocate = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Single(instruction => Opcode(instruction) is Allocate or AllocateHeap);

        return Value(Items(allocate, "Operands")[0]);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
