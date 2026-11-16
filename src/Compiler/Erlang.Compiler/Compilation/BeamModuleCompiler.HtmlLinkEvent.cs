using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record LinkQuery(
            Integer[] Prefix,
            Term[] Keys,
            Expr.Match Match,
            Expr.Call Parse,
            Expr.Call[] Convert,
            Atom Kind,
            Atom Callback,
            Expr.Call Expand,
            bool HasTable
        );

        private bool TryEmitHtmlLinkEvent(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [
                    Pattern.Tuple
                    {
                        Items: [Pattern.Literal { Value: Atom wx }, var w1, var w2, var w3,
                        Pattern.Tuple
                        {
                            Items: [Pattern.Literal { Value: Atom link }, Pattern.Literal { Value: Atom clicked },
                            Pattern.Tuple { Items: [Pattern.Literal { Value: Atom linkInfo }, Pattern.Variable target, var extra] }]
                        }]
                    },
                    AliasPattern
                    {
                        Left: Pattern.Tuple
                        {
                            Items: [Pattern.Literal { Value: Atom stateTag },
                        var s1, var s2, Pattern.Variable app, Pattern.Variable table, var s5, var s6, var s7]
                        }, Right: Pattern.Variable state
                    }
                  ], Body: Expr.Sequence
                  {
                      Items: [
                    Expr.Match
                      {
                          Pattern: Pattern.Variable updated, Value: Expr.Case
                          {
                              Value: Expr.Variable url,
                              Clauses: [var binaryBranch, var observerBranch, var termBranch,
                              {
                                  Patterns: [Pattern.Variable { Name: "_" }], Guard: Expr.Binary
                                  {
                                      Operator: ErlangOperators.ExactEqual, Left: Expr.Variable checkedApp, Right: Expr.Literal { Value: Atom observerApp }
                                  },
                                  Body: Expr.Sequence
                                  {
                                      Items: [Expr.Binary
                                      {
                                          Operator: ErlangOperators.Send, Left: Expr.Literal { Value: Atom recipient },
                                          Right: Expr.Tuple { Items: [Expr.Literal { Value: Atom notification }, Expr.Variable sent] }
                                      } send,
                                  Expr.Variable sentState]
                                  }
                              },
                              {
                                  Guard: null, Patterns: [Pattern.Variable { Name: "_" }], Body: Expr.Sequence
                                  {
                                      Items: [Expr.Call fallback, Expr.Variable fallbackState]
                                  }
                              }]
                          }
                      },
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom reply }, Expr.Variable result] }
                  ]
                  }
                },
                {
                    Guard: null, Patterns: [Pattern.Variable message, Pattern.Variable oldState], Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Call
                        {
                            Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Literal { Value: var format }, Expr.List
                            {
                                Tail: null, Items: [
                            Expr.Literal { Value: Atom module }, Expr.Literal { Value: Integer line }, Expr.Variable logged]
                            }
                    ]
                        } log,
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom otherReply }, Expr.Variable otherState] }
                  ]
                    }
                }
            ] || !new[] { w1, w2, w3, extra, s1, s2, s5, s6, s7 }.All(pattern => pattern is Pattern.Variable { Name: "_" })
                || !DistinctBindings(
                    target,
                    app,
                    table,
                    state,
                    updated
                ) || !DistinctBindings(message, oldState)
                || target.Name != url.Name || app.Name != checkedApp.Name || target.Name != sent.Name
                || state.Name != sentState.Name || state.Name != fallbackState.Name || !IsWindowCall(fallback, target)
                || updated.Name != result.Name || !reply.Equals(otherReply) || logged.Name != message.Name || oldState.Name != otherState.Name
                || ReadLinkQuery(
                    binaryBranch,
                    state,
                    table,
                    hasTable: false
                ) is not { } binary
                || ReadLinkQuery(
                    observerBranch,
                    state,
                    table,
                    hasTable: true
                ) is not { } observer
                || ReadLinkQuery(
                    termBranch,
                    state,
                    table,
                    hasTable: true
                ) is not { } term
                || binary.Expand.Function != observer.Expand.Function || binary.Expand.Function != term.Expand.Function
                || !entries.TryGetValue((binary.Expand.Function, 3), out int expand)
                || !binary.Prefix[0].Equals(observer.Prefix[0]) || !binary.Prefix[0].Equals(term.Prefix[0])
                || binary.Prefix[1].Value >= observer.Prefix[1].Value || observer.Prefix[1].Value >= term.Prefix[1].Value)
            {
                return false;
            }

            int termLabel = Label();
            int observerLabel = Label();
            int binaryLabel = Label();
            int ordinary = Label();
            int external = Label();
            int unhandled = Label();
            int badTerm = Label();
            int badObserver = Label();
            int badBinary = Label();

            EmitDetailRecordTest(
                0,
                5,
                wx,
                unhandled
            );
            owner.EmitTuplePart(0, 4, 2);
            EmitDetailRecordTest(
                2,
                3,
                link,
                unhandled
            );
            owner.EmitTuplePart(2, 1, 3);
            owner.EmitAtomTest(3, clicked.Name, unhandled);
            owner.EmitTuplePart(2, 2, 2);
            EmitDetailRecordTest(
                2,
                3,
                linkInfo,
                unhandled
            );
            EmitDetailRecordTest(
                1,
                8,
                stateTag,
                unhandled
            );
            owner.EmitTuplePart(2, 1, 0);
            owner.EmitNonemptyList(0, ordinary);
            owner.EmitListParts(
                0,
                XRegisterTag,
                2,
                XRegisterTag,
                3
            );
            TestLinkKey(2, binary.Prefix[0], ordinary);
            owner.EmitNonemptyList(3, ordinary);
            owner.EmitTuplePart(1, 4, 2);
            owner.EmitListParts(
                3,
                XRegisterTag,
                4,
                XRegisterTag,
                3
            );
            owner.Op(SelectValue, XRegisterTag, 4);
            owner.Operand(LabelTag, ordinary);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 6);
            foreach (var (query, label) in new[] { (binary, binaryLabel), (observer, observerLabel), (term, termLabel) })
            {
                owner.LiteralOperand(query.Prefix[1]);
                owner.Operand(LabelTag, label);
            }

            Mark(termLabel);
            EmitLinkQuery(
                term,
                ordinary,
                badTerm,
                expand,
                reply
            );
            Mark(observerLabel);
            EmitLinkQuery(
                observer,
                ordinary,
                badObserver,
                expand,
                reply
            );
            Mark(binaryLabel);
            EmitLinkQuery(
                binary,
                ordinary,
                badBinary,
                expand,
                reply
            );

            Mark(ordinary);
            owner.EmitTuplePart(1, 3, 2);
            owner.EmitAtomTest(2, observerApp.Name, external);
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitTupleHeader(XRegisterTag, 2, 1);
            owner.LiteralOperand(notification);
            owner.Operand(XRegisterTag, 0);
            owner.EmitConstant(recipient, 0);
            var previous = location;
            owner.EmitArithmeticLine(send.SourceLine, ref previous);
            owner.instructions.WriteByte(Send);
            EmitLinkReply(reply, 1);

            Mark(external);
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            EmitWindowCall(fallback, ref previous);
            EmitLinkReply(reply, 1);

            Mark(unhandled);
            owner.EmitLoggedReply(
                log,
                format,
                module,
                line,
                reply,
                previous,
                stateRegister: 1
            );
            EmitLinkBadMatch(term, badTerm);
            EmitLinkBadMatch(observer, badObserver);
            EmitLinkBadMatch(binary, badBinary);

            return true;
        }

        private LinkQuery? ReadLinkQuery(
            Clause clause,
            Pattern.Variable state,
            Pattern.Variable table,
            bool hasTable
        )
        {
            if (clause is not
                {
                    Guard: null, Patterns: [Pattern.List { Tail: Pattern.Variable suffix } prefix],
                    Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.List
                            {
                                Tail: null, Items: [
                        Pattern.Tuple { Items: [Pattern.Literal key1, Pattern.Variable first] },
                        Pattern.Tuple { Items: [Pattern.Literal key2, Pattern.Variable second] },
                        Pattern.Tuple { Items: [Pattern.Literal key3, Pattern.Variable third] }
                    ]
                            }, Value: Expr.Call parse
                        } match,
                    Expr.Match
                        {
                            Pattern: Pattern.Variable id, Value: Expr.Tuple
                            {
                                Items: [
                        Expr.Literal { Value: Atom kind }, var value]
                            }
                        },
                    Expr.Call
                        {
                            Module: null, DynamicRemote: false, Arguments: [
                        Expr.Variable identifier, Expr.Literal { Value: Atom callback }, Expr.Variable saved]
                        } expand
                ]
                    }
                }
                || !DistinctBindings(
                    state,
                    table,
                    suffix,
                    first,
                    second,
                    third,
                    id
                )
                || !IsWindowCall(parse, suffix) || identifier.Name != id.Name || saved.Name != state.Name
                || prefix.Items.Count < 2 || prefix.Items.Any(item => item is not Pattern.Literal { Value: Integer }))
            {
                return null;
            }

            if (hasTable)
            {
                if (value is not Expr.Tuple { Items: [Expr.Variable captured, var inner] } || captured.Name != table.Name)
                {
                    return null;
                }

                value = inner;
            }

            if (value is not Expr.Tuple { Items: [Expr.Call convert1, Expr.Call convert2, Expr.Call convert3] }
                || !IsConversion(convert1, first) || !IsConversion(convert2, second) || !IsConversion(convert3, third))
            {
                return null;
            }

            return new(
                prefix.Items.Cast<Pattern.Literal>().Select(item => (Integer)item.Value).ToArray(),
                [key1.Value, key2.Value, key3.Value],
                match,
                parse,
                [convert1, convert2, convert3],
                kind,
                callback,
                expand,
                hasTable
            );

            bool IsConversion(Expr.Call call, Pattern.Variable input) => call is { Function: "list_to_integer", DynamicRemote: false }
                && MatchesDetailArguments(call, input) && owner.ResolvesErlangBif(call);
        }

        private void EmitLinkQuery(
            LinkQuery query,
            int ordinary,
            int badMatch,
            int expand,
            Atom reply
        )
        {
            int head = query.HasTable ? 4 : 2;
            foreach (Integer ch in query.Prefix.Skip(2))
            {
                owner.EmitNonemptyList(3, ordinary);
                owner.EmitListParts(
                    3,
                    XRegisterTag,
                    head,
                    XRegisterTag,
                    3
                );
                TestLinkKey(head, ch, ordinary);
            }

            int frame = query.HasTable ? 4 : 3;
            owner.Op(Allocate, BeamWire.UnsignedTag, frame);
            owner.Operand(BeamWire.UnsignedTag, 4);
            ClearDetailSlots(0, 1);
            if (query.HasTable)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    2,
                    YRegisterTag,
                    2
                );
            }

            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                frame - 1
            );
            owner.MoveRegister(
                XRegisterTag,
                3,
                XRegisterTag,
                0
            );
            var previous = location;
            EmitWindowCall(query.Parse, ref previous);
            for (int index = 0; index < query.Keys.Length; index++)
            {
                int input = index == 0 ? 0 : 2;
                int tag = index == 0 ? XRegisterTag : YRegisterTag;
                int member = index == 2 ? 0 : 1;
                owner.EmitNonemptyList(input, badMatch);
                owner.EmitListParts(
                    input,
                    tag,
                    member,
                    XRegisterTag,
                    2
                );
                owner.Op(IsTuple, LabelTag, badMatch);
                owner.Operand(tag, member);
                owner.Op(TestArity, LabelTag, badMatch);
                owner.Operand(tag, member);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.Op(GetTupleElement, tag, member);
                owner.Operand(BeamWire.UnsignedTag, 0);
                owner.Operand(XRegisterTag, 3);
                TestLinkKey(3, query.Keys[index], badMatch);
            }

            owner.EmitNilTest(XRegisterTag, 2, badMatch);
            owner.EmitTuplePart(1, 1, 0);
            EmitWindowCall(query.Convert[0] with { Module = ErlangModuleNames.Module }, ref previous);
            for (int index = 1; index < query.Convert.Length; index++)
            {
                int slot = 2 - index;
                owner.Op(GetTupleElement, YRegisterTag, slot);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Operand(XRegisterTag, 1);
                SaveWindowResult(slot);
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    XRegisterTag,
                    0
                );
                EmitWindowCall(query.Convert[index] with { Module = ErlangModuleNames.Module }, ref previous);
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, query.HasTable ? 10 : 7);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(
                0,
                (YRegisterTag, 1),
                (YRegisterTag, 0),
                (XRegisterTag, 0)
            );
            if (query.HasTable)
            {
                owner.EmitTagMatchTuple(0, (YRegisterTag, 2), (XRegisterTag, 0));
            }

            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(query.Kind);
            owner.Operand(XRegisterTag, 0);
            Load(frame - 1, 2);
            owner.EmitConstant(query.Callback, 1);
            TrimDetailFrame(frame, 0);
            EmitWindowLocal(query.Expand, expand, ref previous);
            EmitLinkReply(reply, 0);
        }

        private void TestLinkKey(int register, Term key, int failure)
        {
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, register);
            owner.LiteralOperand(key);
        }

        private void EmitLinkReply(Atom reply, int frame)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, frame == 0 ? 1 : 0);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(reply);
            owner.Operand(frame == 0 ? XRegisterTag : YRegisterTag, 0);
            owner.EmitFrameReturn(frame);
        }

        private void EmitLinkBadMatch(LinkQuery query, int label)
        {
            Mark(label);
            owner.EmitLine(query.Match.SourceLine == 0 ? CallLocation(query.Parse, location)
                : new(query.Match.PreprocessorFile, query.Match.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);
        }
    }
}
