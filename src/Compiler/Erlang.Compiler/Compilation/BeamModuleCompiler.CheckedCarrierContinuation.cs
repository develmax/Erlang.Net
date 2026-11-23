using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCheckedCarrierContinuation(
            Clause clause,
            string[] parameters,
            int arity,
            int carrier,
            List<(int Index, int Register)> fields,
            Action emitCheck
        )
        {
            if (clause.Guard is not null || carrier != 0
                || clause.Body is not Expr.Sequence
                {
                    Items: [Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Call first }, .., Expr.Tuple returned]
                } sequence
                || !TryResolveOrdinaryCallTarget(first, out string? firstModule)
                || returned.Items is not
                [
                    Expr.Literal { Value: Atom replyTag },
                    Expr.Variable reply,
                    Expr.Variable outer
                ])
            {
                return false;
            }

            int outerInput = Array.IndexOf(
                parameters,
                outer.Name,
                0,
                arity
            );
            if (outerInput <= 0 || outer.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            var names = parameters.Where(name => name != VariableScopeNames.Wildcard)
                .ToHashSet(StringComparer.Ordinal);
            if (result.Name == VariableScopeNames.Wildcard || !names.Add(result.Name))
            {
                return false;
            }

            var carrierFields = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var (field, register) in fields)
            {
                if (parameters[register] == VariableScopeNames.Wildcard
                    || !carrierFields.TryAdd(parameters[register], field))
                {
                    return false;
                }
            }

            var bindings = new List<(string Name, Expr Value)>();
            int callIndex = 1;
            while (sequence.Items[callIndex] is Expr.Match
                {
                    Pattern: Pattern.Variable bound,
                    Value: Expr.Tuple or Expr.RecordConstruction
                } assignment)
            {
                if (bound.Name == VariableScopeNames.Wildcard || !names.Add(bound.Name))
                {
                    return false;
                }

                bindings.Add((bound.Name, assignment.Value));
                callIndex++;
            }

            if (bindings.Count == 0 || sequence.Items[callIndex] is not Expr.Call second
                || !TryResolveOrdinaryCallTarget(second, out string? secondModule))
            {
                return false;
            }

            bool returnsResult = callIndex == sequence.Items.Count - 2 && reply.Name == result.Name;
            bool returnsAlias = callIndex == sequence.Items.Count - 3 && sequence.Items[callIndex + 1] is Expr.Match
            {
                Pattern: Pattern.Variable alias, Value: Expr.Variable value
            } && alias.Name != VariableScopeNames.Wildcard && !names.Contains(alias.Name)
                && value.Name == result.Name && reply.Name == alias.Name;
            if (!returnsResult && !returnsAlias)
            {
                return false;
            }

            var constants = new List<(int Destination, Term Value)>();
            string? early = null;
            int argument = -1;
            for (int index = 0; index < first.Arguments.Count; index++)
            {
                Expr operand = first.Arguments[index];
                if (AttributeLiteral.TryNormalizeConstant(operand, out Term literal))
                {
                    constants.Add((index, literal));
                }
                else if (early is null && operand is Expr.Variable field && carrierFields.ContainsKey(field.Name))
                {
                    early = field.Name;
                    argument = index;
                }
                else
                {
                    return false;
                }
            }

            if (early is null || first.Arguments.Count >= FirstReservedXRegister
                || second.Arguments.Count >= FirstReservedXRegister)
            {
                return false;
            }

            var reads = bindings.Select(binding => binding.Value).Concat(second.Arguments)
                .SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Variable>()
                .Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
            // The construction planner can skip an unread binding. Keep such
            // sequences on the ordinary path so their evaluation is preserved.
            if (bindings.Any(binding => !reads.Contains(binding.Name)))
            {
                return false;
            }

            var deferred = carrierFields.Where(field => field.Key != early && reads.Contains(field.Key))
                .OrderBy(field => field.Value).ToArray();
            if (deferred.Length < 2 || deferred.Length + 1 >= FirstReservedXRegister)
            {
                return false;
            }

            const int earlySlot = 0;
            const int outerSlot = 1;
            const int carrierSlot = 2;
            var inputs = new Dictionary<string, int>(StringComparer.Ordinal) { [result.Name] = 0 };
            for (int index = 0; index < deferred.Length; index++)
            {
                inputs.Add(deferred[index].Key, index + 1);
            }

            if (ReadCallArgumentConstructions(
                second.Arguments,
                result: -1,
                saved: new Dictionary<string, int>(StringComparer.Ordinal) { [early] = earlySlot },
                registerValue: result.Name,
                inputs: inputs,
                bindings: bindings,
                preserveX0: true
            ) is not { Constructions.Count: >= 2, Loads.Count: 0 } construction
                || construction.Constructions.Any(node => node.Register == 0))
            {
                return false;
            }

            emitCheck();
            EmitFrameAllocation(arity, 3);
            owner.MoveRegister(
                XRegisterTag,
                outerInput,
                YRegisterTag,
                outerSlot
            );
            owner.MoveRegister(
                XRegisterTag,
                carrier,
                YRegisterTag,
                carrierSlot
            );
            GetTupleField(
                carrier,
                carrierFields[early],
                YRegisterTag,
                earlySlot
            );
            owner.EmitOrderedTailMoves([], constants, [(argument, earlySlot)]);
            owner.EmitCallLine(first, location);
            Call(firstModule, first.Function, first.Arguments.Count);

            // The call defines only X0. Reserve the heap before materializing the other X values.
            owner.Op(TestHeap, BeamWire.UnsignedTag, construction.Constructions.Sum(node => node.HeapWords));
            owner.Operand(BeamWire.UnsignedTag, 1);
            for (int index = 0; index < deferred.Length; index++)
            {
                EmitSavedRecordField(carrierSlot, deferred[index].Value, index + 1);
            }

            EmitCallArgumentConstructions(
                construction,
                result: -1,
                heapChecked: true,
                afterConstruction: StoreInReleasedCarrier
            );
            owner.EmitCallLine(second, CallLocation(first, location));
            Call(secondModule, second.Function, second.Arguments.Count);
            owner.Op(TestHeap, BeamWire.UnsignedTag, returned.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTupleHeader(XRegisterTag, returned.Items.Count);
            owner.LiteralOperand(replyTag);
            owner.Operand(YRegisterTag, carrierSlot - 1);
            owner.Operand(YRegisterTag, outerSlot - 1);
            owner.EmitFrameReturn(2);

            return true;

            void StoreInReleasedCarrier()
            {
                // The constructors consume the last projections. Save X0 before outgoing moves replace it.
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    carrierSlot
                );
                owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, 2);
            }
        }
    }
}
