using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCapturedFoldRecordAppend(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable input],
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match { Pattern: Pattern.Variable sourceBinding, Value: var source },
                            Expr.Match
                        {
                            Pattern: Pattern.Variable resultBinding,
                            Value: Expr.Call
                            {
                                Module: "lists", Function: "foldl", DynamicRemote: false,
                                Arguments: [Expr.Fun { SelfName: null } callback, var initial, Expr.Variable list]
                            } fold
                        },
                            Expr.Binary
                        {
                            Operator: ErlangOperators.Append,
                            Left: Expr.Call
                            {
                                Module: "lists", Function: "reverse", DynamicRemote: false,
                                Arguments: [Expr.Variable result]
                            } reverse,
                            Right: Expr.RecordAccess { Value: Expr.Variable record } access
                        }
                        ]
                    }
                }
                ]
                || !DistinctBindings(input, sourceBinding, resultBinding)
                || list.Name != sourceBinding.Name || result.Name != resultBinding.Name || record.Name != input.Name
                || !TryFoldSourceConstant(source, out Term elements)
                || !AttributeLiteral.TryNormalizeConstant(initial, out Term accumulator))
            {
                return false;
            }

            var available = new Dictionary<string, Term>(StringComparer.Ordinal)
            {
                [input.Name] = Nil.Value,
                [sourceBinding.Name] = elements
            };
            var captures = ClosureCaptures.Capture(callback, available);
            if (captures.Count != 1 || !captures.ContainsKey(input.Name))
            {
                return false;
            }

            int failure = Label();
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 1);
            owner.EmitClosureHeap(1, 1, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            var previousVariables = variables;
            variables = new(StringComparer.Ordinal) { [input.Name] = 0 };
            EmitGeneralClosure(
                callback,
                emitCapturedFailure: true,
                heapChecked: true,
                x0Capture: input.Name
            );
            variables = previousVariables;
            owner.EmitConstant(elements, 2);
            owner.EmitConstant(accumulator, 1);
            owner.EmitCallLine(fold, location);
            Call(fold.Module, fold.Function, 3);
            owner.EmitCallLine(reverse, CallLocation(fold, location));
            Call(reverse.Module, reverse.Function, 1);
            owner.EmitRecordTest(
                failure,
                YRegisterTag,
                0,
                Term.A(access.Name),
                access.Arity
            );
            owner.Op(GetTupleElement, YRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, access.Index);
            owner.Operand(XRegisterTag, 1);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Append, 2));
            owner.Operand(BeamWire.UnsignedTag, 1);
            Mark(failure);
            owner.Op(BadRecord, YRegisterTag, 0);

            return true;
        }

        private static bool TryFoldSourceConstant(Expr source, out Term value)
        {
            if (AttributeLiteral.TryNormalizeConstant(source, out value))
            {
                return true;
            }

            if (source is not Expr.Binary { Operator: ErlangOperators.SubtractList } subtraction
                || !AttributeLiteral.TryNormalizeConstant(subtraction.Left, out Term first)
                || !AttributeLiteral.TryNormalizeConstant(subtraction.Right, out Term second))
            {
                return false;
            }

            try
            {
                value = ErlangOperatorValues.Binary(
                    ErlangOperators.SubtractList,
                    first,
                    second,
                    context: null
                );

                return true;
            }
            catch (ErlangException)
            {
                return false;
            }
        }
    }
}
