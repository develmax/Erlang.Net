using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamInheritedResultCaseChecks
{
    internal const string TestName = "compiler/beam/plans-nested-result-in-inherited-frame";
    private const string Fixture = "beam_inherited_result_case_probe.erl";
    private const string WrongSchedule = "The nested result must keep its checked payload in X while the caller frame stays open.";
    private const string WrongResult = "The nested result changed the branch result or the malformed case reason.";
    private const string Source = """
        -module(beam_inherited_result_case_probe).
        %% compile options
        -export([run/3]).
        run(Primary, Backup, Saved) ->
            case first(Primary, Saved) of
                {value, {_, Value}} -> {hit, Value};
                false ->
                    case second(Backup, Saved) of
                        {value, {_, PairValue}} -> {hit, PairValue};
                        false -> last(Saved)
                    end
            end.
        first(Input, _) -> Input.
        second({backup, Input}, _) -> Input;
        second(Input, _) -> Input.
        last(Saved) -> {fallback, Saved}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        var unoptimized = new Parser(Source.Replace("%% compile options", "-compile(no_ssa_opt).", StringComparison.Ordinal)).ParseModule();
        var locations = Enumerable.Range(4, unoptimized.DeclaredFunctions.Count)
            .Select(line => new BeamSourceLocation(null, line)).ToArray();
        byte[] disabled = new BeamModuleCompiler(unoptimized.Name, sourceModule: unoptimized).Emit(
            unoptimized.DeclaredFunctions,
            locations,
            unoptimized.DeclaredExports.ToHashSet(),
            new BeamModuleMetadata(unoptimized.Attributes, null)
        );
        if (!unoptimized.HasOtherCompileOptions || !HasInheritedCheckedPair(image) || HasInheritedCheckedPair(disabled))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        var generated = BeamModule.Prepare(image);
        var control = BeamModule.Prepare(disabled);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term no = Term.A("false");
            Term saved = Term.A("saved");
            Term value = Term.A("wanted");
            Term found = Term.Tuple(Term.A("value"), Term.Tuple(Term.I(9), value));
            Term hit = Term.Tuple(Term.A("hit"), value);
            Term fallback = Term.Tuple(Term.A("fallback"), saved);
            Term malformed = Term.Tuple(Term.A("value"), Term.I(7));
            foreach (BeamModule module in new[] { generated, control })
            {
                if (!(await module.Invoke(
                    context,
                    "run",
                    found,
                    malformed,
                    saved
                )).Equals(hit) || !(await module.Invoke(
                    context,
                    "run",
                    no,
                    Term.Tuple(Term.A("backup"), found),
                    saved
                )).Equals(hit) || !(await module.Invoke(
                    context,
                    "run",
                    no,
                    no,
                    saved
                )).Equals(fallback))
                {
                    throw new InvalidOperationException(WrongResult);
                }

                try
                {
                    _ = await module.Invoke(
                        context,
                        "run",
                        no,
                        malformed,
                        saved
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), malformed)))
                {
                    continue;
                }

                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    private static bool HasInheritedCheckedPair(byte[] image)
    {
        object code = Code(image);
        string?[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string?>().ToArray();
        object instructions = Property(code, "Instructions");
        object[] all = ((IEnumerable)instructions).Cast<object>().ToArray();
        int run = FunctionIndex("run");
        int first = FunctionIndex("first");
        int second = FunctionIndex("second");
        if (run < 0 || first < 0 || second < 0 || first + 1 >= all.Length || second + 1 >= all.Length
            || Opcode(all[first + 1]) != Label || Opcode(all[second + 1]) != Label)
        {
            return false;
        }

        var firstEntry = Value(Items(all[first + 1], "Operands")[0]);
        var secondEntry = Value(Items(all[second + 1], "Operands")[0]);
        int start = run + 1;
        int end = Array.FindIndex(all, start, instruction => Opcode(instruction) == FunctionInfo);
        if (end < 0 || Opcode(all[start]) != Label || Call(
            typeof(BeamModuleCompiler),
            "ReadDisjointFrameOwnership",
            instructions,
            start,
            end,
            false,
            -1,
            true,
            false
        ) is not ValueTuple<int[], Dictionary<int, bool[]>> ownership)
        {
            return false;
        }

        int firstCall = Array.FindIndex(
            all,
            start,
            end - start,
            instruction => IsCall(instruction, firstEntry)
        );
        int secondCall = Array.FindIndex(
            all,
            start,
            end - start,
            instruction => IsCall(instruction, secondEntry)
        );
        if (firstCall < 0 || secondCall <= firstCall || ownership.Item1[firstCall - start] < start
            || ownership.Item1[firstCall - start] != ownership.Item1[secondCall - start])
        {
            return false;
        }

        object[] body = all[start..end].Where(instruction => Opcode(instruction) != Line).ToArray();
        int selector = Array.FindIndex(body, instruction => IsCall(instruction, secondEntry));
        if (selector < 0 || selector + 6 >= body.Length)
        {
            return false;
        }

        // Bind the checked region to the second selector call, not to a
        // preceding tuple case or a later function sharing the same layout.
        if (Opcode(body[selector + 1]) != IsTuple || Opcode(body[selector + 2]) != IsTaggedTuple
            || Opcode(body[selector + 3]) != GetTupleElement
            || Items(body[selector + 3], "Operands") is not [var outer, var offset, var pair]
            || !IsX(outer, 0) || !IsX(pair, 1) || Value(offset) != 1
            || Opcode(body[selector + 4]) != IsTuple || Opcode(body[selector + 5]) != TestArity)
        {
            return false;
        }

        var failure = Value(Items(body[selector + 2], "Operands")[0]);
        if (failure != Value(Items(body[selector + 4], "Operands")[0])
            || failure != Value(Items(body[selector + 5], "Operands")[0]))
        {
            return false;
        }

        int projection = selector + 6;
        if (Opcode(body[projection]) == TestHeap)
        {
            projection++;
        }

        int rejected = Array.FindIndex(body, instruction => Opcode(instruction) == Label
            && Value(Items(instruction, "Operands")[0]) == failure);

        return projection < body.Length && rejected >= 0 && rejected + 1 < body.Length
            && Opcode(body[projection]) == GetTupleElement
            && Items(body[projection], "Operands") is [var source, var payload, var result]
            && IsX(source, 1) && IsX(result, 0) && Value(payload) == 1
            && Opcode(body[rejected + 1]) == CaseEnd && IsX(Items(body[rejected + 1], "Operands")[0], 0);

        static bool IsCall(object instruction, BigInteger entry) => Opcode(instruction) == LocalCall
            && Value(Items(instruction, "Operands")[1]) == entry;

        int FunctionIndex(string name)
        {
            return Array.FindIndex(all, instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Items(instruction, "Operands")[1])] == name);
        }
    }

    private static bool IsX(object operand, int index) => Property(operand, "Kind") is "x" && Value(operand) == index;

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string field) => ((IEnumerable)Property(owner, field)).Cast<object>().ToArray();
}
