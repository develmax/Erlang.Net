using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private static (int Register, Term[] Heads)? ReadRegisterAppendPrefix(Clause clause, Expr.Call call)
        {
            if (call.Arguments is not [Expr.Binary
                {
                    Operator: ErlangOperators.Append, Left: var prefix,
                    Right: Expr.Variable { Name: not VariableScopeNames.Wildcard } tail
                }]
                || !AttributeLiteral.TryNormalizeConstant(prefix, out Term list))
            {
                return null;
            }

            var heads = new List<Term>(4);
            while (list is Cons cell && heads.Count < 4)
            {
                heads.Add(cell.Head);
                list = cell.Tail;
            }

            if (list is not Nil)
            {
                return null;
            }

            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                if (clause.Patterns[index] is Pattern.Variable binding && binding.Name == tail.Name)
                {
                    return (index, heads.ToArray());
                }
            }

            return null;
        }

        private void EmitRegisterAppendPrefix(Expr.Call target, int tail, Term[] heads)
        {
            if (heads.Length == 0)
            {
                if (tail != 0)
                {
                    owner.MoveRegister(XRegisterTag, tail, XRegisterTag, 0);
                }
            }
            else
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, heads.Length * ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, tail + 1);
                for (int index = heads.Length - 1; index >= 0; index--)
                {
                    owner.instructions.WriteByte(PutList);
                    owner.LiteralOperand(heads[index]);
                    owner.Operand(XRegisterTag, tail);
                    owner.Operand(XRegisterTag, 0);
                    tail = 0;
                }
            }

            EmitRegisterTailTarget(target, 1);
        }

        private static (int? Register, Term? Value)[]? ReadRegisterTailList(Clause clause, Expr.Call call)
        {
            if (call.Arguments is not [Expr.List { Tail: null, Items.Count: > 0 } list]
                || AttributeLiteral.TryNormalizeConstant(list, out _))
            {
                return null;
            }

            var elements = new (int? Register, Term? Value)[list.Items.Count];
            for (int index = 0; index < list.Items.Count; index++)
            {
                Expr item = list.Items[index];
                if (item is Expr.Variable { Name: not VariableScopeNames.Wildcard } variable)
                {
                    int register = 0;
                    while (register < clause.Patterns.Count
                        && (clause.Patterns[register] is not Pattern.Variable binding || binding.Name != variable.Name))
                    {
                        register++;
                    }

                    if (register == clause.Patterns.Count)
                    {
                        return null;
                    }

                    elements[index] = (register, null);
                }
                else if (AttributeLiteral.TryNormalizeConstant(item, out Term value))
                {
                    elements[index] = (null, value);
                }
                else
                {
                    return null;
                }
            }

            return elements;
        }

        private void EmitRegisterTailList(Expr.Call call, (int? Register, Term? Value)[] elements, bool heapChecked = false)
        {
            var remaining = new Dictionary<int, int>();
            foreach (var (register, _) in elements)
            {
                if (register is int input)
                {
                    remaining[input] = remaining.GetValueOrDefault(input) + 1;
                }
            }

            int lastRegister = Array.FindLastIndex(elements, element => element.Register.HasValue);
            int consCount = lastRegister + 1;
            Term literalTail = Cons.From(elements.Skip(consCount).Select(element => element.Value!));
            int live = remaining.Keys.Max() + 1;
            if (!heapChecked)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, consCount * ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, live);
            }
            int? tail = null;
            for (int index = lastRegister; index >= 0; index--)
            {
                var (register, value) = elements[index];
                if (register is int input)
                {
                    if (--remaining[input] == 0)
                    {
                        remaining.Remove(input);
                    }

                    owner.Op(PutList, XRegisterTag, input);
                }
                else
                {
                    owner.instructions.WriteByte(PutList);
                    owner.LiteralOperand(value!);
                }

                if (tail is int previous)
                {
                    owner.Operand(XRegisterTag, previous);
                }
                else
                {
                    owner.LiteralOperand(literalTail);
                }

                int destination = !remaining.ContainsKey(0) ? 0
                    : register is int reusable && !remaining.ContainsKey(reusable) ? reusable
                    : tail ?? live;
                owner.Operand(XRegisterTag, destination);
                tail = destination;
            }

            EmitRegisterTailTarget(call, 1);
        }

        private static (int Register, Atom Tag)? ReadRegisterTupleTail(Clause clause, Expr.Call call)
        {
            if (clause.Patterns.Count != call.Arguments.Count)
            {
                return null;
            }

            var bindings = new HashSet<string>(StringComparer.Ordinal);
            (int Register, Atom Tag)? result = null;
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                if (clause.Patterns[index] is Pattern.Variable { Name: not VariableScopeNames.Wildcard } parameter
                    && call.Arguments[index] is Expr.Variable argument
                    && parameter.Name == argument.Name && bindings.Add(parameter.Name))
                {
                    continue;
                }

                if (result is not null
                    || clause.Patterns[index] is not Pattern.Tuple
                    {
                        Items: [Pattern.Variable { Name: not VariableScopeNames.Wildcard } first,
                            Pattern.Variable { Name: not VariableScopeNames.Wildcard } second]
                    }
                    || call.Arguments[index] is not Expr.Tuple
                    {
                        Items: [Expr.Literal { Value: Atom tag }, Expr.Variable field,
                            Expr.List { Items: [Expr.Variable item], Tail: null }]
                    }
                    || first.Name != field.Name || second.Name != item.Name
                    || !bindings.Add(first.Name) || !bindings.Add(second.Name))
                {
                    return null;
                }

                result = (index, tag);
            }

            return result;
        }

        private void EmitRegisterTupleTail(Expr.Call call, int register, Atom tag)
        {
            int arity = call.Arguments.Count;
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4 + ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, arity);
            owner.EmitTuplePart(register, 0, arity);
            owner.EmitTuplePart(register, 1, register);
            owner.Op(PutList, XRegisterTag, register);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, register);
            owner.EmitTagMatchTuple(register, (AtomTag, owner.Intern(tag.Name)), (XRegisterTag, arity), (XRegisterTag, register));
            EmitRegisterTailTarget(call, arity);
        }

        private void EmitRegisterTailCall(EntryCall tail, BeamSourceLocation? previous = null)
        {
            tail = PrepareStackFreeTailThrow(tail);
            EmitEntryCallArguments(tail);
            EmitRegisterTailTarget(tail.Call with { Module = tail.Module }, tail.Call.Arguments.Count, previous);
        }

        private void EmitRegisterTailTarget(Expr.Call call, int arity, BeamSourceLocation? previous = null)
        {
            if (call.Module is null)
            {
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(LabelTag, entries[(call.Function, arity)]);
            }
            else
            {
                owner.EmitCallLine(call, previous ?? location);
                owner.Op(TailExternalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module, call.Function, arity));
            }
        }

        private bool TryEmitListResultTailCall(FunctionDefinition function)
        {
            if (function.Arity < 2 || function.Clauses is not [{ Guard: null, Body: Expr.Call
                { Module: null, DynamicRemote: false } output } clause]
                || !HasUnconditionalParameters(clause) || output.Arguments.Count != function.Arity
                || output.Arguments[^1] is not Expr.Call
                {
                    Module: not (null or ErlangModuleNames.Module),
                    DynamicRemote: false,
                    Arguments: [Expr.List { Tail: null, Items.Count: > 0 } list]
                } input
                || !entries.TryGetValue((output.Function, function.Arity), out int target))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            int frame = function.Arity - 1;
            for (int index = 0; index < frame; index++)
            {
                if (parameters[index] == VariableScopeNames.Wildcard
                    || output.Arguments[index] is not Expr.Variable forwarded || forwarded.Name != parameters[index])
                {
                    return false;
                }
            }

            var registers = new int[list.Items.Count];
            for (int index = 0; index < registers.Length; index++)
            {
                if (list.Items[index] is not Expr.Variable { Name: not VariableScopeNames.Wildcard } item
                    || (registers[index] = Array.IndexOf(parameters, item.Name)) < 0)
                {
                    return false;
                }
            }

            owner.Op(AllocateHeap, BeamWire.UnsignedTag, frame);
            owner.Operand(BeamWire.UnsignedTag, registers.Length * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, function.Arity);
            for (int index = frame - 1; index >= 0; index--)
            {
                owner.MoveRegister(XRegisterTag, index, YRegisterTag, frame - index - 1);
            }

            for (int index = registers.Length - 1; index >= 0; index--)
            {
                bool first = index == registers.Length - 1;
                int register = registers[index];
                if (!first && register == 0)
                {
                    owner.Op(PutList, YRegisterTag, frame - 1);
                }
                else
                {
                    owner.Op(PutList, XRegisterTag, register);
                }

                if (first)
                {
                    owner.LiteralOperand(Nil.Value);
                }
                else
                {
                    owner.Operand(XRegisterTag, 0);
                }

                owner.Operand(XRegisterTag, 0);
            }

            owner.EmitCallLine(input, location);
            owner.EmitStaticRemoteCall(input);
            foreach (int destination in IndependentCallMoveOrder(function.Arity))
            {
                if (destination == frame)
                {
                    owner.MoveRegister(XRegisterTag, 0, XRegisterTag, destination);
                }
                else
                {
                    owner.MoveRegister(YRegisterTag, frame - destination - 1, XRegisterTag, destination);
                }
            }

            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, function.Arity);
            owner.Operand(LabelTag, target);
            owner.Operand(BeamWire.UnsignedTag, frame);

            return true;
        }

        private bool TryEmitSingletonListTailCall(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Call
                { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false } call } clause]
                || !HasUnconditionalParameters(clause) || call.Arguments.Count != function.Arity)
            {
                return false;
            }

            int wrapped = -1;
            for (int index = 0; index < call.Arguments.Count; index++)
            {
                var parameter = (Pattern.Variable)clause.Patterns[index];
                if (parameter.Name == VariableScopeNames.Wildcard)
                {
                    return false;
                }

                if (call.Arguments[index] is Expr.List { Items: [Expr.Variable value], Tail: null }
                    && value.Name == parameter.Name && wrapped < 0)
                {
                    wrapped = index;
                }
                else if (call.Arguments[index] is not Expr.Variable forwarded || forwarded.Name != parameter.Name)
                {
                    return false;
                }
            }

            if (wrapped < 0)
            {
                return false;
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, function.Arity);
            owner.Op(PutList, XRegisterTag, wrapped);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, wrapped);
            owner.EmitCallLine(call, location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, function.Arity);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module, call.Function, function.Arity));
            return true;
        }

        private bool TryEmitTupleListTailCall(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null } clause] || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            Expr.Call? call = clause.Body as Expr.Call;
            Expr? constructed = null;
            if (clause.Body is Expr.Sequence
                {
                    Items: [Expr.Match { Pattern: Pattern.Variable binding, Value: var value }, Expr.Call tail]
                } && binding.Name != VariableScopeNames.Wildcard
                && tail.Arguments.LastOrDefault() is Expr.Variable result && result.Name == binding.Name
                && clause.Patterns.Cast<Pattern.Variable>().All(parameter => parameter.Name != binding.Name))
            {
                call = tail;
                constructed = value;
            }

            if (call is not { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false }
                || call.Arguments.Count == 0)
            {
                return false;
            }

            int destination = call.Arguments.Count - 1;
            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if ((constructed ?? call.Arguments[destination]) is not Expr.List
                {
                    Items: [Expr.Tuple tuple], Tail: null
                }
                || tuple.Items.Count < 2 || destination + tuple.Items.Count != parameters.Length
                || parameters.Contains(VariableScopeNames.Wildcard)
                || !call.Arguments.Take(destination).SequenceEqual(parameters.Take(destination).Select(name => new Expr.Variable(name)))
                || !tuple.Items.SequenceEqual(parameters.Skip(destination).Select(name => new Expr.Variable(name))))
            {
                return false;
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Items.Count + 1 + ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, function.Arity);
            owner.EmitTagMatchTuple(destination,
                Enumerable.Range(destination, tuple.Items.Count).Select(register => (XRegisterTag, register)).ToArray());
            owner.Op(PutList, XRegisterTag, destination);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, destination);
            owner.EmitCallLine(call, location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, call.Arguments.Count);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module, call.Function, call.Arguments.Count));

            return true;
        }

        private bool TryEmitListPrefixTailCall(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null,
                Patterns: [AliasPattern
                {
                    Left: Pattern.List
                    {
                        Items: [Pattern.Variable first, Pattern.Variable second, Pattern.Variable third],
                        Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    },
                    Right: Pattern.Variable input
                }],
                Body: Expr.Sequence { Items: [
                    Expr.Match
                    {
                        Pattern: Pattern.Variable initial,
                        Value: Expr.Call
                        {
                            Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                            Arguments: [var key, Expr.Variable collection, var defaultValue]
                        } lookup
                    },
                    Expr.Match
                    {
                        Pattern: Pattern.Variable computed,
                        Value: Expr.Call
                        {
                            Module: null, DynamicRemote: false,
                            Arguments: [Expr.Variable left, Expr.Variable right, Expr.Variable previous]
                        } local
                    },
                    Expr.Call
                    {
                        Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                        Arguments: [
                            Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable result] },
                            Expr.Literal { Value: Atom module },
                            Expr.List { Items: [Expr.Variable original], Tail: null }
                        ]
                    } tail
                ] }
            }] || !DistinctBindings(first, second, third, input, initial, computed)
                || collection.Name != third.Name || left.Name != first.Name || right.Name != second.Name
                || previous.Name != initial.Name || result.Name != computed.Name || original.Name != input.Name
                || !AttributeLiteral.TryNormalizeConstant(key, out Term lookupKey)
                || !AttributeLiteral.TryNormalizeConstant(defaultValue, out Term fallback)
                || !entries.TryGetValue((local.Function, 3), out int entry))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            owner.EmitNonemptyList(0, failure);
            owner.EmitListParts(0, XRegisterTag, 1, XRegisterTag, 2);
            owner.EmitNonemptyList(2, failure);
            owner.EmitListParts(2, XRegisterTag, 3, XRegisterTag, 2);
            owner.EmitNonemptyList(2, failure);
            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.MoveRegister(XRegisterTag, 3, YRegisterTag, 0);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 2);
            owner.Op(GetHead, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(fallback, 2);
            owner.EmitConstant(lookupKey, 0);
            owner.EmitCallLine(lookup, location);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(lookup.Module!, lookup.Function, 3));
            owner.MoveRegister(YRegisterTag, 0, XRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 2);
            owner.MoveRegister(YRegisterTag, 1, XRegisterTag, 0);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitCallLine(local, CallLocation(lookup, location));
            owner.EmitFieldCall(3, entry);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3 + ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(tag.Name)), (XRegisterTag, 0));
            owner.Op(PutList, YRegisterTag, 0);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, 2);
            owner.EmitConstant(module, 1);
            owner.EmitCallLine(tail, CallLocation(local, location));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(tail.Module!, tail.Function, 3));
            owner.Operand(BeamWire.UnsignedTag, 1);

            return true;
        }
    }
}
