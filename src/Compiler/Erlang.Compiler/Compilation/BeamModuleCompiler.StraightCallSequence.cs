using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private readonly record struct CallListExtraction(int Input, int Head, int Tail);

        private EntryCallChain? ReadStraightCallSequence(
            Expr body,
            string[] parameters,
            CallListExtraction? extraction = null)
        {
            if (body is not Expr.Sequence { Items: [_, .., Expr.Call] } sequence
                || ReadStraightCalls(FlattenCallArguments(sequence.Items), parameters) is not { } calls)
            {
                return null;
            }

            var saved = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int register = 0; register < parameters.Length; register++)
            {
                if (parameters[register] != VariableScopeNames.Wildcard)
                {
                    saved.Add(parameters[register], register);
                }
            }

            var continuation = new List<(EntryCall Call, int? ListDestination)>();
            EntryCall? first = null;
            string? previous = null;
            for (int index = 0; index < calls.Length; index++)
            {
                var (target, result) = calls[index];
                if (index == 0)
                {
                    first = extraction is { } list
                        ? ReadListSequenceFirstCall(
                            calls,
                            parameters,
                            list,
                            allowSavedHead: true
                        )
                        : ReadEntryCall(target, parameters, allowConstructedArgument: true);
                    if (first is null)
                    {
                        return null;
                    }
                }
                else
                {
                    var step = ReadEntryCall(target, previous is null ? [] : [previous], parameters)
                        ?? ReadStraightConstructedCall(target, previous, saved);
                    if (step is null)
                    {
                        return null;
                    }

                    continuation.Add((step, null));
                }

                previous = result;
            }

            continuation.Reverse();

            return new EntryCallChain(
                first!,
                continuation,
                RelocateBeforeTrim: true,
                ListExtraction: extraction
            );

        }

        private EntryCall? ReadListSequenceFirstCall(
            (Expr.Call Call, string? Result)[] calls,
            string[] parameters,
            CallListExtraction list,
            bool allowSavedHead
        )
        {
            string head = parameters[list.Head];
            string tail = parameters[list.Tail];
            var firstReads = calls[0].Call.Arguments.SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Variable>()
                .Select(read => read.Name).ToHashSet(StringComparer.Ordinal);
            var laterReads = calls.Skip(1).SelectMany(item => item.Call.Arguments)
                .SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Variable>()
                .Select(read => read.Name).ToHashSet(StringComparer.Ordinal);
            if (!firstReads.Contains(head) || firstReads.Contains(tail) || !laterReads.Contains(tail)
                || !allowSavedHead && laterReads.Contains(head))
            {
                return null;
            }

            var registers = (string[])parameters.Clone();
            registers[list.Head] = VariableScopeNames.Wildcard;
            if (!laterReads.Contains(head))
            {
                registers[list.Input] = head;
            }

            return ReadEntryCall(calls[0].Call, registers, parameters);
        }

        private static int ListSequenceLive(
            EntryCall first,
            string[] parameters,
            IEnumerable<int> saved,
            CallListExtraction list
        )
        {
            var registers = (string[])parameters.Clone();
            registers[list.Head] = VariableScopeNames.Wildcard;
            registers[list.Tail] = VariableScopeNames.Wildcard;
            int highestSaved = saved.Where(register => register != list.Head && register != list.Tail).DefaultIfEmpty(-1).Max();

            return Math.Max(EntryCallLiveCount(first, registers), Math.Max(list.Input, highestSaved) + 1);
        }

        private void EmitCallListExtraction(CallListExtraction list, int headSlot, int tailSlot)
        {
            owner.EmitListParts(
                list.Input,
                headSlot < 0 ? XRegisterTag : YRegisterTag,
                headSlot < 0 ? list.Input : headSlot,
                YRegisterTag,
                tailSlot
            );
        }

        private (Expr.Call Call, string? Result)[]? ReadStraightCalls(Expr body, string[] parameters)
        {
            return body is Expr.Sequence { Items: [_, .., Expr.Call] } sequence
                ? ReadStraightCalls(sequence.Items, parameters)
                : null;
        }

        private (Expr.Call Call, string? Result)[]? ReadStraightCalls(IReadOnlyList<Expr> items, string[] parameters)
        {
            var names = parameters.ToHashSet(StringComparer.Ordinal);
            var calls = new (Expr.Call Call, string? Result)[items.Count];
            for (int index = 0; index < calls.Length; index++)
            {
                Expr expression = items[index];
                string? result = null;
                if (expression is Expr.Match { Pattern: Pattern.Variable variable, Value: Expr.Call producer })
                {
                    if (variable.Name != VariableScopeNames.Wildcard)
                    {
                        if (!names.Add(variable.Name))
                        {
                            return null;
                        }

                        result = variable.Name;
                    }

                    expression = producer;
                }

                if (expression is not Expr.Call call)
                {
                    return null;
                }

                calls[index] = (owner.ResolveImportedCall(call, entries), result);
            }

            return calls;
        }

        private EntryCall? ReadStraightConstructedCall(
            Expr.Call target,
            string? registerValue,
            IReadOnlyDictionary<string, int> saved,
            bool preserveX0 = false,
            bool allowControlTransfer = false,
            IReadOnlyList<string>? projectedRegisters = null
        )
        {
            var placeholder = target with { Arguments = target.Arguments.Select(_ => (Expr)new Expr.Literal(Nil.Value)).ToArray() };
            if (!TryResolveOrdinaryCall(placeholder, out string? module, allowControlTransfer: allowControlTransfer))
            {
                return null;
            }

            var inputs = new Dictionary<string, int>(StringComparer.Ordinal);
            if (projectedRegisters is not null)
            {
                for (int register = 0; register < projectedRegisters.Count; register++)
                {
                    string name = projectedRegisters[register];
                    if (!string.IsNullOrEmpty(name) && name != VariableScopeNames.Wildcard)
                    {
                        inputs.Add(name, register);
                    }
                }
            }

            if (registerValue is not null)
            {
                inputs.Add(registerValue, 0);
            }

            var constructions = ReadCallArgumentConstructions(
                target.Arguments,
                -1,
                saved,
                inputs: inputs,
                preserveX0: preserveX0
            );

            return constructions is null ? null : new EntryCall(
                target,
                module,
                [],
                [],
                RegisterConstructions: constructions
            );
        }
    }
}
