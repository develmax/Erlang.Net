using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitReusedTupleCaseTail(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Sequence { Items:
                [
                    Expr.Match { Pattern: Pattern.Variable name, Value: Expr.Call read },
                    Expr.Match { Pattern: Pattern.Variable key, Value: Expr.Tuple tuple },
                    Expr.Case
                    {
                        Value: Expr.Call test,
                        Clauses:
                        [
                            { Guard: null, Patterns: [Pattern.Literal { Value: Atom or Integer or Nil } matched], Body: Expr.Call tail },
                            { Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Variable returned }
                        ]
                    }
                ]
            } } clause] || !HasUnconditionalParameters(clause)
                || !DistinctBindings(name, key, fallback) || fallback.Name != returned.Name
                || test.Arguments is not [Expr.Variable first, Expr.Variable second]
                || tail.Arguments is not [Expr.Variable nextFirst, Expr.Variable nextSecond]
                || first.Name != name.Name || second.Name != key.Name
                || nextFirst.Name != name.Name || nextSecond.Name != key.Name
                || !TryResolveOrdinaryCall(test, out string? testModule)
                || !TryResolveOrdinaryCall(tail, out string? tailModule))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(name.Name) || parameters.Contains(key.Name) || parameters.Contains(fallback.Name)
                || ReadEntryCall(read, parameters) is not { } initial)
            {
                return false;
            }

            int[] sources = tuple.Items.Select(item => item is Expr.Variable { Name: not VariableScopeNames.Wildcard } variable
                ? Array.IndexOf(parameters, variable.Name)
                : -1).ToArray();
            if (sources.Length < 2 || sources.Contains(-1) || sources.Distinct().Count() != sources.Length)
            {
                return false;
            }

            int frame = sources.Length;
            int keySlot = frame - 1;
            int nameSlot = frame - 2;
            var slots = sources.OrderDescending().Select((register, slot) => (register, slot))
                .ToDictionary(item => item.register, item => item.slot);
            EmitFrameAllocation(function.Arity, frame);
            foreach (var (register, slot) in slots)
            {
                owner.MoveRegister(XRegisterTag, register, YRegisterTag, slot);
            }

            EmitEntryCall(initial);
            owner.Op(TestHeap, BeamWire.UnsignedTag, frame + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(YRegisterTag, frame, keySlot);
            foreach (int source in sources)
            {
                owner.Operand(YRegisterTag, slots[source]);
            }

            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, nameSlot);
            owner.MoveRegister(YRegisterTag, keySlot, XRegisterTag, 1);
            if (nameSlot > 0)
            {
                owner.EmitSlotKills(Enumerable.Range(0, nameSlot).ToArray());
            }

            var previous = CallLocation(read, location);
            owner.EmitCallLine(test, previous);
            Call(testModule, test.Function, 2);
            int failure = Label();
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(matched.Value);
            owner.MoveRegister(YRegisterTag, keySlot, XRegisterTag, 1);
            owner.MoveRegister(YRegisterTag, nameSlot, XRegisterTag, 0);
            if (tailModule is null)
            {
                owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
                owner.Operand(LabelTag, entries[(tail.Function, 2)]);
            }
            else
            {
                owner.EmitCallLine(tail, CallLocation(test, previous));
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(tailModule, tail.Function, 2));
            }

            owner.Operand(BeamWire.UnsignedTag, frame);
            Mark(failure);
            owner.EmitFrameReturn(frame);

            return true;
        }

        private bool TryEmitEmbeddedConstructorTail(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null } clause] || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            IReadOnlyList<Expr> sequence = clause.Body is Expr.Sequence block ? block.Items : [clause.Body];
            if (sequence.Count is < 1 or > 3 || sequence[^1] is not Expr.Call finish)
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            var arguments = finish.Arguments.ToArray();
            int prefixLength = sequence.Count - 1;
            string? tupleBinding = null;
            int argument;
            Expr? tuple;
            if (prefixLength > 0
                && sequence[^2] is Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.Tuple boundTuple }
                && boundTuple.Items.Any(item => item is Expr.Tuple or Expr.List or Expr.Binary { Operator: "++" }))
            {
                if (binding.Name == VariableScopeNames.Wildcard || parameters.Contains(binding.Name, StringComparer.Ordinal))
                {
                    return false;
                }

                tupleBinding = binding.Name;
                argument = Array.FindIndex(arguments, value => value is Expr.Variable variable && variable.Name == tupleBinding);
                if (argument < 0 || arguments.Skip(argument + 1).Any(value => value is Expr.Variable variable && variable.Name == tupleBinding))
                {
                    return false;
                }

                tuple = boundTuple;
                prefixLength--;
            }
            else
            {
                argument = Array.FindIndex(arguments, value => value is Expr.Tuple or Expr.RecordConstruction
                    && !AttributeLiteral.TryNormalizeConstant(value, out _));
                tuple = argument < 0 ? null : arguments[argument] switch
                {
                    Expr.RecordConstruction { Wildcard: null } record => RecordExpressions.Create(record),
                    Expr.Tuple directTuple => directTuple,
                    _ => null
                };
            }

            if (tuple is null || prefixLength > 1)
            {
                return false;
            }

            EntryCall? prefix = null;
            string? prefixName = null;
            if (prefixLength == 1)
            {
                if (sequence[0] is not Expr.Match { Pattern: Pattern.Variable saved, Value: Expr.Call read }
                    || saved.Name == tupleBinding || saved.Name == VariableScopeNames.Wildcard
                    || parameters.Contains(saved.Name, StringComparer.Ordinal)
                    || ReadEntryCall(read, parameters) is not { } entry)
                {
                    return false;
                }

                prefixName = saved.Name;
                prefix = entry;
            }

            const string result = "\0constructor-result";
            EntryCall? embedded = null;
            var appendResults = new HashSet<Expr>(ReferenceEqualityComparer.Instance);
            if (Rewrite(tuple) is not { } constructed || embedded is null)
            {
                return false;
            }

            arguments[argument] = constructed;
            finish = finish with { Arguments = arguments };
            var reads = arguments.SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Variable>()
                .Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
            var slots = new Dictionary<string, int>(StringComparer.Ordinal);
            if (prefixName is not null)
            {
                if (!reads.Contains(prefixName))
                {
                    return false;
                }

                slots.Add(prefixName, 0);
                reads.UnionWith(embedded.Call.Arguments.OfType<Expr.Variable>().Select(variable => variable.Name));
            }

            var copies = new List<(int Register, int Slot)>();
            for (int index = parameters.Length - 1; index >= 0; index--)
            {
                string name = parameters[index];
                if (name != VariableScopeNames.Wildcard && reads.Contains(name))
                {
                    copies.Add((index, slots.Count));
                    slots.Add(name, slots.Count);
                }
            }

            var inputs = new Dictionary<string, int>(StringComparer.Ordinal) { [result] = 0 };
            var previous = CallLocation(embedded.Call, prefix is null ? location : CallLocation(prefix.Call, location));
            return TryEmitRegisterConstructedArgument(finish, inputs, emitTests: EmitCalls,
                frame: (slots, slots.Count, previous, appendResults));

            void EmitCalls()
            {
                EmitFrameAllocation(parameters.Length, slots.Count);
                EmitFrameInitialization(0, prefix is null ? 0 : 1);
                foreach (var (register, slot) in copies)
                {
                    owner.MoveRegister(XRegisterTag, register, YRegisterTag, slot);
                }

                if (prefix is null)
                {
                    EmitEntryCall(embedded);
                    return;
                }

                EmitEntryCall(prefix);
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 0);
                var stack = new List<(int Destination, int Source)>();
                for (int index = 0; index < embedded.Call.Arguments.Count; index++)
                {
                    if (embedded.Call.Arguments[index] is Expr.Variable variable)
                    {
                        stack.Add((index, slots[variable.Name]));
                    }
                }

                owner.EmitOrderedTailMoves([], embedded.Constants, stack);
                owner.EmitCallLine(embedded.Call, CallLocation(prefix.Call, location));
                Call(embedded.Module, embedded.Call.Function, embedded.Call.Arguments.Count);
            }

            Expr? Rewrite(Expr expression)
            {
                switch (expression)
                {
                    case Expr.Call call when embedded is null && ReadEntryCall(call, parameters) is { } entry:
                        embedded = entry;
                        return new Expr.Variable(result);
                    case Expr.Tuple nested when RewriteItems(nested.Items) is { } fields:
                        return nested with { Items = fields };
                    case Expr.List list when RewriteItems(list.Items) is { } items:
                        Expr? tail = list.Tail is null ? null : Rewrite(list.Tail);
                        return list.Tail is null || tail is not null ? list with { Items = items, Tail = tail } : null;
                    case Expr.Binary { Operator: "++", Left: Expr.List { Tail: null, Items.Count: > 0 } left } append:
                        if (Rewrite(left with { Tail = append.Right }) is not { } appendResult)
                        {
                            return null;
                        }

                        appendResults.Add(appendResult);
                        return appendResult;
                    case Expr.Variable:
                        return expression;
                    default:
                        return AttributeLiteral.TryNormalizeConstant(expression, out _) ? expression : null;
                }
            }

            Expr[]? RewriteItems(IReadOnlyList<Expr> values)
            {
                var items = new Expr[values.Count];
                for (int index = 0; index < items.Length; index++)
                {
                    if (Rewrite(values[index]) is not { } item)
                    {
                        return null;
                    }

                    items[index] = item;
                }

                return items;
            }
        }

        private bool TryEmitRegisterConstructedArgument(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Call call } clause]
                || !HasUnconditionalParameters(clause) || function.Arity >= FirstReservedXRegister)
            {
                return false;
            }

            var inputs = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < function.Arity; index++)
            {
                var parameter = (Pattern.Variable)clause.Patterns[index];
                if (parameter.Name != VariableScopeNames.Wildcard)
                {
                    inputs.Add(parameter.Name, index);
                }
            }

            return TryEmitRegisterConstructedArgument(call, inputs);
        }

        private bool TryEmitRegisterConstructedArgument(
            Expr.Call call,
            IReadOnlyDictionary<string, int> inputs,
            (int Live, Action Emit)? extraction = null,
            Action? emitTests = null,
            (
                IReadOnlyDictionary<string, int> Slots,
                int Size,
                BeamSourceLocation PreviousCall,
                IReadOnlySet<Expr> AppendResults
            )? frame = null,
            bool validateOnly = false,
            bool allowControlTransfer = false
        )
        {
            if (call.Arguments.Count >= FirstReservedXRegister)
            {
                return false;
            }

            int argument = Array.FindIndex(call.Arguments.ToArray(),
                value => value is Expr.Tuple or Expr.List && !AttributeLiteral.TryNormalizeConstant(value, out _));
            if (argument < 0 || !TryResolveOrdinaryCall(call, out string? module, argument, allowControlTransfer))
            {
                return false;
            }

            var remaining = new int[FirstReservedXRegister];
            int live = 0;
            foreach (var variable in call.Arguments.SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Variable>())
            {
                if (!inputs.TryGetValue(variable.Name, out int register))
                {
                    if (frame is not { } saved || !saved.Slots.ContainsKey(variable.Name))
                    {
                        return false;
                    }

                    continue;
                }

                remaining[register]++;
                live = Math.Max(live, register + 1);
            }

            int heapWords = 0;
            int funCount = 0;
            var constructions = new List<(int Register, bool IsCons, (int Register, Term? Constant)[] Items,
                Expr.FunctionReference? Reference, int Entry)>();
            if (ReadOperand(call.Arguments[argument], argument) is not { } constructed)
            {
                return false;
            }

            var moves = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            var stack = new List<(int Destination, int Source)>();
            for (int index = 0; index < call.Arguments.Count; index++)
            {
                if (index == argument || call.Arguments[index] is Expr.Variable)
                {
                    int source = index == argument ? constructed.Register : ReadRegister(((Expr.Variable)call.Arguments[index]).Name);
                    if (source < 0)
                    {
                        stack.Add((index, ~source));
                    }
                    else if (source != index)
                    {
                        moves.Add((index, source));
                    }
                }
                else if (AttributeLiteral.TryNormalizeConstant(call.Arguments[index], out Term constant))
                {
                    constants.Add((index, constant));
                }
            }

            if (validateOnly)
            {
                return true;
            }

            emitTests?.Invoke();
            int heapLive = extraction?.Live ?? live;
            if (funCount == 0)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, heapWords);
                owner.Operand(BeamWire.UnsignedTag, heapLive);
            }
            else
            {
                owner.EmitClosureAllocation(heapWords, heapLive, funCount);
            }
            extraction?.Emit();
            foreach (var (register, isCons, items, reference, entry) in constructions)
            {
                if (reference is not null)
                {
                    owner.EmitDeclaredFunctionReference(reference, entry, register);
                    continue;
                }

                if (isCons)
                {
                    owner.instructions.WriteByte(PutList);
                }
                else
                {
                    owner.EmitTupleHeader(XRegisterTag, items.Length, register);
                }

                foreach (var (source, constant) in items)
                {
                    if (constant is not null)
                    {
                        owner.LiteralOperand(constant);
                    }
                    else
                    {
                        owner.Operand(source < 0 ? YRegisterTag : XRegisterTag, source < 0 ? ~source : source);
                    }
                }

                if (isCons)
                {
                    owner.Operand(XRegisterTag, register);
                }
            }

            if (frame is null)
            {
                owner.EmitArgumentMoves(moves, constants);
            }
            else
            {
                owner.EmitOrderedTailMoves(moves, constants, stack);
            }

            if (module is null)
            {
                owner.Op(frame is null ? TailLocalCall : TailLocalCallWithFrame, BeamWire.UnsignedTag, call.Arguments.Count);
                owner.Operand(LabelTag, entries[(call.Function, call.Arguments.Count)]);
            }
            else
            {
                owner.EmitCallLine(call, frame?.PreviousCall ?? location);
                owner.Op(frame is null ? TailExternalCall : TailExternalCallWithFrame, BeamWire.UnsignedTag, call.Arguments.Count);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(module, call.Function, call.Arguments.Count));
            }

            if (frame is { } allocated)
            {
                owner.Operand(BeamWire.UnsignedTag, allocated.Size);
            }

            return true;

            int ReadRegister(string name) => inputs.TryGetValue(name, out int register) ? register : ~frame!.Value.Slots[name];

            (int Register, Term? Constant)? ReadOperand(Expr expression, int destination = -1)
            {
                if (AttributeLiteral.TryNormalizeConstant(expression, out Term constant))
                {
                    return (-1, constant);
                }

                if (expression is Expr.Variable variable)
                {
                    return (ReadRegister(variable.Name), null);
                }

                if (expression is Expr.FunctionReference reference
                    && entries.TryGetValue((reference.Name, reference.Arity), out int entry))
                {
                    int register = Array.IndexOf(remaining, 0);
                    if (register < 0)
                    {
                        return null;
                    }

                    remaining[register]++;
                    funCount++;
                    constructions.Add((register, false, [], reference, entry));
                    return (register, null);
                }

                if (expression is Expr.List list)
                {
                    return ReadList(list, destination);
                }

                return expression is Expr.Tuple tuple && ReadItems(tuple.Items, scheduleAppends: true) is { } items
                    ? Construct(items, destination, isCons: false)
                    : null;
            }

            (int Register, Term? Constant)[]? ReadItems(IReadOnlyList<Expr> expressions, bool scheduleAppends = false)
            {
                var items = new (int Register, Term? Constant)[expressions.Count];
                IEnumerable<int> order = Enumerable.Range(0, expressions.Count);
                if (scheduleAppends && frame is { } current)
                {
                    // OTP evaluates append as a Core call before constructing the enclosing tuple.
                    order = order.OrderByDescending(index => current.AppendResults.Contains(expressions[index]));
                }

                foreach (int index in order)
                {
                    if (ReadOperand(expressions[index]) is not { } item)
                    {
                        return null;
                    }

                    items[index] = item;
                }

                return items;
            }

            (int Register, Term? Constant)? ReadList(Expr.List list, int destination)
            {
                if (list.Items.Count == 0 || ReadItems(list.Items) is not { } heads)
                {
                    return null;
                }

                (int Register, Term? Constant)? tail = list.Tail is null
                    ? (-1, Nil.Value)
                    : ReadOperand(list.Tail);
                for (int index = heads.Length - 1; index >= 0; index--)
                {
                    if (tail is not { } value)
                    {
                        return null;
                    }

                    tail = Construct([heads[index], value], index == 0 ? destination : -1, isCons: true);
                }

                return tail;
            }

            (int Register, Term? Constant)? Construct(
                (int Register, Term? Constant)[] items,
                int destination,
                bool isCons
            )
            {
                foreach (var (source, _) in items)
                {
                    if (source >= 0)
                    {
                        remaining[source]--;
                    }
                }

                if (destination < 0 || remaining[destination] != 0)
                {
                    destination = Array.IndexOf(remaining, 0);
                    if (destination < 0)
                    {
                        return null;
                    }
                }

                remaining[destination]++;
                heapWords += isCons ? ConsHeapWords : items.Length + 1;
                constructions.Add((destination, isCons, items, null, 0));

                return (destination, null);
            }
        }

        private bool TryEmitGuardedTaggedFieldTailCall(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                    {
                        Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable field] }],
                        Guard: Expr.Call { Function: "is_list", DynamicRemote: false, Arguments: [Expr.Variable tested] } guard,
                        Body: Expr.Call { DynamicRemote: false, Arguments: [Expr.Variable projected] } first
                    },
                    {
                        Patterns: [Pattern.Variable argument], Guard: null,
                        Body: Expr.Call { DynamicRemote: false, Arguments: [Expr.Variable forwarded] } second
                    }
                ] || field.Name == VariableScopeNames.Wildcard || field.Name != tested.Name
                || field.Name != projected.Name || argument.Name == VariableScopeNames.Wildcard
                || argument.Name != forwarded.Name || !owner.ResolvesErlangBif(guard))
            {
                return false;
            }

            first = owner.ResolveImportedCall(first, entries);
            second = owner.ResolveImportedCall(second, entries);
            if (first.Module is null || second.Module is null
                || owner.ResolvesErlangBif(first) || owner.ResolvesErlangBif(second))
            {
                return false;
            }

            int fallback = Label();
            owner.EmitRecordTest(fallback, XRegisterTag, 0, tag, 2);
            owner.EmitTuplePart(0, 1, 1);
            owner.Op(IsList, LabelTag, fallback);
            owner.Operand(XRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            EmitTail(first);
            Mark(fallback);
            EmitTail(second);

            return true;

            void EmitTail(Expr.Call call)
            {
                owner.EmitCallLine(call, location);
                owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module!, call.Function, 1));
            }
        }
    }

    private bool TryEmitTupleArgumentTail(Clause clause, BeamSourceLocation location)
    {
        if (clause is not {
            Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second, Pattern.Variable third],
            Body: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                Expr.Variable forwarded, Expr.Tuple { Items: [Expr.Variable left, Expr.Variable right] }
            ] } call
        } || !DistinctBindings(first, second, third) || forwarded.Name != first.Name
            || left.Name != second.Name || right.Name != third.Name || ResolvesErlangBif(call))
        {
            return false;
        }

        Op(TestHeap, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 3);
        EmitTagMatchTuple(1, (XRegisterTag, 1), (XRegisterTag, 2));
        EmitCallLine(call, location);
        Op(TailExternalCall, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import(call.Module, call.Function, 2));

        return true;
    }
}
