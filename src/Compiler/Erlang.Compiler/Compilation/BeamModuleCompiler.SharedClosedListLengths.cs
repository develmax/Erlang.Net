using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record ClosedListLengthBranch(
            int Length,
            Clause Clause,
            (Pattern.Variable Variable, int Register)[] Inputs,
            EntryCall? Tail,
            SavedCallSequence? Terminal
        );

        private bool TryEmitSharedClosedListLengths(FunctionDefinition function, int functionInfo)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Arity != 1
                || function.Clauses is not [var first, var second]
                || ReadBranch(first) is not { } left || ReadBranch(second) is not { } right
                || left.Length == right.Length)
            {
                return false;
            }

            var (longer, shorter) = left.Length > right.Length ? (left, right) : (right, left);
            const int tail = 2;
            int shorterEntry = Label();
            int current = 0;
            for (int index = 0; index < longer.Length; index++)
            {
                // Failure at the shorter length permits an empty tail there.
                // Earlier and later failures are genuine function-clause failures.
                owner.EmitNonemptyList(current, index == shorter.Length ? shorterEntry : functionInfo);
                owner.EmitListParts(
                    current,
                    XRegisterTag,
                    Head(index),
                    XRegisterTag,
                    tail
                );
                current = tail;
            }

            var frames = new Dictionary<int, int>();
            owner.EmitNilTest(XRegisterTag, tail, functionInfo);
            BeamSourceLocation previous = EmitBranch(longer, 0, location);

            Mark(shorterEntry);
            owner.EmitNilTest(XRegisterTag, tail, functionInfo);
            EmitBranch(shorter, 1, previous);
            if (frames.Count != 0)
            {
                TupleFrames = frames;
            }

            return true;

            ClosedListLengthBranch? ReadBranch(Clause clause)
            {
                if (clause.Guard is not null || clause.Patterns is not
                    [Pattern.List { Tail: null, Items: { Count: > 0 } items }]
                    || items.Count + 2 > FirstReservedXRegister || items.Any(item => item is not Erlang.Pattern.Variable))
                {
                    return null;
                }

                var variables = items.Cast<Pattern.Variable>().ToArray();
                if (!DistinctBindings(variables))
                {
                    return null;
                }

                var parameters = Enumerable.Repeat<Pattern>(new Pattern.Variable(VariableScopeNames.Wildcard),
                    items.Count + 2).ToArray();
                for (int index = 0; index < items.Count; index++)
                {
                    parameters[Head(index)] = variables[index];
                }

                Clause prepared = clause with { Patterns = parameters };
                var used = ReadUsedEntryParameters(prepared);
                var inputs = variables.Select((variable, index) => (Variable: variable, Register: Head(index)))
                    .Where(binding => used is null || used.Contains(binding.Variable.Name)).ToArray();
                string[] names = parameters.Cast<Pattern.Variable>().Select(variable => variable.Name).ToArray();
                EntryCall? call = prepared.Body is Expr.Call tailCall
                    ? ReadEntryCall(tailCall, names, allowControlTransfer: true) : null;
                SavedCallSequence? terminal = null;
                if (call is null)
                {
                    var folded = FoldStraightSequenceConstants(function with
                    {
                        Arity = parameters.Length,
                        Clauses = [prepared]
                    }).Clauses[0];
                    Expr body = InlineImmediateReturnedConstruction(folded.Body, names);
                    terminal = ReadSavedCallSequenceTuple(body, names);
                }

                if (call is null && terminal is null && ReadEntrySequenceBody(prepared.Body, inputs) is null)
                {
                    return null;
                }

                return new(
                    items.Count,
                    prepared,
                    inputs,
                    call,
                    terminal
                );
            }

            BeamSourceLocation EmitBranch(ClosedListLengthBranch branch, int index, BeamSourceLocation previous)
            {
                if (branch.Tail is { } call)
                {
                    EmitRegisterTailCall(call, previous);

                    return CallLocation(call.Call, location);
                }

                if (branch.Terminal is { } terminal)
                {
                    EmitSavedCallSequence(terminal with { DelayReturnCopies = true });

                    return location;
                }

                EmitEntryClauseBody(
                    branch.Clause,
                    branch.Inputs,
                    index,
                    frames,
                    null,
                    null,
                    literalDispatch: false
                );

                return location;
            }

            static int Head(int index) => index == 0 ? 1 : index + 2;
        }

        private static Expr InlineImmediateReturnedConstruction(Expr body, string[] parameters)
        {
            if (body is not Expr.Sequence { Items: [.., Expr.Match { Pattern: Pattern.Variable binding, Value: var value }, Expr.Tuple result] } sequence
                || binding.Name == VariableScopeNames.Wildcard || value is not (Expr.List or Expr.Tuple)
                || parameters.Contains(binding.Name, StringComparer.Ordinal)
                || sequence.Items.SkipLast(2).Any(item => item is not (Expr.Call or Expr.Match { Pattern: Erlang.Pattern.Variable }))
                || sequence.Items.SkipLast(2).OfType<Expr.Match>().Any(match => ((Pattern.Variable)match.Pattern).Name == binding.Name)
                || !LocalFunctionEntries.Walk(result).All(item => item is Expr.Tuple or Expr.List or Expr.Literal or Expr.Variable)
                || LocalFunctionEntries.Walk(result).OfType<Expr.Variable>().Count(variable => variable.Name == binding.Name) != 1)
            {
                return body;
            }

            return sequence with { Items = [.. sequence.Items.SkipLast(2), Replace(result)] };

            Expr Replace(Expr expression) => expression switch
            {
                Expr.Variable variable when variable.Name == binding.Name => value,
                Expr.Tuple tuple => tuple with { Items = tuple.Items.Select(Replace).ToArray() },
                Expr.List list => list with { Items = list.Items.Select(Replace).ToArray(), Tail = list.Tail is null ? null : Replace(list.Tail) },
                _ => expression
            };
        }
    }
}
