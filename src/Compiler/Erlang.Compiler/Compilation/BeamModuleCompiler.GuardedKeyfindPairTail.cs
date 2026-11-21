using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedKeyfindPairTail(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Patterns: [Pattern.Variable input, Pattern.Variable pairs, Pattern.Variable option],
                    Guard: Expr.Call
                    {
                        Function: AutoImportNames.IsAtom, DynamicRemote: false,
                        Arguments: [Expr.Variable checkedInput]
                    } guard,
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Module: ListModuleNames.Module, Function: ListModuleNames.KeyFind, DynamicRemote: false,
                            Arguments: [Expr.Variable key, Expr.Literal { Value: Integer position }, Expr.Variable lookupPairs]
                        } lookup,
                        Clauses:
                                [
                                {
                                    Guard: null,
                                    Patterns:
                                            [
                                                Pattern.Tuple
                                            {
                                                Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable found]
                                            }
                                            ],
                                    Body: Expr.Call
                                    {
                                        Module: null, DynamicRemote: false,
                                        Arguments:
                                                [
                                                    Expr.Variable selectedOption,
                                                    Expr.Call
                                                {
                                                    Module: null, DynamicRemote: false, Arguments: [Expr.Variable selected]
                                                } read
                                                ]
                                    } finish
                                },
                                {
                                    Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                    Body: Expr.Call
                                    {
                                        Function: AutoImportNames.Exit, DynamicRemote: false,
                                        Arguments:
                                                [
                                                    Expr.Tuple
                                                {
                                                    Items:
                                                            [
                                                                Expr.Literal { Value: Atom outer },
                                                                Expr.Tuple
                                                            {
                                                                Items:
                                                                        [
                                                                            Expr.Literal { Value: Atom inner },
                                                                            Expr.Tuple
                                                                        {
                                                                            Items: [Expr.Literal { Value: Atom detail }, Expr.Variable failed]
                                                                        }
                                                                        ]
                                                            }
                                                            ]
                                                }
                                                ]
                                    } exit
                                }
                                ]
                    }
                },
                {
                    Guard: null, Patterns: [Pattern.Variable direct, Pattern.Variable unused, Pattern.Variable otherOption],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments:
                                [
                                    Expr.Variable directOption,
                                    Expr.Call
                                {
                                    Module: null, DynamicRemote: false, Arguments: [Expr.Variable directInput]
                                } directRead
                                ]
                    } directFinish
                }
                ]
                || !DistinctBindings(
                    input,
                    pairs,
                    option,
                    found
                ) || !DistinctBindings(direct, otherOption)
                || unused.Name != VariableScopeNames.Wildcard && (unused.Name == direct.Name || unused.Name == otherOption.Name)
                || checkedInput.Name != input.Name || key.Name != input.Name || lookupPairs.Name != pairs.Name
                || failed.Name != input.Name || selected.Name != found.Name || selectedOption.Name != option.Name
                || directOption.Name != otherOption.Name || directInput.Name != direct.Name
                || directRead.Function != read.Function || directFinish.Function != finish.Function
                || !owner.ResolvesErlangBif(guard) || !owner.ResolvesErlangBif(exit)
                || !entries.TryGetValue((read.Function, 1), out int encodeEntry)
                || !entries.TryGetValue((finish.Function, 2), out int finishEntry))
            {
                return false;
            }

            int missing = Label();
            int directEntry = Label();
            owner.Op(IsAtom, LabelTag, directEntry);
            owner.Operand(XRegisterTag, 0);
            EmitFrameAllocation(arity: 3, size: 2);
            owner.MoveRegister(
                XRegisterTag,
                2,
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
                2
            );
            owner.EmitConstant(position, 1);
            owner.EmitCallLine(lookup, location);
            Call(lookup.Module, lookup.Function, 3);
            owner.Op(IsTuple, LabelTag, missing);
            owner.EmitTypedRegister(0, AtomTypeBits | TupleTypeBits);
            owner.Op(TestArity, LabelTag, missing);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTuplePart(0, 1, 0);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitEncoding(read, CallLocation(lookup, location));

            Mark(missing);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 9);
            owner.Operand(BeamWire.UnsignedTag, 0);
            Tagged(detail, YRegisterTag, 1);
            Tagged(inner, XRegisterTag, 0);
            Tagged(outer, XRegisterTag, 0);
            owner.EmitCallLine(exit, location);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, exit.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, 2);

            Mark(directEntry);
            EmitFrameAllocation(arity: 3, size: 1);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                0
            );
            EmitEncoding(directRead, location);

            return true;

            void EmitEncoding(Expr.Call call, BeamSourceLocation previous)
            {
                owner.EmitCallLine(call, previous);
                owner.Op(LocalCall, BeamWire.UnsignedTag, 1);
                owner.Operand(LabelTag, encodeEntry);
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    XRegisterTag,
                    1
                );
                owner.MoveToX0(YRegisterTag, 0);
                EmitTailLocal(finishEntry, 2, 1);
            }

            void Tagged(Atom atom, int tag, int register)
            {
                owner.EmitTupleHeader(XRegisterTag, 2);
                owner.LiteralOperand(atom);
                owner.Operand(tag, register);
            }
        }
    }
}
