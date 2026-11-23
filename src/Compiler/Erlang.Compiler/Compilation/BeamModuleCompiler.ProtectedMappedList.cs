using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProtectedMappedList(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail }, Pattern.Variable capture],
                    Body: Expr.List
                    {
                        Items: [var transform],
                        Tail: Expr.Call
                        {
                            Module: null, DynamicRemote: false, Arguments: [Expr.Variable rest, Expr.Variable forwarded]
                        } recur
                    }
                },
                {
                    Guard: null, Patterns: [Pattern.List { Items: [], Tail: null }, Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: Expr.Literal { Value: Nil }
                },
                {
                    Guard: null, Patterns: [Pattern.Variable invalid, Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: Expr.Call
                    {
                        Function: AutoImportNames.Error, DynamicRemote: false,
                        Arguments:
                                [
                                    Expr.Tuple { Items: [Expr.Literal { Value: Atom { Name: ErlangErrorReasons.BadGenerator } }, Expr.Variable rejected] }
                                ]
                    } badGenerator
                }
                ]
                || recur.Function != function.Name || rest.Name != tail.Name || forwarded.Name != capture.Name
                || invalid.Name != rejected.Name || invalid.Name == VariableScopeNames.Wildcard
                || !owner.ResolvesErlangBif(badGenerator)
                || !entries.TryGetValue((function.Name, 2), out int entry))
            {
                return false;
            }

            Expr body = transform is Expr.Block block ? block.Body : transform;
            if (body is not Expr.Sequence
                {
                    Items:
                        [
                            Expr.Match { Pattern: Pattern.Variable parsed, Value: var conversion },
                            Expr.Try guarded
                        ]
                }
                || !DistinctBindings(
                    head,
                    tail,
                    capture,
                    parsed
                )
                || ReadSequenceCall(conversion, head.Name) is not { } read
                || ReadTableDecodeTry(guarded, parsed.Name, capture.Name) is not { } caught)
            {
                return false;
            }

            int handler = Label();
            int reported = Label();
            int reraised = Label();
            int noItems = Label();
            int malformed = Label();
            owner.Op(IsNonemptyList, LabelTag, noItems);
            owner.Operand(XRegisterTag, 0);
            EmitFrameAllocation(2, 4);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(YRegisterTag, 3);
            Move(
                XRegisterTag,
                1,
                YRegisterTag,
                2
            );
            owner.Op(GetList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, 1);
            owner.EmitConstant(read.Options, 1);
            BeamSourceLocation previous = location;
            owner.EmitCallLine(read.Call, previous);
            previous = CallLocation(read.Call, previous);
            owner.Op(LocalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, read.Entry);
            owner.Op(TryEnter, YRegisterTag, 3);
            owner.Operand(LabelTag, handler);
            owner.EmitConstant(caught.Options, 2);
            Move(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            Move(
                YRegisterTag,
                2,
                XRegisterTag,
                3
            );
            owner.EmitConstant(caught.Field, 0);
            owner.EmitArithmeticLine(caught.Apply.SourceLine, ref previous);
            owner.Op(CallFun, BeamWire.UnsignedTag, 3);
            Move(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.Op(TryEnd, YRegisterTag, 3);
            Move(
                YRegisterTag,
                2,
                XRegisterTag,
                1
            );
            owner.MoveToX0(YRegisterTag, 1);
            Move(
                YRegisterTag,
                0,
                YRegisterTag,
                3
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitLine(location);
            owner.Op(LocalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);

            Mark(handler);
            owner.Op(TryCase, YRegisterTag, 3);
            owner.instructions.WriteByte(SelectValue);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.Operand(LabelTag, reraised);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.Operand(AtomTag, owner.Intern(ErlangExceptionClasses.Error));
            owner.Operand(LabelTag, reported);
            owner.Operand(AtomTag, owner.Intern(ErlangExceptionClasses.Exit));
            owner.Operand(LabelTag, reported);

            Mark(reported);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(caught.ErrorTag);
            owner.Operand(XRegisterTag, 1);
            owner.EmitCallLine(caught.Exit, location);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, caught.Exit.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, 4);

            Mark(reraised);
            owner.Op(Raise, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);

            Mark(noItems);
            owner.EmitNilTest(XRegisterTag, 0, malformed);
            owner.instructions.WriteByte(Return);
            Mark(malformed);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(Term.A(ErlangErrorReasons.BadGenerator));
            owner.Operand(XRegisterTag, 0);
            owner.EmitLine(location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, badGenerator.Function, 1));

            return true;

            void Move(
                int fromTag,
                int from,
                int toTag,
                int to
            ) => owner.MoveRegister(
                fromTag,
                from,
                toTag,
                to
            );
        }
    }
}
