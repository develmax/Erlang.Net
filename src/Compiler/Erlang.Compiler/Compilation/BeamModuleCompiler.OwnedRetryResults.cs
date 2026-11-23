using static Erlang.Compiler.BeamEmissionFormat;
using SourcePattern = Erlang.Pattern;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record RetryPairReply(Expr.Call Call, string? Module, Term[] Prefix);

        private sealed record OwnedRetryResult(
            Atom Tag,
            Expr.Call Formatter,
            string? FormatterModule,
            Term Format,
            int FormatField,
            Expr.Call Commit,
            string? CommitModule,
            List<(int Destination, Term Value)> Constants,
            Atom ResultTag,
            BeamSourceLocation MatchLocation,
            BeamSourceLocation CaseLocation,
            Term ExactMethod,
            RetryPairReply Exact,
            RetryPairReply Default
        );

        private OwnedRetryResult? ReadOwnedRetryResult(
            Expr.Case result,
            CheckedNilStateRetry plan,
            string carrier
        )
        {
            if (DecoderSlot(1) != 1 || CaseSourceLocation(result) is not { Line: > 0 } caseLocation
                || result.Value is not Expr.Call { Arguments: [Expr.Variable updated] }
                || result.Clauses is not [var tagged, var exact, var tail]
                || tagged is not
                {
                    Guard: null,
                    Patterns:
                    [
                        SourcePattern.Tuple
                    {
                        Items: [SourcePattern.Literal { Value: Atom tag }, SourcePattern.Variable code, SourcePattern.Variable data]
                    }
                    ],
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match
                        {
                            Pattern: SourcePattern.Tuple { Items: [SourcePattern.Variable first, SourcePattern.Variable second] },
                            Value: Expr.Call commit
                        } match,
                            Expr.Tuple
                        {
                            Items: [Expr.Literal { Value: Atom resultTag }, Expr.Variable left, Expr.Variable right]
                        }
                        ]
                    }
                }
                || !Fresh(
                    code.Name,
                    data.Name,
                    first.Name,
                    second.Name
                )
                || left.Name != first.Name || right.Name != second.Name || match.SourceLine <= 0
                || commit.Arguments is not
                [
                    Expr.Variable outgoingCode,
                    Expr.Call
                {
                    Arguments:
                        [
                            var format,
                            Expr.List
                        {
                            Items: [Expr.RecordAccess { Value: Expr.Variable source } access],
                            Tail: null
                        }
                        ]
                } formatter,
                    var fourth,
                    var fifth,
                    var sixth,
                    Expr.RecordUpdate update
                ]
                || outgoingCode.Name != code.Name || source.Name != carrier
                || access.Name != plan.OuterTag.Name || access.Arity != plan.OuterWidth
                || access.Index <= 0 || access.Index >= access.Arity
                || !IsCarrierUpdate(update, data.Name)
                || !AttributeLiteral.TryNormalizeConstant(format, out Term formatValue)
                || !AttributeLiteral.TryNormalizeConstant(fourth, out Term constant2)
                || !AttributeLiteral.TryNormalizeConstant(fifth, out Term constant3)
                || !AttributeLiteral.TryNormalizeConstant(sixth, out Term constant4)
                || !TryResolveOrdinaryCallTarget(formatter, out string? formatterModule)
                || !TryResolveOrdinaryCallTarget(commit, out string? commitModule)
                || exact.Patterns is not [SourcePattern.Tuple { Items: [var tested, _] }]
                || ReadClosedPattern(tested) is not { } exactMethod
                || tail.Patterns is not [SourcePattern.Tuple
                {
                    Items: [SourcePattern.Any or SourcePattern.Variable, _]
                }]
                || ReadPair(exact) is not { } exactPair || ReadPair(tail) is not { } defaultPair)
            {
                return null;
            }

            return new(
                tag,
                formatter,
                formatterModule,
                formatValue,
                access.Index,
                commit,
                commitModule,
                [(2, constant2), (3, constant3), (4, constant4)],
                resultTag,
                new(match.PreprocessorFile ?? location.File, match.SourceLine),
                caseLocation,
                exactMethod,
                exactPair,
                defaultPair
            );

            bool IsCarrierUpdate(Expr.RecordUpdate value, string replacement) =>
                value is
                {
                    Value: Expr.Variable original,
                    Fields: [{ Value: Expr.Variable field } changed]
                }
                && original.Name == carrier && field.Name == replacement && value.Name == plan.OuterTag.Name
                && value.Arity == plan.OuterWidth && changed.Index == plan.OuterField;

            RetryPairReply? ReadPair(Clause branch)
            {
                if (branch is not
                    {
                        Guard: null,
                        Patterns:
                        [
                            SourcePattern.Tuple
                        {
                            Items:
                                [
                                    var method,
                                    SourcePattern.Tuple { Items: [SourcePattern.Variable message, SourcePattern.Variable replacement] }
                                ]
                        }
                        ],
                        Body: Expr.Sequence
                        {
                            Items:
                            [
                                Expr.Match { Pattern: SourcePattern.Variable reply, Value: Expr.Call call },
                                Expr.Tuple tuple
                            ]
                        }
                    }
                    || !Fresh(message.Name, replacement.Name, reply.Name)
                    || method is SourcePattern.Variable variable && variable.Name != VariableScopeNames.Wildcard
                        && !Fresh(
                            message.Name,
                            replacement.Name,
                            reply.Name,
                            variable.Name
                        )
                    || tuple.Items.Count < 2 || tuple.Items[^1] is not Expr.Variable returned || returned.Name != reply.Name
                    || call.Arguments is not [Expr.Variable sent, Expr.RecordUpdate record]
                    || sent.Name != message.Name || !IsCarrierUpdate(record, replacement.Name)
                    || !TryResolveOrdinaryCallTarget(call, out string? module))
                {
                    return null;
                }

                var prefix = new Term[tuple.Items.Count - 1];
                for (int index = 0; index < prefix.Length; index++)
                {
                    if (!AttributeLiteral.TryNormalizeConstant(tuple.Items[index], out prefix[index]))
                    {
                        return null;
                    }
                }

                return new(call, module, prefix);
            }

            bool Fresh(params string[] names) =>
                names.All(name => name != VariableScopeNames.Wildcard && name != updated.Name
                    && !plan.RetryInputs.ContainsKey(name))
                && names.Distinct(StringComparer.Ordinal).Count() == names.Length;
        }

        private void EmitOwnedRetryResult(
            OwnedRetryResult result,
            CheckedNilStateRetry plan,
            BeamSourceLocation selectorLocation
        )
        {
            const int resultSlot = 0;
            const int carrierSlot = 1;
            int tagged = Label();
            int pair = Label();
            int defaultPair = Label();
            int caseFailure = Label();
            int matchFailure = Label();
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                resultSlot
            );
            owner.EmitTupleArityDispatch(caseFailure, [(2, pair), (3, tagged)]);

            Mark(tagged);
            owner.EmitTuplePart(0, 0, 0);
            owner.EmitAtomTest(0, result.Tag.Name, caseFailure);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 0);
            EmitSavedRecordField(carrierSlot, result.FormatField, 0);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(result.Format, 0);
            owner.EmitCallLine(result.Formatter, selectorLocation);
            Call(result.FormatterModule, result.Formatter.Function, 2);

            owner.Op(TestHeap, BeamWire.UnsignedTag, plan.OuterWidth + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitSavedRecordField(resultSlot, 2, 1);
            EmitSavedRecordUpdate(
                plan.OuterWidth,
                plan.OuterField,
                sourceSlot: carrierSlot,
                valueRegister: 1,
                destination: 5
            );
            EmitSavedRecordField(resultSlot, 1, 1);
            owner.EmitOrderedTailMoves([(0, 1), (1, 0)], result.Constants);
            TrimReplyFrame();
            owner.EmitCallLine(result.Commit, CallLocation(result.Formatter, selectorLocation));
            Call(result.CommitModule, result.Commit.Function, 6);
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                2,
                matchFailure
            );
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitTupleHeader(XRegisterTag, 3);
            owner.LiteralOperand(result.ResultTag);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(0);

            Mark(pair);
            owner.EmitTuplePart(0, 0, 0);
            EmitSavedRecordField(resultSlot, 1, 1);
            owner.Op(ExactEqualTest, LabelTag, defaultPair);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(result.ExactMethod);
            owner.Op(IsTuple, LabelTag, caseFailure);
            owner.Operand(XRegisterTag, 1);
            owner.Op(TestArity, LabelTag, defaultPair);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            EmitPair(result.Exact);

            Mark(defaultPair);
            EmitTupleShapeTest(
                XRegisterTag,
                1,
                2,
                caseFailure
            );
            EmitPair(result.Default);

            Mark(matchFailure);
            owner.EmitLine(result.MatchLocation);
            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(caseFailure);
            owner.EmitLine(result.CaseLocation);
            owner.Op(CaseEnd, YRegisterTag, resultSlot);

            void TrimReplyFrame()
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, 2);
                owner.Operand(BeamWire.UnsignedTag, 0);
            }

            void EmitPair(RetryPairReply reply)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, plan.OuterWidth + 1);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.EmitTuplePart(1, 1, 0);
                EmitSavedRecordUpdate(
                    plan.OuterWidth,
                    plan.OuterField,
                    sourceSlot: carrierSlot
                );
                owner.EmitTuplePart(1, 0, 1);
                owner.Op(Swap, XRegisterTag, 0);
                owner.Operand(XRegisterTag, 1);
                TrimReplyFrame();
                owner.EmitCallLine(reply.Call, selectorLocation);
                Call(reply.Module, reply.Call.Function, 2);
                owner.Op(TestHeap, BeamWire.UnsignedTag, reply.Prefix.Length + 2);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.EmitTupleHeader(XRegisterTag, reply.Prefix.Length + 1);
                foreach (Term value in reply.Prefix)
                {
                    owner.LiteralOperand(value);
                }

                owner.Operand(XRegisterTag, 0);
                owner.EmitFrameReturn(0);
            }
        }
    }
}
