using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTupleAtomEntryChecks
{
    internal const string TestName = "compiler/beam/disjoint-tuple-atom-entry-partitions";
    private const string Module = "beam_tuple_atom_entry";
    private const int TupleTypeWireMask = 1 << 11;
    private const int MissingFallbackLine = 34;

    private const string Source = """
        -module(beam_tuple_atom_entry).
        -export([classify/1, contextual/2, guarded/1, overlapping/1, missing_fallback/1]).

        classify({job, Payload}) ->
            {accepted, Payload};
        classify(idle) ->
            paused;
        classify(Term) ->
            {rejected, Term}.

        contextual({job, Payload}, Ctx) ->
            {accepted, Payload, Ctx};
        contextual(idle, Ctx) ->
            {paused, Ctx};
        contextual(Term, Ctx) ->
            {rejected, Term, Ctx}.

        guarded({job, Payload}) when is_integer(Payload) ->
            {accepted, Payload};
        guarded(idle) ->
            paused;
        guarded(Term) ->
            {rejected, Term}.

        overlapping({job, Payload}) when is_integer(Payload) ->
            {accepted, Payload};
        overlapping(idle) ->
            paused;
        overlapping({job, Payload}) ->
            {other_job, Payload};
        overlapping(Term) ->
            {rejected, Term}.

        missing_fallback({job, Payload}) ->
            {accepted, Payload};
        missing_fallback(idle) ->
            paused.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        var code = Code(beam);
        AssertEntryPartition(code, "classify");
        AssertEntryPartition(code, "contextual");
        var image = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term payload = Term.A("payload");
            Term job = Term.Tuple(Term.A("job"), payload);
            Term wrongTag = Term.Tuple(Term.A("foreign"), payload);
            Term wrongArity = Term.Tuple(Term.A("job"), payload, payload);
            Term idle = Term.A("idle");
            await Check("classify", Term.Tuple(Term.A("accepted"), payload), job);
            await Check("classify", Term.A("paused"), idle);
            await Check("classify", Term.Tuple(Term.A("rejected"), wrongTag), wrongTag);
            await Check("classify", Term.Tuple(Term.A("rejected"), wrongArity), wrongArity);
            await Check("classify", Term.Tuple(Term.A("rejected"), payload), payload);
            await Check(
                "contextual",
                Term.Tuple(Term.A("accepted"), payload, idle),
                job,
                idle
            );
            await Check("guarded", Term.Tuple(Term.A("rejected"), job), job);
            await Check("overlapping", Term.Tuple(Term.A("other_job"), payload), job);

            try
            {
                await image.Invoke(context, "missing_fallback", wrongTag);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause"))
                && HasMissingFallbackFrame(error))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(BeamTupleAtomEntryDiagnostics.RejectionLocation);

            async ValueTask Check(string function, Term expected, params Term[] arguments)
            {
                if (!expected.Equals(await image.Invoke(context, function, arguments)))
                {
                    throw new InvalidOperationException(BeamTupleAtomEntryDiagnostics.BranchResult);
                }
            }
        });
    }

    private static bool HasMissingFallbackFrame(ErlangException error) =>
        error.StackTraceTerm is Cons
        {
            Head: TupleTerm { Items: [Atom { Name: Module }, Atom { Name: "missing_fallback" }, _, var source] }
        } && Cons.Items(source).Contains(Term.Tuple(Term.A("line"), Term.I(MissingFallbackLine)));

    private static void AssertEntryPartition(object code, string function)
    {
        object[] instructions = Items(code, "Instructions");
        string?[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string?>().ToArray();
        int info = Array.FindIndex(instructions, instruction =>
            Opcode(instruction) == FunctionInfo && atoms[(int)OperandValue(instruction, 1)] == function);
        if (info < 1 || Opcode(instructions[info - 1]) != Line
            || Opcode(instructions[info + 1]) != Label || Opcode(instructions[info + 2]) != IsTuple
            || Opcode(instructions[info + 3]) != IsTaggedTuple)
        {
            throw new InvalidOperationException(BeamTupleAtomEntryDiagnostics.EntryPartition);
        }

        object partition = instructions[info + 2];
        object tagged = instructions[info + 3];
        BigInteger atomLabel = OperandValue(partition, 0);
        BigInteger fallbackLabel = OperandValue(tagged, 0);
        int atomBranch = Array.FindIndex(
            instructions,
            info + 4,
            instruction => Opcode(instruction) == Label && OperandValue(instruction, 0) == atomLabel
        );
        if (atomBranch < 0 || atomLabel == fallbackLabel || Opcode(instructions[atomBranch + 1]) != ExactEqualTest
            || OperandValue(instructions[atomBranch + 1], 0) != fallbackLabel
            || Property(Items(tagged, "Operands")[1], "TypeIndex") is not int typeIndex
            || (int)Property(Items(Property(code, "Types"), "Entries")[typeIndex], "TypeUnion") != TupleTypeWireMask)
        {
            throw new InvalidOperationException(BeamTupleAtomEntryDiagnostics.EntryPartition);
        }
    }

    private static BigInteger OperandValue(object instruction, int index) =>
        (BigInteger)Property(Items(instruction, "Operands")[index], "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
