using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitClosedTupleInputForwarding(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Patterns: [Pattern.Variable input, Pattern.Tuple { Items: [Pattern.Variable first, Pattern.Variable second] }],
                    Guard: Expr.Call
                    {
                        Function: AutoImportNames.IsTuple, DynamicRemote: false,
                        Arguments: [Expr.Variable tested]
                    } guard,
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments:
                                [
                                    Expr.Call
                                {
                                    Function: AutoImportNames.TupleToList, DynamicRemote: false,
                                    Arguments: [Expr.Variable source]
                                } convert,
                                    Expr.Variable forwardedFirst,
                                    Expr.Variable forwardedSecond
                                ]
                    } target
                },
                {
                    Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Guard: null, Body: Expr.Call { Arguments: [Expr.Literal reason] } fallback
                }
                ]
                || !DistinctBindings(input, first, second) || input.Name != tested.Name || input.Name != source.Name
                || first.Name != forwardedFirst.Name || second.Name != forwardedSecond.Name
                || !owner.ResolvesErlangBif(guard) || !owner.ResolvesErlangBif(convert)
                || !entries.TryGetValue((target.Function, 3), out int entry)
                || ReadStackFreeThrow(fallback, requireCallerHandler: true) is not { } raised
                || !HasClosedPairInput(function))
            {
                return false;
            }

            int failure = Label();
            owner.Op(IsTuple, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            EmitFrameAllocation(arity: 2, size: 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitCallLine(convert, location);
            Call(ErlangModuleNames.Module, convert.Function, 1);
            owner.Op(GetTupleElement, YRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Op(GetTupleElement, YRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, 2);
            EmitTailLocal(entry, 3, 1);

            Mark(failure);
            owner.EmitConstant(reason.Value, 1);
            owner.EmitConstant(Nil.Value, 2);
            owner.EmitConstant(Term.A(ErlangExceptionClasses.Throw), 0);
            owner.EmitCallLine(raised, location);
            EmitRegisterTailTarget(raised, raised.Arguments.Count);

            return true;
        }

        private bool HasClosedPairInput(FunctionDefinition function, Func<Expr, bool>? acceptsPair = null)
        {
            var signature = (function.Name, function.Arity);
            if (owner.exportedFunctions.Contains(signature) || owner.IsDeclaredNative(signature))
            {
                return false;
            }

            // Every statically accessible entrance to the private function
            // must supply a two-tuple. Never drop a check for a fun reference.
            bool found = false;
            foreach (var expression in localFunctions.Values.SelectMany(definition => LocalFunctionEntries.ClauseExpressions(definition.Clauses))
                .SelectMany(LocalFunctionEntries.Walk))
            {
                if (expression is Expr.FunctionReference reference && reference.Name == signature.Name && reference.Arity == signature.Arity)
                {
                    return false;
                }

                if (expression is not Expr.Call { Module: null, DynamicRemote: false, Arguments.Count: 2 } call
                    || call.Function != signature.Name)
                {
                    continue;
                }

                if (call.Arguments[1] is not (Expr.Tuple { Items.Count: 2 } or Expr.Literal { Value: TupleTerm { Items.Count: 2 } })
                    || acceptsPair is not null && !acceptsPair(call.Arguments[1]))
                {
                    return false;
                }

                found = true;
            }

            return found;
        }
    }
}
