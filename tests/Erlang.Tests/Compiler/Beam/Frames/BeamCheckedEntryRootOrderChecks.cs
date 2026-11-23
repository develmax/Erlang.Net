using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedEntryRootOrderChecks
{
    internal const string TestName = "compiler/beam/orders-checked-entry-frame-roots-and-delays-call-result-copies";
    private const string Module = "checked_root_order_fixture";
    private const string Fixture = Module + ".erl";
    private const string RootPass = "OrderCheckedEntryFrameRoots";
    private const string DelayPass = "DelayCheckedEntryCallResultCopies";
    private const int FirstDefinitionSlot = 1;
    private const int SavedResultSlot = 0;
    private const int ProjectedField = 2;

    internal static Task Run()
    {
        var assembly = typeof(BeamCheckedEntryRootOrderChecks).Assembly;
        string name = assembly.GetManifestResourceNames()
            .Single(resource => resource.EndsWith("." + Fixture, StringComparison.Ordinal));
        using Stream input = assembly.GetManifestResourceStream(name)!;
        using var source = new StreamReader(input);
        object code = Code(CodeGeneration.CompileBeamModule(source.ReadToEnd(), Fixture));
        CheckCompiledCall(code, "unary", 1);
        CheckCompiledCall(code, "binary", 2);

        var compiler = new BeamModuleCompiler(Module);
        CheckPass(
            compiler,
            RootPass,
            Image(),
            Image(renumber: true),
            BeamCheckedEntryRootOrderDiagnostics.RootPermutation
        );
        CheckPass(
            compiler,
            DelayPass,
            Image(),
            Image(delay: true),
            BeamCheckedEntryRootOrderDiagnostics.DelayedCopy
        );

        byte[] literalArgument = [Move, A(1), X(0)];
        CheckPass(
            compiler,
            RootPass,
            Image(binaryArgument: literalArgument),
            Image(renumber: true, binaryArgument: literalArgument),
            BeamCheckedEntryRootOrderDiagnostics.RootPermutation
        );
        CheckPass(
            compiler,
            DelayPass,
            Image(binaryArgument: literalArgument),
            Image(delay: true, binaryArgument: literalArgument),
            BeamCheckedEntryRootOrderDiagnostics.DelayedCopy
        );

        // The reject edge allocates its own frame.
        byte[] otherOwner =
        [
            Allocate, U(1), U(1), Move, X(0), Y(0),
            Move, Y(0), X(0), Deallocate, U(1), Return
        ];
        CheckDeclined(compiler, Image(fallback: otherOwner), BeamCheckedEntryRootOrderDiagnostics.SeparateOwner);

        // The tuple is still known, but this checkpoint changes the X live range.
        byte[] heapCheck = [TestHeap, U(2), U(1)];
        CheckDeclined(
            compiler,
            Image(betweenSpillAndProjection: heapCheck),
            BeamCheckedEntryRootOrderDiagnostics.HeapBoundary
        );

        // This call reads the saved return value as its unprojected argument.
        byte[] savedArgument = [Move, Y(1), X(0)];
        CheckDeclined(
            compiler,
            Image(binaryArgument: savedArgument),
            BeamCheckedEntryRootOrderDiagnostics.SavedFirstArgument
        );

        return Task.CompletedTask;
    }

    private static void CheckCompiledCall(object code, string function, int arity)
    {
        object entry = Children(code, "Functions").Single(item => (string)Property(item, "Name") == function);
        int start = (int)Property(entry, "FirstInstructionIndex");
        int end = (int)Property(entry, "EndInstructionIndex");
        object[] body = Children(code, "Instructions")[start..end].Where(item => Opcode(item) != Line).ToArray();
        int allocation = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
        int producer = Array.FindIndex(body, instruction => Opcode(instruction) == ExternalCall);
        if (allocation < 1 || producer <= allocation + 2 || producer + 4 >= body.Length
            || Opcode(body[allocation - 1]) != IsTaggedTuple)
        {
            throw new InvalidOperationException(BeamCheckedEntryRootOrderDiagnostics.RootPermutation);
        }

        int width = (int)Value(Operands(body[allocation])[0]);
        // The emitter can reserve another slot for a later result. The checked
        // carrier and immediate saved result are the first two definitions.
        int carrierSlot = width - 1;
        int savedResultSlot = carrierSlot - 1;
        if (width < 2
            || !InitializesFutureRoots(body[allocation + 1], carrierSlot)
            || !IsMove(body[allocation + 2], ("x", 0), ("y", carrierSlot)))
        {
            throw new InvalidOperationException(BeamCheckedEntryRootOrderDiagnostics.RootPermutation);
        }

        object projection = body[producer + 1];
        if (Opcode(projection) != GetTupleElement
            || Operands(projection) is not [var carrier, var field, var output]
            || !IsRegister(carrier, "y", carrierSlot) || Value(field) != ProjectedField
            || !IsRegister(output, "x", 1)
            || !IsMove(body[producer + 2], ("x", 0), ("y", savedResultSlot))
            || Opcode(body[producer + 4]) != ExternalCall
            || Value(Operands(body[producer + 4])[0]) != arity
            || (arity == 1
                ? !IsMove(body[producer + 3], ("x", 1), ("x", 0))
                : !IsMove(body[producer + 3], ("atom", null), ("x", 0))))
        {
            throw new InvalidOperationException(BeamCheckedEntryRootOrderDiagnostics.DelayedCopy);
        }
    }

    private static bool InitializesFutureRoots(object instruction, int carrierSlot)
    {
        object[] operands = Operands(instruction);

        return Opcode(instruction) == InitYRegisters
            && operands.Length == carrierSlot + 1 && Value(operands[0]) == carrierSlot
            && operands.Skip(1).Select((slot, index) => IsRegister(slot, "y", index)).All(valid => valid);
    }

    private static bool IsMove(
        object instruction,
        (string Kind, int? Value) source,
        (string Kind, int Value) destination
    ) =>
        Opcode(instruction) == Move && Operands(instruction) is [var from, var to]
        && (string)Property(from, "Kind") == source.Kind && (!source.Value.HasValue || Value(from) == source.Value.Value)
        && IsRegister(to, destination.Kind, destination.Value);

    private static bool IsRegister(object operand, string kind, int value) =>
        (string)Property(operand, "Kind") == kind && Value(operand) == value;

    private static object[] Children(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static object[] Operands(object instruction) => Children(instruction, "Operands");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static void CheckPass(
        BeamModuleCompiler compiler,
        string pass,
        byte[] original,
        byte[] expected,
        string diagnostic
    )
    {
        if (Rewrite(compiler, pass, original) is not { } actual || !actual.SequenceEqual(expected))
        {
            throw new InvalidOperationException(diagnostic);
        }
    }

    private static void CheckDeclined(BeamModuleCompiler compiler, byte[] original, string diagnostic)
    {
        if (Rewrite(compiler, RootPass, original) is not null || Rewrite(compiler, DelayPass, original) is not null)
        {
            throw new InvalidOperationException(diagnostic);
        }
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, string pass, ReadOnlyMemory<byte> raw) =>
        (byte[]?)Call(
            compiler,
            pass,
            raw,
            4
        );

    private static byte[] Image(
        bool renumber = false,
        bool delay = false,
        byte[]? fallback = null,
        byte[]? betweenSpillAndProjection = null,
        byte[]? binaryArgument = null
    )
    {
        byte carrier = Y(renumber ? FirstDefinitionSlot : SavedResultSlot);
        byte retained = Y(renumber ? SavedResultSlot : FirstDefinitionSlot);
        byte[] alreadySaved = [Move, X(0), retained];
        byte[] field = [GetTupleElement, carrier, U(ProjectedField), X(delay || binaryArgument is not null ? 1 : 0)];
        byte[] finishArgument = binaryArgument ?? (delay ? [Move, X(1), X(0)] : []);
        byte[] callSetup = delay
            ? [.. field, .. alreadySaved, .. finishArgument]
            : [.. alreadySaved, .. (betweenSpillAndProjection ?? []), .. field, .. finishArgument];

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            IsTaggedTuple, F(3), X(0), U(4), A(1),
            Allocate, U(2), U(1),
            InitYRegisters, ExtendedListOperand, U(1), retained,
            Move, X(0), carrier, GetTupleElement, X(0), U(1), X(0),
            ExternalCall, U(1), U(0), .. callSetup, ExternalCall, U(binaryArgument is null ? 1 : 2), U(1),
            Move, retained, X(0), Deallocate, U(2), Return,
            Label, U(3), .. (fallback ?? [BadRecord, X(0)]), EndCode
        ];
    }
}
