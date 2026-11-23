using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record EntryEffectFailure(int Label, int Register, BeamSourceLocation? Line);
        private sealed record FiniteEffectCall(Expr.Call Expression, string? Module, (string? Binding, Term? Constant)[] Operands);
        private sealed record FiniteEffectArm(Term? Key, FiniteEffectCall[] Calls);

        private bool TryEmitFiniteEntryEffects(
            Expr body,
            Dictionary<string, (int Input, int? Field)> bindings,
            List<EntryEffectFailure> failures
        )
        {
            if (body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Case { Value: Expr.Variable decisionInput, Clauses.Count: >= 2 } decision,
                        Expr.Tuple { Items.Count: > 0 } result
                    ]
                } || !bindings.TryGetValue(decisionInput.Name, out var selector)
                || ReadFiniteEffectArguments(result.Items, bindings) is not { } values)
            {
                return false;
            }

            var alternatives = new List<FiniteEffectArm>();
            var keys = new HashSet<Term>();
            FiniteEffectArm? fallback = null;
            for (int index = 0; index < decision.Clauses.Count; index++)
            {
                var clause = decision.Clauses[index];
                if (clause is not { Guard: null, Patterns: [var pattern] }
                    || ReadFiniteEffectCalls(clause.Body, bindings) is not { } calls)
                {
                    return false;
                }

                if (pattern is Pattern.Literal { Value: Atom or Integer } literal
                    && (literal.Value is not Integer integer || integer.Value >= int.MinValue && integer.Value <= int.MaxValue)
                    && keys.Add(literal.Value))
                {
                    alternatives.Add(new FiniteEffectArm(literal.Value, calls));
                }
                else if (index == decision.Clauses.Count - 1 && IsSharedEntryWildcard(pattern))
                {
                    fallback = new FiniteEffectArm(null, calls);
                }
                else
                {
                    return false;
                }
            }

            if (alternatives.Count < 2 || fallback is { Calls.Length: > 0 })
            {
                return false;
            }

            var returned = values.Select(operand => operand.Binding).OfType<string>().ToHashSet(StringComparer.Ordinal);
            var live = alternatives.SelectMany(arm => arm.Calls).SelectMany(call => call.Operands)
                .Select(operand => operand.Binding).OfType<string>().Concat(returned).ToHashSet(StringComparer.Ordinal);
            var later = alternatives.SelectMany(arm => arm.Calls.Skip(1)).SelectMany(call => call.Operands)
                .Select(operand => operand.Binding).OfType<string>().ToHashSet(StringComparer.Ordinal);
            var projected = live.Except(returned).Where(name => bindings[name].Field is not null)
                .GroupBy(name => bindings[name].Input)
                .Where(fields => fields.Count(later.Contains) >= 2)
                .SelectMany(fields => fields)
                .ToHashSet(StringComparer.Ordinal);
            var saved = new Dictionary<string, int>(StringComparer.Ordinal);
            var roots = new Dictionary<int, int>();
            int slotCount = 0;
            foreach (string name in bindings.Keys.Where(live.Contains).Reverse())
            {
                if (projected.Contains(name))
                {
                    int input = bindings[name].Input;
                    if (!roots.TryGetValue(input, out int slot))
                    {
                        slot = slotCount++;
                        roots.Add(input, slot);
                    }

                    saved.Add(name, slot);
                }
                else
                {
                    saved.Add(name, slotCount++);
                }
            }

            var bindingsToSave = saved.Keys.Where(name => !projected.Contains(name)).ToArray();
            int selectorRegister = roots.Count == 0 ? 1 : 0;
            int inputs = Math.Max(selector.Input, saved.Keys.Select(name => bindings[name].Input).DefaultIfEmpty(-1).Max()) + 1;
            EmitFrameAllocation(inputs, slotCount);
            foreach (var (input, slot) in roots.OrderByDescending(root => root.Key))
            {
                owner.MoveRegister(
                    XRegisterTag,
                    input,
                    YRegisterTag,
                    slot
                );
            }

            var initialBindings = bindingsToSave.Where(name => bindings[name].Field is null
                || bindings[name].Input == selectorRegister || roots.Count != 0);
            if (roots.Count != 0)
            {
                initialBindings = initialBindings.OrderBy(name => bindings[name].Field is not null);
            }

            foreach (string name in initialBindings)
            {
                SaveBinding(name);
            }

            if (selector.Field is int selectorField)
            {
                owner.EmitTuplePart(selector.Input, selectorField, selectorRegister);
            }
            else
            {
                owner.MoveRegister(
                    XRegisterTag,
                    selector.Input,
                    XRegisterTag,
                    selectorRegister
                );
            }

            foreach (string name in bindingsToSave.Where(name => roots.Count == 0
                && bindings[name] is { Field: not null } source && source.Input != selectorRegister))
            {
                SaveBinding(name);
            }

            int join = Label();
            int reject = fallback is null ? Label() : join;
            if (fallback is null)
            {
                failures.Add(new EntryEffectFailure(reject, selectorRegister, CaseSourceLocation(decision)));
            }

            alternatives.Sort((left, right) => TermOrder.Compare(left.Key!, right.Key!, exact: true));
            int[] targets = alternatives.Select(_ => Label()).Reverse().ToArray();
            owner.EmitMembershipSelect(selectorRegister, reject, alternatives.Select((arm, index) => (arm.Key!, targets[index])));
            for (int arm = alternatives.Count - 1; arm >= 0; arm--)
            {
                Mark(targets[arm]);
                var branch = alternatives[arm];
                var remaining = saved.Keys.Except(returned).Select(name => saved[name]).ToHashSet();
                for (int index = 0; index < branch.Calls.Length; index++)
                {
                    var call = branch.Calls[index];
                    var usedLater = branch.Calls.Skip(index + 1).SelectMany(next => next.Operands)
                        .Select(operand => operand.Binding).OfType<string>().Select(name => saved[name]).ToHashSet();
                    var released = remaining.Except(usedLater).Order().ToArray();
                    var constants = new List<(int Destination, Term Value)>();
                    var loads = new List<(int Destination, int Source)>();
                    var projections = new List<(int Source, int Tag, int Field, int Destination)>();
                    for (int argument = 0; argument < call.Operands.Length; argument++)
                    {
                        var operand = call.Operands[argument];
                        if (operand.Binding is { } name)
                        {
                            if (projected.Contains(name))
                            {
                                var (input, field) = bindings[name];
                                bool inRegister = index == 0 && input != selectorRegister && input >= call.Operands.Length;
                                projections.Add((
                                    inRegister ? input : saved[name],
                                    inRegister ? XRegisterTag : YRegisterTag,
                                    field!.Value,
                                    argument
                                ));
                            }
                            else
                            {
                                loads.Add((argument, saved[name]));
                            }
                        }
                        else
                        {
                            constants.Add((argument, operand.Constant!));
                        }
                    }

                    owner.EmitOrderedTailMoves([], constants, loads);
                    foreach (var (source, tag, field, destination) in projections)
                    {
                        owner.EmitTuplePart(
                            source,
                            field,
                            destination,
                            tag
                        );
                    }

                    if (released.Length != 0)
                    {
                        owner.EmitSlotKills(released);
                        remaining.ExceptWith(released);
                    }

                    owner.EmitCallLine(call.Expression, location);
                    Call(call.Module, call.Expression.Function, call.Operands.Length);
                }

                if (arm != 0)
                {
                    JumpTo(join);
                }
            }

            Mark(join);
            EmitEntryEffectTupleReturn(values, saved, slotCount);

            return true;

            void SaveBinding(string name)
            {
                var (register, position) = bindings[name];
                if (position is int field)
                {
                    owner.Op(GetTupleElement, XRegisterTag, register);
                    owner.Operand(BeamWire.UnsignedTag, field);
                    owner.Operand(YRegisterTag, saved[name]);
                }
                else
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        register,
                        YRegisterTag,
                        saved[name]
                    );
                }
            }
        }

        private FiniteEffectCall[]? ReadFiniteEffectCalls(Expr body, Dictionary<string, (int Input, int? Field)> bindings)
        {
            var calls = new List<FiniteEffectCall>();
            IReadOnlyList<Expr> effects = body is Expr.Sequence sequence ? sequence.Items : [body];
            foreach (Expr expression in effects)
            {
                if (AttributeLiteral.TryNormalizeConstant(expression, out _))
                {
                    continue;
                }

                if (expression is not Expr.Call call || call.Arguments.Count >= FirstReservedXRegister
                    || !TryResolveFiniteEffectTarget(call, out string? module)
                    || ReadFiniteEffectArguments(call.Arguments, bindings) is not { } operands)
                {
                    return null;
                }

                calls.Add(new FiniteEffectCall(call, module, operands));
            }

            return calls.ToArray();
        }

        private bool TryResolveFiniteEffectTarget(Expr.Call call, out string? module)
        {
            if (call is
                {
                    DynamicRemote: false,
                    Module: ErlangModuleNames.Module,
                    Function: TimerCancelOperation,
                    Arguments.Count: 1
                })
            {
                module = ErlangModuleNames.Module;

                return true;
            }

            return TryResolveOrdinaryCallTarget(call, out module);
        }

        private const string TimerCancelOperation = "cancel_timer";

        private static (string? Binding, Term? Constant)[]? ReadFiniteEffectArguments(
            IReadOnlyList<Expr> source,
            Dictionary<string, (int Input, int? Field)> bindings
        )
        {
            var values = new (string? Binding, Term? Constant)[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                if (source[index] is Expr.Variable variable && bindings.ContainsKey(variable.Name))
                {
                    values[index] = (variable.Name, null);
                }
                else if (AttributeLiteral.TryNormalizeConstant(source[index], out var value))
                {
                    values[index] = (null, value);
                }
                else
                {
                    return null;
                }
            }

            return values;
        }
    }
}
