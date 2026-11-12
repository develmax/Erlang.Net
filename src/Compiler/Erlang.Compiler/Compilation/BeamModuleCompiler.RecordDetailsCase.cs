using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool TryEmitLocalCallTupleTail(Clause clause, BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (clause is not
            {
                Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second],
                Body: Expr.Sequence
                {
                    Items: [
                    Expr.Match
                    {
                        Pattern: Pattern.Variable bound,
                        Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [] } prepare
                    },
                    Expr.Call
                    {
                        Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Variable forwarded,
                        Expr.Tuple { Items: [Expr.Variable result, Expr.Variable kept, var constant] }
                    ]
                    } finish
                ]
                }
            } || !DistinctBindings(first, second, bound) || forwarded.Name != first.Name
            || kept.Name != second.Name || result.Name != bound.Name
            || !entries.TryGetValue((prepare.Function, 0), out int target)
            || !AttributeLiteral.TryNormalizeConstant(constant, out Term last))
        {
            return false;
        }

        Op(Allocate, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 2);
        MoveRegister(
            XRegisterTag,
            1,
            YRegisterTag,
            0
        );
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            1
        );
        EmitCallLine(prepare, location);
        EmitFieldCall(0, target);
        Op(TestHeap, BeamWire.UnsignedTag, 4);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTupleHeader(XRegisterTag, 3, 1);
        Operand(XRegisterTag, 0);
        Operand(YRegisterTag, 0);
        LiteralOperand(last);
        MoveRegister(
            YRegisterTag,
            1,
            XRegisterTag,
            0
        );
        EmitCallLine(finish, CallLocation(prepare, location));
        Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import(finish.Module, finish.Function, 2));
        Operand(BeamWire.UnsignedTag, 2);

        return true;
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRecordDetailsCase(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable id, Pattern.Variable ignored],
                    Body: Expr.Case
                    {
                        Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable key] } read,
                        Clauses: [
                        {
                            Guard: null, Patterns: [Pattern.Tuple
                            {
                                Items: [
                            Pattern.Literal { Value: Atom ok }, Pattern.Variable record, Pattern.Variable extra
                        ]
                            }], Body: Expr.Sequence
                            {
                                Items: [
                            Expr.Match
                                {
                                    Pattern: Pattern.Variable props, Value: Expr.Call
                                    {
                                        Module: not null,
                                        DynamicRemote: false, Arguments: [Expr.Literal keys, Expr.Variable item]
                                    } convert
                                },
                            Expr.Match
                                {
                                    Pattern: Pattern.Variable title, Value: Expr.Call
                                    {
                                        Module: not null,
                                        DynamicRemote: false, Arguments: [Expr.Literal format, Expr.List
                                        {
                                            Tail: null, Items: [
                                    Expr.RecordAccess { Value: Expr.Variable accessed } field, Expr.Variable displayed
                                ]
                                        }]
                                    } render
                                },
                            Expr.Tuple
                                {
                                    Items: [Expr.Literal { Value: Atom tag }, Expr.Tuple
                                    {
                                        Items: [Expr.Variable titleResult, Expr.Variable propResult, Expr.Variable extraResult]
                                    }]
                                }
                        ]
                            }
                        },
                        {
                            Guard: null, Patterns: [Pattern.Tuple
                            {
                                Items: [
                            Pattern.Literal { Value: Atom error }, Pattern.Literal { Value: Atom reason }
                        ]
                            }]
                        } missing
                    ]
                    } decision
                }] || !DistinctBindings(
                    id,
                    record,
                    extra,
                    props,
                    title
                )
                || new[] { id, record, extra, props, title }.Any(variable => variable.Name == ignored.Name)
                || key.Name != id.Name || displayed.Name != id.Name || accessed.Name != record.Name || item.Name != record.Name
                || titleResult.Name != title.Name || propResult.Name != props.Name || extraResult.Name != extra.Name
                || missing.Body is not Expr.Sequence { Items: [Expr.Match { Pattern: Pattern.Variable binding }, _] }
                || binding.Name == id.Name || binding.Name == ignored.Name
                || !TryFoldBindings(missing, out Term absent))
            {
                return false;
            }

            int success = Label();
            int notFound = Label();
            int badRecord = Label();
            int badCase = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(YRegisterTag, 1);
            owner.Operand(YRegisterTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                3
            );
            owner.EmitCallLine(read, location);
            owner.EmitStaticRemoteCall(read);
            owner.EmitTupleArityDispatch(badCase, [(2, notFound), (3, success)]);

            Mark(success);
            owner.EmitTuplePart(0, 0, 1);
            GetTupleField(
                0,
                2,
                YRegisterTag,
                2
            );
            owner.EmitAtomTest(1, ok.Name, badCase);
            GetTupleField(
                0,
                1,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                1
            );
            owner.EmitConstant(keys.Value, 0);
            var readLocation = CallLocation(read, location);
            owner.EmitCallLine(convert, readLocation);
            owner.EmitStaticRemoteCall(convert);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.Op(IsTaggedTuple, LabelTag, badRecord);
            owner.Operand(YRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, field.Arity);
            owner.Operand(AtomTag, owner.Intern(field.Name));
            owner.Op(TestHeap, BeamWire.UnsignedTag, 2 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Op(GetTupleElement, YRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, field.Index);
            owner.Operand(XRegisterTag, 0);
            owner.Op(PutList, YRegisterTag, 3);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                3
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitConstant(format.Value, 0);
            owner.EmitCallLine(render, CallLocation(convert, readLocation));
            owner.EmitStaticRemoteCall(render);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 7);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(
                0,
                (XRegisterTag, 0),
                (YRegisterTag, 1),
                (YRegisterTag, 0)
            );
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(tag.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(2);

            Mark(notFound);
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitAtomTest(1, error.Name, badCase);
            owner.EmitTuplePart(0, 1, 1);
            owner.EmitAtomTest(1, reason.Name, badCase);
            owner.EmitConstant(absent, 0);
            owner.EmitFrameReturn(4);
            Mark(badRecord);
            owner.Op(BadRecord, YRegisterTag, 1);
            Mark(badCase);
            owner.EmitLine(decision.SourceLine == 0 ? readLocation : new(decision.PreprocessorFile, decision.SourceLine));
            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;
        }

        private void GetTupleField(
            int input,
            int field,
            int tag,
            int output
        )
        {
            owner.Op(GetTupleElement, XRegisterTag, input);
            owner.Operand(BeamWire.UnsignedTag, field);
            owner.Operand(tag, output);
        }
    }
}
