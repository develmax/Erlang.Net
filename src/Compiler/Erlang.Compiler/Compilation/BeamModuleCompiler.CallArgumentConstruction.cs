using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private readonly record struct ConstructionOperand(int Tag, int Register, Term? Literal = null);

        private sealed record ArgumentConstruction(
            int Register,
            bool IsTuple,
            ConstructionOperand[] Operands,
            int? MapLive
        )
        {
            internal int HeapWords => MapLive.HasValue ? 0 : IsTuple ? Operands.Length + 1 : ConsHeapWords;
        }

        private sealed record CallArgumentConstructions(
            List<ArgumentConstruction> Constructions,
            List<(int Destination, int Source)> Loads,
            List<(int Destination, Term Value)> Constants,
            List<(int Destination, int Source)> Moves,
            int Live
        );

        private (EntryCall Call, CallArgumentConstructions Arguments)? ReadConstructedSelectorCall(
            Expr.Call selector,
            string[] parameters
        )
        {
            if (owner.ResolvesErlangBif(selector) || !TryResolveOrdinaryCallTarget(selector, out string? module)
                || !selector.Arguments.Where((argument, index) => index < parameters.Length
                    && argument is Expr.Variable { Name: not VariableScopeNames.Wildcard } input
                    && input.Name == parameters[index]).Any())
            {
                return null;
            }

            var inputs = parameters.Select((name, register) => (name, register))
                .Where(input => input.name != VariableScopeNames.Wildcard)
                .ToDictionary(input => input.name, input => input.register, StringComparer.Ordinal);
            if (ReadCallArgumentConstructions(
                selector.Arguments,
                -1,
                new Dictionary<string, int>(),
                inputs: inputs
            ) is not { Moves.Count: 0, Loads.Count: 0, Constructions.Count: >= 2 } plan)
            {
                return null;
            }

            return (new EntryCall(
                selector,
                module,
                [],
                []
            ), plan);
        }

        private static CallArgumentConstructions? ReadCallArgumentConstructions(
            IReadOnlyList<Expr> arguments,
            int result,
            IReadOnlyDictionary<string, int> saved,
            string? registerValue = null,
            IReadOnlyDictionary<string, int>? inputs = null,
            IReadOnlyList<(string Name, Expr Value)>? bindings = null,
            bool allowMap = false,
            bool preserveX0 = false,
            bool stageConsOperands = false
        )
        {
            var namedReads = inputs is null ? null : (bindings?.Select(binding => binding.Value) ?? []).Concat(arguments)
                .SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Variable>()
                .GroupBy(variable => variable.Name).ToDictionary(group => group.Key, group => group.Count());
            var inputReads = inputs?.Where(input => namedReads!.ContainsKey(input.Key)).GroupBy(input => input.Value)
                .ToDictionary(group => group.Key, group => group.Sum(input => namedReads![input.Key]));
            if (preserveX0 && inputReads is not null)
            {
                // Keep the previous result live through the heap check and every constructor.
                inputReads[0] = inputReads.GetValueOrDefault(0) + 1;
            }

            var locals = bindings is null ? null : new Dictionary<string, ConstructionOperand>(StringComparer.Ordinal);
            var occupied = inputReads is null ? new HashSet<int> { 0 } : inputReads.Keys.ToHashSet();
            var plan = new CallArgumentConstructions(
                [],
                [],
                [],
                [],
                inputReads is null ? 1 : occupied.DefaultIfEmpty(-1).Max() + 1
            );
            foreach (var (name, value) in bindings ?? [])
            {
                if (!namedReads!.TryGetValue(name, out int reads))
                {
                    continue;
                }

                if (Read(value, -1) is not { Tag: XRegisterTag } operand)
                {
                    return null;
                }

                locals!.Add(name, operand);
                inputReads![operand.Register] = reads;
            }

            for (int index = 0; index < arguments.Count; index++)
            {
                if (index == result)
                {
                    continue;
                }

                if (Read(arguments[index], index) is not { } operand)
                {
                    return null;
                }

                if (operand.Literal is { } literal)
                {
                    plan.Constants.Add((index, literal));
                }
                else if (operand.Tag == YRegisterTag)
                {
                    plan.Loads.Add((index, operand.Register));
                }
                else if (operand.Register != index)
                {
                    plan.Moves.Add((index, operand.Register));
                }
            }

            int mapIndex = plan.Constructions.FindIndex(construction => construction.MapLive.HasValue);
            if (mapIndex >= 0)
            {
                // The map runs its own heap check. Allocate preceding tuples and cons cells before it.
                if (mapIndex != plan.Constructions.Count - 1)
                {
                    return null;
                }

                int live = plan.Constructions[mapIndex].MapLive!.Value;
                var initialized = plan.Constructions.Take(mapIndex).Select(construction => construction.Register).ToHashSet();
                for (int register = plan.Live; register < live; register++)
                {
                    if (!initialized.Contains(register))
                    {
                        return null;
                    }
                }
            }

            return plan.Constructions.Count == 0 ? null : plan;

            ConstructionOperand? Read(Expr expression, int destination)
            {
                if (expression is Expr.RecordConstruction record)
                {
                    expression = RecordExpressions.Create(record);
                }

                if (AttributeLiteral.TryNormalizeConstant(expression, out Term literal))
                {
                    return new ConstructionOperand(0, 0, literal);
                }

                if (expression is Expr.Variable variable && saved.TryGetValue(variable.Name, out int slot))
                {
                    return new ConstructionOperand(YRegisterTag, slot);
                }

                if (expression is Expr.Variable input && input.Name == registerValue)
                {
                    return new ConstructionOperand(XRegisterTag, 0);
                }

                if (expression is Expr.Variable register && inputs?.TryGetValue(register.Name, out int source) == true)
                {
                    return new ConstructionOperand(XRegisterTag, source);
                }

                if (expression is Expr.Variable bound && locals?.TryGetValue(bound.Name, out var operand) == true)
                {
                    return operand;
                }

                if (allowMap && expression is Expr.Map { Base: null } map)
                {
                    var fields = new SortedDictionary<Term, ConstructionOperand>(
                        Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true)));
                    foreach (MapField field in map.Fields)
                    {
                        if (field.Exact || !AttributeLiteral.TryNormalizeConstant(field.Key, out Term key)
                            || Read(field.Value, -1) is not { } value || !fields.TryAdd(key, value))
                        {
                            return null;
                        }
                    }

                    int live = occupied.DefaultIfEmpty(-1).Max() + 1;

                    return Add(
                        destination,
                        false,
                        fields.SelectMany(field => new[] { new ConstructionOperand(0, 0, field.Key), field.Value }).ToArray(),
                        live
                    );
                }

                if (expression is not (Expr.Tuple or Expr.List))
                {
                    return null;
                }

                if (expression is Expr.Tuple tuple)
                {
                    return ReadTupleFields(tuple) is { } fields ? Add(destination, true, fields) : null;
                }

                var list = (Expr.List)expression;
                if (stageConsOperands)
                {
                    return ReadStagedList(list, 0, destination);
                }

                if (Read(list.Tail ?? new Expr.Literal(Nil.Value), list.Items.Count == 0 ? destination : -1) is not { } tail)
                {
                    return null;
                }

                for (int head = list.Items.Count - 1; head >= 0; head--)
                {
                    if (Read(list.Items[head], -1) is not { } item)
                    {
                        return null;
                    }

                    if (item.Literal is { } value && tail.Literal is { } suffix)
                    {
                        tail = new ConstructionOperand(0, 0, new Cons(value, suffix));
                    }
                    else if (Add(head == 0 ? destination : -1, false, [item, tail]) is { } cons)
                    {
                        tail = cons;
                    }
                    else
                    {
                        return null;
                    }
                }

                return tail;
            }

            ConstructionOperand[]? ReadTupleFields(Expr.Tuple tuple)
            {
                var fields = new ConstructionOperand[tuple.Items.Count];
                for (int field = 0; field < fields.Length; field++)
                {
                    if (Read(tuple.Items[field], -1) is not { } value)
                    {
                        return null;
                    }

                    fields[field] = value;
                }

                return fields;
            }

            ConstructionOperand? ReadStagedList(Expr.List list, int index, int destination)
            {
                if (index == list.Items.Count)
                {
                    return Read(list.Tail ?? new Expr.Literal(Nil.Value), destination);
                }

                // Core evaluates tuple fields before the suffix, then materializes
                // the cons operands in reverse. Only tuple heads are admitted here.
                if (list.Items[index] is not Expr.Tuple head)
                {
                    return null;
                }

                ConstructionOperand[]? fields = null;
                ConstructionOperand? operand = null;
                if (AttributeLiteral.TryNormalizeConstant(head, out Term literal))
                {
                    operand = new ConstructionOperand(0, 0, literal);
                }
                else if (ReadTupleFields(head) is not { } prepared)
                {
                    return null;
                }
                else
                {
                    fields = prepared;
                }

                if (ReadStagedList(list, index + 1, -1) is not { } tail)
                {
                    return null;
                }

                operand ??= Add(-1, true, fields!);
                if (operand is not { } item)
                {
                    return null;
                }

                return item.Literal is { } value && tail.Literal is { } suffix
                    ? new ConstructionOperand(0, 0, new Cons(value, suffix))
                    : Add(destination, false, [item, tail]);
            }

            ConstructionOperand? Add(
                int destination,
                bool tuple,
                ConstructionOperand[] operands,
                int? mapLive = null
            )
            {
                foreach (var operand in operands)
                {
                    if (operand.Tag == XRegisterTag)
                    {
                        if (inputReads?.TryGetValue(operand.Register, out int reads) == true && reads > 0)
                        {
                            inputReads[operand.Register] = --reads;
                            if (reads > 0)
                            {
                                continue;
                            }
                        }

                        occupied.Remove(operand.Register);
                    }
                }

                if (destination < 0 || inputs is not null && occupied.Contains(destination))
                {
                    destination = registerValue is null && inputs is null ? 1 : 0;
                    while (occupied.Contains(destination))
                    {
                        destination++;
                    }
                }

                if (destination >= FirstReservedXRegister || !occupied.Add(destination))
                {
                    return null;
                }

                plan.Constructions.Add(new ArgumentConstruction(
                    destination,
                    tuple,
                    operands,
                    mapLive
                ));

                return new ConstructionOperand(XRegisterTag, destination);
            }
        }

        private void EmitCallArgumentConstructions(
            CallArgumentConstructions plan,
            int result,
            int[]? stackSlots = null,
            bool heapChecked = false,
            int? spillSlot = null,
            Action? afterConstruction = null
        )
        {
            int heapWords = plan.Constructions.Sum(item => item.HeapWords);
            if (!heapChecked && heapWords > 0)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, heapWords);
                owner.Operand(BeamWire.UnsignedTag, plan.Live);
            }

            foreach (var construction in plan.Constructions)
            {
                if (construction.MapLive is int live)
                {
                    EmitMapInstruction(
                        exact: false,
                        failure: 0,
                        source: null,
                        live: live,
                        count: construction.Operands.Length / 2,
                        destination: construction.Register
                    );
                }
                else if (construction.IsTuple)
                {
                    owner.EmitTupleHeader(XRegisterTag, construction.Operands.Length, construction.Register);
                }
                else
                {
                    owner.instructions.WriteByte(PutList);
                }

                foreach (var operand in construction.Operands)
                {
                    if (operand.Literal is { } literal)
                    {
                        owner.LiteralOperand(literal);
                    }
                    else
                    {
                        owner.Operand(operand.Tag, operand.Tag == YRegisterTag ? StackSlot(operand.Register) : operand.Register);
                    }
                }

                if (!construction.IsTuple && !construction.MapLive.HasValue)
                {
                    owner.Operand(XRegisterTag, construction.Register);
                }
            }

            afterConstruction?.Invoke();

            if (spillSlot is int slot)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    slot
                );
            }

            if (result > 0)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    XRegisterTag,
                    result
                );
            }

            var loads = stackSlots is null ? plan.Loads
                : plan.Loads.Select(move => (move.Destination, StackSlot(move.Source))).ToList();
            owner.EmitOrderedTailMoves(plan.Moves, plan.Constants, loads);

            int StackSlot(int slot) => stackSlots is null ? slot : stackSlots[slot];
        }
    }
}
