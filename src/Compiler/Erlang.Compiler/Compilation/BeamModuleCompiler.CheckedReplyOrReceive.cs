using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCheckedReplyOrReceive(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Variable ps, Pattern.Variable pa, Pattern.Variable pp,
                            Pattern.Literal { Value: Atom inactive }, Pattern.Variable pt],
                    Body: Expr.Case
                    {
                        Value: Expr.Call { Arguments: [Expr.Variable socket, Expr.Call passiveTimer] } read,
                        Clauses:
                            [
                            {
                                Guard: null,
                                Patterns: [Pattern.Tuple
                                {
                                    Items: [Pattern.Literal { Value: Atom envelope },
                                        Pattern.Tuple
                                    {
                                        Items: [Pattern.Variable address, Pattern.Variable port,
                                            Pattern.Variable { Name: VariableScopeNames.Wildcard }, AliasPattern passiveRecord]
                                    }]
                                }],
                                Body: Expr.If { Clauses: [var accepted, var rejected] }
                            },
                            {
                                Guard: null,
                                Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom sameEnvelope }, Pattern.Variable other] }],
                                Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom alternativeTag }, Expr.Variable carried] }
                            },
                            {
                                Guard: null, Patterns: [AliasPattern errorRecord], Body: Expr.Variable unchanged
                            }
                            ]
                    } reply
                },
                {
                    Guard: null,
                    Patterns: [Pattern.Variable rs, Pattern.Variable ra, Pattern.Variable rp, Pattern.Variable active, Pattern.Variable rt],
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Match { Pattern: Pattern.Variable duration, Value: Expr.Call receiveTimer },
                                Expr.Receive
                            {
                                Timeout: Expr.Variable timeout,
                                After: Expr timedOut,
                                Clauses:
                                    [
                                    {
                                        Guard: null,
                                        Patterns: [Pattern.Tuple
                                        {
                                            Items: [Pattern.Literal { Value: Atom messageTag }, Pattern.Variable recipient,
                                                    Pattern.Variable from, Pattern.Variable service, Pattern.Tuple
                                            {
                                                Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, AliasPattern receivedRecord]
                                            }]
                                        }],
                                        Body: Expr.Sequence
                                        {
                                            Items:
                                                [
                                                    Expr.Match
                                                {
                                                    Pattern: Pattern.Variable outcome,
                                                    Value: Expr.Case
                                                    {
                                                        Value: Expr.Variable selected,
                                                        Clauses:
                                                            [
                                                            {
                                                                Guard: null,
                                                                Patterns: [Pattern.Literal { Value: Atom option }],
                                                                Body: Expr.Call
                                                                {
                                                                    Arguments: [Expr.Variable ownerArgument, Expr.Literal key, Expr.Literal setting]
                                                                } set
                                                            },
                                                            {
                                                                Guard: null,
                                                                Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                                                Body: Expr.Literal { Value: Atom idleResult }
                                                            }
                                                            ]
                                                    }
                                                },
                                                    Expr.Case
                                                {
                                                    Value: Expr.Tuple { Items: [Expr.Variable eventState, Expr.Variable resultState] },
                                                    Clauses:
                                                        [
                                                        {
                                                            Guard: null,
                                                            Patterns: [Pattern.Tuple
                                                            {
                                                                Items: [Pattern.Literal { Value: Atom acceptedState },
                                                                        Pattern.Literal { Value: Atom acceptedResult }]
                                                            }],
                                                            Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom success }, Expr.Variable delivered] }
                                                        },
                                                        {
                                                            Guard: null,
                                                            Patterns: [Pattern.Tuple
                                                            {
                                                                Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                                                        Pattern.Literal { Value: Atom rejectedResult }]
                                                            }],
                                                            Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom failure }, Expr.Variable returned] }
                                                        },
                                                        {
                                                            Guard: null,
                                                            Patterns: [Pattern.Tuple
                                                            {
                                                                Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable error]
                                                            }],
                                                            Body: Expr.Variable errorValue
                                                        }
                                                        ]
                                                }
                                                ]
                                        }
                                    }
                                    ]
                            }
                            ]
                    }
                }
                ]
                || !DistinctBindings(
                    ps,
                    pa,
                    pp,
                    pt,
                    other
                ) || !envelope.Equals(sameEnvelope)
                || socket.Name != ps.Name || address.Name != pa.Name || port.Name != pp.Name || carried.Name != other.Name
                || !ReadsTimer(passiveTimer, pt) || !ReadsTimer(receiveTimer, rt)
                || !DistinctBindings(
                    rs,
                    ra,
                    rp,
                    active,
                    rt,
                    duration,
                    outcome,
                    error
                )
                || recipient.Name != rs.Name || from.Name != ra.Name || service.Name != rp.Name
                || selected.Name != active.Name || ownerArgument.Name != rs.Name || timeout.Name != duration.Name
                || resultState.Name != outcome.Name || errorValue.Name != error.Name
                || !idleResult.Equals(acceptedResult) || !idleResult.Equals(rejectedResult) || !alternativeTag.Equals(failure)
                || !AttributeLiteral.TryNormalizeConstant(timedOut, out Term timeoutResult)
                || !TryResolveOrdinaryCallTarget(read, out string? readModule)
                || !TryResolveOrdinaryCallTarget(passiveTimer, out string? passiveModule)
                || !TryResolveOrdinaryCallTarget(receiveTimer, out string? receiveModule)
                || !TryResolveOrdinaryCallTarget(set, out string? setModule)
                || !TryReadPatternAlias(errorRecord, out var errorBinding, out var errorPattern)
                || errorBinding.Name != unchanged.Name || errorPattern is not Pattern.Tuple
                { Items: [Pattern.Literal { Value: Atom errorTag }, Pattern.Variable { Name: VariableScopeNames.Wildcard }] }
                || errorTag.Equals(envelope)
                || ReadRecord(passiveRecord) is not { } passive
                || ReadRecord(receivedRecord) is not { } received
                || !passive.Tag.Equals(received.Tag) || passive.Width != received.Width || passive.Field != received.Field
                || !DistinctBindings(
                    ps,
                    pa,
                    pp,
                    pt,
                    other,
                    errorBinding,
                    passive.Alias,
                    passive.Status
                )
                || !DistinctBindings(
                    rs,
                    ra,
                    rp,
                    active,
                    rt,
                    duration,
                    outcome,
                    error,
                    received.Alias,
                    received.Status
                )
                || eventState.Name != received.Status.Name || delivered.Name != received.Alias.Name || returned.Name != received.Alias.Name
                || accepted is not
                {
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.ExactEqual,
                        Left: Expr.Variable status, Right: Expr.Literal { Value: Atom expected }
                    },
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom acceptedTag }, Expr.Variable passed] }
                }
                || status.Name != passive.Status.Name || !expected.Equals(acceptedState)
                || !acceptedTag.Equals(success) || passed.Name != passive.Alias.Name
                || rejected is not
                {
                    Guard: Expr.Literal { Value: Atom { Name: "true" } },
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom rejectedTag }, Expr.Variable rejectedEvent] }
                }
                || !rejectedTag.Equals(failure) || rejectedEvent.Name != passive.Alias.Name)
            {
                return false;
            }

            const int passiveFrame = 3;
            const int receiveFrame = 5;
            int receiveEntry = Label();
            int passiveMatch = Label();
            int passiveRejected = Label();
            int unmatchedEvent = Label();
            int passiveError = Label();
            int invalidReply = Label();
            int scan = Label();
            int skip = Label();
            int wait = Label();
            int noOptionChange = Label();
            int receiveResult = Label();
            int wrongState = Label();
            int optionError = Label();

            owner.EmitAtomTest(3, inactive.Name, receiveEntry);
            EmitFrameAllocation(5, passiveFrame);
            SaveInput(2, 0);
            SaveInput(1, 1);
            SaveInput(0, 2);
            CallTimer(passiveTimer, passiveModule);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            Load(2, 0);
            owner.EmitSlotKills([2]);
            owner.EmitCallLine(read, CallLocation(passiveTimer, location));
            Call(readModule, read.Function, 2);
            owner.EmitTupleCheck(0, 2, invalidReply);
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitMembershipSelect(
                1,
                invalidReply,
                new[] { ((Term)envelope, passiveMatch), ((Term)errorTag, passiveError) }
                .OrderBy(entry => entry.Item1, Comparer<Term>.Create((a, b) => TermOrder.Compare(a, b, exact: true)))
            );

            Mark(passiveMatch);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitTupleCheck(0, 4, unmatchedEvent);
            owner.EmitTuplePart(0, 3, 1);
            owner.EmitRecordTest(
                unmatchedEvent,
                XRegisterTag,
                1,
                passive.Tag,
                passive.Width
            );
            owner.EmitTuplePart(0, 0, 2);
            EqualSaved(2, 1, unmatchedEvent);
            owner.EmitTuplePart(0, 1, 2);
            EqualSaved(2, 0, unmatchedEvent);
            owner.EmitTuplePart(1, passive.Field, 0);
            owner.EmitAtomTest(0, expected.Name, passiveRejected);
            EmitStateResult(
                success,
                XRegisterTag,
                1,
                2
            );
            owner.EmitFrameReturn(passiveFrame);
            Mark(passiveRejected);
            EmitStateResult(
                failure,
                XRegisterTag,
                1,
                2
            );
            owner.EmitFrameReturn(passiveFrame);
            Mark(unmatchedEvent);
            EmitStateResult(
                failure,
                XRegisterTag,
                0,
                1
            );
            owner.EmitFrameReturn(passiveFrame);
            Mark(passiveError);
            owner.EmitFrameReturn(passiveFrame);

            Mark(receiveEntry);
            EmitFrameAllocation(5, receiveFrame);
            EmitFrameInitialization(0, 1);
            SaveInput(3, 1);
            SaveInput(2, 2);
            SaveInput(1, 3);
            SaveInput(0, 4);
            CallTimer(receiveTimer, receiveModule);
            SaveInput(0, 0);
            Mark(scan);
            owner.Op(LoopReceive, LabelTag, wait);
            owner.Operand(XRegisterTag, 0);
            owner.EmitRecordTest(
                skip,
                XRegisterTag,
                0,
                messageTag,
                5
            );
            owner.EmitTuplePart(0, 1, 1);
            owner.EmitTuplePart(0, 2, 2);
            owner.EmitTuplePart(0, 3, 3);
            owner.EmitTuplePart(0, 4, 0);
            owner.EmitTupleCheck(0, 2, skip);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitRecordTest(
                skip,
                XRegisterTag,
                0,
                received.Tag,
                received.Width
            );
            EqualSaved(1, 4, skip);
            EqualSaved(2, 3, skip);
            EqualSaved(3, 2, skip);
            SaveInput(0, 3);
            owner.instructions.WriteByte(RemoveMessage);
            owner.Op(ExactEqualTest, LabelTag, noOptionChange);
            owner.Operand(YRegisterTag, 1);
            owner.LiteralOperand(option);
            owner.EmitConstant(key.Value, 1);
            owner.EmitConstant(setting.Value, 2);
            Load(4, 0);
            owner.EmitSlotKills([0, 1, 2, 4]);
            owner.EmitCallLine(set, CallLocation(receiveTimer, location));
            Call(setModule, set.Function, 3);
            JumpTo(receiveResult);
            Mark(noOptionChange);
            owner.EmitConstant(idleResult, 0);
            Mark(receiveResult);
            owner.Op(GetTupleElement, YRegisterTag, 3);
            owner.Operand(BeamWire.UnsignedTag, received.Field);
            owner.Operand(XRegisterTag, 1);
            owner.EmitAtomTest(1, expected.Name, wrongState);
            owner.EmitAtomTest(0, idleResult.Name, optionError);
            EmitStateResult(
                success,
                YRegisterTag,
                3,
                0
            );
            owner.EmitFrameReturn(receiveFrame);
            Mark(wrongState);
            owner.EmitAtomTest(0, idleResult.Name, optionError);
            EmitStateResult(
                failure,
                YRegisterTag,
                3,
                0
            );
            owner.EmitFrameReturn(receiveFrame);
            Mark(optionError);
            owner.EmitFrameReturn(receiveFrame);

            Mark(skip);
            owner.Op(LoopReceiveEnd, LabelTag, scan);
            Mark(wait);
            owner.Op(WaitTimeout, LabelTag, scan);
            owner.Operand(YRegisterTag, 0);
            owner.instructions.WriteByte(ReceiveTimeout);
            owner.EmitConstant(timeoutResult, 0);
            owner.EmitFrameReturn(receiveFrame);
            Mark(invalidReply);
            EmitCaseEnd(CaseSourceLocation(reply));

            return true;

            static bool ReadsTimer(Expr.Call timer, Pattern.Variable input) =>
                timer.Arguments is [Expr.Variable argument] && argument.Name == input.Name;

            static (Atom Tag, int Width, int Field, Pattern.Variable Alias, Pattern.Variable Status)? ReadRecord(AliasPattern pattern)
            {
                if (!TryReadPatternAlias(pattern, out var alias, out var shape)
                    || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } tuple
                    || tuple.Items.Skip(1).Any(field => field is not Erlang.Pattern.Variable))
                {
                    return null;
                }

                var fields = tuple.Items.Select((pattern, ordinal) => (pattern, ordinal)).Skip(1)
                    .Where(entry => entry.pattern is Pattern.Variable variable && variable.Name != VariableScopeNames.Wildcard)
                    .ToArray();
                if (fields is not [var field] || field.pattern is not Pattern.Variable status)
                {
                    return null;
                }

                return (tag, tuple.Items.Count, field.ordinal, alias, status);
            }

            void CallTimer(Expr.Call timer, string? module)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    4,
                    XRegisterTag,
                    0
                );
                owner.EmitCallLine(timer, location);
                Call(module, timer.Function, 1);
            }

            void SaveInput(int input, int slot) => owner.MoveRegister(
                XRegisterTag,
                input,
                YRegisterTag,
                slot
            );

            void Load(int slot, int output) => owner.MoveRegister(
                YRegisterTag,
                slot,
                XRegisterTag,
                output
            );

            void EqualSaved(int value, int slot, int failureLabel)
            {
                owner.Op(ExactEqualTest, LabelTag, failureLabel);
                owner.Operand(XRegisterTag, value);
                owner.Operand(YRegisterTag, slot);
            }
        }
    }
}
