using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamForwardedChoiceMeasurementChecks
{
    internal const string TestName = "compiler/beam/keeps-forwarded-choice-measurements-in-registers";
    private const string Fixture = "beam_forwarded_choice_measurement_probe.erl";
    private const string Entry = "entry";
    private const string Select = "select";
    private const string Proof = " when is_binary(V)";
    private const string IncorrectValue = "A measured choice changed its result or error.";
    private const string IncorrectLayout = "Only measured choices with proved result types may use the zero-slot frame.";

    private const string Source = """
        -module(beam_forwarded_choice_measurement_probe).
        -export([entry/1]).

        entry(Value) ->
            try select(Value, []) catch
                exit:{error, Reason} -> {rejected, Reason};
                error:badarg -> badarg
            end.

        select(Value, Tags) ->
            Found = prepare(Value, Tags),
            case (case Found of [Only] -> Only; _ -> Found end) of
                {1, Binary} ->
                    {bytes, begin
                        Result = read_binary(Binary, []),
                        Size = byte_size(Result),
                        if
                            1 =< Size, Size =< 3 -> binary_to_list(Result);
                            true -> exit({error, byte_range})
                        end
                    end};
                {2, List} ->
                    {items, begin
                        Result = read_list(List, []),
                        Size = length(Result),
                        if
                            0 =< Size, Size =< 2 -> Result;
                            true -> exit({error, list_range})
                        end
                    end};
                Other -> exit({error, {choice, Other}})
            end.

        prepare(Value, []) -> Value.
        read_binary(V, []) when is_binary(V) -> V.
        read_list(V, []) -> lists:reverse(V).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] unknown = CodeGeneration.CompileBeamModule(Source.Replace(Proof, string.Empty, StringComparison.Ordinal), Fixture);
        var dependencies = new BeamDependencySelection(["lists"]);
        var module = BeamModule.Prepare(image, dependencies);
        var unproved = BeamModule.Prepare(unknown, dependencies);
        Term bytes = Term.I(1);
        Term items = Term.I(2);
        Term expected = Term.Tuple(Term.A("bytes"), Term.List(Term.I(65), Term.I(66)));
        Term badChoice = Term.A("unmatched");
        Term rejected = Term.A("rejected");

        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!expected.Equals(await module.Invoke(context, Entry, Term.List(Term.Tuple(bytes, new BitString([65, 66])))))
                || !Term.Tuple(Term.A("items"), Nil.Value).Equals(await module.Invoke(context, Entry, Term.Tuple(items, Nil.Value)))
                || !Term.Tuple(rejected, Term.A("byte_range")).Equals(
                    await module.Invoke(context, Entry, Term.Tuple(bytes, new BitString([]))))
                || !Term.Tuple(rejected, Term.A("list_range")).Equals(
                    await module.Invoke(context, Entry, Term.Tuple(items, Term.List(Term.I(1), Term.I(2), Term.I(3)))))
                || !Term.Tuple(rejected, Term.Tuple(Term.A("choice"), badChoice)).Equals(
                    await module.Invoke(context, Entry, badChoice))
                || !Term.A("badarg").Equals(await unproved.Invoke(context, Entry, Term.Tuple(bytes, badChoice))))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            return Nil.Value;
        });

        if (FrameSize(image) != 0 || FrameSize(unknown) == 0)
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
                && atoms[Value(name)] == Select && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        object allocation = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .First(instruction => Opcode(instruction) == Allocate);

        return Value(Items(allocation, "Operands")[0]);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
