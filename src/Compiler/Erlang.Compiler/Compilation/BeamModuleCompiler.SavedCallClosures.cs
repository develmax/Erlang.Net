using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record CallClosure(string Binding, Expr.Fun Function, string[] Captures);

        private sealed record PreparedCallClosure(
            CallClosure Closure,
            Dictionary<string, int> CaptureSlots,
            string? X0Capture,
            bool LiveX0,
            bool OnStack,
            int Destination,
            bool MoveResult = false
        );

        private (Expr.Call Call, string? Result)[]? ReadCallsWithClosures(
            IEnumerable<Expr> expressions,
            string[] parameters,
            out Dictionary<int, CallClosure> closures
        )
        {
            closures = [];
            var statements = new List<Expr>();
            var bindings = parameters.Where(name => name != VariableScopeNames.Wildcard)
                .ToDictionary(name => name, _ => (Term)Nil.Value, StringComparer.Ordinal);
            foreach (Expr expression in expressions)
            {
                if (expression is Expr.Match { Pattern: Pattern.Variable variable, Value: Expr.Fun function })
                {
                    if (statements.Count == 0 || parentFunction is null || function.SelfName is not null
                        || variable.Name == VariableScopeNames.Wildcard || bindings.ContainsKey(variable.Name))
                    {
                        return null;
                    }

                    var captures = ClosureCaptures.Capture(function, bindings);
                    if (function.Clauses[0].Patterns.Count + captures.Count > BeamWire.MaximumArity
                        || !closures.TryAdd(statements.Count, new CallClosure(
                            variable.Name,
                            function,
                            bindings.Keys.Where(captures.ContainsKey).ToArray()
                        )))
                    {
                        return null;
                    }

                    bindings.Add(variable.Name, Nil.Value);
                    continue;
                }

                statements.Add(expression);
                if (expression is Expr.Match { Pattern: Pattern.Variable binding }
                    && binding.Name != VariableScopeNames.Wildcard && !bindings.TryAdd(binding.Name, Nil.Value))
                {
                    return null;
                }
            }

            return closures.ContainsKey(statements.Count) ? null : ReadStraightCalls(statements, parameters);
        }

        private static PreparedCallClosure? PrepareCallClosure(
            CallClosure closure,
            int index,
            Expr.Call target,
            string? previous,
            Dictionary<string, int> saved,
            List<int> lastSlotReads,
            Dictionary<string, int> lastReads,
            out int operand
        )
        {
            operand = -1;
            if (!lastReads.TryGetValue(closure.Binding, out int lastRead) || lastRead < index)
            {
                return null;
            }

            var captures = new Dictionary<string, int>(StringComparer.Ordinal);
            string? x0Capture = null;
            foreach (string capture in closure.Captures)
            {
                if (capture == previous)
                {
                    x0Capture = capture;
                }
                else if (saved.TryGetValue(capture, out int slot))
                {
                    captures.Add(capture, slot);
                }
                else
                {
                    return null;
                }
            }

            bool liveX0 = previous is not null && lastReads.GetValueOrDefault(previous) >= index;
            if (lastRead == index)
            {
                var uses = Enumerable.Range(0, target.Arguments.Count)
                    .Where(argument => target.Arguments[argument] is Expr.Variable variable && variable.Name == closure.Binding)
                    .ToArray();
                if (uses is not [int argument])
                {
                    return null;
                }

                operand = argument;
                bool moveResult = argument == 0 && previous is not null && x0Capture is null
                    && lastReads.GetValueOrDefault(previous) == index
                    && target.Arguments is [_, Expr.Variable value] && value.Name == previous
                    && closure.Function.Clauses is [{ Guard: null, Body: Expr.Variable } clause]
                    && HasUnconditionalParameters(clause);
                int register = argument == 0 && liveX0 && !moveResult ? 1 : argument;

                return new PreparedCallClosure(
                    closure,
                    captures,
                    x0Capture,
                    liveX0,
                    OnStack: false,
                    Destination: register,
                    MoveResult: moveResult
                );
            }

            var argumentReads = target.Arguments.SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Variable>()
                .Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
            var reusable = captures.Where(capture => lastReads[capture.Key] == index && !argumentReads.Contains(capture.Key))
                .OrderBy(capture => capture.Value).FirstOrDefault();
            int destination = reusable.Value;
            if (reusable.Key is { } dyingCapture)
            {
                saved.Remove(dyingCapture);
            }
            else
            {
                while (saved.ContainsValue(destination))
                {
                    destination++;
                }
            }

            if (destination == lastSlotReads.Count)
            {
                lastSlotReads.Add(lastRead);
            }
            else
            {
                lastSlotReads[destination] = lastRead;
            }

            if (!saved.TryAdd(closure.Binding, destination))
            {
                return null;
            }

            return new PreparedCallClosure(
                closure,
                captures,
                x0Capture,
                liveX0,
                OnStack: true,
                Destination: destination
            );
        }

        private void EmitPreparedCallClosure(
            PreparedCallClosure plan,
            int[] remap,
            CallArgumentConstructions? construction
        )
        {
            var previousVariables = variables;
            var previousOrder = entryCaptureOrder;
            variables = plan.CaptureSlots.ToDictionary(capture => capture.Key, capture => remap[capture.Value], StringComparer.Ordinal);
            entryCaptureOrder = plan.Closure.Captures.Select((name, index) => (name, index))
                .ToDictionary(capture => capture.name, capture => capture.index, StringComparer.Ordinal);
            int heapWords = plan.Closure.Captures.Length + (construction?.Constructions.Sum(item => item.HeapWords) ?? 0);
            owner.EmitClosureAllocation(heapWords, Math.Max(plan.LiveX0 ? 1 : 0, construction?.Live ?? 0), 1);
            if (plan.MoveResult)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    XRegisterTag,
                    1
                );
            }

            EmitGeneralClosure(
                plan.Closure.Function,
                emitCapturedFailure: true,
                heapChecked: true,
                destination: plan.OnStack ? remap[plan.Destination] : plan.Destination,
                x0Capture: plan.X0Capture,
                destinationTag: plan.OnStack ? YRegisterTag : XRegisterTag
            );
            variables = previousVariables;
            entryCaptureOrder = previousOrder;
        }
    }
}
