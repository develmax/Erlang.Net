using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTupleFieldLiteralRetryChecks
{
    internal const string TestName = "compiler/beam/retains-a-tuple-field-on-exclusive-literal-and-nil-retries";
    private const string WrongPath = "The tuple field must reach the retry unchanged from both admitted tests.";
    private const int FunctionInfoLabel = 1;
    private const int EntryLabel = 2;
    private const int RetryLabel = 3;
    private const int RejectLabel = 4;
    private const int LabelCount = 5;
    private const int Arity = 2;

    internal static async Task Run()
    {
        CheckSourceEntry();
        CheckStableProjectionRetry();
        CheckInterleavedStableProjectionRetry();
        byte[] original = Fixture();
        if (Rewrite(original) is not { } rewritten || !rewritten.SequenceEqual(Fixture(shared: true))
            || Rewrite(Fixture(extraPredecessor: true)) is not null
            || Rewrite(Fixture(overwrite: true)) is not null)
        {
            throw new InvalidOperationException(WrongPath);
        }

        var before = BeamModule.Prepare(Image(original[..^1], labels: LabelCount, arity: Arity));
        var after = BeamModule.Prepare(Image(rewritten[..^1], labels: LabelCount, arity: Arity));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term accepted = Term.I(7);
            Term rejected = Term.I(8);
            Term nonempty = Term.List(rejected);
            var cases = new (Term Input, Term State, Term Expected)[]
            {
                (Term.Tuple(rejected, accepted), Nil.Value, Term.A(BeamOpcodeEdgeNames.Module)),
                (Term.Tuple(rejected, rejected), Nil.Value, rejected),
                (Term.Tuple(rejected, accepted), nonempty, accepted),
                (Term.Tuple(rejected), nonempty, nonempty)
            };
            foreach (var (input, state, expected) in cases)
            {
                Term prior = await before.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    input,
                    state
                );
                Term current = await after.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    input,
                    state
                );
                if (!expected.Equals(prior) || !expected.Equals(current))
                {
                    throw new InvalidOperationException(WrongPath);
                }
            }

            return Nil.Value;
        });
    }

    private static void CheckStableProjectionRetry()
    {
        var compiler = new BeamModuleCompiler("beam_stable_retry_fixture");
        _ = Call(compiler, "Intern", "stable_retry_tag");
        byte[] checkedTuple =
        [
            .. Start(
                FunctionInfoLabel,
                EntryLabel,
                2,
                Arity
            ), IsTuple, F(RejectLabel), X(0), TestArity, F(RejectLabel), X(0), U(3)
        ];
        byte[] fields = [GetTupleElement, X(0), U(1), X(2), GetTupleElement, X(0), U(2), X(3)];
        byte[] guard =
        [
            IsNonemptyList, F(RetryLabel), X(1), GetList, X(1), X(4), X(5),
            IsTaggedTuple, F(RetryLabel), X(4), U(2), A(2), IsNil, F(RetryLabel), X(5),
            Move, X(2), X(0), Return, Label, U(RetryLabel)
        ];
        byte[] continuation = [Move, X(3), X(0), Return, Label, U(RejectLabel), Move, X(1), X(0), Return, EndCode];
        byte[] input = [.. checkedTuple, .. fields, .. guard, .. fields, .. continuation];
        byte[] expected = [.. checkedTuple, .. fields, .. guard, .. continuation];
        byte[] otherPredecessor = [.. checkedTuple, IsAtom, F(RetryLabel), X(1), .. fields, .. guard, .. fields, .. continuation];
        byte[] overwritten = [.. checkedTuple, .. fields, Move, X(1), X(2), .. guard, .. fields, .. continuation];
        byte[] collects = [.. checkedTuple, .. fields, TestHeap, U(2), U(4), .. guard, .. fields, .. continuation];
        if (Apply(input) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Apply(otherPredecessor) is not null || Apply(overwritten) is not null || Apply(collects) is not null)
        {
            throw new InvalidOperationException(WrongPath);
        }

        byte[]? Apply(byte[] bytes) => (byte[]?)Call(
            compiler,
            "ReuseStableRetryTupleProjections",
            (ReadOnlyMemory<byte>)bytes,
            LabelCount
        );
    }

    private static void CheckInterleavedStableProjectionRetry()
    {
        var compiler = new BeamModuleCompiler("beam_interleaved_stable_retry");
        _ = Call(compiler, "Intern", "stable_retry_tag");
        _ = Call(compiler, "Intern", "stable_retry_field");
        byte[] prefix =
        [
            .. Start(
                FunctionInfoLabel,
                EntryLabel,
                2,
                Arity
            ),
            IsTaggedTuple, F(RejectLabel), X(0), U(3), A(2)
        ];
        byte[] first = [GetTupleElement, X(0), U(1), X(2)];
        byte[] second = [GetTupleElement, X(0), U(2), X(3)];
        byte[] guard = [ExactEqualTest, F(RetryLabel), X(3), I(7), Move, X(2), X(0), Return, Label, U(RetryLabel)];
        byte[] literal = [ExactEqualTest, F(RejectLabel), X(2), A(3)];
        byte[] end = [ExactEqualTest, F(RejectLabel), X(3), I(8), Move, X(1), X(0), Return,
            Label, U(RejectLabel), Return, EndCode];
        byte[] input = [.. prefix, .. first, .. second, .. guard, .. first, .. literal, .. second, .. end];
        byte[] expected = [.. prefix, .. first, .. second, .. guard, .. literal, .. end];
        byte[] earlyRetry = [.. prefix, IsAtom, F(RetryLabel), X(1), .. input[prefix.Length..]];
        byte[] clobber = [.. prefix, .. first, .. second, Move, X(1), X(2), .. input[(prefix.Length + first.Length + second.Length)..]];
        byte[] duplicatedTag = prefix[^5..];
        byte[] independentCheck = prefix.ToArray();
        independentCheck[^4] = F(RetryLabel);
        byte[] beforeTagSharing =
        [
            .. independentCheck, .. first, .. second, .. guard, .. duplicatedTag, .. first, .. literal, .. second, .. end
        ];
        if (Apply(beforeTagSharing) is not null
            || Call(
                compiler,
                "ShareCheckedTaggedTupleGuardRetries",
                (ReadOnlyMemory<byte>)beforeTagSharing,
                LabelCount
            ) is not byte[] shared || !shared.AsSpan().SequenceEqual(input)
            || Apply(shared) is not { } actual || !actual.AsSpan().SequenceEqual(expected)
            || Apply(expected) is not null || Apply(earlyRetry) is not null || Apply(clobber) is not null)
        {
            throw new InvalidOperationException(WrongPath);
        }

        byte[]? Apply(byte[] bytes) => (byte[]?)Call(
            compiler,
            "ReuseInterleavedRetryTupleProjections",
            (ReadOnlyMemory<byte>)bytes,
            LabelCount
        );
    }

    private static void CheckSourceEntry()
    {
        const string source = """
            -module(final_checked_retries).
            -export([pick/2]).

            pick(start, State) -> State;
            pick({open, _, "root", _, _}, []) -> [{"root", []}];
            pick({open, _, Name, _, _}, State) -> [{Name, []}|State];
            pick({characters, Value}, [{Name, _}|State]) -> [{Name, Value}|State];
            pick({close, _, _, _}, [Value, {Name, Old}|State]) ->
                [{Name, [Value|Old]}|State];
            pick({close, _, "root", _}, [{"root", Value}]) -> Value;
            pick(report, {_, Result}) -> Result;
            pick(_, State) -> State.
            """;

        byte[] bytes = CodeGeneration.CompileBeamModule(source, "final_checked_retries.erl");
        object[] instructions = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>().ToArray();
        int select = Array.FindIndex(instructions, instruction => Opcode(instruction) == SelectTupleArity);
        if (select < 0)
        {
            throw new InvalidOperationException(WrongPath);
        }

        int open = FindArityLabel(5);
        int close = FindArityLabel(4);
        int nil = Array.FindIndex(instructions, open + 1, instruction => Opcode(instruction) == IsNil);
        int first = Array.FindIndex(instructions, close + 1, instruction => Opcode(instruction) == IsNonemptyList);
        if (nil < 0 || first < 0)
        {
            throw new InvalidOperationException(WrongPath);
        }

        int next = Array.FindIndex(instructions, first + 1, instruction => Opcode(instruction) == IsNonemptyList);
        if (next < 0 || Operand(instructions[first], 0) == Operand(instructions[next], 0)
            || Opcode(instructions[FindLabel(Operand(instructions[nil], 0)) + 1]) != TestHeap)
        {
            throw new InvalidOperationException(WrongPath);
        }

        int retry = FindLabel(Operand(instructions[next], 0));
        int retryEnd = Array.FindIndex(instructions, retry + 1, instruction => Opcode(instruction) == Label);
        object split = instructions[first + 1];
        if (retryEnd < 0 || first + 2 != next || Opcode(split) != GetList
            || Operand(split, 0) != Operand(instructions[first], 1))
        {
            throw new InvalidOperationException(WrongPath);
        }

        var retryBody = instructions[(retry + 1)..retryEnd];
        if (Operand(instructions[next], 1) != Operand(split, 2)
            || retryBody.Any(operation => Opcode(operation) is GetList or IsNonemptyList)
            || !retryBody.Any(operation => Opcode(operation) == IsTuple && Operand(operation, 1) == Operand(split, 1))
            || !retryBody.Any(operation => Opcode(operation) == IsNil && Operand(operation, 1) == Operand(split, 2)))
        {
            throw new InvalidOperationException(WrongPath);
        }

        int FindArityLabel(int arity)
        {
            object[] operands = ((IEnumerable)Property(instructions[select], "Operands")).Cast<object>().ToArray();
            for (int index = 3; index + 1 < operands.Length; index += 2)
            {
                if ((int)(BigInteger)Property(operands[index], "Value") == arity)
                {
                    return FindLabel((int)(BigInteger)Property(operands[index + 1], "Value"));
                }
            }

            throw new InvalidOperationException(WrongPath);
        }

        int FindLabel(int name)
        {
            int index = Array.FindIndex(instructions, operation => Opcode(operation) == Label && Operand(operation, 0) == name);

            return index >= 0 ? index : throw new InvalidOperationException(WrongPath);
        }

        static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

        static int Operand(object instruction, int index) =>
            (int)(BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(index), "Value");
    }


    private static byte[] Fixture(bool shared = false, bool extraPredecessor = false, bool overwrite = false)
    {
        byte[] added = extraPredecessor ? [IsNil, F(RetryLabel), X(1)] : [];
        byte[] replaced = overwrite ? [Move, X(1), X(2)] : [];
        byte[] duplicate = shared ? [] : [GetTupleElement, X(0), U(1), X(2)];

        return
        [
            .. Start(
                FunctionInfoLabel,
                EntryLabel,
                2,
                Arity
            ),
            IsTuple, F(RejectLabel), X(0), TestArity, F(RejectLabel), X(0), U(2),
            .. added,
            GetTupleElement, X(0), U(1), X(2), ExactEqualTest, F(RetryLabel), X(2), I(7),
            .. replaced,
            IsNil, F(RetryLabel), X(1), Move, A(1), X(0), Return,
            Label, U(RetryLabel), .. duplicate, Move, X(2), X(0), Return,
            Label, U(RejectLabel), Move, X(1), X(0), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(ReadOnlyMemory<byte> code)
    {
        var labels = new List<(int Start, int End, int Label)>();
        Action<int, int, int> label = (start, end, key) => labels.Add((start, end, key));
        object header = New(
            "BeamCodeHeader",
            0,
            (int)DebugLine,
            LabelCount,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            8,
            0,
            label,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RewriteFailureProjectionJoins",
            code,
            body,
            labels,
            false,
            true
        );
    }
}
