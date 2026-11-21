using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProtectedSingletonOptions(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Variable value, Pattern.Variable options],
                    Body: Expr.Try
                    {
                        Body: Expr.Case { Value: Expr.Variable tested } selection,
                        Clauses: [],
                        Catches:
                        [
                        {
                            Guard: null,
                            Patterns: [Pattern.Tuple
                            {
                                Items:
                                    [
                                        Pattern.Literal { Value: Atom firstClass },
                                        Pattern.Literal { Value: Atom specialReason },
                                        Pattern.Variable { Name: VariableScopeNames.Wildcard }
                                    ]
                            }],
                            Body: var specialized
                        },
                        {
                            Guard: null,
                            Patterns: [Pattern.Tuple
                            {
                                Items:
                                    [
                                        Pattern.Literal { Value: Atom secondClass },
                                        Pattern.Variable reason,
                                        Pattern.Variable { Name: VariableScopeNames.Wildcard }
                                    ]
                            }],
                            Body: var ordinary
                        }
                        ],
                        After: null
                    }
                }]
                || !DistinctBindings(value, options, reason) || options.Name != tested.Name
                || !firstClass.Equals(secondClass) || selection.Clauses.Count < 4
                || selection.Clauses[^2] is not { Guard: null, Patterns: [var nil] } empty
                || ListEntryTest(nil) != IsNil
                || selection.Clauses[^1] is not
                {
                    Guard: null,
                    Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module,
                        Function: AutoImportNames.Error, DynamicRemote: false, Arguments: [var rejected]
                    } reject
                }
                || !owner.ResolvesErlangBif(reject)
                || !AttributeLiteral.TryNormalizeConstant(rejected, out Term rejectedValue)
                || ReadConstructor(empty.Body) is not { } emptyPlan
                || ReadHandler(specialized, returnsReason: false) is not { Value: not null } special
                || ReadHandler(ordinary, returnsReason: true) is not { } general)
            {
                return false;
            }

            var tagged = new Dictionary<Atom, (Atom Tag, Expr.Call Call, string Module, Term[] Constants)>();
            foreach (Clause branch in selection.Clauses.SkipLast(2))
            {
                if (branch is not
                    {
                        Guard: null,
                        Patterns: [Pattern.List { Items: [Pattern.Literal { Value: Atom option }], Tail: null }]
                    } || ReadConstructor(branch.Body) is not { } constructor || !tagged.TryAdd(option, constructor))
                {
                    return false;
                }
            }

            var entries = tagged.OrderBy(pair => pair.Key,
                Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true)))
                .Reverse().Select(pair => (Key: pair.Key, Plan: pair.Value, Target: Label())).ToArray();
            int nilOption = Label();
            int success = Label();
            int invalid = Label();
            int handler = Label();
            int otherReason = Label();
            int rethrow = Label();
            const int valueSlot = 1;
            const int optionsSlot = 0;
            const int token = 2;
            const int frame = token + 1;

            EmitFrameAllocation(function.Arity, frame);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                optionsSlot
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                valueSlot
            );
            owner.Op(TryEnter, YRegisterTag, token);
            owner.Operand(LabelTag, handler);
            owner.EmitNonemptyList(1, nilOption);
            owner.EmitListParts(
                1,
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitMembershipSelect(0, invalid, entries.Reverse().Select(entry => ((Term)entry.Key, entry.Target)));
            foreach (var entry in entries)
            {
                Mark(entry.Target);
                owner.EmitNilTest(XRegisterTag, 1, invalid);
                EmitConstructor(entry.Plan, saved: true);
                JumpTo(success);
            }

            Mark(nilOption);
            owner.EmitNilTest(XRegisterTag, 1, invalid);
            EmitConstructor(emptyPlan, saved: false);
            Mark(success);
            owner.Op(TryEnd, YRegisterTag, token);
            owner.EmitFrameReturn(frame);

            Mark(invalid);
            owner.EmitConstant(rejectedValue, 0);
            owner.EmitCallLine(reject, location);
            Call(ErlangModuleNames.Module, reject.Function, reject.Arguments.Count);

            Mark(handler);
            owner.Op(TryCase, YRegisterTag, token);
            owner.Op(ExactEqualTest, LabelTag, rethrow);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.LiteralOperand(firstClass);
            owner.Op(ExactEqualTest, LabelTag, otherReason);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(specialReason);
            EmitHandler(special, live: 0, listRegister: 1);

            Mark(otherReason);
            EmitHandler(general, live: 2, listRegister: 0);

            Mark(rethrow);
            owner.Op(Raise, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);

            return true;

            (Atom Tag, Expr.Call Call, string Module, Term[] Constants)? ReadConstructor(Expr expression)
            {
                if (expression is not Expr.Tuple
                    {
                        Items: [
                            Expr.Literal { Value: Atom tag },
                            Expr.Call { Module: not null and not ErlangModuleNames.Module, DynamicRemote: false } call
                        ]
                    } || call.Arguments.Count == 0 || call.Arguments.Count >= FirstReservedXRegister
                    || call.Arguments[0] is not Expr.Variable parameter || parameter.Name != value.Name
                    || !TryResolveOrdinaryCall(call, out string? module) || module is null)
                {
                    return null;
                }

                var constants = new List<Term>();
                foreach (Expr argument in call.Arguments.Skip(1))
                {
                    if (!AttributeLiteral.TryNormalizeConstant(argument, out Term literal))
                    {
                        return null;
                    }

                    constants.Add(literal);
                }

                return (tag, call, module, constants.ToArray());
            }

            (Expr.Call Call, Term Metadata, Term? Value)? ReadHandler(Expr expression, bool returnsReason)
            {
                var temporaries = new Dictionary<string, Expr>(StringComparer.Ordinal);
                if (expression is Expr.Sequence { Items.Count: > 1 } sequence)
                {
                    foreach (Expr item in sequence.Items.SkipLast(1))
                    {
                        if (item is not Expr.Match { Pattern: Pattern.Variable temporary, Value: var bound }
                            || temporary.Name == VariableScopeNames.Wildcard || temporary.Name == reason.Name
                            || temporary.Name == value.Name || temporary.Name == options.Name
                            || !(IsArguments(bound) || AttributeLiteral.TryNormalizeConstant(bound, out _))
                            || !temporaries.TryAdd(temporary.Name, bound))
                        {
                            return null;
                        }
                    }

                    expression = sequence.Items[^1];
                }

                if (expression is not Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module,
                        Function: AutoImportNames.Error, DynamicRemote: false,
                        Arguments: [var returned, var args, var metadata]
                    } call
                    || !owner.ResolvesErlangBif(call) || !IsArguments(Resolve(args))
                    || !AttributeLiteral.TryNormalizeConstant(Resolve(metadata), out Term information))
                {
                    return null;
                }

                if (returnsReason)
                {
                    return returned is Expr.Variable caught && caught.Name == reason.Name ? (call, information, null) : null;
                }

                return AttributeLiteral.TryNormalizeConstant(Resolve(returned), out Term constant)
                    ? (call, information, constant) : null;

                Expr Resolve(Expr item) => item is Expr.Variable binding && temporaries.TryGetValue(binding.Name, out var term)
                    ? term : item;

                bool IsArguments(Expr item) => item is Expr.List
                {
                    Items: [Expr.Variable first, Expr.Variable second], Tail: null
                } && first.Name == value.Name && second.Name == options.Name;
            }

            void EmitConstructor((Atom Tag, Expr.Call Call, string Module, Term[] Constants) plan, bool saved)
            {
                for (int index = plan.Constants.Length - 1; index >= 0; index--)
                {
                    owner.EmitConstant(plan.Constants[index], index + 1);
                }

                if (saved)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        valueSlot,
                        XRegisterTag,
                        0
                    );
                }

                owner.EmitCallLine(plan.Call, location);
                Call(plan.Module, plan.Call.Function, plan.Call.Arguments.Count);
                EmitTaggedRegisterResult(plan.Tag);
            }

            void EmitHandler((Expr.Call Call, Term Metadata, Term? Value) plan, int live, int listRegister)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, token * ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Op(PutList, YRegisterTag, optionsSlot);
                owner.LiteralOperand(Nil.Value);
                owner.Operand(XRegisterTag, 0);
                owner.Op(PutList, YRegisterTag, valueSlot);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, listRegister);
                owner.EmitConstant(plan.Metadata, 2);
                if (plan.Value is { } constant)
                {
                    owner.EmitConstant(constant, 0);
                }
                else
                {
                    owner.Op(Swap, XRegisterTag, 0);
                    owner.Operand(XRegisterTag, 1);
                }

                owner.EmitCallLine(plan.Call, location);
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, plan.Call.Arguments.Count);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, plan.Call.Function, plan.Call.Arguments.Count));
                owner.Operand(BeamWire.UnsignedTag, frame);
            }
        }
    }
}
