using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEvaluatedConfiguration(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable parameter],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Tuple { Items: [
                        Pattern.Literal { Value: Atom valueTag }, Pattern.Variable result, Pattern.Variable bindings
                    ] }, Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Variable input, Expr.Call { Module: not null, DynamicRemote: false, Arguments: [] } emptyBindings
                    ] } evaluate } match,
                    Expr.Case { Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable inspect] } inspectCall,
                        Clauses: [
                            { Guard: null, Patterns: [var empty], Body: Expr.Variable returned },
                            { Guard: null, Patterns: [Pattern.Variable unexpected], Body: Expr.Call { Arguments: [
                                Expr.Tuple { Items: [Expr.Literal { Value: Atom context },
                                    Expr.Tuple { Items: [Expr.Literal { Value: Atom reason }, Expr.Variable other] }
                                ] }
                            ] } fail }
                        ]
                    }
                ] }
            }] || !IsEmptyListPattern(empty) || !DistinctBindings(parameter, result, bindings, unexpected)
                || input.Name != parameter.Name || inspect.Name != bindings.Name || returned.Name != result.Name
                || unexpected.Name != other.Name || !IsConfigurationError(fail))
            {
                return false;
            }

            int badBindings = Label();
            int badValue = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            SaveWindowResult(0);
            var previous = location;
            EmitWindowCall(emptyBindings, ref previous);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            Load(0);
            owner.InitStackSlot(0);
            EmitWindowCall(evaluate, ref previous);
            EmitDetailRecordTest(0, 3, valueTag, badValue);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 0);
            owner.EmitTuplePart(0, 2, 0);
            EmitWindowCall(inspectCall, ref previous);
            owner.EmitNilTest(XRegisterTag, 0, badBindings);
            Load(0);
            owner.EmitFrameReturn(1);
            Mark(badBindings);
            EmitNestedConfigurationError(fail, context, reason, 0, 1, 1, previous);
            Mark(badValue);
            owner.EmitLine(new(match.PreprocessorFile ?? location.File, match.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }
    }
}
