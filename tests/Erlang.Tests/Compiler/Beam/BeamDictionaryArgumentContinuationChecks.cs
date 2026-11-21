using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDictionaryArgumentContinuationChecks
{
    internal const string TestName = "compiler/beam/dictionary-argument-continuation";
    private const string Spill = "Keep the dictionary value in X0 through pure list construction.";
    private const string Reordered = "Preserve the dictionary value observed before the effectful argument.";

    private const string Source = """
        -module(snmp_call_root_probe).
        -export([pure/2, effect/2, branched/3, observe/5, touch/1, consume/2]).
        -record(state, {id}).
        pure(#state{id=Id}, Key) ->
            ?MODULE:observe(get(verbosity), trace, probe, message, [Id,Key]),
            ?MODULE:consume(Id, Key).
        effect(#state{id=Id}, Key) ->
            ?MODULE:observe(get(verbosity), trace, probe, message, [?MODULE:touch(Id),Key]),
            ?MODULE:consume(Id, Key).
        branched(#state{id=Id}, Key, pure) ->
            ?MODULE:observe(get(verbosity), trace, probe, message, [Id,Key]),
            ?MODULE:consume(Id, Key);
        branched(#state{id=Id}, Key, effect) ->
            ?MODULE:observe(get(verbosity), trace, probe, message, [?MODULE:touch(Id),Key]),
            ?MODULE:consume(Id, Key).

        observe(Mode, _Level, _Device, _Text, Inputs) ->
            put(observed,{Mode,get(verbosity),Inputs}), ok.
        touch(Id) -> put(verbosity,changed), Id.
        consume(Id, Key) -> {Id,Key,get(observed)}.
        """;

    internal static void Run()
    {
        CheckLateCorridor();
        object code = Code(CodeGeneration.CompileBeamModule(Source, "snmp_call_root_probe.erl"));
        object[] instructions = Items(code, "Instructions");
        object[] functions = Items(code, "Functions");
        object[] pure = Body("pure");
        int getter = Array.FindIndex(pure, instruction => Opcode(instruction) == BifOne);
        int call = Array.FindIndex(pure, instruction => Opcode(instruction) == ExternalCall);
        object? allocation = pure.FirstOrDefault(instruction => Opcode(instruction) == AllocateHeap);
        if (allocation is null || !Items(allocation, "Operands").Select(Number).SequenceEqual([2, 4, 2])
            || getter < 0 || getter >= call || !IsRegister(Items(pure[getter], "Operands")[^1], "x", 0)
            || !pure[(getter + 1)..call].Any(instruction => Opcode(instruction) == PutList)
            || pure[(getter + 1)..call].Any(SavesX0))
        {
            throw new InvalidOperationException(Spill);
        }

        object[] effect = Body("effect");
        int lookup = Array.FindIndex(effect, instruction => Opcode(instruction) == BifOne);
        int mutation = Array.FindIndex(effect, instruction => Opcode(instruction) == ExternalCall);
        if (lookup < 0 || lookup >= mutation
            || ((string)Property(Items(effect[lookup], "Operands")[^1], "Kind") != "y"
                && !effect[(lookup + 1)..mutation].Any(SavesX0)))
        {
            throw new InvalidOperationException(Reordered);
        }

        object[] branched = Body("branched");
        int[] lookups = Enumerable.Range(0, branched.Length).Where(index => Opcode(branched[index]) == BifOne).ToArray();
        if (lookups is not [var pureLookup, var effectLookup])
        {
            throw new InvalidOperationException(Spill);
        }

        int pureCall = Array.FindIndex(branched, pureLookup + 1, instruction => Opcode(instruction) == ExternalCall);
        int effectCall = Array.FindIndex(branched, effectLookup + 1, instruction => Opcode(instruction) == ExternalCall);
        if (pureCall < 0 || !IsRegister(Items(branched[pureLookup], "Operands")[^1], "x", 0)
            || !branched[(pureLookup + 1)..pureCall].Any(instruction => Opcode(instruction) == PutList)
            || branched[(pureLookup + 1)..pureCall].Any(SavesX0)
            || branched[(pureLookup + 1)..pureCall].Any(instruction => Opcode(instruction) == TestHeap
                && Number(Items(instruction, "Operands")[^1]) < 1))
        {
            throw new InvalidOperationException(Spill);
        }

        if (effectCall < 0 || (string)Property(Items(branched[effectLookup], "Operands")[^1], "Kind") != "y"
            && !branched[(effectLookup + 1)..effectCall].Any(SavesX0))
        {
            throw new InvalidOperationException(Reordered);
        }

        object[] Body(string name)
        {
            object function = functions.Single(candidate => (string)Property(candidate, "Name") == name);
            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");

            return instructions[start..end];
        }
    }

    private static void CheckLateCorridor()
    {
        byte[] prefix =
        [
            Label, U(1), FunctionInfo, A(1), A(2), U(2), Label, U(2),
            Allocate, U(2), U(2), Move, X(1), Y(1),
            InitYRegisters, ExtendedListOperand, U(1), Y(0)
        ];
        byte[] lookup = [BifOne, F(0), U(0), A(3), X(0)];
        byte[] oldConstruction =
        [
            Move, X(0), Y(0), TestHeap, U(4), U(0),
            PutList, Y(1), A(0), X(0), PutList, Y(1), X(0), X(4),
            Move, A(4), X(1), Move, A(5), X(2), Move, A(6), X(3), Move, Y(0), X(0)
        ];
        byte[] newConstruction =
        [
            InitYRegisters, ExtendedListOperand, U(1), Y(0), TestHeap, U(4), U(1),
            PutList, Y(1), A(0), X(1), PutList, Y(1), X(1), X(4),
            Move, A(5), X(2), Move, A(6), X(3), Move, A(4), X(1)
        ];
        byte[] call = [Line, U(1), ExternalCall, U(5), U(1)];
        byte[] tail = [Deallocate, U(2), Return, EndCode];
        byte[] expected = [.. prefix, .. lookup, .. newConstruction, .. call, .. tail];
        if (Rewrite([.. prefix, .. lookup, .. oldConstruction, .. call, .. tail]) is not { } rewritten
            || !expected.AsSpan().SequenceEqual(rewritten)
            || Rewrite([.. prefix, .. lookup, .. oldConstruction, .. call, Move, Y(0), X(0), .. tail]) is not null
            || Rewrite([.. prefix, .. lookup, .. oldConstruction, Label, U(3), .. call, .. tail]) is not null
            || Rewrite([.. prefix, BifOne, F(0), U(2), A(3), X(0), .. oldConstruction, .. call, .. tail]) is not null
            || Rewrite([.. prefix, .. lookup, Move, X(0), Y(0), TestHeap, U(2), U(0),
                PutList, Y(0), A(0), X(2), Move, A(4), X(1), Move, Y(0), X(0),
                ExternalCall, U(3), U(1), .. tail]) is not null)
        {
            throw new InvalidOperationException(Spill);
        }

        var compiler = new BeamModuleCompiler("dictionary_frame_probe");
        _ = Call(
            compiler,
            "Import",
            "erlang",
            "get",
            1
        );
        foreach (string atom in new[] { "tag", "label", "detail" })
        {
            _ = Call(compiler, "Intern", atom);
        }

        byte[] corridor = [.. prefix, .. lookup, .. oldConstruction, .. call, .. tail];
        ReadOnlyMemory<byte> ownedCode = corridor;
        byte[] compacted =
        [
            Label, U(1), FunctionInfo, A(1), A(2), U(2), Label, U(2),
            Allocate, U(1), U(2), Move, X(1), Y(0),
            .. lookup, TestHeap, U(4), U(1),
            PutList, Y(0), A(0), X(1), PutList, Y(0), X(1), X(4),
            Move, A(5), X(2), Move, A(6), X(3), Move, A(4), X(1),
            .. call, Deallocate, U(1), Return, EndCode
        ];
        if (Call(
            compiler,
            "RetainDictionaryResultThroughCons",
            ownedCode,
            4
        ) is not byte[] compactedCode
            || !compacted.AsSpan().SequenceEqual(compactedCode))
        {
            throw new InvalidOperationException(Spill);
        }

        CheckOccupiedFrame();

        void CheckOccupiedFrame()
        {
            byte[] initialized =
            [
                Label, U(1), FunctionInfo, A(1), A(2), U(2), Label, U(2),
                Allocate, U(2), U(2), InitYRegisters, ExtendedListOperand, U(1), Y(0), Move, X(1), Y(1)
            ];
            byte[] successor = [Move, X(0), Y(0), Move, Y(0), X(0), .. tail];
            byte[] reused =
            [
                Label, U(1), FunctionInfo, A(1), A(2), U(2), Label, U(2),
                Allocate, U(1), U(2), InitYRegisters, ExtendedListOperand, U(1), Y(0), Move, X(1), Y(0),
                .. lookup, TestHeap, U(4), U(1),
                PutList, Y(0), A(0), X(1), PutList, Y(0), X(1), X(4),
                Move, A(5), X(2), Move, A(6), X(3), Move, A(4), X(1), .. call,
                Move, X(0), Y(0), Move, Y(0), X(0), Deallocate, U(1), Return, EndCode
            ];
            if (Compact([.. initialized, .. lookup, .. oldConstruction, .. call, .. successor]) is not { } shrunk
                || !shrunk.AsSpan().SequenceEqual(reused))
            {
                throw new InvalidOperationException(Spill);
            }

            byte[] assigned = [.. initialized, Move, X(0), Y(0)];
            byte[] assignedInput = [.. assigned, .. lookup, .. oldConstruction, .. call, .. successor];
            byte[] assignedExpected = [.. assigned, .. lookup, .. newConstruction, .. call, .. successor];
            byte[] split = [.. initialized, Jump, F(3), Label, U(3)];
            byte[] splitInput = [.. split, .. lookup, .. oldConstruction, .. call, .. successor];
            byte[] splitExpected = [.. split, .. lookup, .. newConstruction, .. call, .. successor];
            if (Compact(assignedInput) is not { } assignedCode || !assignedCode.AsSpan().SequenceEqual(assignedExpected)
                || Compact(splitInput) is not { } splitCode || !splitCode.AsSpan().SequenceEqual(splitExpected))
            {
                throw new InvalidOperationException(Spill);
            }

            byte[]? Compact(byte[] bytes) => Call(
                compiler,
                "RetainDictionaryResultThroughCons",
                (ReadOnlyMemory<byte>)bytes,
                4
            ) as byte[];
        }

        static byte[]? Rewrite(byte[] source)
        {
            ReadOnlyMemory<byte> bytes = source;
            object header = New(
                "BeamCodeHeader",
                0,
                (int)InitYRegisters,
                4,
                1,
                bytes
            );
            object body = Call(
                Type("BeamInstructionReader"),
                "Read",
                header,
                7,
                0,
                null,
                null
            );

            return Call(
                typeof(BeamModuleCompiler),
                "RewriteDictionaryResultThroughCons",
                bytes,
                body,
                0,
                null
            ) as byte[];
        }
    }

    private static byte U(int value) => Compact(value, 0);

    private static byte X(int value) => Compact(value, XRegisterTag);

    private static byte Y(int value) => Compact(value, YRegisterTag);

    private static byte A(int value) => Compact(value, AtomTag);

    private static byte F(int value) => Compact(value, LabelTag);

    private static byte Compact(int value, int tag) => (byte)((value << 4) | tag);

    private static bool SavesX0(object instruction) => Opcode(instruction) == Move
        && Items(instruction, "Operands") is [var source, var destination]
        && IsRegister(source, "x", 0) && (string)Property(destination, "Kind") == "y";

    private static bool IsRegister(object operand, string kind, int number) =>
        (string)Property(operand, "Kind") == kind && Number(operand) == number;

    private static int Number(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string property) =>
        ((IEnumerable)Property(owner, property)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
