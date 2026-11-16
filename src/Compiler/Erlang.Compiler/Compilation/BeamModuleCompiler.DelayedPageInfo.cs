using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitDelayedPageInfo(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Patterns: [Pattern.Literal { Value: Atom active }, AliasPattern
                    {
                        Left: Pattern.Tuple
                        {
                            Items: [Pattern.Literal { Value: Atom stateTag }, Pattern.Variable parent, Pattern.Variable panel,
                        Pattern.Variable { Name: "_" }, Pattern.Variable { Name: "_" }, Pattern.Variable { Name: "_" },
                        Pattern.Variable callback, Pattern.Variable { Name: "_" }]
                        }, Right: Pattern.Variable state
                    }],
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.ExactNotEqual, Left: Expr.Variable checkedCallback,
                        Right: Expr.Literal { Value: Atom absent }
                    },
                    Body: Expr.Sequence
                    {
                        Items: [Expr.Call progress,
                    Expr.Match
                        {
                            Pattern: Pattern.Tuple
                            {
                                Items: [Pattern.Tuple
                                {
                                    Items: [
                        Pattern.Literal { Value: Atom expand }, Pattern.Variable title, Pattern.Variable info, Pattern.Variable table
                    ]
                                }, Pattern.Variable warning]
                            }, Value: Expr.Call
                            {
                                DynamicRemote: true,
                                Arguments: [Expr.Variable target, Expr.Literal { Value: Atom method }, Expr.List { Items: [], Tail: null }]
                            } fetch
                        } match,
                    Expr.Match { Pattern: Pattern.Variable colors, Value: Expr.Call getColors },
                    Expr.Match { Pattern: Pattern.Variable html, Value: Expr.Call render },
                    Expr.Call close, Expr.Call busy, Expr.Call setPage, Expr.Call setStatus, Expr.Call idle,
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom reply }, Expr.RecordUpdate update] }]
                    }
                },
                {
                    Guard: null, Patterns: [Pattern.Literal { Value: Atom otherActive }, Pattern.Variable unchanged],
                    Body: Expr.Sequence
                    {
                        Items: [Expr.Call
                        {
                            Module: null, DynamicRemote: false, Arguments: [
                    Expr.Variable statusState, Expr.RecordAccess { Value: Expr.Variable record, Index: 7, Arity: 8 } access
                  ]
                        } status, Expr.Tuple { Items: [Expr.Literal { Value: Atom otherReply }, Expr.Variable returned] }]
                    }
                },
                {
                    Guard: null, Patterns: [Pattern.Variable message, Pattern.Variable fallback],
                    Body: Expr.Sequence
                    {
                        Items: [Expr.Call
                        {
                            Module: not null, DynamicRemote: false, Arguments: [
                    Expr.Literal { Value: var format }, Expr.List
                            {
                                Tail: null, Items: [
                        Expr.Literal { Value: Atom module }, Expr.Literal { Value: Integer line }, Expr.Variable logged
                    ]
                            }
                  ]
                        } log, Expr.Tuple { Items: [Expr.Literal { Value: Atom fallbackReply }, Expr.Variable fallbackResult] }]
                    }
                }
            ] || !DistinctBindings(
                parent,
                panel,
                callback,
                state,
                title,
                info,
                table,
                warning,
                colors,
                html
            )
                || !DistinctBindings(message, fallback) || unchanged.Name == "_"
                || callback.Name != checkedCallback.Name || callback.Name != target.Name
                || !active.Equals(otherActive) || !reply.Equals(otherReply) || !reply.Equals(fallbackReply)
                || statusState.Name != unchanged.Name || record.Name != unchanged.Name || returned.Name != unchanged.Name
                || logged.Name != message.Name || fallbackResult.Name != fallback.Name || access.Name != stateTag.Name
                || progress.Arguments is not [Expr.Variable progressPanel, Expr.Literal { Value: var heading }, Expr.Literal { Value: var description }]
                || !IsStaticWindowCall(progress) || progressPanel.Name != panel.Name
                || !IsWindowCall(getColors, parent) || !IsWindowCall(
                    render,
                    title,
                    info,
                    table,
                    colors
                )
                || !IsWindowCall(close) || !IsWindowCall(busy) || !IsWindowCall(setPage, panel, html) || !IsWindowCall(idle)
                || setStatus.Module is not null || setStatus.DynamicRemote || setStatus.Function != status.Function
                || !MatchesDetailArguments(setStatus, state, warning) || !entries.TryGetValue((status.Function, 2), out int statusEntry)
                || update.Name != stateTag.Name || update.Arity != 8 || update.Value is not Expr.Variable original || original.Name != state.Name
                || update.Fields.Count != 3
                || update.Fields.SingleOrDefault(field => field.Index == 4)?.Value is not Expr.Variable nextTable || nextTable.Name != table.Name
                || update.Fields.SingleOrDefault(field => field.Index == 6)?.Value is not Expr.Literal { Value: Atom cleared } || !cleared.Equals(absent)
                || update.Fields.SingleOrDefault(field => field.Index == 7)?.Value is not Expr.Variable nextWarning || nextWarning.Name != warning.Name)
            {
                return false;
            }

            int delayed = Label();
            int other = Label();
            int badMatch = Label();
            int badRecord = Label();
            owner.EmitAtomTest(0, active.Name, other);
            EmitDetailRecordTest(
                1,
                8,
                stateTag,
                badRecord
            );
            owner.EmitTuplePart(1, 6, 0);
            owner.EmitAtomTest(0, absent.Name, delayed);
            Frame(1);
            Save(1, 0);
            owner.EmitTuplePart(1, 7, 1);
            Load(0);
            var previous = location;
            EmitWindowLocal(status, statusEntry, ref previous);
            ReplyFromStack();

            Mark(delayed);
            Frame(5);
            ClearDetailSlots(0, 1);
            Save(0, 3);
            Save(1, 4);
            GetTupleField(
                1,
                2,
                YRegisterTag,
                2
            );
            owner.EmitConstant(heading, 1);
            owner.EmitConstant(description, 2);
            Load(2);
            EmitWindowCall(progress, ref previous);
            owner.EmitConstant(method, 1);
            Load(3);
            ClearDetailSlots(3);
            owner.EmitCallLine(fetch, previous);
            var fetchLine = CallLocation(fetch, previous);
            previous = fetchLine;
            owner.Op(DynamicApply, BeamWire.UnsignedTag, 0);
            owner.EmitTupleCheck(0, 2, badMatch);
            GetTupleField(
                0,
                0,
                YRegisterTag,
                3
            );
            GetTupleField(
                0,
                1,
                YRegisterTag,
                1
            );
            owner.Op(IsTaggedTuple, LabelTag, badMatch);
            owner.Operand(YRegisterTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.LiteralOperand(expand);
            Field(
                4,
                1,
                XRegisterTag,
                0
            );
            EmitWindowCall(getColors, ref previous);
            Field(
                3,
                1,
                XRegisterTag,
                1
            );
            Field(
                3,
                2,
                XRegisterTag,
                2
            );
            Field(
                3,
                3,
                YRegisterTag,
                3
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                3
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                1
            );
            Load(3, 2);
            EmitWindowCall(render, ref previous);
            Save(0, 0);
            EmitWindowCall(close, ref previous);
            EmitWindowCall(busy, ref previous);
            Load(0, 1);
            Load(2);
            owner.MoveRegister(
                YRegisterTag,
                1,
                YRegisterTag,
                2
            );
            TrimDetailFrame(2, 3);
            EmitWindowCall(setPage, ref previous);
            Load(0, 1);
            Load(2);
            EmitWindowLocal(setStatus, statusEntry, ref previous);
            EmitWindowCall(idle, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 12);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Op(UpdateRecord, AtomTag, owner.Intern("copy"));
            owner.Operand(BeamWire.UnsignedTag, 8);
            owner.EmitTypedRegister(2, TupleTypeBits, registerTag: YRegisterTag);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 6);
            owner.Operand(BeamWire.UnsignedTag, 5);
            owner.Operand(YRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 7);
            owner.LiteralOperand(cleared);
            owner.Operand(BeamWire.UnsignedTag, 8);
            owner.Operand(YRegisterTag, 0);
            Reply(XRegisterTag, 3);

            Mark(other);
            owner.EmitLoggedReply(log, format, module, line, reply, previous, stateRegister: 1);
            previous = CallLocation(log, previous);

            Mark(badMatch);
            owner.EmitLine(match.SourceLine == 0 ? fetchLine : new(match.PreprocessorFile, match.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(badRecord);
            owner.EmitCallLine(status, previous);
            owner.Op(BadRecord, XRegisterTag, 1);

            return true;

            void Frame(int size)
            {
                owner.Op(Allocate, BeamWire.UnsignedTag, size);
                owner.Operand(BeamWire.UnsignedTag, 2);
            }

            void Save(int register, int slot) => owner.MoveRegister(
                XRegisterTag,
                register,
                YRegisterTag,
                slot
            );

            void Field(
                int source,
                int index,
                int destinationTag,
                int destination
            )
            {
                owner.Op(GetTupleElement, YRegisterTag, source);
                owner.Operand(BeamWire.UnsignedTag, index);
                owner.Operand(destinationTag, destination);
            }

            void Reply(int tag, int frame)
            {
                owner.EmitTupleHeader(XRegisterTag, 2);
                owner.LiteralOperand(reply);
                owner.Operand(tag, 0);
                owner.EmitFrameReturn(frame);
            }

            void ReplyFromStack()
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, 0);
                Reply(YRegisterTag, 1);
            }
        }

    }

    private bool TryEmitLoggedCallReply(Clause clause, BeamSourceLocation location)
    {
        if (clause is not
            {
                Guard: null, Patterns: [Pattern.Variable message, Pattern.Variable ignored, Pattern.Variable state],
                Body: Expr.Sequence { Items: [
                    Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Literal { Value: var format }, Expr.List { Tail: null, Items: [
                            Expr.Literal { Value: Atom module }, Expr.Literal { Value: Integer line }, Expr.Variable logged
                        ] }
                    ] } log,
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom reply }, Expr.Literal { Value: Atom detail }, Expr.Variable returned] }
                ] }
            }
            || !HasUnconditionalParameters(clause) || !DistinctBindings(message, state)
            || logged.Name != message.Name || returned.Name != state.Name)
        {
            return false;
        }

        EmitLoggedReply(log, format, module, line, reply, location, stateRegister: 2, detail);
        return true;
    }

    private void EmitLoggedReply(Expr.Call log, Term format, Atom module, Integer line, Atom reply, BeamSourceLocation previous, int stateRegister, Atom? detail = null)
    {
        Op(AllocateHeap, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 3 * ConsHeapWords);
        Operand(BeamWire.UnsignedTag, stateRegister + 1);
        MoveRegister(XRegisterTag, stateRegister, YRegisterTag, 0);
        Op(PutList, XRegisterTag, 0);
        Operand(AtomTag, NilAtomIndex);
        Operand(XRegisterTag, 0);
        instructions.WriteByte(PutList);
        LiteralOperand(line);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        instructions.WriteByte(PutList);
        LiteralOperand(module);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 1);
        EmitConstant(format, 0);
        EmitCallLine(log, previous);
        EmitStaticRemoteCall(log);
        int tupleArity = detail is null ? 2 : 3;
        Op(TestHeap, BeamWire.UnsignedTag, tupleArity + 1);
        Operand(BeamWire.UnsignedTag, 0);
        EmitTupleHeader(XRegisterTag, tupleArity);
        LiteralOperand(reply);
        if (detail is not null)
        {
            LiteralOperand(detail);
        }

        Operand(YRegisterTag, 0);
        EmitFrameReturn(1);
    }
}
