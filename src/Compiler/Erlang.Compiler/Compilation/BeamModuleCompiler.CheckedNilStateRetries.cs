using static Erlang.Compiler.BeamEmissionFormat;
using SourcePattern = Erlang.Pattern;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const string CheckedNilStateField = "#checked_nil_state_field";

        private sealed record CheckedNilStateRetry(
            Clause First,
            Clause Retry,
            Atom State,
            Atom Role,
            Atom OuterTag,
            int OuterWidth,
            int OuterField,
            Atom InnerTag,
            int InnerWidth,
            Expr.RecordAccess Access,
            Dictionary<string, int> FirstInputs,
            Dictionary<string, int> RetryInputs,
            int? MessageField
        );

        private static CheckedNilStateRetry? ReadCheckedNilStateRetry(IReadOnlyList<Clause> rows, int input)
        {
            if (rows is not [var first, var retry] || input != 1
                || first.Patterns.Count != 4 || retry.Patterns.Count != 4
                || !IsDiscarded(first.Patterns[0]) || !IsDiscarded(retry.Patterns[0])
                || first.Guard is not null || retry.Guard is not null
                || LocalFunctionEntries.Walk(first.Body).Concat(LocalFunctionEntries.Walk(retry.Body)).Any(
                    expression => expression is Expr.Try or Expr.Catch or Expr.Receive or Expr.Maybe or Expr.Fun))
            {
                return null;
            }

            var firstInputs = new Dictionary<string, int>(StringComparer.Ordinal);
            var retryInputs = new Dictionary<string, int>(StringComparer.Ordinal);
            var firstState = ReadBoundTuple(first.Patterns[2], 2, firstInputs);
            var retryState = ReadBoundTuple(retry.Patterns[2], 2, retryInputs);
            var firstOuter = ReadBoundTuple(first.Patterns[3], 3, firstInputs);
            var retryOuter = ReadBoundTuple(retry.Patterns[3], 3, retryInputs);
            if (firstState?.Items is not
                [SourcePattern.Literal { Value: Atom state }, SourcePattern.Literal { Value: Atom role }]
                || retryState?.Items is not
                [SourcePattern.Literal { Value: Atom otherState }, SourcePattern.Literal { Value: Atom otherRole }]
                || !state.Equals(otherState) || !role.Equals(otherRole)
                || firstOuter is null || !HasTupleTag(firstOuter, out Atom outerTag)
                || retryOuter is null || !HasTupleTag(retryOuter, out Atom otherOuterTag)
                || !outerTag.Equals(otherOuterTag) || firstOuter.Items.Count != retryOuter.Items.Count
                || ReadOnlyField(firstOuter) is not { Pattern: SourcePattern.Tuple inner } outerField
                || ReadOnlyField(retryOuter) is not { Pattern: SourcePattern.Variable innerBinding } otherOuterField
                || outerField.Index != otherOuterField.Index || !retryInputs.TryAdd(innerBinding.Name, 0)
                || !HasTupleTag(inner, out Atom innerTag)
                || ReadOnlyField(inner) is not { } innerField
                || innerField.Pattern is not (SourcePattern.List { Items.Count: 0, Tail: null }
                    or SourcePattern.Literal { Value: Nil }))
            {
                return null;
            }

            if (first.Patterns[1] is not SourcePattern.Tuple firstMessage
                || retry.Patterns[1] is not SourcePattern.Tuple retryMessage
                || !HasTupleTag(firstMessage, out Atom messageTag)
                || !HasTupleTag(retryMessage, out Atom otherMessageTag) || !messageTag.Equals(otherMessageTag)
                || !firstMessage.Items.Skip(1).All(IsDiscarded))
            {
                return null;
            }

            int? messageField = null;
            if (!retryMessage.Items.Skip(1).All(IsDiscarded))
            {
                if (ReadOnlyField(retryMessage) is not { Pattern: SourcePattern.Variable payload } field
                    || !retryInputs.TryAdd(payload.Name, first.Patterns.Count + 1))
                {
                    return null;
                }

                messageField = field.Index;
            }

            Expr head = retry.Body is Expr.Sequence { Items: [var firstItem, ..] } ? firstItem : retry.Body;
            Expr start = head is Expr.Match match ? match.Value : head;
            if (start is not Expr.Case
                {
                    Value: Expr.RecordAccess { Value: Expr.Variable source } access
                } selection
                || source.Name != innerBinding.Name || access.Name != innerTag.Name
                || access.Arity != inner.Items.Count || access.Index != innerField.Index
                || access.SourceLine <= 0 || !retryInputs.TryAdd(CheckedNilStateField, 1))
            {
                return null;
            }

            // The retry reads the same field before any other source effect. Its
            // selector can use the value already read by the exact nil test.
            Expr rewritten = selection with { Value = new Expr.Variable(CheckedNilStateField) };
            if (head is Expr.Match binding)
            {
                rewritten = binding with { Value = rewritten };
            }

            if (retry.Body is Expr.Sequence sequence)
            {
                rewritten = sequence with { Items = [rewritten, .. sequence.Items.Skip(1)] };
            }

            rewritten = RematerializeCheckedRetryState(
                rewritten,
                state,
                role,
                retryInputs
            );

            var checkedInner = new AliasPattern(
                innerBinding,
                new SourcePattern.Tuple(
                    [new SourcePattern.Literal(innerTag), .. inner.Items.Skip(1).Select(_ => new SourcePattern.Any())]
                )
            );
            var checkedRetryOuter = new SourcePattern.Tuple(
                retryOuter.Items.Select((pattern, index) => index == outerField.Index ? checkedInner : pattern).ToArray()
            );
            SourcePattern proved = retry.Patterns[3] is AliasPattern alias
                && TryReadPatternAlias(alias, out var outerBinding, out _)
                ? new AliasPattern(outerBinding, checkedRetryOuter)
                : checkedRetryOuter;
            var terminal = retry with { Body = rewritten, Patterns = [.. retry.Patterns.Take(3), proved] };

            return new(
                first,
                terminal,
                state,
                role,
                outerTag,
                firstOuter.Items.Count,
                outerField.Index,
                innerTag,
                inner.Items.Count,
                access,
                firstInputs,
                retryInputs,
                messageField
            );

            static bool IsDiscarded(SourcePattern pattern) =>
                pattern is SourcePattern.Any or SourcePattern.Variable { Name: VariableScopeNames.Wildcard };

            static SourcePattern.Tuple? ReadBoundTuple(
                SourcePattern pattern,
                int register,
                Dictionary<string, int> bindings
            )
            {
                if (pattern is AliasPattern alias)
                {
                    if (!TryReadPatternAlias(alias, out var binding, out var shape)
                        || !bindings.TryAdd(binding.Name, register))
                    {
                        return null;
                    }

                    pattern = shape;
                }

                return pattern as SourcePattern.Tuple;
            }

            static (int Index, SourcePattern Pattern)? ReadOnlyField(SourcePattern.Tuple tuple)
            {
                var used = tuple.Items.Select((pattern, index) => (Index: index, Pattern: pattern))
                    .Skip(1).Where(field => !IsDiscarded(field.Pattern)).ToArray();

                return used is [var field] ? field : null;
            }
        }

        private static Expr RematerializeCheckedRetryState(
            Expr body,
            Atom state,
            Atom role,
            Dictionary<string, int> inputs
        )
        {
            string alias = inputs.FirstOrDefault(binding => binding.Value == 2).Key;
            if (alias is null || LocalFunctionEntries.Walk(body).Any(ReferencesStatePattern))
            {
                return body;
            }

            // A successful state selection and role check prove the whole
            // immutable tuple. No pattern in this continuation uses its name.
            var value = Term.Tuple(state, role);
            Expr rewritten = BeamTargetedInlining.Rewrite(
                body,
                expression => expression is Expr.Variable variable && variable.Name == alias
                    ? new Expr.Literal(value)
                    : expression
            );
            var remaining = new Clause([new SourcePattern.Variable(alias)], null, rewritten);
            if (ReadUsedEntryParameters(remaining)?.Contains(alias) != false)
            {
                return body;
            }

            inputs.Remove(alias);

            return rewritten;

            bool ReferencesStatePattern(Expr expression) => expression switch
            {
                Expr.Match match => MentionsAlias(match.Pattern),
                Expr.Case choice => choice.Clauses.SelectMany(clause => clause.Patterns).Any(MentionsAlias),
                Expr.If choice => choice.Clauses.SelectMany(clause => clause.Patterns).Any(MentionsAlias),
                Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension => true,
                _ => false
            };

            bool MentionsAlias(SourcePattern pattern) => pattern switch
            {
                SourcePattern.Any or SourcePattern.Literal => false,
                SourcePattern.Variable variable => variable.Name == alias,
                SourcePattern.Tuple tuple => tuple.Items.Any(MentionsAlias),
                SourcePattern.List list => list.Items.Any(MentionsAlias) || list.Tail is { } tail && MentionsAlias(tail),
                AliasPattern binding => MentionsAlias(binding.Left) || MentionsAlias(binding.Right),
                _ => true
            };
        }

        private void EmitCheckedNilStateRetry(
            CheckedNilStateRetry plan,
            int roleRegister,
            int? sharedMessageField,
            int failure,
            Dictionary<int, int> frames,
            List<(int Label, BeamSourceLocation Location)> recordFailures,
            List<(int Label, BeamSourceLocation Location)> tupleMatchFailures,
            ref int nextFrameKey
        )
        {
            const int carrier = 3;
            const int inner = 0;
            const int selector = 1;
            int retry = Label();
            int badRecord = Label();

            // All recoverable mismatches retain the original arguments. After
            // these tests, a malformed inner value raises in the retry clause.
            owner.EmitAtomTest(roleRegister, plan.Role.Name, failure);
            EmitDetailRecordTest(
                carrier,
                plan.OuterWidth,
                plan.OuterTag,
                failure
            );
            int payloadRegister = plan.First.Patterns.Count + 1;
            if (plan.MessageField is int field && field != sharedMessageField)
            {
                owner.EmitTuplePart(1, field, payloadRegister);
            }

            owner.EmitTuplePart(carrier, plan.OuterField, inner);
            EmitDetailRecordTest(
                inner,
                plan.InnerWidth,
                plan.InnerTag,
                badRecord
            );
            owner.EmitTuplePart(inner, plan.Access.Index, selector);
            owner.EmitNilTest(XRegisterTag, selector, retry);

            int previousFrame = frameSize;
            int previousSlot = nextSlot;
            int previousPeak = peakSlot;
            var previousVariables = variables;
            var previousRecords = checkedRecordBindings;
            int firstFrame = nextFrameKey--;
            if (TryEmitCheckedNilStatePairResult(plan, tupleMatchFailures))
            {
                frames.Add(firstFrame, 1);
                previousPeak = Math.Max(previousPeak, 1);
            }
            else
            {
                EmitArm(plan.First, plan.FirstInputs, firstFrame);
            }

            Mark(retry);
            EmitArm(
                plan.Retry,
                plan.RetryInputs,
                nextFrameKey--,
                optionalUpdate: true
            );
            recordFailures.Add((badRecord, new(plan.Access.PreprocessorFile, plan.Access.SourceLine)));

            frameSize = previousFrame;
            nextSlot = previousSlot;
            peakSlot = previousPeak;
            variables = previousVariables;
            checkedRecordBindings = previousRecords;

            void EmitArm(
                Clause clause,
                Dictionary<string, int> inputs,
                int frameKey,
                bool optionalUpdate = false
            )
            {
                frameSize = plannedTupleFrames?.GetValueOrDefault(frameKey, previousFrame) ?? previousFrame;
                nextSlot = 0;
                peakSlot = 0;
                variables = new(StringComparer.Ordinal);
                checkedRecordBindings = new(StringComparer.Ordinal);
                if (!optionalUpdate || !TryEmitCheckedRetryOptionalUpdate(plan))
                {
                    EmitNestedTupleLeaf(
                        new(
                            clause,
                            0,
                            plan.Role,
                            inputs,
                            [],
                            null
                        ),
                        frameSize,
                        recordFailures,
                        checkedPatterns: true
                    );
                }

                frames.Add(frameKey, peakSlot);
                previousPeak = Math.Max(previousPeak, peakSlot);
            }
        }
    }
}
