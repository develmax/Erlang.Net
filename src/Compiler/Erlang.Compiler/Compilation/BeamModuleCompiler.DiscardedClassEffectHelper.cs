using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitWxCheckedListCommand(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || function.Clauses is not
                [
                {
                    Patterns: [Pattern.Variable scalar, Pattern.Variable list],
                    Guard: { } guard,
                    Body: Expr.Sequence
                    {
                        Items: [
                            Expr.Match
                        {
                            Pattern: Pattern.Variable { Name: VariableScopeNames.Wildcard },
                            Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable checkedList] } check
                        },
                            Expr.Call
                        {
                            Module: "wxe_util", Function: "queue_cmd", DynamicRemote: false,
                            Arguments: [Expr.Variable first, Expr.Variable second, Expr.Case environment, Expr.Literal { Value: Integer command }]
                        } queue,
                            Expr.Call
                        {
                            Module: "wxe_util", Function: "rec", DynamicRemote: false,
                            Arguments: [Expr.Literal { Value: Integer response }]
                        } receive
                        ]
                    }
                }]
                || first.Name != scalar.Name || second.Name != list.Name || checkedList.Name != list.Name
                || !DistinctBindings(scalar, list) || !command.Equals(response) || !IsWxEnvironment(environment)
                || !entries.TryGetValue((check.Function, 1), out int helper)
                || !localFunctions.TryGetValue((check.Function, 1), out var mapping) || !mapping.IsRecursiveListMapper)
            {
                return false;
            }

            var guards = new List<(byte Opcode, int Register)>();
            if (!ReadWxStaticGuards(guard, [scalar.Name, list.Name], guards)
                || guards is not [(IsInteger, 0), (IsList, 1)])
            {
                return false;
            }

            int functionFailure = owner.nextLabel - 2;
            int missingEnvironment = Label();
            foreach (var (opcode, register) in guards)
            {
                owner.Op(opcode, LabelTag, functionFailure);
                owner.Operand(XRegisterTag, register);
            }

            EmitFrameAllocation(2, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(check, location);
            owner.EmitFieldCall(1, helper);
            EmitWxEnvironmentCheck(0, missingEnvironment);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, 2);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitConstant(command, 3);
            owner.MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitCallLine(queue, CallLocation(check, location));
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(queue.Module!, queue.Function, 4));
            owner.EmitConstant(command, 0);
            owner.EmitCallLine(receive, CallLocation(queue, location));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(receive.Module!, receive.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, 0);

            Mark(missingEnvironment);
            owner.EmitConstant(Term.Tuple(Term.A("wx"), Term.A("unknown_env")), 0);
            owner.EmitCallLine(queue, CallLocation(receive, location));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, "error", 1));
            owner.Operand(BeamWire.UnsignedTag, 2);

            return true;
        }

        private bool TryEmitDiscardedClassEffectHelper(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || !function.IsRecursiveListMapper || function.IsStrictRecursiveListMapper
                || function.Arity != 1 || function.Clauses.Count != 4 || function.Clauses[0] is not
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [Pattern.Tuple tuple], Tail: Pattern.Variable tail }],
                    Body: Expr.Sequence
                    {
                        Items: [
                            Expr.Binary
                        {
                            Operator: ErlangOperators.OrElse,
                            Left: Expr.Binary
                            {
                                Operator: ErlangOperators.ExactEqual,
                                Left: Expr.Variable checkedReceiver,
                                Right: Expr.Literal { Value: Atom checkedClass }
                            },
                            Right: Expr.Call
                            {
                                DynamicRemote: true,
                                Arguments: [Expr.Variable calledReceiver, Expr.Literal { Value: Atom method },
                                        Expr.List { Items: [Expr.Literal { Value: var argument }], Tail: null }]
                            }
                        },
                            Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable recurTail] } recur
                        ]
                    }
                } || recur.Function != function.Name || recurTail.Name != tail.Name || calledReceiver.Name != checkedReceiver.Name
                || tuple.Items.Count < 2 || tuple.Items[0] is not Pattern.Literal { Value: Atom tag }
                || !entries.TryGetValue((function.Name, function.Arity), out int entry))
            {
                return false;
            }

            int field = -1;
            for (int index = 1; index < tuple.Items.Count; index++)
            {
                if (tuple.Items[index] is Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    continue;
                }

                if (field != -1 || tuple.Items[index] is not Pattern.Variable variable || variable.Name != calledReceiver.Name)
                {
                    return false;
                }

                field = index;
            }
            if (field < 0)
            {
                return false;
            }

            int advance = Label();
            int skip = Label();
            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.Op(IsTaggedTuple, LabelTag, skip);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
            owner.Operand(AtomTag, owner.Intern(tag.Name));
            EmitFrameAllocation(arity: 2, size: 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.Op(GetTupleElement, XRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, field);
            owner.Operand(XRegisterTag, 0);
            owner.Op(ExactNotEqualTest, LabelTag, advance);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(AtomTag, owner.Intern(checkedClass.Name));
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitConstant(method, 2);
            owner.EmitConstant(argument, 0);
            owner.Op(DynamicApply, BeamWire.UnsignedTag, 1);
            Mark(advance);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, 1);
            Mark(skip);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, entry);
            EmitRecursiveListMappingEnd(function, empty);

            return true;
        }
    }
}
