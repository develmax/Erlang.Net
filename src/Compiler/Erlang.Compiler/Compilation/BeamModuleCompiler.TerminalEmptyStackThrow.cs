using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? EndFramedEmptyStackThrow(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(ExternalCall)
            || !atoms.TryGetValue(ErlangModuleNames.Module, out int erlang)
            || !atoms.TryGetValue(ErlangRaiseBifNames.Raise, out int raise)
            || !atoms.TryGetValue(ErlangExceptionClasses.Throw, out int throwAtom)
            || !imports.TryGetValue((erlang, raise, BeamRaiseEmptyTraceNames.Arity), out int import)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        int raiseImport = emittedImports?[import] ?? import;
        int emittedClass = MapEmittedAtom(throwAtom);
        var entryThrow = RewriteFramedEmptyStackThrow(
            code,
            body,
            raiseImport,
            emittedClass
        );
        if (!code.Span.Contains(CatchEnd))
        {
            return entryThrow;
        }

        if (entryThrow is { } entryRewritten)
        {
            code = entryRewritten;
            if (DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var rewrittenBody })
            {
                return entryThrow;
            }

            body = rewrittenBody;
        }

        return RewriteClosedCatchEmptyStackThrow(
            code,
            body,
            raiseImport,
            emittedClass
        ) ?? entryThrow;
    }

    private static byte[]? RewriteFramedEmptyStackThrow(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int raiseImport,
        int throwAtom
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            if (entry + 8 >= end || body[entry].Definition.Opcode != Label
                || body[entry + 1] is not { Definition.Opcode: Allocate or AllocateHeap, Operands: [var width, ..] })
            {
                continue;
            }

            var prefixLabels = new HashSet<int> { (int)body[entry].Operands[0].Value };
            for (int call = entry + 2; call + 2 < end; call++)
            {
                BeamInstruction instruction = body[call];
                if (instruction.Definition.Opcode is Allocate or AllocateHeap or TrimFrame or Deallocate
                    or CatchEnter or TryEnter or CatchEnd or TryEnd or TryCase or LoopReceive or Wait
                    or WaitTimeout or DebugLine or ExecutableLine)
                {
                    break;
                }

                if (instruction is { Definition.Opcode: Label, Operands: [var label] })
                {
                    prefixLabels.Add((int)label.Value);
                }

                if (call < entry + 5 || instruction is not
                    {
                        Definition.Opcode: ExternalCall,
                        Operands: [var arity, var import]
                    } || arity.Value != BeamRaiseEmptyTraceNames.Arity || import.Value != raiseImport
                    || !HasConstantEmptyThrowArguments(body, call, throwAtom)
                    || body[call + 1].Definition.Opcode != Jump
                    || (body[call + 2].Definition.Opcode is not (Label or FunctionInfo or EndCode)))
                {
                    continue;
                }

                if (body.Skip(call + 1).Take(end - call - 1).Any(candidate =>
                    candidate.Definition.Opcode is not (LocalCall or TailLocalCall or TailLocalCallWithFrame)
                    && candidate.Operands.Any(operand => operand.Kind is BeamOperandParameters.Label or BeamOperandParameters.Failure
                        && prefixLabels.Contains((int)operand.Value))))
                {
                    break;
                }

                // A constant throw class and nil stack are the lowering of a
                // proven stack-free throw. That BIF must raise. With no active
                // local handler, the entry frame can leave with the call.
                output.Write(code.Span[copied..instruction.Offset]);
                output.WriteByte(TailExternalCallWithFrame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, arity.Value);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, import.Value);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, width.Value);
                copied = body[call + 2].Offset;
                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static byte[]? RewriteClosedCatchEmptyStackThrow(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int raiseImport,
        int throwAtom
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int info = 0; info < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo)
            {
                continue;
            }

            int end = info + 1;
            while (end < body.Count && body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            int allocation = FindEntryFrameAllocation(
                body,
                info + 1,
                end,
                allowUnframedProjections: true
            );
            if (allocation < 0)
            {
                continue;
            }

            var width = body[allocation].Operands[0].Value;
            int entered = -1;
            int closed = -1;
            bool singleHandler = true;
            for (int index = allocation + 1; index < end; index++)
            {
                var instruction = body[index];
                switch (instruction.Definition.Opcode)
                {
                    case CatchEnter when entered < 0:
                        entered = index;
                        break;
                    case CatchEnd when entered >= 0 && closed < 0:
                        closed = index;
                        break;
                    case CatchEnter:
                    case CatchEnd:
                    case TryEnter:
                    case TryEnd:
                    case TryCase:
                    case DebugLine:
                    case ExecutableLine:
                        singleHandler = false;
                        break;
                }
            }

            if (!singleHandler || entered < 0 || closed < entered + 3
                || body[entered] is not
                {
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } slot,
                    { Kind: BeamOperandParameters.Label } handler]
                }
                || slot.Value < 0 || slot.Value >= width || body[closed].Operands[0] != slot
                || body[closed - 2].Definition.Opcode is not (DynamicApply or LocalCall or ExternalCall))
            {
                continue;
            }

            for (int call = closed + 4; call + 3 < end; call++)
            {
                var instruction = body[call];
                if (instruction is not { Definition.Opcode: ExternalCall, Operands: [var arity, var import] }
                    || arity.Value != BeamRaiseEmptyTraceNames.Arity || import.Value != raiseImport
                    || !HasConstantEmptyThrowArguments(body, call, throwAtom)
                    || body[call + 1].Definition.Opcode != Label
                    || body[call + 2] is not { Definition.Opcode: Deallocate, Operands: [var release] }
                    || release.Value != width || body[call + 3].Definition.Opcode != Return)
                {
                    continue;
                }

                var ownership = flow ??= new BeamProjectionFlow(body);
                if (!ownership.TryGetExclusiveForwardTarget(
                    entered,
                    closed - 2,
                    (int)handler.Value,
                    out int landing
                ) || landing != closed - 1
                    || !ownership.IsNormalContinuationOfHandlerClose(allocation, entered, requireStableFrame: true)
                    || !ownership.IsNormalContinuationOfHandlerClose(entered, closed - 2, requireStableFrame: true)
                    || !ownership.IsNormalContinuationOfHandlerClose(closed, call, requireStableFrame: true))
                {
                    continue;
                }

                // The class and empty trace are already explicit. This BIF
                // cannot return, and both exits have closed the same catch.
                // Preserve the deallocation for other branches of the case.
                output.Write(code.Span[copied..instruction.Offset]);
                output.WriteByte(TailExternalCallWithFrame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, arity.Value);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, import.Value);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, width);
                copied = body[call + 1].Offset;
            }

            info = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool HasConstantEmptyThrowArguments(IReadOnlyList<BeamInstruction> body, int call, int throwAtom) =>
        body[call - 1].Definition.Opcode == Line
        && body[call - 2] is
        {
            Definition.Opcode: Move,
            Operands: [{ Kind: BeamOperandParameters.Atom } kind,
            { Kind: BeamOperandParameters.X, Value.IsZero: true }]
        }
        && kind.Value == throwAtom
        && body[call - 3] is
        {
            Definition.Opcode: Move,
            Operands: [{ Kind: BeamOperandParameters.Nil }, { Kind: BeamOperandParameters.X } trace]
        }
        && trace.Value == BeamTryParameters.RawRaiseTraceRegister;
}
