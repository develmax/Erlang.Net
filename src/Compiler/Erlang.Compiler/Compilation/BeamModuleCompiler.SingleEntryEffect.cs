using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record EntryEffectArgument(
            int Destination,
            int? Source,
            int? Projection,
            Term? Literal,
            bool Singleton
        );

        private sealed record EntryEffectProjectionPlan(
            int Tuple,
            (int Field, int Register)[] Fields,
            List<(int Destination, int Source)> Moves,
            List<(int Destination, Term Value)> Constants
        );

        private bool TryEmitSingleEntryEffect(
            Clause clause,
            Dictionary<string, (int Input, int? Field)> bindings,
            Action? emitChecks = null,
            bool projectCheckedFieldsInOrder = false
        )
        {
            if (clause.Body is not Expr.Sequence
                {
                    Items: [Expr.Call effect, Expr.Tuple { Items.Count: > 0 } result]
                } || effect.Arguments.Count >= FirstReservedXRegister
                || !TryResolveOrdinaryCallTarget(effect, out string? module)
                || ReadFiniteEffectArguments(result.Items, bindings) is not { } values)
            {
                return false;
            }

            var pending = new List<EntryEffectArgument>();
            for (int destination = 0; destination < effect.Arguments.Count; destination++)
            {
                if (ReadArgument(effect.Arguments[destination], destination) is not { } operand)
                {
                    return false;
                }

                if (operand is not { Projection: null, Singleton: false } || operand.Source != destination)
                {
                    pending.Add(operand);
                }
            }

            string[] returned = values.Select(value => value.Binding).OfType<string>().Distinct(StringComparer.Ordinal).ToArray();
            var projections = projectCheckedFieldsInOrder ? ReadProjectionPlan() : null;
            if (projectCheckedFieldsInOrder && projections is null)
            {
                return false;
            }

            int live = pending.Select(operand => operand.Source ?? -1)
                .Concat(returned.Select(name => bindings[name].Input)).DefaultIfEmpty(-1).Max() + 1;
            var ordered = new List<EntryEffectArgument>();
            if (projections is null)
            {
                while (pending.Count != 0)
                {
                    int next = pending.FindLastIndex(argument =>
                        !pending.Any(other => !ReferenceEquals(other, argument) && other.Source == argument.Destination));
                    if (next < 0)
                    {
                        return false;
                    }

                    ordered.Add(pending[next]);
                    pending.RemoveAt(next);
                }
            }

            emitChecks?.Invoke();
            int construction = ordered.Count(operand => operand.Singleton) * ConsHeapWords;
            owner.Op(construction == 0 ? Allocate : AllocateHeap, BeamWire.UnsignedTag, returned.Length);
            if (construction != 0)
            {
                owner.Operand(BeamWire.UnsignedTag, construction);
            }

            owner.Operand(BeamWire.UnsignedTag, Math.Max(live, InitialArgumentLive()));
            var slots = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string name in returned)
            {
                int slot = slots.Count;
                slots.Add(name, slot);
                var (source, field) = bindings[name];
                if (field is int position)
                {
                    owner.Op(GetTupleElement, XRegisterTag, source);
                    owner.Operand(BeamWire.UnsignedTag, position);
                    owner.Operand(YRegisterTag, slot);
                }
                else
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        source,
                        YRegisterTag,
                        slot
                    );
                }
            }

            if (projections is { } plan)
            {
                foreach (var (field, register) in plan.Fields)
                {
                    owner.EmitTuplePart(plan.Tuple, field, register);
                }

                owner.EmitOrderedTailMoves(plan.Moves, plan.Constants);
            }

            for (int index = 0; index < ordered.Count; index++)
            {
                var argument = ordered[index];
                if (argument.Literal is { } literal)
                {
                    owner.EmitConstant(literal, argument.Destination);
                }
                else if (argument.Singleton)
                {
                    int head = argument.Source!.Value;
                    if (argument.Projection is int position)
                    {
                        // X0 is free until its later argument preparation when nothing still reads it.
                        var later = ordered.Skip(index + 1);
                        int temporary = later.Any(operand => operand.Destination == 0)
                            && !later.Any(operand => operand.Source == 0) ? 0 : argument.Destination;
                        owner.EmitTuplePart(head, position, temporary);
                        head = temporary;
                    }

                    owner.Op(PutList, XRegisterTag, head);
                    owner.LiteralOperand(Nil.Value);
                    owner.Operand(XRegisterTag, argument.Destination);
                }
                else if (argument.Projection is int field)
                {
                    owner.EmitTuplePart(argument.Source!.Value, field, argument.Destination);
                }
                else
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        argument.Source!.Value,
                        XRegisterTag,
                        argument.Destination
                    );
                }
            }

            owner.EmitCallLine(effect, location);
            Call(module, effect.Function, effect.Arguments.Count);
            EmitEntryEffectTupleReturn(values, slots);

            return true;

            EntryEffectProjectionPlan? ReadProjectionPlan()
            {
                if (pending.Count != effect.Arguments.Count || returned.Any(name => bindings[name].Field is not null)
                    || pending.Any(argument => argument.Singleton || argument.Projection is null && argument.Literal is null))
                {
                    return null;
                }

                EntryEffectArgument[] fields = pending.Where(argument => argument.Projection.HasValue)
                    .OrderBy(argument => argument.Projection).ToArray();
                if (fields.Length < 2 || fields[0].Source is not int tuple
                    || fields.Any(field => field.Source != tuple)
                    || fields.Select(field => field.Projection!.Value).Distinct().Count() != fields.Length)
                {
                    return null;
                }

                // The returned inputs are saved before the call-only projections. Keep the
                // checked tuple intact until its final read, then reuse its register.
                var reads = new (int Field, int Register)[fields.Length];
                var moves = new List<(int Destination, int Source)>();
                int nextRegister = 0;
                for (int index = 0; index < fields.Length; index++)
                {
                    if (nextRegister == tuple)
                    {
                        nextRegister++;
                    }

                    int register = index + 1 == fields.Length ? tuple : nextRegister++;
                    reads[index] = (fields[index].Projection!.Value, register);
                    if (register != fields[index].Destination)
                    {
                        moves.Add((fields[index].Destination, register));
                    }
                }

                var constants = pending.Where(argument => argument.Literal is not null)
                    .Select(argument => (argument.Destination, argument.Literal!)).ToList();

                return new EntryEffectProjectionPlan(
                    tuple,
                    reads,
                    moves,
                    constants
                );
            }

            // Arguments already in place have no moves but remain live at allocation.
            int InitialArgumentLive() => effect.Arguments
                .Select((argument, index) => argument is Expr.Variable variable
                    && bindings.TryGetValue(variable.Name, out var input)
                    && input.Field is null && input.Input == index ? index + 1 : 0).DefaultIfEmpty(0).Max();

            EntryEffectArgument? ReadArgument(Expr expression, int destination)
            {
                if (AttributeLiteral.TryNormalizeConstant(expression, out Term literal))
                {
                    return new EntryEffectArgument(
                        destination,
                        null,
                        null,
                        literal,
                        false
                    );
                }

                if (expression is Expr.Variable variable && bindings.TryGetValue(variable.Name, out var bound))
                {
                    return new EntryEffectArgument(
                        destination,
                        bound.Input,
                        bound.Field,
                        null,
                        false
                    );
                }

                if (expression is Expr.List { Items: [Expr.Variable head], Tail: null }
                    && bindings.TryGetValue(head.Name, out var list) && (list.Field is null || emitChecks is not null))
                {
                    return new EntryEffectArgument(
                        destination,
                        list.Input,
                        list.Field,
                        null,
                        true
                    );
                }

                if (expression is Expr.RecordAccess { Value: Expr.Variable record } access
                    && ReadCheckedEntryProjection(
                        clause,
                        bindings,
                        record.Name,
                        access.Name,
                        access.Arity
                    )
                    && access.Index > 0 && access.Index < access.Arity)
                {
                    return new EntryEffectArgument(
                        destination,
                        bindings[record.Name].Input,
                        access.Index,
                        null,
                        false
                    );
                }

                return null;
            }
        }

        private void EmitEntryEffectTupleReturn((string? Binding, Term? Constant)[] values, Dictionary<string, int> slots, int? frame = null)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, values.Length + 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTupleHeader(XRegisterTag, values.Length, 0);
            foreach (var value in values)
            {
                if (value.Binding is { } name)
                {
                    owner.Operand(YRegisterTag, slots[name]);
                }
                else
                {
                    owner.LiteralOperand(value.Constant!);
                }
            }

            owner.EmitFrameReturn(frame ?? slots.Count);
        }
    }
}
