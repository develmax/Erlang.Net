using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record TaggedCallMatch(Expr.Match Match, Expr.Call Call, Atom Tag, int Arity);

        private static TaggedCallMatch? ReadDiscardedTaggedCallMatch(Expr? expression)
        {
            if (expression is not Expr.Match { Pattern: Pattern.Tuple tuple, Value: Expr.Call call } match
                || !HasTupleTag(tuple, out Atom tag)
                || tuple.Items.Skip(1).Any(item => item is not Pattern.Variable { Name: VariableScopeNames.Wildcard }))
            {
                return null;
            }

            return new(match, call, tag, tuple.Items.Count);
        }

        private bool TryEmitEntryDiscardedTaggedMatch(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence { Items: [_, Expr.Call] or [Expr.Match, _, Expr.Call] } sequence
                || ReadDiscardedTaggedCallMatch(sequence.Items[^2]) is not { } check)
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            var tail = (Expr.Call)sequence.Items[^1];
            (Expr.Call Call, string Result, bool Preserve)? preceding = null;
            if (sequence.Items.Count == 3)
            {
                if (sequence.Items[0] is not Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding,
                        Value: Expr.Call producer
                    } || parameters.Contains(binding.Name, StringComparer.Ordinal))
                {
                    return false;
                }

                preceding = (producer, binding.Name, tail.Arguments.OfType<Expr.Variable>()
                    .Any(argument => argument.Name == binding.Name));
            }

            var used = tail.Arguments.OfType<Expr.Variable>().Select(argument => argument.Name).ToHashSet(StringComparer.Ordinal);
            int[] survivors = Enumerable.Range(0, parameters.Length)
                .Where(index => used.Contains(parameters[index])).Reverse().ToArray();
            if (ReadEntrySelector(check.Call, parameters, survivors, preceding: preceding) is not { } selector)
            {
                return false;
            }

            int frame = selector.FrameSize - selector.Trimmed;
            var stack = new string[frame];
            for (int slot = selector.Trimmed; slot < selector.Saved.Length; slot++)
            {
                stack[slot - selector.Trimmed] = parameters[selector.Saved[slot]];
            }

            if (selector.PrefixResultSlot is int result)
            {
                stack[result - selector.Trimmed] = preceding!.Value.Result;
            }

            if (ReadEntryCall(tail, [], stack) is not { } finish)
            {
                return false;
            }

            EmitEntrySelector(selector, check.Call, parameters);
            var beforeCheck = selector.Prefix is { } prefix ? CallLocation(prefix.Call, location) : location;
            var checkedAt = CallLocation(check.Call, beforeCheck);
            int failure = Label();
            EmitDetailRecordTest(0, check.Arity, check.Tag, failure);
            owner.EmitOrderedTailMoves(finish.Moves, finish.Constants, finish.SavedInputs);
            EmitPreparedFrameTail(finish, checkedAt, frame);

            var beforeFailure = finish.Module is null ? checkedAt : CallLocation(tail, checkedAt);
            EmitTaggedCallMatchFailure(check.Match, failure, checkedAt, beforeFailure);
            return true;
        }

        private static (TaggedCallMatch Check, (int Field, string Name)? Payload)? ReadSimpleTaggedCallMatch(Expr expression)
        {
            if (ReadDiscardedTaggedCallMatch(expression) is { } discarded)
            {
                return (discarded, null);
            }

            if (expression is Expr.Match { Pattern: Pattern.Tuple tuple, Value: Expr.Call call } match
                && ReadSingleTupleField(tuple) is { Field: Pattern.Variable { Name: not VariableScopeNames.Wildcard } payload } field)
            {
                return (new(match, call, field.Tag, tuple.Items.Count), (field.Index, payload.Name));
            }

            return null;
        }

        private bool TryEmitTaggedCallSequence(Expr.Sequence sequence, string[] parameters)
        {
            if (sequence.Items.Count < 3 || sequence.Items[^1] is not Expr.Call
                || ReadSimpleTaggedCallMatch(sequence.Items[0]) is not { } tagged
                || ReadEntryCall(tagged.Check.Call, parameters) is not { } first)
            {
                return false;
            }

            var names = new HashSet<string>(parameters, StringComparer.Ordinal);
            string? previous = tagged.Payload?.Name;
            if (previous is not null && !names.Add(previous))
            {
                return false;
            }

            bool keepPayload = tagged.Payload is { } matchedPayload
                && sequence.Items.Skip(2).Any(item => UsesPayload(item, matchedPayload.Name));
            string[] savedInputs = keepPayload ? [.. parameters, tagged.Payload!.Value.Name] : parameters;
            int? projected = null;
            var steps = new List<(EntryCall Call, int? ListDestination)>();
            for (int index = 1; index < sequence.Items.Count; index++)
            {
                Expr item = sequence.Items[index];
                string? binding = null;
                if (item is Expr.Match { Pattern: Pattern.Variable variable, Value: Expr.Call value })
                {
                    if (variable.Name != VariableScopeNames.Wildcard)
                    {
                        if (!names.Add(variable.Name))
                        {
                            return false;
                        }

                        binding = variable.Name;
                    }

                    item = value;
                }

                if (item is not Expr.Call call
                    || ReadEntryCall(call, previous is null || keepPayload && index == 1 ? [] : [previous], savedInputs) is not { } step)
                {
                    return false;
                }

                if (index == 1 && tagged.Payload is { } payload && UsesPayload(call, payload.Name))
                {
                    projected = payload.Field;
                }

                steps.Add((step, null));
                previous = binding;
            }

            steps.Reverse();
            EmitEntryCallChain(
                first,
                parameters,
                steps,
                relocateBeforeTrim: true,
                taggedMatch: (tagged.Check, keepPayload ? tagged.Payload!.Value.Field : projected, keepPayload)
            );
            return true;

            static bool UsesPayload(Expr item, string name)
            {
                Expr expression = item is Expr.Match match ? match.Value : item;
                return expression is Expr.Call call && call.Arguments.OfType<Expr.Variable>()
                    .Any(argument => argument.Name == name);
            }
        }

        private bool TryEmitRegisterTaggedMatch(Expr.Match match)
        {
            if (ReadSimpleTaggedCallMatch(match) is not { } tagged
                || !HasSimpleDecodeArguments(tagged.Check.Call)
                || tagged.Payload is { } bound && (variables.ContainsKey(bound.Name) || deferredPairMembers.ContainsKey(bound.Name)))
            {
                return false;
            }

            Expression(tagged.Check.Call);
            int failure = Label();
            int done = Label();
            EmitDetailRecordTest(0, tagged.Check.Arity, tagged.Check.Tag, failure);
            if (tagged.Payload is { } payload)
            {
                int slot = Slot();
                owner.Op(GetTupleElement, XRegisterTag, 0);
                owner.Operand(BeamWire.UnsignedTag, payload.Field);
                owner.Operand(YRegisterTag, slot);
                variables.Add(payload.Name, slot);
            }

            JumpTo(done);
            Mark(failure);
            owner.EmitArithmeticLine(match.SourceLine, ref location);
            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(done);
            return true;
        }

        private void EmitTaggedCallMatchFailure(
            Expr.Match match,
            int label,
            BeamSourceLocation checkedAt,
            BeamSourceLocation previous,
            bool forceLine = false
        )
        {
            Mark(label);
            var failedAt = match.SourceLine == 0
                ? checkedAt
                : new BeamSourceLocation(match.PreprocessorFile, match.SourceLine);
            if (forceLine || failedAt != previous)
            {
                owner.EmitLine(failedAt);
            }

            owner.Op(BadMatch, XRegisterTag, 0);
        }
    }
}
