using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitConfigurationDecode(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable file],
                Body: Expr.Case {
                    Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable input] } read,
                    Clauses: [
                        { Guard: null, Patterns: [Pattern.Tuple { Items: [
                            Pattern.Literal { Value: Atom ok }, Pattern.Variable binary
                        ] }], Body: Expr.Sequence { Items: [
                            Expr.Match { Pattern: Pattern.Variable encoding, Value: Expr.Case {
                                Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable encodingInput] } detect,
                                Clauses: [
                                    { Guard: null, Patterns: [Pattern.Literal { Value: Atom none }], Body: Expr.Literal { Value: Atom defaultEncoding } },
                                    { Guard: null, Patterns: [Pattern.Variable explicitEncoding], Body: Expr.Variable explicitResult }
                                ]
                            } },
                            Expr.Case {
                                Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                                    Expr.Variable decodeInput, Expr.Variable decodeEncoding
                                ] } decode,
                                Clauses: [var firstError, var secondError, {
                                    Guard: Expr.Call { Module: null or ErlangModuleNames.Module, Function: AutoImportNames.IsList,
                                        DynamicRemote: false, Arguments: [Expr.Variable checkedList] } listGuard,
                                    Patterns: [Pattern.Variable text], Body: Expr.Call {
                                        Module: null, DynamicRemote: false, Arguments: [Expr.Variable parseInput]
                                    } parse
                                }]
                            } conversion
                        ] } },
                        { Guard: null, Patterns: [Pattern.Literal { Value: Atom readError }],
                            Body: Expr.Call { Arguments: [Expr.Tuple { Items: [
                                Expr.Literal { Value: Atom context }, Expr.Variable failedFile
                            ] }] } failRead }
                    ]
                } decision
            }] || !DistinctBindings(file, binary, encoding, text) || !DistinctBindings(file, binary, explicitEncoding)
                || input.Name != file.Name || encodingInput.Name != binary.Name || decodeInput.Name != binary.Name
                || decodeEncoding.Name != encoding.Name || explicitEncoding.Name != explicitResult.Name
                || checkedList.Name != text.Name || parseInput.Name != text.Name || failedFile.Name != file.Name
                || !owner.ResolvesErlangBif(listGuard) || !IsConfigurationError(failRead)
                || !entries.TryGetValue((parse.Function, 1), out int parseEntry)
                || ReadDecodeFailure(firstError, context, file, binary, encoding) is not { } first
                || ReadDecodeFailure(secondError, context, file, binary, encoding) is not { } second
                || first.Tag.Equals(second.Tag))
            {
                return false;
            }

            int explicitChoice = Label();
            int haveEncoding = Label();
            int secondFailure = Label();
            int firstFailure = Label();
            int list = Label();
            int readFailed = Label();
            int invalidDecode = Label();
            int invalidRead = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            SaveWindowResult(0);
            var readLocation = location;
            EmitWindowCall(read, ref readLocation);
            owner.Op(IsTuple, LabelTag, readFailed);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsTaggedTuple, LabelTag, invalidRead);
            owner.EmitTypedRegister(0, TupleTypeBits);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(ok);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 0);
            Load(0);
            var decodeLocation = readLocation;
            EmitWindowCall(detect, ref decodeLocation);
            EmitConfigurationAtomTest(none, explicitChoice);
            owner.EmitConstant(defaultEncoding, 1);
            JumpTo(haveEncoding);
            Mark(explicitChoice);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            Mark(haveEncoding);
            Load(0);
            owner.InitStackSlot(0);
            EmitWindowCall(decode, ref decodeLocation);
            owner.EmitTupleCheck(0, 3, list);
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitTuplePart(0, 2, 2);
            if (TermOrder.Compare(first.Tag, second.Tag, exact: true) < 0)
            {
                owner.EmitAtomSelection(1, list, first.Tag.Name, firstFailure, second.Tag.Name, secondFailure);
            }
            else
            {
                owner.EmitAtomSelection(1, list, second.Tag.Name, secondFailure, first.Tag.Name, firstFailure);
            }

            Mark(secondFailure);
            EmitNestedConfigurationError(second.Call, context, second.Reason, 2, 3, 1, decodeLocation);
            Mark(firstFailure);
            EmitNestedConfigurationError(first.Call, context, first.Reason, 2, 3, 1, decodeLocation);
            Mark(list);
            owner.Op(IsList, LabelTag, invalidDecode);
            owner.Operand(XRegisterTag, 0);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, parseEntry);
            owner.Operand(BeamWire.UnsignedTag, 1);
            Mark(readFailed);
            EmitConfigurationAtomTest(readError, invalidRead);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(context.Name)), (YRegisterTag, 0));
            EmitConfigurationTail(failRead with { Module = ErlangModuleNames.Module }, 1, readLocation);
            Mark(invalidDecode);
            EmitConfigurationCaseFailure(conversion, decodeLocation);
            Mark(invalidRead);
            EmitConfigurationCaseFailure(decision, readLocation);

            return true;
        }

        private (Atom Tag, Atom Reason, Expr.Call Call)? ReadDecodeFailure(
            Clause clause,
            Atom context,
            Pattern.Variable file,
            Pattern.Variable binary,
            Pattern.Variable encoding
        )
        {
            if (clause is not {
                Guard: null, Patterns: [Pattern.Tuple { Items: [
                    Pattern.Literal { Value: Atom tag }, Pattern.Variable ignored, Pattern.Variable rest
                ] }], Body: Expr.Call { Arguments: [Expr.Tuple { Items: [
                    Expr.Literal { Value: Atom errorContext },
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom reason }, Expr.Variable returned] }
                ] }] } error
            } || !context.Equals(errorContext) || !DistinctBindings(file, binary, encoding, rest)
                || (ignored.Name != VariableScopeNames.Wildcard && !DistinctBindings(file, binary, encoding, rest, ignored))
                || returned.Name != rest.Name || !IsConfigurationError(error))
            {
                return null;
            }

            return (tag, reason, error);
        }
    }
}
