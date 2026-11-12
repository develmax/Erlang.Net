using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitPreservedTaggedResult(FunctionDefinition function)
        {
            if (function.Clauses is not
                [{
                    Guard: null, Patterns: [],
                    Body: Expr.Case
                    {
                        Value: Expr.Call { Module: not null, DynamicRemote: false } create,
                        Clauses:
                        [
                            { Guard: null,
                                Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable payload] }],
                                Body: Expr.Sequence { Items:
                                    [Expr.Call { Module: not null, DynamicRemote: false } effect,
                                        Expr.Tuple { Items: [Expr.Literal { Value: Atom returnedTag }, Expr.Variable returned] }] } },
                            { Guard: null, Patterns: [Pattern.Variable failure], Body: Expr.Variable unchanged }
                        ]
                    }
                }] || payload.Name == VariableScopeNames.Wildcard || payload.Name != returned.Name
                || failure.Name == VariableScopeNames.Wildcard || failure.Name != unchanged.Name
                || !tag.Equals(returnedTag) || !ReadArguments(create, out var initial) || !ReadArguments(effect, out var followup))
            {
                return false;
            }

            int fallback = Label();
            owner.EmitEmptyCallFrame(1);
            owner.EmitIndependentCallMoves(initial);
            owner.EmitCallLine(create, location);
            owner.EmitStaticRemoteCall(create);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 0);
            owner.EmitRecordTest(fallback, XRegisterTag, 0, tag, 2);
            owner.EmitIndependentCallMoves(followup);
            owner.EmitCallLine(effect, CallLocation(create, location));
            owner.EmitStaticRemoteCall(effect);
            owner.MoveRegister(YRegisterTag, 0, XRegisterTag, 0);
            owner.EmitFrameReturn(1);
            Mark(fallback);
            owner.EmitFrameReturn(1);

            return true;

            static bool ReadArguments(Expr.Call call, out List<(int Destination, Term Value)> arguments)
            {
                arguments = [];
                foreach (Expr expression in call.Arguments)
                {
                    if (!AttributeLiteral.TryNormalizeConstant(expression, out Term value))
                    {
                        return false;
                    }

                    arguments.Add((arguments.Count, value));
                }

                return true;
            }
        }

        private bool TryEmitTaggedPairWithSavedCall(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [],
                    Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.Variable saved,
                            Value: Expr.Call { Module: not null, DynamicRemote: false } create
                        },
                    Expr.Match
                        {
                            Pattern: Pattern.Tuple
                            {
                                Items: [
                        Pattern.Literal { Value: Atom tag }, Pattern.Variable left, Pattern.Variable right
                    ]
                            }, Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [] } read
                        } match,
                    Expr.Tuple { Items: [Expr.Tuple inner, Expr.Variable second] }
                ]
                    }
                }] || !DistinctBindings(saved, left, right) || second.Name != right.Name)
            {
                return false;
            }

            var arguments = new List<(int Destination, Term Value)>();
            foreach (Expr expression in create.Arguments)
            {
                if (!AttributeLiteral.TryNormalizeConstant(expression, out Term value))
                {
                    return false;
                }
                arguments.Add((arguments.Count, value));
            }
            var fields = new List<(Term? Literal, int Tag, int Register)>();
            foreach (Expr expression in inner.Items)
            {
                switch (expression)
                {
                    case Expr.Variable variable when variable.Name == saved.Name:
                        fields.Add((null, YRegisterTag, 0));
                        break;
                    case Expr.Variable variable when variable.Name == left.Name:
                        fields.Add((null, XRegisterTag, 1));
                        break;
                    case Expr.Variable variable when variable.Name == right.Name:
                        fields.Add((null, XRegisterTag, 0));
                        break;
                    default:
                        if (!AttributeLiteral.TryNormalizeConstant(expression, out Term literal))
                        {
                            return false;
                        }
                        fields.Add((literal, 0, 0));
                        break;
                }
            }

            int failure = Label();
            owner.EmitEmptyCallFrame(1);
            owner.EmitIndependentCallMoves(arguments);
            owner.EmitCallLine(create, location);
            owner.EmitStaticRemoteCall(create);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            var previous = CallLocation(create, location);
            owner.EmitCallLine(read, previous);
            previous = CallLocation(read, previous);
            owner.EmitStaticRemoteCall(read);
            owner.Op(IsTaggedTuple, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.LiteralOperand(tag);
            owner.Op(TestHeap, BeamWire.UnsignedTag, fields.Count + 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(0, 1, 1);
            owner.EmitTuplePart(0, 2, 0);
            owner.Op(PutTuple, XRegisterTag, 1);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, fields.Count);
            foreach (var (literal, registerTag, register) in fields)
            {
                if (literal is null)
                {
                    owner.Operand(registerTag, register);
                }
                else
                {
                    owner.LiteralOperand(literal);
                }
            }
            owner.EmitTagMatchTuple(0, (XRegisterTag, 1), (XRegisterTag, 0));
            owner.EmitFrameReturn(1);

            Mark(failure);
            var failureLocation = match.SourceLine == 0 ? previous : new(match.PreprocessorFile, match.SourceLine);
            if (failureLocation != previous)
            {
                owner.EmitLine(failureLocation);
            }
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }
    }
}
