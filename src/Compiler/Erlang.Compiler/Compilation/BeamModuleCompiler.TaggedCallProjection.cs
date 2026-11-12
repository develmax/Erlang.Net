using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEntryTaggedForward(Clause clause)
        {
            var sequence = clause.Body as Expr.Sequence;
            Expr body = sequence?.Items.LastOrDefault() ?? clause.Body;
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || body is not Expr.Case { Value: Expr.Call read, Clauses: [
                    { Patterns: [Pattern.Tuple tuple], Guard: null, Body: Expr.Call tail },
                    { Patterns: [Pattern.Variable fallback], Guard: null, Body: Expr.Variable returned }
                ] }
                || fallback.Name == VariableScopeNames.Wildcard || fallback.Name != returned.Name
                || !HasTupleTag(tuple, out Atom tag))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(fallback.Name))
            {
                return false;
            }

            var arguments = tail.Arguments.OfType<Expr.Variable>().Select(argument => argument.Name).ToArray();
            var bound = parameters.ToHashSet(StringComparer.Ordinal);
            int fieldIndex = -1;
            int fieldRegister = -1;
            string? fieldName = null;
            for (int index = 1; index < tuple.Items.Count; index++)
            {
                if (tuple.Items[index] is Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    continue;
                }

                if (tuple.Items[index] is not Pattern.Variable field || !bound.Add(field.Name))
                {
                    return false;
                }

                int destination = Array.FindIndex(tail.Arguments.ToArray(),
                    operand => operand is Expr.Variable variable && variable.Name == field.Name);
                if (destination >= 0)
                {
                    if (fieldIndex >= 0)
                    {
                        return false;
                    }

                    fieldIndex = index;
                    fieldRegister = destination;
                    fieldName = field.Name;
                }
            }

            var calls = new List<(Expr.Call Call, bool ForwardsResult)>();
            if (sequence is not null)
            {
                foreach (Expr expression in sequence.Items.SkipLast(1))
                {
                    if (expression is not Expr.Call call)
                    {
                        return false;
                    }

                    ReadPrefix(call);
                }
            }

            calls.Add((read, false));
            const string result = "\0prefix-result";
            var used = calls.Skip(1).SelectMany(call => call.Call.Arguments.OfType<Expr.Variable>())
                .Select(variable => variable.Name).Concat(arguments).ToHashSet(StringComparer.Ordinal);
            var inputs = parameters.Where(used.Contains).Reverse().ToArray();
            var plans = new List<EntryCall>();
            foreach (var (call, forwardsResult) in calls)
            {
                Expr.Call prepared = forwardsResult ? call with { Arguments = [new Expr.Variable(result)] } : call;
                string[] incoming = plans.Count == 0 ? parameters : forwardsResult ? [result] : [];
                if (ReadEntryCall(prepared, incoming, plans.Count == 0 ? null : inputs) is not { } plan)
                {
                    return false;
                }

                plans.Add(plan with { Call = call });
            }
            var registers = new string[fieldRegister + 1];
            if (fieldName is not null)
            {
                registers[fieldRegister] = fieldName;
            }

            if (ReadEntryCall(tail, registers, inputs) is not { } finish)
            {
                return false;
            }

            var lastUse = new Dictionary<int, int>();
            for (int index = 1; index < plans.Count; index++)
            {
                foreach (var (_, slot) in plans[index].SavedInputs ?? [])
                {
                    lastUse[slot] = index;
                }
            }

            foreach (var (_, slot) in finish.SavedInputs ?? [])
            {
                lastUse.Remove(slot);
            }

            int unmatched = Label();
            int live = EntryCallLiveCount(plans[0], parameters);
            for (int slot = 0; slot < inputs.Length; slot++)
            {
                live = Math.Max(live, Array.IndexOf(parameters, inputs[slot]) + 1);
            }

            EmitFrameAllocation(live, inputs.Length);
            for (int slot = 0; slot < inputs.Length; slot++)
            {
                owner.MoveRegister(XRegisterTag, Array.IndexOf(parameters, inputs[slot]), YRegisterTag, slot);
            }

            EmitEntryCall(plans[0]);
            var previous = CallLocation(plans[0].Call, location);
            for (int index = 1; index < plans.Count; index++)
            {
                EntryCall plan = plans[index];
                var expired = lastUse.Where(pair => pair.Value == index).Select(pair => pair.Key).Order().ToArray();
                owner.EmitOrderedTailMoves(plan.Moves, plan.Constants, plan.SavedInputs, expired);
                owner.EmitCallLine(plan.Call, previous);
                Call(plan.Module, plan.Call.Function, plan.Call.Arguments.Count);
                previous = CallLocation(plan.Call, previous);
            }
            EmitDetailRecordTest(0, tuple.Items.Count, tag, unmatched, ReturnsTuple(read, 0, []));
            if (fieldIndex >= 0)
            {
                owner.EmitTuplePart(0, fieldIndex, fieldRegister);
            }

            owner.EmitOrderedTailMoves(finish.Moves, finish.Constants, finish.SavedInputs);
            EmitPreparedFrameTail(finish, previous, inputs.Length);
            Mark(unmatched);
            owner.EmitFrameReturn(inputs.Length);
            return true;

            void ReadPrefix(Expr.Call call)
            {
                if (call.Arguments is [Expr.Call inner])
                {
                    ReadPrefix(inner);
                    calls.Add((call, true));
                }
                else
                {
                    calls.Add((call, false));
                }
            }
        }

        private bool TryEmitEntryTaggedTailCall(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Case { Value: Expr.Call read, Clauses.Count: >= 2 } decision
                || decision.Clauses[^1] is not
                {
                    Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Guard: null, Body: var fallback
                }
                || !AttributeLiteral.TryNormalizeConstant(fallback, out Term otherwise))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (ReadEntryCall(read, parameters) is not { } plan)
            {
                return false;
            }

            var tags = new HashSet<string>(StringComparer.Ordinal);
            var branches = new List<(Atom Tag, Expr.Call Tail, Dictionary<int, string> Fields)>();
            int arity = 0;
            foreach (Clause branch in decision.Clauses.Take(decision.Clauses.Count - 1))
            {
                if (branch is not { Patterns: [Pattern.Tuple tuple], Guard: null, Body: Expr.Call tail }
                    || !HasTupleTag(tuple, out Atom tag) || !tags.Add(tag.Name)
                    || arity != 0 && arity != tuple.Items.Count
                    || ReadTaggedTailFields(tuple, tail, parameters) is not { } fields)
                {
                    return false;
                }

                arity = tuple.Items.Count;
                branches.Add((tag, tail, fields));
            }

            bool shared = branches.Count > 1;
            int[] positions = branches.SelectMany(branch => branch.Fields.Keys).Distinct().Order().ToArray();
            int registerCount = positions.Length + (shared ? 1 : 0);
            if (registerCount == 0 || registerCount > FirstReservedXRegister)
            {
                return false;
            }

            var plans = new List<(Atom Tag, EntryCall Tail)>();
            foreach (var branch in branches.OrderByDescending(branch => branch.Tag.Name, StringComparer.Ordinal))
            {
                var registers = new string[registerCount];
                for (int index = 0; index < positions.Length; index++)
                {
                    if (branch.Fields.TryGetValue(positions[index], out string? name))
                    {
                        registers[FieldRegister(index)] = name;
                    }
                }

                if (ReadEntryCall(branch.Tail, registers) is not { } tailPlan)
                {
                    return false;
                }

                plans.Add((branch.Tag, tailPlan));
            }

            var targets = shared ? plans.Select(_ => Label()).ToArray() : [];
            int unmatched = Label();
            EmitEntryCallWithEmptyFrame(plan, parameters);
            if (shared)
            {
                EmitTupleShapeTest(XRegisterTag, 0, arity, unmatched);
                owner.EmitTuplePart(0, 0, 1);
            }
            else
            {
                EmitDetailRecordTest(0, arity, plans[0].Tag, unmatched);
            }

            for (int index = 0; index < positions.Length; index++)
            {
                owner.EmitTuplePart(0, positions[index], FieldRegister(index));
            }

            if (shared)
            {
                owner.EmitMembershipSelect(
                    1,
                    unmatched,
                    plans.Select((branch, index) => ((Term)branch.Tag, targets[index])).Reverse()
                );
            }

            for (int index = 0; index < plans.Count; index++)
            {
                if (shared)
                {
                    Mark(targets[index]);
                }

                EmitEmptyFrameTail(plans[index].Tail, CallLocation(read, location));
            }

            Mark(unmatched);
            owner.EmitConstant(otherwise, 0);
            owner.EmitFrameReturn(0);

            return true;

            int FieldRegister(int index) => (index + (shared ? 2 : 1)) % registerCount;
        }

        private static Dictionary<int, string>? ReadTaggedTailFields(
            Pattern.Tuple tuple, Expr.Call tail, string[] parameters)
        {
            var used = tail.Arguments.OfType<Expr.Variable>().Select(variable => variable.Name)
                .ToHashSet(StringComparer.Ordinal);
            var bound = parameters.ToHashSet(StringComparer.Ordinal);
            var fields = new Dictionary<int, string>();
            for (int index = 1; index < tuple.Items.Count; index++)
            {
                if (tuple.Items[index] is Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    continue;
                }

                if (tuple.Items[index] is not Pattern.Variable field || !bound.Add(field.Name))
                {
                    return null;
                }

                if (used.Contains(field.Name))
                {
                    fields.Add(index, field.Name);
                }
            }

            return fields.Count == used.Count ? fields : null;
        }

        private void EmitEmptyFrameTail(EntryCall tailPlan, BeamSourceLocation previous)
        {
            owner.EmitOrderedTailMoves(tailPlan.Moves, tailPlan.Constants);
            EmitPreparedFrameTail(tailPlan, previous, 0);
        }

        private void EmitPreparedFrameTail(EntryCall tailPlan, BeamSourceLocation previous, int slots)
        {
            var tail = tailPlan.Call;
            if (tailPlan.Module is null)
            {
                owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, tail.Arguments.Count);
                owner.Operand(LabelTag, entries[(tail.Function, tail.Arguments.Count)]);
            }
            else
            {
                owner.EmitCallLine(tail, previous);
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, tail.Arguments.Count);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(tailPlan.Module, tail.Function, tail.Arguments.Count));
            }

            owner.Operand(BeamWire.UnsignedTag, slots);
        }

        private bool TryEmitEntryTaggedCallTuple(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Case { Value: Expr.Call read, Clauses:
                    [
                        { Guard: null, Patterns: [Pattern.Tuple pattern], Body: Expr.Tuple tuple },
                        { Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Variable returned }
                    ]
                }
                || ReadSingleTupleField(pattern) is not { Field: Pattern.Variable projected } access
                || projected.Name == VariableScopeNames.Wildcard
                || fallback.Name == VariableScopeNames.Wildcard || returned.Name != fallback.Name)
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(projected.Name, StringComparer.Ordinal)
                || parameters.Contains(fallback.Name, StringComparer.Ordinal)
                || ReadEntryCall(read, parameters) is not { } selector
                || tuple.Items.OfType<Expr.Call>().ToArray() is not [Expr.Call
                    { DynamicRemote: false, Arguments: [Expr.Variable argument] } convert]
                || argument.Name != projected.Name)
            {
                return false;
            }

            string? module;
            if (convert.Function == AutoImportNames.BinaryToTerm && owner.ResolvesErlangBif(convert))
            {
                module = ErlangModuleNames.Module;
            }
            else if (!TryResolveOrdinaryCall(convert, out module))
            {
                return false;
            }

            var prepared = tuple with
            {
                Items = tuple.Items.Select(item => ReferenceEquals(item, convert) ? new Expr.Variable(projected.Name) : item).ToArray()
            };
            if (tuple.Items.OfType<Expr.Variable>().Any()
                || !TryReadBoundTupleOperands(prepared, projected.Name, new Dictionary<string, int>(StringComparer.Ordinal), out var operands))
            {
                return false;
            }

            int unmatched = Label();
            EmitEntryCallWithEmptyFrame(selector, parameters);
            EmitDetailRecordTest(0, pattern.Items.Count, access.Tag, unmatched);
            owner.EmitTuplePart(0, access.Index, 0);
            owner.EmitCallLine(convert, CallLocation(read, location));
            Call(module, convert.Function, 1);
            EmitTupleWithX0(operands);
            owner.EmitFrameReturn(0);
            Mark(unmatched);
            owner.EmitFrameReturn(0);

            return true;
        }

        private bool TryEmitEntryTaggedProjection(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Case { Value: Expr.Call read } decision
                || decision.Clauses.Count is not (2 or 3)
                || decision.Clauses[^2] is not
                {
                    Patterns: [Pattern.Tuple tuple], Guard: null, Body: Expr.Variable returned
                }
                || ReadSingleTupleField(tuple) is not { Field: Pattern.Variable field } projected
                || field.Name != returned.Name
                || decision.Clauses[^1] is not
                {
                    Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Guard: null, Body: var fallback
                }
                || !AttributeLiteral.TryNormalizeConstant(fallback, out Term otherwise))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(field.Name, StringComparer.Ordinal)
                || ReadEntryCall(read, parameters) is not { } plan)
            {
                return false;
            }

            Expr.Call? convert = null;
            if (decision.Clauses.Count == 3)
            {
                if (decision.Clauses[0] is not
                    {
                        Patterns: [Pattern.Tuple guardedTuple],
                        Guard: Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                            Function: AutoImportNames.IsList, Arguments: [Expr.Variable tested]
                        } guard,
                        Body: Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                            Function: AutoImportNames.ListToAtom, Arguments: [Expr.Variable argument]
                        } conversion
                    }
                    || guardedTuple.Items.Count != tuple.Items.Count
                    || ReadSingleTupleField(guardedTuple) is not { Field: Pattern.Variable guardedField } guarded
                    || guarded.Index != projected.Index || !guarded.Tag.Equals(projected.Tag)
                    || tested.Name != guardedField.Name || argument.Name != guardedField.Name
                    || parameters.Contains(guardedField.Name, StringComparer.Ordinal)
                    || !owner.ResolvesErlangBif(guard) || !owner.ResolvesErlangBif(conversion))
                {
                    return false;
                }

                convert = conversion;
            }

            int returnField = convert is null ? 0 : Label();
            int unmatched = Label();
            EmitEntryCallWithEmptyFrame(plan, parameters);
            EmitDetailRecordTest(0, tuple.Items.Count, projected.Tag, unmatched);
            owner.EmitTuplePart(0, projected.Index, 0);
            if (convert is not null)
            {
                owner.Op(IsList, LabelTag, returnField);
                owner.Operand(XRegisterTag, 0);
                owner.EmitCallLine(convert, CallLocation(read, location));
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, convert.Function, 1));
                owner.Operand(BeamWire.UnsignedTag, 0);
                Mark(returnField);
            }

            owner.EmitFrameReturn(0);
            Mark(unmatched);
            owner.EmitConstant(otherwise, 0);
            owner.EmitFrameReturn(0);

            return true;
        }

        private void EmitEntryCallWithEmptyFrame(EntryCall plan, string[] parameters)
        {
            EmitFrameAllocation(EntryCallLiveCount(plan, parameters), 0);
            EmitEntryCall(plan);
        }

        private static int EntryCallLiveCount(EntryCall plan, string[] parameters) =>
            plan.Call.Arguments.OfType<Expr.Variable>()
                .Select(argument => Array.IndexOf(parameters, argument.Name) + 1).DefaultIfEmpty(0).Max();

        private bool TryEmitTaggedCallProjection(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [],
                Body: Expr.Sequence { Items: [
                    Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag },
                            Pattern.Variable projected, Pattern.Variable carried] },
                        Value: Expr.Call { Module: not (null or ErlangModuleNames.Module),
                            DynamicRemote: false, Arguments: [] } read
                    } match,
                    Expr.Match
                    {
                        Pattern: Pattern.Variable saved,
                        Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [] } local
                    },
                    Expr.Match
                    {
                        Pattern: Pattern.Variable converted,
                        Value: Expr.Call { Module: not (null or ErlangModuleNames.Module),
                            DynamicRemote: false, Arguments: [var literal, Expr.Variable input] } convert
                    },
                    Expr.Tuple { Items: [Expr.Variable first, Expr.Variable second, Expr.Variable third] }
                ] }
            }] || !DistinctBindings(projected, carried, saved, converted)
                || input.Name != projected.Name || first.Name != saved.Name
                || second.Name != converted.Name || third.Name != carried.Name
                || !AttributeLiteral.TryNormalizeConstant(literal, out Term argument)
                || !entries.TryGetValue((local.Function, 0), out int localEntry))
            {
                return false;
            }

            int failure = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(YRegisterTag, 1);
            owner.EmitCallLine(read, location);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(read.Module!, read.Function, 0));
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 1);
            owner.Op(IsTaggedTuple, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.LiteralOperand(tag);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(YRegisterTag, 0);
            owner.EmitCallLine(local, CallLocation(read, location));
            owner.EmitFieldCall(0, localEntry);
            owner.Op(GetTupleElement, YRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 1);
            owner.EmitConstant(argument, 0);
            owner.EmitCallLine(convert, CallLocation(local, location));
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(convert.Module!, convert.Function, 2));
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (YRegisterTag, 1), (XRegisterTag, 0), (YRegisterTag, 0));
            owner.EmitFrameReturn(2);
            Mark(failure);
            owner.EmitLine(new(location.File, match.SourceLine == 0 ? location.Line : match.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }
    }
}
