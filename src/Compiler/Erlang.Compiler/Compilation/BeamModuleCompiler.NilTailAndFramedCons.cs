using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNilTailAndFramedCons(IReadOnlyList<Clause> clauses, int functionInfo)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || clauses is not
                [
                { Guard: null, Body: Expr.Call { Arguments: [Expr.Tuple] } emptyCall } empty,
                {
                    Guard: null,
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Match { Pattern: Pattern.Variable returned, Value: Expr.Call first },
                                _, ..
                            ]
                    } sequence
                } cons
                ]
                || cons.Patterns.Count + 2 >= FirstReservedXRegister)
            {
                return false;
            }

            int selector = -1;
            var nilInputs = new Dictionary<string, int>(StringComparer.Ordinal);
            var incoming = new Pattern.Variable[cons.Patterns.Count];
            Pattern.Variable? head = null;
            Pattern.Variable? tail = null;
            Pattern.Variable? alias = null;
            for (int index = 0; index < empty.Patterns.Count; index++)
            {
                Pattern pattern = cons.Patterns[index];
                if (selector < 0 && ListEntryTest(empty.Patterns[index]) == IsNil
                    && EntryShape(pattern) is Pattern.List
                    {
                        Items: [Pattern.Variable car],
                        Tail: Pattern.Variable cdr
                    })
                {
                    selector = index;
                    incoming[index] = new Pattern.Variable(VariableScopeNames.Wildcard);
                    head = car;
                    tail = cdr;
                    if (pattern is AliasPattern wrapped)
                    {
                        alias = (wrapped.Left as Pattern.Variable) ?? (Pattern.Variable)wrapped.Right;
                    }

                    continue;
                }

                if (empty.Patterns[index] is not Pattern.Variable nilVariable
                    || nilVariable.Name != VariableScopeNames.Wildcard && !nilInputs.TryAdd(nilVariable.Name, index)
                    || pattern is not Pattern.Variable consVariable)
                {
                    return false;
                }

                incoming[index] = consVariable;
            }

            if (selector < 0 || head is null || tail is null
                || !TryEmitRegisterConstructedArgument(
                    emptyCall,
                    nilInputs,
                    validateOnly: true,
                    allowControlTransfer: true
                )
                || ReadEntryCall(first, incoming.Select(input => input.Name).ToArray()) is not { } producer)
            {
                return false;
            }

            Pattern.Variable[] names = [.. incoming.Where(input => input.Name != VariableScopeNames.Wildcard),
                head, tail, returned, .. alias is { Name: not VariableScopeNames.Wildcard } ? [alias] : Array.Empty<Pattern.Variable>()];
            Expr rest = sequence.Items.Count == 2 ? sequence.Items[1] : sequence with { Items = sequence.Items.Skip(1).ToArray() };
            if (!DistinctBindings(names) || ReadUsedEntryParameters(new Clause(names, null, rest)) is not { } used
                || !used.Contains(head.Name) || !used.Contains(tail.Name) || !used.Contains(returned.Name)
                || alias is { } carrier && used.Contains(carrier.Name))
            {
                return false;
            }

            // The nil and cons tests precede every change to the original arguments.
            // Only the cons branch owns a frame. Save values that cross the first call.
            int nil = Label();
            owner.EmitNonemptyList(selector, nil);
            decoderSlotCount = frameSize;
            var saved = new List<(int Register, int Slot)>();
            for (int index = 0; index < incoming.Length; index++)
            {
                Pattern.Variable parameter = incoming[index];
                if (used.Contains(parameter.Name))
                {
                    int slot = Slot();
                    saved.Add((index, slot));
                    variables.Add(parameter.Name, slot);
                }
            }

            int headSlot = Slot();
            int tailSlot = Slot();
            variables.Add(head.Name, headSlot);
            variables.Add(tail.Name, tailSlot);
            EmitFrameAllocation(cons.Patterns.Count, frameSize);
            EmitFrameInitialization(0, frameSize - saved.Count - 2);
            for (int index = saved.Count - 1; index >= 0; index--)
            {
                var (register, slot) = saved[index];
                owner.MoveRegister(
                    XRegisterTag,
                    register,
                    YRegisterTag,
                    slot
                );
            }

            owner.EmitListParts(
                selector,
                YRegisterTag,
                headSlot,
                YRegisterTag,
                tailSlot
            );
            EmitEntryCall(producer);
            variables.Add(returned.Name, Save());
            rest = TryEmitRestoredSlotResultPrefix(rest) ?? rest;
            guardedBinaryMatches = ReadGuardedBinaryMatches(rest, null);
            if (!TryEmitFramedTaggedGuardTails(rest)
                && !TailLocal(rest, reuseCallSlots: true, directFunctionReturn: true))
            {
                EmitReturnExpression(rest, reuseCallSlots: true, terminalCase: true);
                owner.EmitFrameReturn(frameSize);
            }

            Mark(nil);
            owner.EmitNilTest(XRegisterTag, selector, functionInfo);
            TryEmitRegisterConstructedArgument(emptyCall, nilInputs, allowControlTransfer: true);

            return true;
        }
    }
}
