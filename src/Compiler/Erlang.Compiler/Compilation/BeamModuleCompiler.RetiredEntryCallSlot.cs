using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRetiredEntryCallSlot(
            EntrySequenceBody sequence,
            (Pattern.Variable Variable, int Register)[] inputs
        )
        {
            if (!CanUseEntryTypeFacts()
                || sequence.Binding is not Pattern.Variable { Name: not VariableScopeNames.Wildcard } firstResult
                || sequence.Continuation is not Expr.Sequence { Items.Count: 2 or 3 } continuation)
            {
                return false;
            }

            var binding = continuation.Items[0] as Expr.Match;
            if ((binding?.Value ?? continuation.Items[0]) is not Expr.Call { DynamicRemote: false } middle
                || middle.Arguments.Count is 0 or >= FirstReservedXRegister)
            {
                return false;
            }

            EntryCall? restore = null;
            Term? returnedConstant = null;
            Term? checkedLiteral = null;
            if (continuation.Items.Count == 2)
            {
                if (binding is not null && !IsWildcard(binding.Pattern)
                    || continuation.Items[1] is not Expr.Variable returned || returned.Name != firstResult.Name)
                {
                    return false;
                }
            }
            else
            {
                Expr restoreSource = continuation.Items[1] is Expr.Match
                { Pattern: Pattern.Variable { Name: VariableScopeNames.Wildcard } } ignored ? ignored.Value : continuation.Items[1];
                if (restoreSource is not Expr.Call
                    { DynamicRemote: false, Arguments: [Expr.Variable input] } call
                    || input.Name != firstResult.Name || ReadEntryCall(call, [firstResult.Name]) is not { } prepared)
                {
                    return false;
                }

                restore = prepared;
                if (binding?.Pattern is Pattern.Literal { Value: Atom or Integer } expected
                    && AttributeLiteral.TryNormalizeConstant(continuation.Items[2], out Term constant))
                {
                    checkedLiteral = expected.Value;
                    returnedConstant = constant;
                }
                else if (binding?.Pattern is not Pattern.Variable { Name: not VariableScopeNames.Wildcard } result
                    || result.Name == firstResult.Name || inputs.Any(argument => argument.Variable.Name == result.Name)
                    || continuation.Items[2] is not Expr.Variable returned || returned.Name != result.Name)
                {
                    return false;
                }
            }

            var saved = inputs.Where(input => sequence.Used.Contains(input.Variable.Name))
                .OrderByDescending(input => input.Register).ToArray();
            if (saved.Length == 0 || saved.Any(input => input.Variable.Name == firstResult.Name))
            {
                return false;
            }

            Expr.Call? probe = null;
            int preparedArgument = -1;
            for (int index = 0; index < middle.Arguments.Count; index++)
            {
                if (middle.Arguments[index] is Expr.Variable)
                {
                    continue;
                }

                if (preparedArgument >= 0 || middle.Arguments[index] is not Expr.Call
                    {
                        DynamicRemote: false,
                        Module: null or ErlangModuleNames.Module,
                        Function: AutoImportNames.Self or AutoImportNames.Node,
                        Arguments: []
                    } bif || !owner.ResolvesErlangBif(bif))
                {
                    return false;
                }

                probe = bif;
                preparedArgument = index;
            }

            string[] savedNames = saved.Select(input => input.Variable.Name).ToArray();
            if (middle is { Module: ErlangModuleNames.Module, Function: ErlangModuleNames.MapGet, Arguments.Count: 2 }
                && owner.ResolvesErlangBif(middle))
            {
                return TryEmitRetiredMapLookup();
            }

            if (ReadEntryCall(
                middle,
                [],
                savedNames,
                preparedOperand: preparedArgument
            ) is not { SavedInputs: { } loads } target
                || loads.Count != saved.Length || loads.Select(load => load.Source).Distinct().Count() != saved.Length)
            {
                return false;
            }

            int size = saved.Length;
            int live = Math.Max(sequence.Live, saved[0].Register + 1);
            int scratch = middle.Arguments.Count;
            EmitFrameAllocation(live, size);
            for (int slot = 0; slot < size; slot++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    saved[slot].Register,
                    YRegisterTag,
                    slot
                );
            }

            EmitEntryCall(sequence.Call);
            if (probe is not null)
            {
                EmitZeroGuardBif(probe.Function, preparedArgument == 0 ? scratch : preparedArgument);
            }

            // X0 owns the first result. All stack reads below are for the next call, and the
            // old top-slot value retires before X0 is spilled into that slot.
            foreach (var (destination, source) in loads)
            {
                Load(source, destination == 0 ? scratch : destination);
            }

            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                size - 1
            );
            if (size > 1)
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, size - 1);
                owner.Operand(BeamWire.UnsignedTag, 1);
            }

            owner.MoveToX0(XRegisterTag, scratch);
            owner.EmitCallLine(middle, CallLocation(sequence.Call.Call, location));
            Call(target.Module, target.Call.Function, target.Call.Arguments.Count);

            int failure = 0;
            if (restore is null)
            {
                Load(0);
                owner.EmitFrameReturn(1);
            }
            else if (checkedLiteral is not null)
            {
                failure = Label();
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, 0);
                owner.LiteralOperand(checkedLiteral);
                Load(0);
                owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, 0);
                EmitEntryCall(restore, CallLocation(middle, location));
                owner.EmitConstant(returnedConstant!, 0);
                owner.EmitFrameReturn(0);
            }
            else
            {
                owner.Op(Swap, YRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
                EmitEntryCall(restore, CallLocation(middle, location));
                Load(0);
                owner.EmitFrameReturn(1);
            }

            if (failure != 0)
            {
                Mark(failure);
                if (binding!.SourceLine != 0)
                {
                    owner.EmitLine(new(middle.PreprocessorFile ?? location.File, binding.SourceLine));
                }

                owner.Op(BadMatch, XRegisterTag, 0);
            }

            return true;

            bool TryEmitRetiredMapLookup()
            {
                if (saved.Length + (probe is null ? 0 : 1) != middle.Arguments.Count
                    || middle.Arguments.OfType<Expr.Variable>().Select(variable => variable.Name)
                        .Distinct(StringComparer.Ordinal).Count() != saved.Length
                    || middle.Arguments.OfType<Expr.Variable>().Any(variable => !savedNames.Contains(variable.Name)))
                {
                    return false;
                }

                int width = saved.Length;
                int live = Math.Max(sequence.Live, saved[0].Register + 1);
                int resultSlot = width - 1;
                EmitFrameAllocation(live, width);
                for (int slot = 0; slot < width; slot++)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        saved[slot].Register,
                        YRegisterTag,
                        slot
                    );
                }

                EmitEntryCall(sequence.Call);
                if (probe is not null)
                {
                    EmitZeroGuardBif(probe.Function, 1);
                }

                bool retainLookup = restore is not null && checkedLiteral is null;
                owner.EmitCallLine(middle, CallLocation(sequence.Call.Call, location));
                owner.Op(BifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, middle.Function, 2));
                foreach (Expr argument in middle.Arguments)
                {
                    if (argument is Expr.Variable variable)
                    {
                        owner.Operand(YRegisterTag, Array.IndexOf(savedNames, variable.Name));
                    }
                    else
                    {
                        owner.Operand(XRegisterTag, 1);
                    }
                }

                owner.Operand(retainLookup ? YRegisterTag : XRegisterTag, retainLookup ? resultSlot : 1);
                if (restore is null)
                {
                    owner.EmitFrameReturn(width);

                    return true;
                }

                int rejected = 0;
                if (checkedLiteral is not null)
                {
                    rejected = Label();
                    owner.Op(ExactEqualTest, LabelTag, rejected);
                    owner.Operand(XRegisterTag, 1);
                    owner.LiteralOperand(checkedLiteral);
                }

                int remaining = retainLookup ? 1 : 0;
                int dropped = width - remaining;
                if (dropped > 0)
                {
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, dropped);
                    owner.Operand(BeamWire.UnsignedTag, remaining);
                }

                EmitEntryCall(restore, CallLocation(middle, location));
                if (retainLookup)
                {
                    Load(0);
                }
                else
                {
                    owner.EmitConstant(returnedConstant!, 0);
                }

                owner.EmitFrameReturn(remaining);
                if (rejected != 0)
                {
                    Mark(rejected);
                    if (binding!.SourceLine != 0)
                    {
                        owner.EmitLine(new(middle.PreprocessorFile ?? location.File, binding.SourceLine));
                    }

                    owner.Op(BadMatch, XRegisterTag, 1);
                }

                return true;
            }
        }
    }
}
