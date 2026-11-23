using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitClosedAtomConsDispatch(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Arity != 3 || function.Clauses.Count < 2)
            {
                return false;
            }

            var choices = new SortedDictionary<Atom, Action<int>>(Comparer<Atom>.Create(
                (left, right) => TermOrder.Compare(left, right, exact: true)
            ));
            bool hasCons = false;
            var returnCount = 0;
            var localCount = 0;
            var externalCount = 0;
            var constantExits = 0;
            var localTargets = new HashSet<(string Function, int Arity)>();
            foreach (var clause in function.Clauses)
            {
                if (clause is not
                    {
                        Guard: null, Patterns: [Pattern.Literal { Value: Atom key }, Pattern.Variable input, var context]
                    }
                    || ReadClosedAtomConsArm(
                        clause,
                        input,
                        context,
                        out var armTail
                    ) is not { } emit
                    || !choices.TryAdd(key, emit))
                {
                    return false;
                }

                hasCons |= context is Pattern.List;
                switch (armTail)
                {
                    case null:
                        returnCount++;
                        break;
                    case { Module: null } local:
                        localCount++;
                        localTargets.Add((local.Function, local.Arguments.Count));
                        break;
                    default:
                        externalCount++;
                        if (armTail is { Module: ErlangModuleNames.Module, Function: AutoImportNames.Exit })
                        {
                            constantExits++;
                        }

                        break;
                }
            }

            // SSA reverse postorder visits independent selector arms in descending atom order.
            bool mixedCalls = returnCount == 1 && localCount == 1 && externalCount == 1;
            bool distinctLocalCalls = returnCount == 1 && localCount == 2 && localTargets.Count == 2;
            bool constantExitPair = choices.Count == 2 && localCount == 1 && constantExits == 1;
            if (!hasCons && !mixedCalls && !distinctLocalCalls && !constantExitPair
                || hasCons && choices.Count == 2 && (localCount != 1 || externalCount != 1))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            var branches = choices.Reverse().Select(pair => (pair.Key, Emit: pair.Value, Entry: Label())).ToArray();
            EmitAritySelect(0, branches.Length, failure);
            foreach (var branch in branches.Reverse())
            {
                owner.LiteralOperand(branch.Key);
                owner.Operand(LabelTag, branch.Entry);
            }

            foreach (var branch in branches)
            {
                Mark(branch.Entry);
                branch.Emit(failure);
            }

            return true;
        }

        private Action<int>? ReadClosedAtomConsArm(
            Clause clause,
            Pattern.Variable input,
            Pattern context,
            out Expr.Call? tailTarget
        )
        {
            tailTarget = null;
            if (context is Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable rest })
            {
                if (!DistinctBindings(input, head, rest)
                    || clause.Body is not Expr.Call
                    {
                        DynamicRemote: false,
                        Arguments: [Expr.Variable first, Expr.Variable second, Expr.Variable third]
                    } forward || first.Name != head.Name || second.Name != input.Name || third.Name != rest.Name
                    || !TryResolveOrdinaryCallTarget(forward, out string? module)
                    || module == ErlangModuleNames.Module
                    || module is null && !entries.ContainsKey((forward.Function, 3)))
                {
                    return null;
                }

                tailTarget = forward with { Module = module };

                return failure =>
                {
                    owner.EmitNonemptyList(2, failure);
                    owner.EmitListParts(
                        2,
                        XRegisterTag,
                        0,
                        XRegisterTag,
                        2
                    );
                    if (module is null)
                    {
                        EmitTailLocal(entries[(forward.Function, 3)], 3, null);
                    }
                    else
                    {
                        owner.EmitCallLine(forward, location);
                        owner.Op(TailExternalCall, BeamWire.UnsignedTag, 3);
                        owner.Operand(BeamWire.UnsignedTag, owner.Import(module, forward.Function, 3));
                    }
                };
            }

            if (context is not Pattern.Variable unused || unused.Name == input.Name && input.Name != VariableScopeNames.Wildcard)
            {
                return null;
            }

            if (clause.Body is Expr.Call
                {
                    DynamicRemote: false, Function: AutoImportNames.Exit, Arguments: [var reason]
                } exit && owner.ResolvesErlangBif(exit) && AttributeLiteral.TryNormalizeConstant(reason, out Term exception))
            {
                tailTarget = exit with { Module = ErlangModuleNames.Module };

                return _ =>
                {
                    owner.EmitConstant(exception, 0);
                    owner.EmitCallLine(exit, location);
                    owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
                    owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, exit.Function, 1));
                };
            }

            if (input.Name == VariableScopeNames.Wildcard)
            {
                return null;
            }

            if (clause.Body is Expr.Variable returned && returned.Name == input.Name)
            {
                return _ =>
                {
                    owner.MoveToX0(XRegisterTag, 1);
                    owner.instructions.WriteByte(Return);
                };
            }

            if (clause.Body is Expr.Tuple { Items: [Expr.Variable value, Expr.Literal literal] } && value.Name == input.Name)
            {
                return _ =>
                {
                    owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                    owner.Operand(BeamWire.UnsignedTag, 2);
                    owner.EmitTupleHeader(XRegisterTag, 2);
                    owner.Operand(XRegisterTag, 1);
                    owner.LiteralOperand(literal.Value);
                    owner.instructions.WriteByte(Return);
                };
            }

            if (ReadClosedAtomParameterTail(clause.Body, input.Name) is { } tail)
            {
                tailTarget = tail.Call;

                return _ =>
                {
                    owner.MoveToX0(XRegisterTag, 1);
                    owner.EmitConstant(tail.Value, 1);
                    EmitClosedAtomTail(tail.Call, frame: null, location);
                };
            }

            if (clause.Body is Expr.Sequence
                {
                    Items:
                        [
                            Expr.Match
                        {
                            Pattern: Pattern.Variable converted,
                            Value: Expr.Call
                            {
                                Module: null, DynamicRemote: false, Arguments: [Expr.Variable source]
                            } prepare
                        },
                            var continuation
                        ]
                }
                && DistinctBindings(input, converted) && source.Name == input.Name
                && ReadClosedAtomParameterTail(continuation, converted.Name) is { } after
                && entries.TryGetValue((prepare.Function, 1), out int prepareEntry))
            {
                tailTarget = after.Call;

                return _ =>
                {
                    EmitFrameAllocation(arity: 2, size: 0);
                    owner.MoveToX0(XRegisterTag, 1);
                    owner.EmitCallLine(prepare, location);
                    owner.Op(LocalCall, BeamWire.UnsignedTag, 1);
                    owner.Operand(LabelTag, prepareEntry);
                    owner.EmitConstant(after.Value, 1);
                    EmitClosedAtomTail(after.Call, frame: 0, CallLocation(prepare, location));
                };
            }

            return null;
        }

        private (Expr.Call Call, Term Value)? ReadClosedAtomParameterTail(Expr body, string input) =>
            body is Expr.Call
            {
                DynamicRemote: false, Arguments: [Expr.Variable value, var constant]
            } call && value.Name == input && TryResolveOrdinaryCallTarget(call, out string? module)
                && module != ErlangModuleNames.Module
                && AttributeLiteral.TryNormalizeConstant(constant, out Term materialized)
                    ? (call with { Module = module }, materialized)
                    : null;

        private void EmitClosedAtomTail(Expr.Call call, int? frame, BeamSourceLocation previous)
        {
            if (call.Module is { } module)
            {
                owner.EmitCallLine(call, previous);
                owner.Op(frame.HasValue ? TailExternalCallWithFrame : TailExternalCall, BeamWire.UnsignedTag, 2);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(module, call.Function, 2));
                if (frame is int size)
                {
                    owner.Operand(BeamWire.UnsignedTag, size);
                }

                return;
            }

            EmitTailLocal(entries[(call.Function, 2)], 2, frame);
        }

        private void EmitTailLocal(int entry, int arity, int? frame)
        {
            owner.Op(frame.HasValue ? TailLocalCallWithFrame : TailLocalCall, BeamWire.UnsignedTag, arity);
            owner.Operand(LabelTag, entry);
            if (frame is int size)
            {
                owner.Operand(BeamWire.UnsignedTag, size);
            }
        }
    }
}
