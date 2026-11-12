using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record CallResultCheck(
            Expr.Match Match,
            Atom? Tag,
            int? Arity,
            int? Field,
            int Destination = 0,
            (int Field, string Name)[]? Bindings = null,
            (int Field, int Register, bool Saved)[]? Projections = null,
            bool ShapeKnown = false,
            int? SavedTupleSlot = null
        );

        private sealed record DeferredCallTuple(
            string Binding,
            int Consumer,
            (int Field, string Name)[] Fields
        );

        private sealed record TupleFieldCase(
            int Field,
            Term Key,
            TupleFieldArm Match,
            TupleFieldArm Fallback
        );

        private sealed record TupleFieldArm(
            CaseTupleOperand[] Operands, (int Field, int Register)[] Projections, int Live
        );

        private static TupleFieldCase? ReadTupleFieldCase(
            Expr.Case decision, (int Field, string Name)[] fields, Dictionary<string, int> saved
        )
        {
            if (decision is not
                {
                    Value: Expr.Variable selector,
                    Clauses: [
                    { Guard: null, Patterns: [var pattern], Body: Expr.Tuple match },
                    { Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Tuple alternate }
                    ]
                }
                || saved.ContainsKey(fallback.Name) || fields.Any(field => field.Name == fallback.Name))
            {
                return null;
            }

            Term? key = pattern switch
            {
                Pattern.Literal literal => literal.Value,
                Pattern.List { Items.Count: 0, Tail: null } => Nil.Value,
                _ => null
            };
            if (key is null || ReadTupleFieldArm(
                match,
                fields,
                saved,
                selector.Name,
                key
            ) is not { } matched
                || ReadTupleFieldArm(
                    alternate,
                    fields,
                    saved,
                    selector.Name,
                    alias: fallback.Name
                ) is not { } unmatched)
            {
                return null;
            }

            return new(
                fields.Single(field => field.Name == selector.Name).Field,
                key,
                matched,
                unmatched
            );
        }

        private static TupleFieldArm? ReadTupleFieldArm(
            Expr.Tuple tuple,
            (int Field, string Name)[] fields,
            Dictionary<string, int> saved,
            string? selector = null,
            Term? key = null,
            string? alias = null
        )
        {
            var projections = new Dictionary<int, int>();
            var operands = new CaseTupleOperand[tuple.Items.Count];
            bool usesSelector = false;
            for (int index = 0; index < operands.Length; index++)
            {
                Expr item = tuple.Items[index];
                if (item is Expr.Variable variable)
                {
                    if (variable.Name == selector || variable.Name == alias)
                    {
                        usesSelector |= key is null;
                        operands[index] = key is not null ? new(0, 0, key) : new(XRegisterTag, 1, null);
                        continue;
                    }

                    if (saved.TryGetValue(variable.Name, out int slot))
                    {
                        operands[index] = new(YRegisterTag, slot, null);
                        continue;
                    }

                    int fieldIndex = Array.FindIndex(fields, candidate => candidate.Name == variable.Name);
                    if (fieldIndex < 0 || projections.Count >= FirstReservedXRegister - 2)
                    {
                        return null;
                    }

                    int field = fields[fieldIndex].Field;
                    int register = projections.Count == 0 || selector is null ? projections.Count : projections.Count + 1;
                    projections.TryAdd(field, register);
                    operands[index] = new(XRegisterTag, projections[field], null);
                }
                else if (AttributeLiteral.TryNormalizeConstant(item, out Term constant))
                {
                    operands[index] = new(0, 0, constant);
                }
                else
                {
                    return null;
                }
            }

            int live = usesSelector ? 2 : projections.Count == 0 ? 0 : 1;

            return new(
                operands,
                projections.OrderBy(projection => projection.Value == 0)
                .Select(projection => (projection.Key, projection.Value)).ToArray(),
                live
            );
        }

        private void EmitTupleFieldCase(TupleFieldCase plan, int frame)
        {
            owner.EmitTuplePart(0, plan.Field, 1);
            int fallback = Label();
            owner.Op(plan.Key is Nil ? IsNil : ExactEqualTest, LabelTag, fallback);
            owner.Operand(XRegisterTag, 1);
            if (plan.Key is not Nil)
            {
                owner.LiteralOperand(plan.Key);
            }

            EmitTupleFieldArm(plan.Match, frame);
            Mark(fallback);
            EmitTupleFieldArm(plan.Fallback, frame);
        }

        private void EmitTupleFieldArm(TupleFieldArm arm, int frame)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, arm.Operands.Length + 1);
            owner.Operand(BeamWire.UnsignedTag, arm.Live);
            foreach (var (field, register) in arm.Projections)
            {
                owner.EmitTuplePart(0, field, register);
            }

            owner.EmitTupleHeader(XRegisterTag, arm.Operands.Length);
            foreach (var operand in arm.Operands)
            {
                EmitCaseOperand(operand);
            }

            owner.EmitFrameReturn(frame);
        }

        private bool TryEmitCheckedCallSequence(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence { Items: [_, .., var result] } sequence
                || !sequence.Items.Any(item => ReadTupleCallCheck(item) is not null))
            {
                return false;
            }

            Term? constant = null;
            if (result is not Expr.Call && !AttributeLiteral.TryNormalizeConstant(result, out constant))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            var statements = constant is null ? sequence.Items : sequence.Items.Take(sequence.Items.Count - 1);
            var items = FlattenCallArguments(statements);
            if (ReadCallsWithChecks(items, parameters, out var checks) is not { Length: > 0 } calls
                || ReadSavedCallSequence(
                    calls,
                    parameters,
                    extraction: null,
                    constant: constant,
                    checks: checks
                ) is not { } plan)
            {
                return false;
            }

            EmitSavedCallSequence(plan);

            return true;
        }

        private (Expr.Call Call, string? Result)[]? ReadCallsWithChecks(
            IReadOnlyList<Expr> items,
            string[] parameters,
            out Dictionary<int, CallResultCheck> checks
        )
        {
            checks = [];
            var rewritten = items.ToArray();
            var names = parameters.ToHashSet(StringComparer.Ordinal);
            for (int index = 0; index < items.Count; index++)
            {
                if (ReadTupleCallCheck(items[index]) is { } tuple)
                {
                    var (check, binding) = tuple;
                    if (check.Bindings?.Any(field => !names.Add(field.Name)) == true
                        || binding is not null && !names.Add(binding))
                    {
                        return null;
                    }

                    checks.Add(index, check);
                    rewritten[index] = check.Match with
                    {
                        Pattern = new Pattern.Variable(binding ?? VariableScopeNames.Wildcard)
                    };
                }
                else if (items[index] is Expr.Match { Pattern: Pattern.Literal { Value: Atom atom }, Value: Expr.Call } exact)
                {
                    checks.Add(index, new CallResultCheck(
                        exact,
                        atom,
                        null,
                        null
                    ));
                    rewritten[index] = exact with { Pattern = new Pattern.Variable(VariableScopeNames.Wildcard) };
                }
                else if (items[index] is Expr.Match { Pattern: Pattern.Variable variable, Value: Expr.Call }
                    && variable.Name != VariableScopeNames.Wildcard && !names.Add(variable.Name))
                {
                    return null;
                }
            }

            return ReadStraightCalls(rewritten, parameters);
        }

        private static (CallResultCheck Check, string? Binding)? ReadTupleCallCheck(Expr expression)
        {
            if (ReadSimpleTaggedCallMatch(expression) is { } tagged)
            {
                var (check, payload) = tagged;

                return (new(
                    check.Match,
                    check.Tag,
                    check.Arity,
                    payload?.Field
                ), payload?.Name);
            }

            if (expression is not Expr.Match { Pattern: Pattern.Tuple tuple, Value: Expr.Call } match)
            {
                return null;
            }

            Atom? tag = HasTupleTag(tuple, out Atom atom) ? atom : null;
            var fields = new List<(int Field, string Name)>();
            for (int index = tag is null ? 0 : 1; index < tuple.Items.Count; index++)
            {
                if (tuple.Items[index] is not Pattern.Variable variable)
                {
                    return null;
                }

                if (variable.Name != VariableScopeNames.Wildcard)
                {
                    fields.Add((index, variable.Name));
                }
            }

            if (fields is [var field])
            {
                return (new(
                    match,
                    tag,
                    tuple.Items.Count,
                    field.Field
                ), field.Name);
            }

            return (new(
                match,
                tag,
                tuple.Items.Count,
                null,
                Bindings: fields.Count == 0 ? null : fields.ToArray()
            ), null);
        }

        private static string[] ReadCallResultRegisters(
            CallResultCheck check,
            int nextCall,
            Expr.Call consumer,
            IReadOnlyDictionary<string, int> lastReads,
            Dictionary<string, int> saved,
            List<int> lastSlotReads,
            Dictionary<int, CallResultCheck> checks
        )
        {
            var fields = check.Bindings!.Where(field => lastReads.GetValueOrDefault(field.Name, -1) == nextCall).ToArray();
            var registers = new string[fields.Length];
            var projections = new List<(int Field, int Register, bool Saved)>();
            for (int index = 0; index < fields.Length; index++)
            {
                int register = (index + 1) % fields.Length;
                registers[register] = fields[index].Name;
                projections.Add((fields[index].Field, register, false));
            }

            var occupied = saved.Values.ToHashSet();
            int slot = 0;
            foreach (var (field, name) in check.Bindings!)
            {
                int lastUse = lastReads.GetValueOrDefault(name, -1);
                if (lastUse <= nextCall)
                {
                    continue;
                }

                while (occupied.Contains(slot))
                {
                    slot++;
                }

                saved.Add(name, slot);
                if (slot == lastSlotReads.Count)
                {
                    lastSlotReads.Add(lastUse);
                }
                else
                {
                    lastSlotReads[slot] = lastUse;
                }

                projections.Add((field, slot, true));
                slot++;
            }

            if (fields is [var single] && projections.Count > 1)
            {
                int[] destinations = Enumerable.Range(0, consumer.Arguments.Count)
                    .Where(index => consumer.Arguments[index] is Expr.Variable variable && variable.Name == single.Name)
                    .ToArray();
                int destination = destinations is [int argument] && argument > 0 ? argument
                    : projections.Any(field => field.Saved && field.Field > single.Field) ? 1 : 0;
                if (destination != 0)
                {
                    registers = Enumerable.Repeat(VariableScopeNames.Wildcard, destination + 1).ToArray();
                    registers[destination] = single.Name;
                    projections[0] = (single.Field, destination, false);
                }
            }

            checks[nextCall - 1] = check with
            {
                // X0 holds the source tuple until the final projection.
                Projections = projections.OrderBy(field => !field.Saved && field.Register == 0)
                    .ThenBy(field => field.Field).ToArray()
            };

            return registers;
        }

        private static IEnumerable<string> ReadAdditionalCallResultBindings(Dictionary<int, CallResultCheck>? checks) =>
            checks?.Values.SelectMany(check => check.Bindings ?? []).Select(field => field.Name) ?? [];

        private static DeferredCallTuple? ReadDeferredCallTuple(
            CallResultCheck check,
            int nextCall,
            (Expr.Call Call, string? Result)[] calls,
            IReadOnlyDictionary<string, int> firstReads,
            IReadOnlyDictionary<string, int> lastReads
        )
        {
            var fields = check.Bindings!.Where(field => firstReads.GetValueOrDefault(field.Name, -1) > nextCall
                && firstReads[field.Name] == lastReads[field.Name] && lastReads[field.Name] < calls.Length).ToArray();
            if (fields.Length < 2)
            {
                return null;
            }

            int consumer = lastReads[fields[0].Name];
            if (fields.Any(field => lastReads[field.Name] != consumer)
                || !calls[consumer].Call.Arguments.All(IsPreparedOperand))
            {
                return null;
            }

            return new(FormattableString.Invariant($"\0checked-tuple-{nextCall}"), consumer, fields);
        }

        private static void PrepareCallResultChecks(
            (Expr.Call Call, string? Result)[] calls,
            Dictionary<string, int> lastReads,
            List<SavedCallStep> steps,
            CallSequenceTupleSource? tuple,
            Dictionary<int, CallResultCheck> checks
        )
        {
            foreach (int index in checks.Keys.ToArray())
            {
                CallResultCheck check = checks[index];
                if (check.Field is null || calls[index].Result is not { } name)
                {
                    continue;
                }

                int lastUse = lastReads.GetValueOrDefault(name, -1);
                if (lastUse <= index && (index < calls.Length - 1 || tuple?.Reads.Contains(name) != true))
                {
                    checks[index] = check with { Field = null };
                    continue;
                }

                if (index >= steps.Count || lastUse != index + 1
                    || steps[index] is not { Spill: null, Closure: null } next
                    || next.Call is not { ConstructedArgument: null, RegisterConstructions: null } call)
                {
                    continue;
                }

                int[] uses = Enumerable.Range(0, call.Call.Arguments.Count)
                    .Where(argument => call.Call.Arguments[argument] is Expr.Variable variable && variable.Name == name)
                    .ToArray();
                if (uses is [int destination] && destination != 0 && call.Moves.Remove((destination, 0)))
                {
                    checks[index] = check with { Destination = destination };
                }
            }
        }

        private void EmitCallResultCheck(CallResultCheck check, int failure, int frame)
        {
            if (check.SavedTupleSlot is int slot)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    frame - 1 - slot
                );
            }

            if (check.Arity is int arity)
            {
                if (check.Tag is { } tag)
                {
                    EmitDetailRecordTest(
                        0,
                        arity,
                        tag,
                        failure
                    );
                }
                else if (!check.ShapeKnown)
                {
                    EmitTupleShapeTest(
                        XRegisterTag,
                        0,
                        arity,
                        failure
                    );
                }

                if (check.Field is int field)
                {
                    owner.EmitTuplePart(0, field, check.Destination);
                }

                if (check.Projections is { } projections)
                {
                    foreach (var (element, register, saved) in projections)
                    {
                        owner.Op(GetTupleElement, XRegisterTag, 0);
                        owner.Operand(BeamWire.UnsignedTag, element);
                        owner.Operand(saved ? YRegisterTag : XRegisterTag, saved ? frame - 1 - register : register);
                    }
                }
            }
            else
            {
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, 0);
                owner.LiteralOperand(check.Tag!);
            }
        }
    }
}
