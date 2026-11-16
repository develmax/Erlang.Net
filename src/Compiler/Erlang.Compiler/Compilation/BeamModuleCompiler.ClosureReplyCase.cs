using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record ClosureReplyCase(
        Expr.Case Decision,
        Expr.Call Read,
        Atom Success,
        Expr.Call Convert,
        Atom ResultTag,
        Atom Failure,
        Atom Nested,
        Atom Response,
        Term Message,
        Expr.Fun Closure,
        Clause LiftedBody,
        Atom Missing,
        Term MissingResult,
        ExpandedReply? Expanded
    );

    private sealed record ExpandedReply(
        Expr.Call Create,
        Term Options,
        Atom Property,
        Expr.Call Render,
        Expr.RecordAccess Field
    );

    private sealed record ReplySuccess(
        Atom Tag,
        Expr.Call Convert,
        Atom ResultTag,
        ExpandedReply? Expanded = null
    );

    private static ReplySuccess? ReadReplySuccess(Clause clause, Pattern.Variable id, Pattern.Variable ignored)
    {
        if (clause is not
            {
                Guard: null, Patterns: [Pattern.Tuple
                {
                    Items: [
            Pattern.Literal { Value: Atom tag }, Pattern.Variable data, Pattern.Variable extra]
                }],
                Body: Expr.Sequence sequence
            })
        {
            return null;
        }

        if (sequence.Items is [
            Expr.Match
            {
                Pattern: Pattern.Variable properties, Value: Expr.Call
                {
                    Module: not null, DynamicRemote: false,
                    Arguments: [Expr.Literal, Expr.Variable input]
                } convert
            },
            Expr.Tuple
            {
                Items: [Expr.Literal { Value: Atom result }, Expr.Tuple
                {
                    Items: [
                Expr.Variable resultId, Expr.Variable resultProps, Expr.Variable resultExtra]
                }]
            }
        ] && DistinctBindings(
            id,
            data,
            extra,
            properties
        )
            && !new[] { data, extra, properties }.Any(variable => variable.Name == ignored.Name)
            && input.Name == data.Name && resultId.Name == id.Name && resultProps.Name == properties.Name && resultExtra.Name == extra.Name)
        {
            return new(tag, convert, result);
        }

        if (sequence.Items is [
            Expr.Match
            {
                Pattern: Pattern.Variable table, Value: Expr.Call
                {
                    Module: not null, DynamicRemote: false,
                    Arguments: [Expr.Literal, var options]
                } create
            },
            Expr.Match
            {
                Pattern: Pattern.Variable initial, Value: Expr.Call
                {
                    Module: not null, DynamicRemote: false,
                    Arguments: [Expr.Literal, Expr.Variable record]
                } project
            },
            Expr.Match
            {
                Pattern: Pattern.Variable combined, Value: Expr.List
                {
                    Items: [Expr.Tuple
                    {
                        Items: [
                Expr.Literal { Value: Atom property }, Expr.Variable tableUse]
                    }], Tail: Expr.Variable tail
                }
            },
            Expr.Match
            {
                Pattern: Pattern.Variable title, Value: Expr.Call
                {
                    Module: not null, DynamicRemote: false,
                    Arguments: [Expr.Literal, Expr.List
                    {
                        Tail: null, Items: [
                    Expr.RecordAccess { Value: Expr.Variable accessed } field, Expr.Variable titleId]
                    }]
                } render
            },
            Expr.Tuple
            {
                Items: [Expr.Literal { Value: Atom returned }, Expr.Tuple
                {
                    Items: [
                Expr.Variable resultTitle, Expr.Variable resultList, Expr.Variable resultWarning]
                }]
            }
        ] && DistinctBindings(
            id,
            data,
            extra,
            table,
            initial,
            combined,
            title
        )
            && !new[] { data, extra, table, initial, combined, title }.Any(variable => variable.Name == ignored.Name)
            && record.Name == data.Name && accessed.Name == data.Name && titleId.Name == id.Name
            && tableUse.Name == table.Name && tail.Name == initial.Name && resultTitle.Name == title.Name
            && resultList.Name == combined.Name && resultWarning.Name == extra.Name
            && AttributeLiteral.TryNormalizeConstant(options, out Term optionValue))
        {
            return new(
                tag,
                project,
                returned,
                new(
                    create,
                    optionValue,
                    property,
                    render,
                    field
                )
            );
        }

        return null;
    }

    private static ClosureReplyCase? ReadClosureReplyCase(FunctionDefinition function)
    {
        if (function.Clauses is not [
            {
                Guard: null, Patterns: [Pattern.Variable id, Pattern.Variable ignored],
                Body: Expr.Case
                {
                    Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable key] } read,
                    Clauses: [var success, var remote, var missing]
                } decision
            }]
            || id.Name != key.Name || !DistinctBindings(id)
            || ignored.Name == id.Name
            || ReadReplySuccess(success, id, ignored) is not { } result
            || remote is not
            {
                Guard: null, Patterns: [Pattern.Tuple
                {
                    Items: [
                Pattern.Literal { Value: Atom error }, Pattern.Tuple
                    {
                        Items: [
                    Pattern.Literal { Value: Atom nested }, Pattern.Variable captured]
                    }]
                }],
                Body: Expr.Sequence
                {
                    Items: [
                    Expr.Match { Pattern: Pattern.Variable message, Value: var messageExpression },
                    Expr.Match
                    {
                        Pattern: Pattern.Variable callback, Value: Expr.Fun
                        {
                            SelfName: null, Clauses: [
                                {
                                    Guard: null, Patterns: [], Body: Expr.Call
                                    {
                                        Module: not null, DynamicRemote: false,
                                        Arguments: [Expr.Variable captureUse, Expr.Literal]
                                    }
                                }]
                        } closure
                    },
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom response }, Expr.Variable text, Expr.Variable fun] }]
                }
            }
            || !DistinctBindings(
                id,
                captured,
                message,
                callback
            )
            || new[] { captured, message, callback }.Any(variable => variable.Name == ignored.Name)
            || captured.Name != captureUse.Name || message.Name != text.Name || callback.Name != fun.Name
            || !AttributeLiteral.TryNormalizeConstant(messageExpression, out Term messageValue)
            || missing is not
            {
                Guard: null, Patterns: [Pattern.Tuple
                {
                    Items: [
                Pattern.Literal { Value: Atom missingTag }, Pattern.Literal { Value: Atom missingReason }]
                }],
                Body: Expr.Sequence { Items: [Expr.Match { Pattern: Pattern.Variable missingBinding }, _] }
            }
            || !error.Equals(missingTag) || missingBinding.Name == id.Name || missingBinding.Name == ignored.Name
            || !TryFoldBindings(missing, out Term missingValue))
        {
            return null;
        }

        return new(
            decision,
            read,
            result.Tag,
            result.Convert,
            result.ResultTag,
            error,
            nested,
            response,
            messageValue,
            closure,
            closure.Clauses[0] with { Patterns = [captured] },
            missingReason,
            missingValue,
            result.Expanded
        );
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitClosureReplyCase(FunctionDefinition function)
        {
            if (ReadClosureReplyCase(function) is not { } flow)
            {
                return false;
            }

            int success = Label();
            int failure = Label();
            int missing = Label();
            bool expanded = flow.Expanded is not null;
            int frame = expanded ? 4 : 2;
            int badRecord = expanded ? Label() : 0;
            int badCase = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, frame);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, frame - 1);
            for (int slot = 0; slot < frame - 1; slot++)
            {
                owner.Operand(YRegisterTag, slot);
            }
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                frame - 1
            );
            owner.EmitCallLine(flow.Read, location);
            owner.EmitStaticRemoteCall(flow.Read);
            if (expanded)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    2
                );
            }
            owner.EmitTupleArityDispatch(badCase, [(2, failure), (3, success)]);

            Mark(success);
            var readLocation = CallLocation(flow.Read, location);
            if (flow.Expanded is { } details)
            {
                EmitExpandedReply(
                    flow,
                    details,
                    readLocation,
                    badRecord,
                    badCase
                );
            }
            else
            {
                EmitSimpleReply(flow, readLocation, badCase);
            }

            int tagRegister = expanded ? 0 : 1;
            Mark(failure);
            owner.EmitTuplePart(0, 0, tagRegister);
            owner.EmitAtomTest(tagRegister, flow.Failure.Name, badCase);
            if (expanded)
            {
                EmitSavedReplyField(
                    2,
                    1,
                    XRegisterTag,
                    0
                );
            }
            else
            {
                owner.EmitTuplePart(0, 1, 1);
            }
            owner.Op(IsTuple, LabelTag, missing);
            owner.Operand(XRegisterTag, tagRegister);
            owner.Op(IsTaggedTuple, LabelTag, badCase);
            owner.EmitTypedRegister(tagRegister, TupleTypeBits);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(AtomTag, owner.Intern(flow.Nested.Name));
            owner.EmitClosureAllocation(5, tagRegister + 1, 1);
            owner.EmitTuplePart(tagRegister, 1, 0);
            owner.Op(MakeFun, BeamWire.UnsignedTag, owner.closures.Count);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.closures.Add(new(
                $"-{function.Name}/{function.Arity}-fun-0-",
                flow.LiftedBody,
                flow.Closure.ClauseLine == 0 ? location : new(flow.Closure.ClauseFile, flow.Closure.ClauseLine),
                1,
                null
            ));
            owner.EmitTupleHeader(XRegisterTag, 3);
            owner.LiteralOperand(flow.Response);
            owner.LiteralOperand(flow.Message);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame);

            Mark(missing);
            owner.EmitAtomTest(tagRegister, flow.Missing.Name, badCase);
            owner.EmitConstant(flow.MissingResult, 0);
            owner.EmitFrameReturn(frame);
            if (expanded)
            {
                Mark(badRecord);
                owner.Op(BadRecord, YRegisterTag, 1);
            }
            Mark(badCase);
            owner.EmitLine(flow.Decision.SourceLine == 0 ? readLocation : new(flow.Decision.PreprocessorFile, flow.Decision.SourceLine));
            owner.Op(CaseEnd, expanded ? YRegisterTag : XRegisterTag, expanded ? 2 : 0);

            return true;
        }

        private void EmitSimpleReply(ClosureReplyCase flow, BeamSourceLocation readLocation, int badCase)
        {
            owner.EmitTuplePart(0, 0, 1);
            GetTupleField(
                0,
                2,
                YRegisterTag,
                0
            );
            owner.EmitAtomTest(1, flow.Success.Name, badCase);
            owner.EmitTuplePart(0, 1, 1);
            owner.EmitConstant(((Expr.Literal)flow.Convert.Arguments[0]).Value, 0);
            owner.EmitCallLine(flow.Convert, readLocation);
            owner.EmitStaticRemoteCall(flow.Convert);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 7);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(
                0,
                (YRegisterTag, 1),
                (XRegisterTag, 0),
                (YRegisterTag, 0)
            );
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(flow.ResultTag.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(2);
        }

        private void EmitExpandedReply(
            ClosureReplyCase flow,
            ExpandedReply details,
            BeamSourceLocation readLocation,
            int badRecord,
            int badCase
        )
        {
            owner.EmitTuplePart(0, 0, 0);
            owner.EmitAtomTest(0, flow.Success.Name, badCase);
            owner.EmitConstant(details.Options, 1);
            owner.EmitConstant(details.Create.Arguments[0], 0);
            owner.EmitCallLine(details.Create, readLocation);
            owner.EmitStaticRemoteCall(details.Create);
            EmitSavedReplyField(
                2,
                1,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            Load(1, 1);
            owner.EmitConstant(flow.Convert.Arguments[0], 0);
            var createLocation = CallLocation(details.Create, readLocation);
            owner.EmitCallLine(flow.Convert, createLocation);
            owner.EmitStaticRemoteCall(flow.Convert);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 5);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 2, 1);
            owner.LiteralOperand(details.Property);
            owner.Operand(YRegisterTag, 0);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, 0);
            owner.Op(IsTaggedTuple, LabelTag, badRecord);
            owner.Operand(YRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, details.Field.Arity);
            owner.Operand(AtomTag, owner.Intern(details.Field.Name));
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 0);
            EmitSavedReplyField(
                1,
                details.Field.Index,
                XRegisterTag,
                0
            );
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
            owner.EmitConstant(details.Render.Arguments[0], 0);
            owner.EmitCallLine(details.Render, CallLocation(flow.Convert, createLocation));
            owner.EmitStaticRemoteCall(details.Render);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 7);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitSavedReplyField(
                0,
                2,
                XRegisterTag,
                1
            );
            owner.EmitTagMatchTuple(
                0,
                (XRegisterTag, 0),
                (YRegisterTag, 1),
                (XRegisterTag, 1)
            );
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(flow.ResultTag.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(2);
        }

        private void EmitSavedReplyField(
            int slot,
            int field,
            int destinationTag,
            int destination
        )
        {
            owner.Op(GetTupleElement, YRegisterTag, slot);
            owner.Operand(BeamWire.UnsignedTag, field);
            owner.Operand(destinationTag, destination);
        }
    }
}
