using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSharedEntryRecordChecks
{
    internal const string TestName = "compiler/beam/shares-entry-record-checks-with-a-field-rejection";
    private const string Fixture = "beam_shared_entry_record_check.erl";
    private const string SharedCheck = "A common record test must guard both clauses and branch directly to function_clause.";
    private const string SharedFields = "The later clause must reuse the two unchanged projections.";
    private const string DifferentRecords = "Distinct record tags must retain their own matching clauses.";
    internal const string Source = """
        -module(beam_shared_entry_record_check).
        -export([pick/1, different/1, with_call/1]).

        -record(entry, {id, file, label}).
        -record(packet, {id, file, label}).

        pick(#entry{id = Id, file = undefined}) ->
            {simple, Id};
        pick(#entry{id = Id, file = File, label = Label}) ->
            {Id, File, Label}.

        different(#entry{id = Id, file = undefined}) ->
            {simple, Id};
        different(#packet{id = Id, file = File, label = Label}) ->
            {Id, File, Label}.

        with_call(#entry{id = Id, file = undefined}) ->
            _ = erlang:get(Id),
            erlang:erase(Id);
        with_call(#entry{id = Id, file = File, label = Label}) ->
            _ = erlang:get(Label),
            erlang:put(Id, {File, Label}).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        CheckShared("pick");
        CheckShared("with_call");
        var (_, different) = ReadFunction("different");
        var tags = different.SelectMany(CheckedAtoms)
            .Where(operand => (string)Property(operand, "Kind") == BeamBinaryHostNames.AtomKind)
            .Select(operand => atoms[(int)Value(operand)]).ToHashSet(StringComparer.Ordinal);
        if (!tags.Contains("entry") || !tags.Contains("packet"))
        {
            throw new InvalidOperationException(DifferentRecords);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term id = Term.A("id");
            Term file = Term.A("path");
            Term label = Term.A("label");
            Term undefined = Term.A("undefined");
            Term simple = Term.Tuple(Term.A("simple"), id);
            if (!simple.Equals(await module.Invoke(context, "different", Term.Tuple(
                Term.A("entry"),
                id,
                undefined,
                label
            )))
                || !Term.Tuple(id, file, label).Equals(await module.Invoke(context, "different", Term.Tuple(
                    Term.A("packet"),
                    id,
                    file,
                    label
                ))))
            {
                throw new InvalidOperationException(DifferentRecords);
            }

            await Reject(Term.Tuple(
                Term.A("entry"),
                id,
                file,
                label
            ));
            await Reject(Term.Tuple(
                Term.A("other"),
                id,
                undefined,
                label
            ));

            return Nil.Value;

            async Task Reject(Term value)
            {
                try
                {
                    await module.Invoke(context, "different", value);
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.A("function_clause")))
                {
                    return;
                }

                throw new InvalidOperationException(DifferentRecords);
            }
        });

        void CheckShared(string name)
        {
            var (entry, shared) = ReadFunction(name);
            object failureEntry = instructions.Take(entry).Last(instruction => Opcode(instruction) == Label);
            object[] tests = shared.Where(instruction => Opcode(instruction) == IsTaggedTuple).ToArray();
            if (tests.Length != 1 || Value(Operands(tests[0])[0]) != Value(Operands(failureEntry)[0]))
            {
                throw new InvalidOperationException(SharedCheck);
            }

            int[] fields = shared.Where(instruction => Opcode(instruction) == GetTupleElement)
                .Select(instruction => (int)Value(Operands(instruction)[1])).ToArray();
            if (fields.Count(field => field == 1) != 1 || fields.Count(field => field == 2) != 1)
            {
                throw new InvalidOperationException(SharedFields);
            }
        }

        (int Entry, object[] Body) ReadFunction(string name)
        {
            int entry = Array.FindIndex(instructions, instruction =>
                Opcode(instruction) == FunctionInfo && atoms[(int)Value(Operands(instruction)[1])] == name);
            if (entry < 0)
            {
                throw new InvalidOperationException(SharedCheck);
            }

            object[] body = instructions.Skip(entry + 1)
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();

            return (entry, body);
        }
    }

    private static IEnumerable<object> CheckedAtoms(object instruction)
    {
        object[] operands = Operands(instruction);

        return Opcode(instruction) switch
        {
            IsTaggedTuple => [operands[3]],
            ExactEqualTest => operands.Skip(1),
            SelectValue => operands.Skip(3).Where((_, index) => index % 2 == 0),
            _ => []
        };
    }

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Operands(object instruction) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");
}
