using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RetainDictionaryResultThroughCons(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(BifOne)
            || !atoms.TryGetValue(ErlangModuleNames.Module, out int erlang)
            || !atoms.TryGetValue(AutoImportNames.Get, out int get)
            || !imports.TryGetValue((erlang, get, 1), out int import)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        int lookupImport = emittedImports?[import] ?? import;

        var rewrittenEntries = new HashSet<int>();
        if (RewriteDictionaryResultThroughCons(
            code,
            decoded.Body,
            lookupImport,
            rewrittenEntries
        ) is not { } retained)
        {
            return null;
        }

        // Only compact independently owned frames whose dictionary save was
        // actually removed. The existing frame walk preserves every live slot.
        if (rewrittenEntries.Count == 0 || DecodeLateAtomicProjectionSlots(retained, labelCount) is not { } compactInput)
        {
            return retained;
        }

        return RewriteDisjointFrames(
            retained,
            compactInput.Body,
            compactInput.StackOperands,
            atomicOnly: false,
            entriesToCompact: rewrittenEntries,
            reuseOccupiedEntrySlots: true
        ) ?? retained;
    }

    private static byte[]? RewriteDictionaryResultThroughCons(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int lookupImport,
        HashSet<int>? rewrittenEntries
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int nextEntry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = nextEntry;
            nextEntry = end + 1;
            if (Enumerable.Range(entry, end - entry).Any(index => body[index].Definition.Opcode is
                TryEnter or CatchEnter or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout
                or RemoveMessage or ReceiveTimeout or DebugLine or ExecutableLine))
            {
                continue;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int lookup = entry; lookup + 5 < end; lookup++)
            {
                int spill = lookup + 1;
                int heap = lookup + 2;
                int firstCons = lookup + 3;
                if (!IsInfallibleDictionaryLookup(body[lookup], lookupImport)
                    || body[spill] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                    } || result != x0
                    || body[heap] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
                    || ReadHeapWords(code, body[heap]) is not { } heapWords)
                {
                    continue;
                }

                BeamOperand? outgoing = null;
                int lastCons = firstCons;
                for (; lastCons < end; lastCons++)
                {
                    if (body[lastCons] is not
                        {
                            Definition.Opcode: PutList,
                            Operands: [var head, var tail, { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
                        }
                        || head is not
                        {
                            Kind: BeamOperandParameters.Y or BeamOperandParameters.Integer or BeamOperandParameters.Atom
                                or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil,
                            TypeIndex: null
                        }
                        || SameProjectionRegister(head, saved)
                        || (lastCons == firstCons ? tail.Kind != BeamOperandParameters.Nil : tail != x0))
                    {
                        break;
                    }

                    if (destination.Value.Sign > 0)
                    {
                        outgoing = destination;
                        break;
                    }
                }

                if (outgoing is null || heapWords.Words != (lastCons - firstCons + 1) * ConsHeapWords)
                {
                    continue;
                }

                int argument = (int)outgoing.Value;
                var moves = new SortedDictionary<int, int>();
                int restore = lastCons + 1;
                while (restore < end && body[restore] is
                    {
                        Definition.Opcode: Move,
                        Operands: [var value, { Kind: BeamOperandParameters.X, TypeIndex: null } target]
                    } && value.Kind is BeamOperandParameters.Atom or BeamOperandParameters.Nil
                        or BeamOperandParameters.LiteralValue or BeamOperandParameters.Integer
                    && target.Value.Sign > 0 && target.Value < argument
                    && moves.TryAdd((int)target.Value, restore))
                {
                    restore++;
                }

                if (moves.Count != argument - 1 || restore >= end
                    || body[restore] is not { Definition.Opcode: Move, Operands: [var stored, var restored] }
                    || stored != saved || restored != x0)
                {
                    continue;
                }

                int call = restore + 1;
                while (call < end && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                if (call >= end || body[call] is not
                    {
                        Definition.Opcode: LocalCall or ExternalCall,
                        Operands: [var arity, _]
                    }
                    || arity.Value != argument + 1)
                {
                    continue;
                }

                ownership ??= ReadDisjointFrameOwnership(
                    body,
                    entry,
                    end,
                    allowCompleteTrims: true
                );
                if (ownership is not { } frame || frame.Owners[spill - entry] < 0
                    || frame.Owners[spill - entry] != frame.Owners[call - entry]
                    || !(flow ??= new BeamProjectionFlow(body)).IsDead(
                        call,
                        saved,
                        allowBadRecordExit: true,
                        dictionaryLookupImport: lookupImport
                    ))
                {
                    continue;
                }

                output.Write(code.Span[copied..body[spill].Offset]);
                // The old spill initializes this root before the heap check. Clear
                // it explicitly so the new X root does not retain an older value.
                WriteFrameInitialization(output, [(int)saved.Value]);
                output.Write(code.Span[body[heap].Offset..heapWords.End]);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                for (int cons = firstCons; cons <= lastCons; cons++)
                {
                    int destination = cons == lastCons ? argument : 0;
                    int suffix = 1 + CompactRegisterCountWidth(destination);
                    output.Write(code.Span[body[cons].Offset..(body[cons + 1].Offset - suffix)]);
                    if (cons == firstCons)
                    {
                        BeamCompactWriter.Write(output, AtomTag, 0);
                    }
                    else
                    {
                        WriteRegister(output, x1);
                    }

                    WriteRegister(output, cons == lastCons ? outgoing : x1);
                }

                int[] ordered = moves.Values.ToArray();
                foreach (int index in IndependentCallMoveOrder(ordered.Length))
                {
                    int move = ordered[index];
                    output.Write(code.Span[body[move].Offset..body[move + 1].Offset]);
                }

                copied = body[restore + 1].Offset;
                lookup = restore;
                if (frame.Uses.Count == 1 && body[entry] is { Definition.Opcode: Label, Operands: [var entryLabel] })
                {
                    rewrittenEntries?.Add((int)entryLabel.Value);
                }
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool IsInfallibleDictionaryLookup(BeamInstruction instruction, int import) =>
        instruction is
        {
            Definition.Opcode: BifOne,
            Operands:
            [
            { Kind: BeamOperandParameters.Failure },
            { Kind: BeamOperandParameters.Unsigned } target,
            { Kind: BeamOperandParameters.Atom },
            { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null }
            ]
        } && target.Value == import;
}
