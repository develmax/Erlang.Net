using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRecordFieldListGeneratorChecks
{
    internal const string TestName = "compiler/beam/lifts-list-generators-from-checked-record-fields";
    private const string Fixture = "beam_record_field_list_generator_probe.erl";
    private const string MissingHelper = "The checked record field was not passed to a recursive list-comprehension helper.";
    private const string InvalidValue = "Lifting the record-field generator changed a result or filter.";

    internal const string Source = """
        -module(beam_record_field_list_generator_probe).
        -export([pair/1, filtered/2, ordered/2]).
        -record(bucket, {rows, other}).

        pair(Bucket) -> [{row, Item} || Item <- Bucket#bucket.rows].
        filtered(Bucket, Skipped) -> [Item || Item <- Bucket#bucket.rows, Item =/= Skipped].
        ordered(Bucket, Target) -> [mark(Item, Target) || Item <- Bucket#bucket.rows].

        mark(Item, Target) ->
            Target ! {seen, Item},
            {item, Item}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!UsesHelper("pair", 1, 1) || !UsesHelper("filtered", 2, 2) || !UsesHelper("ordered", 2, 2))
        {
            throw new InvalidOperationException(MissingHelper);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term bucket = Term.Tuple(Term.A("bucket"), Term.List(first, second, first), Term.A("ignored"));
            Term pairs = Term.List(
                Term.Tuple(Term.A("row"), first),
                Term.Tuple(Term.A("row"), second),
                Term.Tuple(Term.A("row"), first)
            );
            if (!pairs.Equals(await module.Invoke(context, "pair", [bucket]))
                || !Term.List(second).Equals(await module.Invoke(context, "filtered", [bucket, first])))
            {
                throw new InvalidOperationException(InvalidValue);
            }

            return Nil.Value;
        });

        bool UsesHelper(string name, int arity, int helperArity)
        {
            string helper = FormattableString.Invariant($"-{name}/{arity}-lc$^0/1-0-");
            int body = FindFunction(name);
            int definition = FindFunction(helper);
            if (body < 0 || definition < 0 || Opcode(instructions[definition + 1]) != Label
                || Value(Operands(instructions[definition])[2]) != helperArity)
            {
                return false;
            }

            int target = Value(Operands(instructions[definition + 1])[0]);
            object[] operations = instructions.Skip(body + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();

            return operations.Count(instruction => Opcode(instruction) == GetTupleElement) == 1
                && operations.Any(instruction => Opcode(instruction) is LocalCall or TailLocalCall or TailLocalCallWithFrame
                    && Value(Operands(instruction)[1]) == target);
        }

        int FindFunction(string name) => Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && Operands(instruction) is [_, var atom, _] && atoms[Value(atom)] == name
        );
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
