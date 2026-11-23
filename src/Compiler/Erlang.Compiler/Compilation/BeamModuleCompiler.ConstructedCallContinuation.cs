using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitConstructedCallContinuation(Clause clause)
        {
            if (TryEmitConstructedMapCaseContinuation(clause))
            {
                return true;
            }

            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                    {
                        Pattern: Erlang.Pattern.Variable binding,
                        Value: Expr.Tuple or Expr.List
                    } assignment,
                        Expr.Call tail
                    ]
                }
                || binding.Name == VariableScopeNames.Wildcard
                || !tail.Arguments.Any(argument => argument is Expr.Tuple or Expr.List
                    && !AttributeLiteral.TryNormalizeConstant(argument, out _)))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(binding.Name, StringComparer.Ordinal)
                || FlattenCallArguments([], new Expr.Tuple([assignment.Value])) is not
                    [Expr.Match { Pattern: Erlang.Pattern.Variable result, Value: Expr.Call producer },
                        Expr.Tuple { Items: [var constructor] }]
                || ReadEntryCall(producer, parameters) is not { } first
                || !TryResolveOrdinaryCallTarget(tail, out string? module)
                || ReadUsedEntryParameters(clause with
                {
                    Body = new Expr.Tuple([constructor, .. tail.Arguments])
                }) is not { } used)
            {
                return false;
            }

            int[] saved = Enumerable.Range(0, parameters.Length).Reverse()
                .Where(register => used.Contains(parameters[register])).ToArray();
            var slots = saved.Select((register, slot) => (Name: parameters[register], Slot: slot))
                .ToDictionary(input => input.Name, input => input.Slot, StringComparer.Ordinal);
            var inputs = new Dictionary<string, int>(StringComparer.Ordinal) { [result.Name] = 0 };
            if (ReadCallArgumentConstructions(
                tail.Arguments,
                -1,
                slots,
                inputs: inputs,
                bindings: [(binding.Name, constructor)]
            ) is not { } plan)
            {
                return false;
            }

            EmitConstructedContinuationPrefix(
                first,
                parameters,
                saved,
                saved.Length,
                plan
            );
            EmitPreparedFrameTail(new EntryCall(
                tail,
                module,
                [],
                []
            ), CallLocation(producer, location), saved.Length);

            return true;
        }

        private bool TryEmitConstructedMapCaseContinuation(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding,
                        Value: Expr.Map { Base: null } map
                    },
                        Expr.Case { Value: Expr.Call call } decision
                    ]
                }
                || !TryResolveOrdinaryCallTarget(call, out string? module)
                || call.Arguments.Any(argument => argument is not Expr.Variable
                    && !AttributeLiteral.TryNormalizeConstant(argument, out _))
                || call.Arguments.OfType<Expr.Variable>().Count(argument => argument.Name == binding.Name) != 1
                || map.Fields.Where(field => field.Value is Expr.Call).ToArray() is not [var resultField])
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(binding.Name, StringComparer.Ordinal)
                || ReadUsedEntryParameters(new Clause(
                    [.. clause.Patterns, binding],
                    null,
                    decision with { Value = new Expr.Literal(Nil.Value) }
                )) is not { Count: 0 })
            {
                return false;
            }

            var producer = (Expr.Call)resultField.Value;
            EntryCall? first = ReadEntryCall(producer, parameters);
            if (first is null && producer.Arguments.Count == 0 && !producer.DynamicRemote
                && owner.ResolvesErlangBif(producer) && IsSafeExternalBif(producer.Function, 0)
                && producer.Function is not (AutoImportNames.Self or AutoImportNames.Node))
            {
                first = new EntryCall(
                    producer,
                    ErlangModuleNames.Module,
                    [],
                    []
                );
            }

            if (first is null)
            {
                return false;
            }

            const string result = "\0map-field-call-result";
            var constructor = map with
            {
                Fields = map.Fields.Select(field => ReferenceEquals(field, resultField)
                    ? field with { Value = new Expr.Variable(result) } : field).ToArray()
            };
            // The selector's other arguments are effect-free, so the map's sole
            // read can be placed directly in its outgoing argument register.
            Expr[] arguments = call.Arguments.Select(argument => argument is Expr.Variable variable
                && variable.Name == binding.Name ? constructor : argument).ToArray();
            if (ReadUsedEntryParameters(clause with { Body = new Expr.Tuple(arguments) }) is not { } used)
            {
                return false;
            }

            int[] saved = Enumerable.Range(0, parameters.Length).Reverse()
                .Where(register => used.Contains(parameters[register])).ToArray();
            var slots = saved.Select((register, slot) => (Name: parameters[register], Slot: slot))
                .ToDictionary(input => input.Name, input => input.Slot, StringComparer.Ordinal);
            var inputs = new Dictionary<string, int>(StringComparer.Ordinal) { [result] = 0 };
            if (ReadCallArgumentConstructions(
                arguments,
                -1,
                slots,
                inputs: inputs,
                allowMap: true
            ) is not { } plan)
            {
                return false;
            }

            // The planning pass records how many physical slots the real emission needs.
            peakSlot = Math.Max(peakSlot, saved.Length);
            int frame = Math.Max(frameSize, saved.Length);
            EmitConstructedContinuationPrefix(
                first,
                parameters,
                saved,
                frame,
                plan
            );
            if (saved.Length != 0)
            {
                owner.EmitSlotKills(Enumerable.Range(0, saved.Length).ToArray());
            }

            owner.EmitCallLine(call, CallLocation(producer, location));
            Call(module, call.Function, call.Arguments.Count);
            if (!TryEmitTerminalTaggedProjections(decision, frame))
            {
                EmitCase(
                    decision,
                    null,
                    inputPrepared: true,
                    reuseBranchSlots: true,
                    terminalCase: true
                );
                owner.EmitFrameReturn(frame);
            }

            return true;
        }

        private void EmitConstructedContinuationPrefix(
            EntryCall producer,
            string[] parameters,
            int[] saved,
            int frame,
            CallArgumentConstructions plan
        )
        {
            int live = Math.Max(EntryCallLiveCount(producer, parameters), saved.DefaultIfEmpty(-1).Max() + 1);
            EmitFrameAllocation(live, frame);
            EmitFrameInitialization(saved.Length, frame);
            for (int slot = 0; slot < saved.Length; slot++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    saved[slot],
                    YRegisterTag,
                    slot
                );
            }

            EmitEntryCall(producer);
            EmitCallArgumentConstructions(plan, -1);
        }
    }
}
