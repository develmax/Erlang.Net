using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record MapIteratorFramePlan(
        int Cut,
        int ErrorExit,
        int Success,
        int Helper,
        int FinalExit,
        int OldSlot,
        int?[] Registers,
        Dictionary<int, int>? Roots
    );

    private byte[]? RewriteOwnedMapIteratorBranches(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || mapIteratorHelpers.Count == 0
            || !TryFindMapIteratorImport(ErlangModuleNames.Module, ErlangModuleNames.Error, out int error)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return ReallocateLeafFrameRegisters(
            code,
            decoded.Body,
            decoded.StackOperands,
            afterCanonicalization: true,
            canDiscardExternalTailFrame: IsColdError,
            isColdMapErrorTail: IsColdError,
            ownedMapIteratorsOnly: true
        );

        bool IsColdError(BeamInstruction instruction) =>
            instruction.Definition.Opcode == TailExternalCallWithFrame
            && instruction.Operands[0].Value.IsOne && instruction.Operands[1].Value == error;
    }

    private MapIteratorFramePlan? PlanMapIteratorBranchFrame(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int arity,
        string function,
        Func<BeamInstruction, bool>? isArithmetic,
        Func<BeamInstruction, bool>? canDiscardExternalTailFrame,
        Func<BeamInstruction, bool> isColdMapErrorTail
    )
    {
        string helperName = MappingName(function, arity);
        if (!mapIteratorHelpers.ContainsKey((helperName, MapIteratorHelperArity))
            || !atoms.TryGetValue(helperName, out int helperAtom)
            || !TryFindMapIteratorImport(MapIteratorModule, MapIteratorStart, out int iteratorImport)
            || !TryFindMapIteratorImport(MapModuleNames.Module, MapModuleNames.FromList, out int fromListImport))
        {
            return null;
        }

        int limit = end;
        if (body[end].Definition.Opcode == FunctionInfo)
        {
            while (limit > start && body[limit - 1].Definition.Opcode == Line)
            {
                limit--;
            }

            if (limit > start && body[limit - 1].Definition.Opcode == Label)
            {
                limit--;
            }
        }

        int allocation = FindEntryFrameAllocation(body, start, limit);
        if (allocation < 0 || limit - allocation < 12
            || body[allocation] is not
            {
                Definition.Opcode: Allocate,
                Operands: [var width, _]
            } || width.Value <= 1)
        {
            return null;
        }

        int finalExit = limit - 1;
        if (body[finalExit] is not
            {
                Definition.Opcode: TailExternalCallWithFrame,
                Operands: [{ Value.IsOne: true }, var finish, var finalWidth]
            } || finish.Value != fromListImport || finalWidth != width)
        {
            return null;
        }

        int helper = finalExit - 1;
        if (body[helper].Definition.Opcode == Line)
        {
            helper--;
        }

        if (body[helper] is not
            {
                Definition.Opcode: LocalCall,
                Operands: [{ Value.IsOne: true }, var localEntry]
            } || !IsMapIteratorHelperEntry(body, MapEmittedAtom(helperAtom), localEntry))
        {
            return null;
        }

        int success = helper - 1;
        if (body[success] is
            {
                Definition.Opcode: Move,
                Operands: [var savedResult, { Kind: BeamOperandParameters.Y, TypeIndex: null } scratch]
            } && IsBareX0(savedResult) && scratch.Value < width.Value)
        {
            // The sole next call reads X0, and its result is immediately passed to maps:from_list.
            success--;
        }

        int errorExit = success - 1;
        int tuple = errorExit - 1;
        int heap = tuple - 1;
        int check = heap - 1;
        int call = check - 1;
        int cut = call - (body[call - 1].Definition.Opcode == Line ? 1 : 0);
        int restore = cut - 1;
        if (restore <= allocation || body[restore - 1].Definition.Opcode != Label
            || body[restore] is not
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.Y, TypeIndex: null } source,
                    var argument
                ]
            } || !IsBareX0(argument) || source.Value >= width.Value
            || body[call] is not { Definition.Opcode: ExternalCall, Operands: [{ Value.IsOne: true }, var iterator] }
            || iterator.Value != iteratorImport
            || body[check] is not { Definition.Opcode: IsNil, Operands: [var accepted, var result] }
            || !IsBareX0(result) || accepted.Kind != BeamOperandParameters.Label
            || body[success] is not { Definition.Opcode: Label, Operands: [var next] } || next.Value != accepted.Value
            || body[heap] is not { Definition.Opcode: TestHeap, Operands: [var heapWords, { Value.IsZero: true }] }
            || heapWords.Value != 3
            || body[tuple] is not
            {
                Definition.Opcode: PutTuple,
                Operands: [var destination, { Kind: BeamOperandParameters.Count } fields, var reason, var payload]
            } || fields.Value != 2 || !IsBareX0(destination) || payload != source || !IsBadMapIteratorReason(reason)
            || body[errorExit] is not
            {
                Definition.Opcode: TailExternalCallWithFrame,
                Operands: [_, _, var errorWidth]
            } || errorWidth != width || !isColdMapErrorTail(body[errorExit])
            || ReadDisjointFrameOwnership(body, start, limit) is not { } ownership
            || ownership.Uses.Count != 1 || ownership.Owners[restore - start] != allocation
            || ownership.Owners[errorExit - start] != allocation || ownership.Owners[finalExit - start] != allocation)
        {
            return null;
        }

        var prefix = body.Skip(start).Take(cut - start).ToArray();
        var labels = prefix.Where(instruction => instruction.Definition.Opcode == Label)
            .Select(instruction => instruction.Operands[0].Value).ToHashSet();
        if (prefix.Any(instruction => instruction.Operands.Any(operand =>
            operand.Kind == BeamOperandParameters.Label && !labels.Contains(operand.Value))))
        {
            return null;
        }

        // Own the existing restore as a terminal X0 read while planning the frameless prefix.
        // No retired Y slot may cross the only edge into the call branch.
        BeamInstruction[] leaf = [.. prefix, new(body[cut].Offset, BeamOpcodeCatalog.All[Return - 1], [])];
        if (PlanLeafFrameRegisters(
            leaf,
            0,
            leaf.Length,
            arity,
            true,
            isArithmetic,
            out var roots,
            canDiscardExternalTailFrame: canDiscardExternalTailFrame,
            isColdMapErrorTail: isColdMapErrorTail,
            allowStackMapBase: true
        ) is not { } registers)
        {
            return null;
        }

        return new(
            cut,
            errorExit,
            success,
            helper,
            finalExit,
            (int)source.Value,
            registers,
            roots?.ToDictionary(pair => pair.Key + start, pair => pair.Value)
        );
    }

    private bool TryFindMapIteratorImport(string module, string function, out int import)
    {
        import = -1;

        if (!atoms.TryGetValue(module, out int moduleAtom) || !atoms.TryGetValue(function, out int functionAtom)
            || !imports.TryGetValue((moduleAtom, functionAtom, MapIteratorHelperArity), out int original))
        {
            return false;
        }

        import = emittedImports?[original] ?? original;

        return true;
    }

    private bool IsBadMapIteratorReason(BeamOperand reason) =>
        reason.Kind == BeamOperandParameters.Atom
        && atoms.TryGetValue(ComprehensionErrorReasons.BadGenerator, out int atom) && reason.Value == MapEmittedAtom(atom);

    private static bool IsMapIteratorHelperEntry(
        IReadOnlyList<BeamInstruction> body,
        int atom,
        BeamOperand entry
    )
    {
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is
                {
                    Definition.Opcode: FunctionInfo,
                    Operands: [_, var function, { Value.IsOne: true }]
                } && function.Value == atom && body[index + 1] is
                { Definition.Opcode: Label, Operands: [var label] } && label.Value == entry.Value)
            {
                return true;
            }
        }

        return false;
    }

    private static int WriteMapIteratorBranchFrame(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        Stream output,
        int copied,
        MapIteratorFramePlan plan
    )
    {
        const int retainedSlots = 1;
        const int callRoots = 1;
        const int savedSource = 0;

        int cut = body[plan.Cut].Offset;
        output.Write(code.Span[copied..cut]);
        output.WriteByte(Allocate);
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, retainedSlots);
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, callRoots);
        output.WriteByte(Move);
        BeamCompactWriter.Write(output, XRegisterTag, 0);
        BeamCompactWriter.Write(output, YRegisterTag, savedSource);

        int errorExit = body[plan.ErrorExit].Offset;
        output.Write(code.Span[cut..(errorExit - CompactRegisterCountWidth(plan.OldSlot))]);
        BeamCompactWriter.Write(output, YRegisterTag, savedSource);
        WriteTailWidth(plan.ErrorExit, retainedSlots);

        output.Write(code.Span[body[plan.Success].Offset..body[plan.Success + 1].Offset]);
        output.WriteByte(TrimFrame);
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, retainedSlots);
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);

        output.Write(code.Span[body[plan.Helper].Offset..body[plan.FinalExit].Offset]);
        WriteTailWidth(plan.FinalExit, 0);

        return body[plan.FinalExit + 1].Offset;

        void WriteTailWidth(int index, int width)
        {
            int previous = (int)body[index].Operands[^1].Value;
            int next = body[index + 1].Offset;
            output.Write(code.Span[body[index].Offset..(next - CompactRegisterCountWidth(previous))]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, width);
        }
    }
}
