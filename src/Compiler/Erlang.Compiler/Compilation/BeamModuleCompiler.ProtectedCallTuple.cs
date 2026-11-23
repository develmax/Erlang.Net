using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCaughtPreparedTupleCall(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.Tuple tuple },
                                Expr.Catch { Operand: Expr.Call call }
                            ]
                    }
                } clause
                ]
                || !HasUnconditionalParameters(clause) || binding.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(binding.Name))
            {
                return false;
            }

            Expr[] arguments = call.Arguments.ToArray();
            int argument = -1;
            for (int index = 0; index < arguments.Length; index++)
            {
                if (arguments[index] is Expr.Variable variable && variable.Name == binding.Name)
                {
                    if (argument >= 0)
                    {
                        return false;
                    }

                    argument = index;
                }
            }

            if (argument < 0)
            {
                return false;
            }

            arguments[argument] = tuple;
            if (ReadEntryCall(call with { Arguments = arguments }, parameters, allowConstructedArgument: true) is not
                { Module: not null, ConstructedArgument: { IsTuple: true } prepared } producer
                || producer.Module == ErlangModuleNames.Module || prepared.Register != argument)
            {
                return false;
            }

            // The original tuple binding is outside the protected effect and
            // none of its inputs are overwritten before the outgoing copy.
            int live = Math.Max(EntryCallLiveCount(producer, parameters),
                prepared.Operands.Max(operand => operand.Source.GetValueOrDefault(-1)) + 1);
            int handler = Label();
            EmitFrameAllocation(live, 1, prepared.HeapWords);
            EmitEntryConstruction(prepared);
            EnterCatch(0, handler);
            EmitEntryCall(producer with { ConstructedArgument = null });
            EndCatch(0, handler);
            owner.EmitFrameReturn(1);

            return true;
        }

        private bool TryEmitProtectedCallTuple(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Body: Expr.Try
                    {
                        After: null,
                        Body: var body,
                        Clauses.Count: 0,
                        Catches:
                            [
                            {
                                Guard: null,
                                Patterns:
                                    [
                                        Pattern.Tuple
                                    {
                                        Items:
                                            [
                                                Pattern.Literal { Value: Atom kind },
                                                Pattern.Literal { Value: Atom reason },
                                                Pattern.Variable { Name: VariableScopeNames.Wildcard }
                                            ]
                                    }
                                    ],
                                Body: var fallback
                            }
                            ]
                    }
                } clause
                ]
                || !HasUnconditionalParameters(clause)
                || kind.Name is not (ErlangExceptionClasses.Error or ErlangExceptionClasses.Throw or ErlangExceptionClasses.Exit)
                || !AttributeLiteral.TryNormalizeConstant(fallback, out Term constant))
            {
                return false;
            }

            while (body is Expr.Block block)
            {
                body = block.Body;
            }

            if (body is not Expr.Tuple { Items: [Expr.Literal tag, Expr.Call call] })
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (ReadEntryCall(call, parameters) is not { Module: not null } producer
                || producer.Module == ErlangModuleNames.Module)
            {
                return false;
            }

            // The call consumes the incoming operands before the try succeeds.
            // Keep the landingpad operands in X and construct the success tuple after releasing the tag.
            int handler = Label();
            int unmatched = Label();
            EmitFrameAllocation(EntryCallLiveCount(producer, parameters), 1);
            owner.Op(TryEnter, YRegisterTag, 0);
            owner.Operand(LabelTag, handler);
            EmitEntryCall(producer);
            owner.Op(TryEnd, YRegisterTag, 0);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(tag.Value);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);

            Mark(handler);
            owner.Op(TryCase, YRegisterTag, 0);
            owner.Op(ExactEqualTest, LabelTag, unmatched);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.LiteralOperand(kind);
            owner.Op(ExactEqualTest, LabelTag, unmatched);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(reason);
            owner.EmitConstant(constant, 0);
            owner.EmitFrameReturn(1);

            Mark(unmatched);
            owner.Op(Raise, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);

            return true;
        }
    }
}
