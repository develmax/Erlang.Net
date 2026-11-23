using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record LinearEffectArgument(
            string? Binding,
            Term? Literal,
            int? Field,
            bool Previous
        );
        private sealed record LinearEffectCall(Expr.Call Expression, string? Module, LinearEffectArgument[] Arguments);
        private sealed record LinearEntryEffects(LinearEffectCall[] Calls, (string? Binding, Term? Constant)[] Result);

        private IEnumerable<int> ReadDelayedNestedFields(Clause clause, Pattern.Tuple tuple, int selector)
        {
            if (ReadRawEntryCalls(clause, out _) is not { Length: >= 2 } calls)
            {
                return [];
            }

            var delayed = calls.Skip(1).SelectMany(call => call.Call.Arguments)
                .OfType<Expr.Variable>().Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);

            return tuple.Items.Select((item, index) => (item, index))
                .Where(field => field.index != selector && field.item is Pattern.Variable variable && delayed.Contains(variable.Name))
                .Select(field => field.index);
        }

        private (Expr.Call Call, string? Result)[]? ReadRawEntryCalls(Clause clause, out Expr.Tuple result)
        {
            if (clause.Body is not Expr.Sequence { Items: [_, _, .., Expr.Tuple tuple] } sequence)
            {
                result = null!;

                return null;
            }

            result = tuple;
            string[] parameters = clause.Patterns.SelectMany(Semantics.Variables).ToArray();

            return ReadStraightCalls(FlattenCallArguments(sequence.Items.SkipLast(1)), parameters);
        }

        private LinearEntryEffects? ReadLinearEntryEffects(Clause clause, Dictionary<string, (int Input, int? Field)> bindings)
        {
            if (ReadRawEntryCalls(clause, out var tuple) is not { Length: >= 2 } source)
            {
                return null;
            }

            var returned = tuple.Items.Select(item => item is Expr.RecordUpdate
            { Fields.Count: 0, Value: Expr.Variable variable } update
                && ReadCheckedEntryProjection(
                    clause,
                    bindings,
                    variable.Name,
                    update.Name,
                    update.Arity
                )
                    ? (Expr)variable : item).ToArray();
            if (ReadFiniteEffectArguments(returned, bindings) is not { } result)
            {
                return null;
            }

            var calls = new List<LinearEffectCall>();
            string? preceding = null;
            for (int index = 0; index < source.Length; index++)
            {
                var (call, resultBinding) = source[index];
                if (call.Arguments.Count >= FirstReservedXRegister || !TryResolveOrdinaryCallTarget(call, out string? module))
                {
                    return null;
                }

                var arguments = new List<LinearEffectArgument>();
                foreach (Expr expr in call.Arguments)
                {
                    if (expr is Expr.Variable variable && variable.Name == preceding)
                    {
                        arguments.Add(new LinearEffectArgument(
                            null,
                            null,
                            null,
                            true
                        ));
                    }
                    else if (expr is Expr.Variable input && bindings.ContainsKey(input.Name))
                    {
                        arguments.Add(new LinearEffectArgument(
                            input.Name,
                            null,
                            null,
                            false
                        ));
                    }
                    else if (expr is Expr.RecordAccess { Value: Expr.Variable record } access
                        && ReadCheckedEntryProjection(
                            clause,
                            bindings,
                            record.Name,
                            access.Name,
                            access.Arity
                        )
                        && access.Index > 0 && access.Index < access.Arity)
                    {
                        arguments.Add(new LinearEffectArgument(
                            record.Name,
                            null,
                            access.Index,
                            false
                        ));
                    }
                    else if (AttributeLiteral.TryNormalizeConstant(expr, out Term literal))
                    {
                        arguments.Add(new LinearEffectArgument(
                            null,
                            literal,
                            null,
                            false
                        ));
                    }
                    else
                    {
                        return null;
                    }
                }

                calls.Add(new LinearEffectCall(call, module, arguments.ToArray()));
                preceding = resultBinding;
            }

            return new LinearEntryEffects(calls.ToArray(), result);
        }

        private static bool ReadCheckedEntryProjection(
            Clause clause,
            Dictionary<string, (int Input, int? Field)> bindings,
            string binding,
            string tag,
            int arity
        ) => bindings.TryGetValue(binding, out var source) && source.Field is null && source.Input < clause.Patterns.Count
            && clause.Patterns[source.Input] is AliasPattern alias && TryReadPatternAlias(alias, out var variable, out var shape)
            && variable.Name == binding && shape is Pattern.Tuple tuple && tuple.Items.Count == arity
            && HasTupleTag(tuple, out Atom name) && name.Name == tag;

        private bool TryEmitLinearEntryEffects(Clause clause, Dictionary<string, (int Input, int? Field)> bindings)
        {
            if (ReadLinearEntryEffects(clause, bindings) is not { } plan)
            {
                return false;
            }

            var later = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < plan.Calls.Length; index++)
            {
                foreach (var name in plan.Calls[index].Arguments.Select(argument => argument.Binding).OfType<string>())
                {
                    later[name] = index;
                }
            }

            foreach (var name in plan.Result.Select(value => value.Binding).OfType<string>())
            {
                later[name] = plan.Calls.Length;
            }

            var valueIds = bindings.Keys.Select((name, id) => (name, id))
                .ToDictionary(entry => entry.name, entry => entry.id, StringComparer.Ordinal);
            var sourceOperations = new List<BeamFrameValueLiveness.Operation>(plan.Calls.Length + 1);
            for (int index = 0; index < plan.Calls.Length; index++)
            {
                LinearEffectCall call = plan.Calls[index];
                int[] uses = call.Arguments.Where(argument => argument.Binding is not null || argument.Previous)
                    .Select(argument => argument.Previous ? valueIds.Count + index - 1 : valueIds[argument.Binding!])
                    .ToArray();
                sourceOperations.Add(new(uses, valueIds.Count + index, ClobbersXRegisters: true));
            }

            int[] returned = plan.Result.Select(value => value.Binding).OfType<string>()
                .Select(name => valueIds[name]).ToArray();
            sourceOperations.Add(new(returned, null, ClobbersXRegisters: false));
            var saved = BeamFrameValueLiveness.FindSavedValues(valueIds.Values, sourceOperations);
            var slots = bindings.Keys.Where(name => saved.Contains(valueIds[name])).Reverse()
                .Select((name, slot) => (name, slot)).ToDictionary(entry => entry.name, entry => entry.slot, StringComparer.Ordinal);
            var prepared = new List<((int Destination, int? X, int? Y, int? Field, Term? Literal)[] Moves, int Trim, int[] Kills)>();
            var finalSlots = new Dictionary<string, int>(slots, StringComparer.Ordinal);
            for (int index = 0; index < plan.Calls.Length; index++)
            {
                var call = plan.Calls[index];
                var moves = call.Arguments.Select((argument, destination) =>
                {
                    if (argument.Binding is string name)
                    {
                        var bound = bindings[name];
                        int? field = argument.Field ?? bound.Field;

                        return (destination, X: index == 0 ? (int?)bound.Input : null,
                            Y: index == 0 ? null : (int?)finalSlots[name], Field: field, argument.Literal);
                    }

                    return (destination, X: argument.Previous ? (int?)0 : null, Y: (int?)null, Field: (int?)null, argument.Literal);
                }).Where(move => move.X != move.destination || move.Field is not null).ToList();

                var ordered = new List<(int Destination, int? X, int? Y, int? Field, Term? Literal)>();
                while (moves.Count != 0)
                {
                    int next = moves.FindLastIndex(move =>
                        moves.All(other => other.destination == move.destination || other.X != move.destination));
                    if (next < 0)
                    {
                        return false;
                    }

                    ordered.Add(moves[next]);
                    moves.RemoveAt(next);
                }

                var dead = finalSlots.Where(entry => later[entry.Key] == index).Select(entry => entry.Value).ToHashSet();
                int trim = 0;
                while (dead.Contains(trim))
                {
                    trim++;
                }

                foreach (string name in finalSlots.Keys.ToArray())
                {
                    if (dead.Contains(finalSlots[name]))
                    {
                        finalSlots.Remove(name);
                    }
                    else
                    {
                        finalSlots[name] -= trim;
                    }
                }

                prepared.Add((ordered.ToArray(), trim, dead.Where(slot => slot >= trim).Order().ToArray()));
            }

            int live = slots.Keys.Concat(plan.Calls[0].Arguments.Select(argument => argument.Binding).OfType<string>())
                .Select(name => bindings[name].Input).DefaultIfEmpty(-1).Max() + 1;
            EmitFrameAllocation(live, slots.Count);
            foreach (var (name, slot) in slots)
            {
                var (register, field) = bindings[name];
                if (field is int position)
                {
                    owner.Op(GetTupleElement, XRegisterTag, register);
                    owner.Operand(BeamWire.UnsignedTag, position);
                    owner.Operand(YRegisterTag, slot);
                }
                else
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        register,
                        YRegisterTag,
                        slot
                    );
                }
            }

            int frame = slots.Count;
            for (int index = 0; index < prepared.Count; index++)
            {
                var (operations, trim, kills) = prepared[index];
                if (operations is
                    [{ Destination: var argument, X: 0, Y: null, Field: null }, { Destination: 0, Y: int slot, Field: int field }])
                {
                    owner.EmitTuplePart(
                        slot,
                        field,
                        argument,
                        YRegisterTag
                    );
                    owner.Op(Swap, XRegisterTag, 0);
                    owner.Operand(XRegisterTag, argument);
                }
                else
                {
                    foreach (var operation in operations)
                    {
                        if (operation.Literal is { } literal)
                        {
                            owner.EmitConstant(literal, operation.Destination);
                        }
                        else if (operation.Field is int projection)
                        {
                            owner.EmitTuplePart(
                                operation.X ?? operation.Y!.Value,
                                projection,
                                operation.Destination,
                                operation.Y is null ? XRegisterTag : YRegisterTag
                            );
                        }
                        else
                        {
                            owner.MoveRegister(
                                operation.Y is null ? XRegisterTag : YRegisterTag,
                                operation.X ?? operation.Y!.Value,
                                XRegisterTag,
                                operation.Destination
                            );
                        }
                    }
                }

                if (kills.Length != 0)
                {
                    owner.EmitSlotKills(kills);
                }

                if (trim != 0)
                {
                    frame -= trim;
                    TrimDetailFrame(trim, frame);
                }

                var call = plan.Calls[index];
                owner.EmitCallLine(call.Expression, location);
                Call(call.Module, call.Expression.Function, call.Arguments.Length);
            }

            EmitEntryEffectTupleReturn(plan.Result, finalSlots, frame);

            return true;
        }
    }
}
