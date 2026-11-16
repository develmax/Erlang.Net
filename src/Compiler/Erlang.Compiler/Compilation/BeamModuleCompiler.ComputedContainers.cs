using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (Atom Tag, Term Flags, Expr.List Calls)? ReadCallListResult(Clause clause)
    {
        if (clause is not
            {
                Guard: null, Body: Expr.Sequence { Items: [
                    Expr.Match,
                    ..,
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom tag },
                        Expr.Tuple { Items: [Expr.Variable first, var second] }] }
                ] } sequence
            })
        {
            return null;
        }

        Expr.Match? flags = sequence.Items switch
        {
            [Expr.Match { Pattern: Pattern.Variable variable } binding, ..] when variable.Name == first.Name => binding,
            [_, Expr.Match { Pattern: Pattern.Variable variable } binding, _] when variable.Name == first.Name => binding,
            _ => null
        };
        if (flags is not { Pattern: Pattern.Variable flag } || flag.Name == VariableScopeNames.Wildcard
            || !AttributeLiteral.TryNormalizeConstant(flags.Value, out Term flagValue))
        {
            return null;
        }

        Expr.List? calls = (sequence.Items, second) switch
        {
            ([_, _], Expr.List { Tail: null } inline) => inline,
            ([Expr.Match a, Expr.Match b, _], Expr.Variable reference)
                when (ReferenceEquals(a, flags) ? b : a) is
                    { Pattern: Pattern.Variable list, Value: Expr.List { Tail: null } bound }
                    && DistinctBindings(flag, list) && reference.Name == list.Name => bound,
            _ => null
        };
        return calls is { Items.Count: > 0 } ? (tag, flagValue, calls) : null;
    }

    private bool TryEmitCallListResult(
        Clause clause,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (ReadCallListResult(clause) is not { } result || ReadZeroArgumentCalls(result.Calls.Items, entries) is not { } children)
        {
            return false;
        }

        int slots = children.Length - 1;
        EmitEmptyCallFrame(slots);
        EmitOrderedZeroArgumentCalls(children, slots, location);
        EmitChildListResult(result.Tag, result.Flags, children.Length);
        EmitFrameReturn(slots);

        return true;
    }

    private void EmitChildListResult(Atom tag, Term flags, int children)
    {
        Op(TestHeap, BeamWire.UnsignedTag, children * ConsHeapWords + 6);
        Operand(BeamWire.UnsignedTag, 1);
        EmitListFromCallResults(children - 1);
        Op(PutTuple, XRegisterTag, 0);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 2);
        LiteralOperand(flags);
        Operand(XRegisterTag, 0);
        EmitTagMatchTuple(0, (AtomTag, Intern(tag.Name)), (XRegisterTag, 0));
    }

    private bool TryEmitComputedMap(
        Clause clause,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (clause is not
            {
                Guard: null, Patterns: [],
                Body: Expr.Sequence { Items: [_, .., Expr.Map { Base: null } map] } sequence
            })
        {
            return false;
        }

        var names = new List<string>();
        var expressions = new List<Expr>();
        foreach (Expr expression in sequence.Items.SkipLast(1))
        {
            if (expression is not Expr.Match { Pattern: Pattern.Variable name, Value: var call }
                || name.Name == VariableScopeNames.Wildcard || names.Contains(name.Name, StringComparer.Ordinal))
            {
                return false;
            }
            names.Add(name.Name);
            expressions.Add(call);
        }
        if (ReadZeroArgumentCalls(expressions, entries, allowRemote: true) is not { } calls)
        {
            return false;
        }

        var fields = new Dictionary<Atom, Term?>();
        (Atom First, Atom Second)? constructor = null;
        foreach (MapField field in map.Fields)
        {
            if (field is not { Exact: false, Key: Expr.Literal { Value: Atom key } } || fields.ContainsKey(key))
            {
                return false;
            }
            if (AttributeLiteral.TryNormalizeConstant(field.Value, out Term constant))
            {
                fields.Add(key, constant);
            }
            else if (constructor is null && field.Value is Expr.Tuple
            {
                Items: [
                    Expr.Literal { Value: Atom first }, Expr.Literal { Value: Atom second },
                    Expr.List { Tail: null } arguments
                ]
            } && arguments.Items.OfType<Expr.Variable>().Select(variable => variable.Name).SequenceEqual(names)
                && arguments.Items.Count == names.Count)
            {
                constructor = (first, second);
                fields.Add(key, null);
            }
            else
            {
                return false;
            }
        }
        if (constructor is not { } built)
        {
            return false;
        }

        int slots = calls.Length - 1;
        EmitEmptyCallFrame(slots);
        EmitOrderedZeroArgumentCalls(calls, slots, location);
        Op(TestHeap, BeamWire.UnsignedTag, calls.Length * ConsHeapWords + 4);
        Operand(BeamWire.UnsignedTag, 1);
        EmitListFromCallResults(slots);
        EmitTagMatchTuple(
            0,
            (AtomTag, Intern(built.First.Name)),
            (AtomTag, Intern(built.Second.Name)),
            (XRegisterTag, 0)
        );
        EmitComputedMapFields(fields);
        EmitFrameReturn(slots);

        return true;
    }

    private bool TryEmitArgumentTupleMap(Clause clause)
    {
        if (!HasUnconditionalParameters(clause) || clause.Body is not Expr.Map { Base: null } map)
        {
            return false;
        }

        var fields = new Dictionary<Atom, Term?>();
        (Atom First, Atom Second, int Register, int ListLength)? constructor = null;
        foreach (MapField field in map.Fields)
        {
            if (field is not { Exact: false, Key: Expr.Literal { Value: Atom key } } || fields.ContainsKey(key))
            {
                return false;
            }

            if (AttributeLiteral.TryNormalizeConstant(field.Value, out Term constant))
            {
                fields.Add(key, constant);
                continue;
            }

            if (constructor is not null || field.Value is not Expr.Tuple
                {
                    Items: [Expr.Literal { Value: Atom first }, Expr.Literal { Value: Atom second }, var argument]
                })
            {
                return false;
            }

            if (argument is Expr.Variable forwarded)
            {
                constructor = (first, second, ArgumentRegister(clause, forwarded.Name), 0);
            }
            else if (argument is Expr.List { Tail: null, Items.Count: > 0 } list
                && list.Items.Count == clause.Patterns.Count
                && list.Items.All(item => item is Expr.Variable)
                && list.Items.Cast<Expr.Variable>().Select(item => item.Name)
                    .SequenceEqual(clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name)))
            {
                constructor = (first, second, 0, list.Items.Count);
            }
            else
            {
                return false;
            }

            fields.Add(key, null);
        }

        if (constructor is not { } tuple)
        {
            return false;
        }

        Op(TestHeap, BeamWire.UnsignedTag, 4 + tuple.ListLength * ConsHeapWords);
        Operand(BeamWire.UnsignedTag, Math.Max(tuple.Register + 1, tuple.ListLength));
        for (int register = tuple.ListLength - 1; register >= 0; register--)
        {
            Op(PutList, XRegisterTag, register);
            if (register == tuple.ListLength - 1)
            {
                LiteralOperand(Nil.Value);
            }
            else
            {
                Operand(XRegisterTag, register + 1);
            }

            Operand(XRegisterTag, register);
        }

        EmitTagMatchTuple(
            0,
            (AtomTag, Intern(tuple.First.Name)),
            (AtomTag, Intern(tuple.Second.Name)),
            (XRegisterTag, tuple.Register)
        );
        EmitComputedMapFields(fields);
        instructions.WriteByte(Return);

        return true;
    }

    private void EmitComputedMapFields(Dictionary<Atom, Term?> fields)
    {
        Op(PutMapAssoc, LabelTag, 0);
        LiteralOperand(new MapTerm([]));
        Operand(XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, fields.Count * 2);
        foreach (var (key, value) in fields.OrderBy(pair => pair.Key, Comparer<Atom>.Create(
                     (left, right) => TermOrder.Compare(left, right, exact: true))))
        {
            LiteralOperand(key);
            if (value is null)
            {
                Operand(XRegisterTag, 0);
            }
            else
            {
                LiteralOperand(value);
            }
        }
    }

    private static (Expr.Call Call, int? Entry)[]? ReadZeroArgumentCalls(
        IReadOnlyList<Expr> expressions,
        IReadOnlyDictionary<(string Name, int Arity), int> entries,
        bool allowRemote = false
    )
    {
        var calls = new (Expr.Call Call, int? Entry)[expressions.Count];
        for (int index = 0; index < calls.Length; index++)
        {
            if (expressions[index] is not Expr.Call { DynamicRemote: false, Arguments: [] } call)
            {
                return null;
            }

            if (call.Module is null && entries.TryGetValue((call.Function, 0), out int entry))
            {
                calls[index] = (call, entry);
            }
            else if (allowRemote && call.Module is not (null or ErlangModuleNames.Module))
            {
                calls[index] = (call, null);
            }
            else
            {
                return null;
            }
        }

        return calls;
    }

    private void EmitEmptyCallFrame(int slots)
    {
        Op(Allocate, BeamWire.UnsignedTag, slots);
        Operand(BeamWire.UnsignedTag, 0);
        if (slots != 0)
        {
            instructions.WriteByte(InitYRegisters);
            instructions.WriteByte(ExtendedListOperand);
            Operand(BeamWire.UnsignedTag, slots);
            for (int index = 0; index < slots; index++)
            {
                Operand(YRegisterTag, index);
            }
        }
    }

    private void EmitOrderedZeroArgumentCalls(
        (Expr.Call Call, int? Entry)[] calls,
        int slots,
        BeamSourceLocation location
    )
    {
        for (int index = 0; index < calls.Length; index++)
        {
            var (call, entry) = calls[index];
            EmitCallLine(call, location);
            location = CallLocation(call, location);
            if (entry is int target)
            {
                EmitFieldCall(0, target);
            }
            else
            {
                EmitStaticRemoteCall(call);
            }
            if (index < slots)
            {
                MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    slots - index - 1
                );
            }
        }
    }

    private void EmitListFromCallResults(int slots)
    {
        Op(PutList, XRegisterTag, 0);
        LiteralOperand(Nil.Value);
        Operand(XRegisterTag, 0);
        for (int index = 0; index < slots; index++)
        {
            Op(PutList, YRegisterTag, index);
            Operand(XRegisterTag, 0);
            Operand(XRegisterTag, 0);
        }
    }
}
