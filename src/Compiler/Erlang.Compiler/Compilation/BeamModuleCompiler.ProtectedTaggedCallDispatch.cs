using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProtectedTaggedCallDispatch(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || function.Arity is < 1 or >= FirstReservedXRegister
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Body: Expr.Try
                    {
                        Body: var protectedBody,
                        Clauses: [],
                        Catches:
                        [
                        {
                            Guard: null,
                            Patterns: [Pattern.Tuple
                            {
                                Items:
                                    [
                                        Pattern.Literal { Value: Atom caughtClass },
                                        Pattern.Variable reason,
                                        Pattern.Variable { Name: VariableScopeNames.Wildcard }
                                    ]
                            }],
                            Body: var caughtBody
                        }
                        ],
                        After: null
                    }
                } clause]
                || !HasUnconditionalParameters(clause) || reason.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            string[] inputs = clause.Patterns.Cast<Pattern.Variable>().Select(variable => variable.Name).ToArray();
            if (inputs.Contains(reason.Name) || inputs.Contains(VariableScopeNames.Wildcard)
                || ReadErrorArguments(caughtBody, inputs, reason.Name) is not { } handlerPlan)
            {
                return false;
            }

            Expr.Unary? prefix = null;
            string[] branchInputs = inputs;
            if (inputs.Length == 3
                && protectedBody is Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                    {
                        Pattern: Pattern.Variable computed,
                        Value: Expr.Unary { Operator: ErlangOperators.Minus, Operand: Expr.Variable original } arithmetic
                    },
                        Expr.Case decision
                    ]
                }
                && original.Name == inputs[2] && computed.Name != VariableScopeNames.Wildcard
                && !inputs.Contains(computed.Name))
            {
                prefix = arithmetic;
                branchInputs = [inputs[0], inputs[1], computed.Name];
                protectedBody = decision;
            }

            if (protectedBody is not Expr.Case { Value: Expr.Variable reference, Clauses.Count: >= 3 } selection
                || reference.Name != inputs[0]
                || selection.Clauses[^1] is not
                {
                    Guard: null,
                    Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module,
                        Function: AutoImportNames.Error, DynamicRemote: false, Arguments: [var badReference]
                    } reject
                }
                || !owner.ResolvesErlangBif(reject)
                || !AttributeLiteral.TryNormalizeConstant(badReference, out Term badValue))
            {
                return false;
            }

            var (reroute, extraInfo) = handlerPlan;
            var targets = new Dictionary<Atom, (Expr.Call Call, string Module, bool DirectMapLookup)>();
            foreach (Clause branch in selection.Clauses.SkipLast(1))
            {
                if (branch is not
                    {
                        Guard: null,
                        Patterns: [Pattern.Tuple
                        {
                            Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable payload]
                        }],
                        Body: Expr.Call { Module: not null, DynamicRemote: false } call
                    }
                    || payload.Name == VariableScopeNames.Wildcard || inputs.Contains(payload.Name) || branchInputs.Contains(payload.Name)
                    || !ReadsInputs(call.Arguments, [payload.Name, .. branchInputs.Skip(1)]))
                {
                    return false;
                }

                bool directMapLookup = prefix is null && function.Arity == 2
                    && call.Module == ErlangModuleNames.Module && call.Function == ErlangModuleNames.MapGet
                    && owner.ResolvesErlangBif(call);
                string? module = call.Module;
                if ((!directMapLookup && (module == ErlangModuleNames.Module
                        || !TryResolveOrdinaryCall(call, out module) || module is null))
                    || !targets.TryAdd(tag, (call, module!, directMapLookup)))
                {
                    return false;
                }
            }

            var branches = targets.OrderBy(entry => entry.Key,
                Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true)))
                .Reverse().Select(entry => (
                    Tag: entry.Key,
                    entry.Value.Call,
                    entry.Value.Module,
                    entry.Value.DirectMapLookup,
                    Entry: Label()
                )).ToArray();
            int success = Label();
            int invalid = Label();
            int handler = Label();
            int rethrow = Label();
            int token = function.Arity;
            int frame = token + 1;
            int savedReference = token - 1;

            EmitFrameAllocation(function.Arity, frame);
            // The entry copies and the handler installation define all Y slots before any GC.
            for (int register = function.Arity - 1; register >= 0; register--)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    register,
                    YRegisterTag,
                    savedReference - register
                );
            }

            owner.Op(TryEnter, YRegisterTag, token);
            owner.Operand(LabelTag, handler);
            int selector = 0;
            int payloadRegister = 1;
            if (prefix is null)
            {
                EmitTupleShapeTest(
                    XRegisterTag,
                    0,
                    2,
                    invalid
                );
                owner.EmitTuplePart(0, 0, selector);
            }
            else
            {
                var previous = location;
                owner.EmitArithmeticLine(prefix.SourceLine, ref previous);
                owner.Op(GcBifOne, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, function.Arity);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, prefix.Operator, 1));
                owner.Operand(XRegisterTag, 2);
                owner.Operand(XRegisterTag, 0);
                EmitTupleShapeTest(
                    YRegisterTag,
                    savedReference,
                    2,
                    invalid
                );
                selector = 1;
                payloadRegister = 2;
                owner.EmitTuplePart(
                    savedReference,
                    0,
                    selector,
                    YRegisterTag
                );
            }

            owner.EmitTuplePart(
                savedReference,
                1,
                payloadRegister,
                YRegisterTag
            );
            owner.EmitMembershipSelect(selector, invalid,
                branches.Reverse().Select(branch => ((Term)branch.Tag, branch.Entry)));

            for (int branch = 0; branch < branches.Length; branch++)
            {
                var arm = branches[branch];
                Mark(arm.Entry);
                if (arm.DirectMapLookup)
                {
                    owner.EmitCallLine(arm.Call, location);
                    owner.Op(BifTwo, LabelTag, 0);
                    owner.Operand(BeamWire.UnsignedTag, owner.Import(arm.Module, arm.Call.Function, 2));
                    owner.Operand(XRegisterTag, payloadRegister);
                    owner.Operand(YRegisterTag, savedReference - 1);
                    owner.Operand(XRegisterTag, 0);
                    if (branch < branches.Length - 1)
                    {
                        JumpTo(success);
                    }

                    continue;
                }

                if (prefix is null)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        payloadRegister,
                        XRegisterTag,
                        0
                    );
                }

                if (function.Arity > 1)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        savedReference - 1,
                        XRegisterTag,
                        1
                    );
                }

                if (prefix is not null)
                {
                    owner.Op(Swap, XRegisterTag, 0);
                    owner.Operand(XRegisterTag, payloadRegister);
                }

                owner.EmitCallLine(arm.Call, location);
                Call(arm.Module, arm.Call.Function, function.Arity);
                if (branch < branches.Length - 1)
                {
                    JumpTo(success);
                }
            }

            Mark(success);
            owner.Op(TryEnd, YRegisterTag, token);
            owner.EmitFrameReturn(frame);
            Mark(invalid);
            owner.EmitConstant(badValue, 0);
            owner.EmitCallLine(reject, location);
            Call(ErlangModuleNames.Module, reject.Function, reject.Arguments.Count);

            Mark(handler);
            owner.Op(TryCase, YRegisterTag, token);
            owner.Op(ExactEqualTest, LabelTag, rethrow);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.LiteralOperand(caughtClass);
            owner.Op(TestHeap, BeamWire.UnsignedTag, token * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 2);
            for (int slot = 0; slot < token; slot++)
            {
                owner.Op(PutList, YRegisterTag, slot);
                if (slot == 0)
                {
                    owner.LiteralOperand(Nil.Value);
                }
                else
                {
                    owner.Operand(XRegisterTag, 0);
                }

                owner.Operand(XRegisterTag, 0);
            }

            owner.EmitConstant(extraInfo, 2);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.EmitCallLine(reroute, location);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, reroute.Arguments.Count);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, reroute.Function, reroute.Arguments.Count));
            owner.Operand(BeamWire.UnsignedTag, frame);

            Mark(rethrow);
            owner.Op(Raise, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);

            return true;

            (Expr.Call Call, Term Metadata)? ReadErrorArguments(Expr body, string[] parameters, string reasonName)
            {
                Expr.List? prepared = null;
                string? binding = null;
                if (body is Expr.Sequence
                    {
                        Items: [
                            Expr.Match { Pattern: Pattern.Variable temporary, Value: Expr.List { Tail: null } arguments },
                            Expr.Call call
                        ]
                    })
                {
                    binding = temporary.Name;
                    if (binding == reasonName || binding == VariableScopeNames.Wildcard || parameters.Contains(binding))
                    {
                        return null;
                    }

                    prepared = arguments;
                    body = call;
                }

                if (body is not Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module,
                        Function: AutoImportNames.Error, DynamicRemote: false,
                        Arguments: [Expr.Variable errorReason, var args, var information]
                    } reroute || errorReason.Name != reasonName || !owner.ResolvesErlangBif(reroute)
                    || !AttributeLiteral.TryNormalizeConstant(information, out Term metadata))
                {
                    return null;
                }

                Expr.List? list = prepared is null ? args as Expr.List
                    : args is Expr.Variable variable && variable.Name == binding ? prepared : null;

                return list is { Tail: null } && ReadsInputs(list.Items, parameters) ? (reroute, metadata) : null;
            }

            static bool ReadsInputs(IReadOnlyList<Expr> arguments, string[] names) =>
                arguments.Count == names.Length && arguments.Select((argument, index) =>
                    argument is Expr.Variable source && source.Name == names[index]).All(matches => matches);
        }
    }
}
