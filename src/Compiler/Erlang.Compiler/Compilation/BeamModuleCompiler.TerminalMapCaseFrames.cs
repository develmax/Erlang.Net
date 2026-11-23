using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitPlannedTerminalMapCase(
            FunctionDefinition function,
            Expr.Case decision,
            string[] parameters,
            int selector,
            bool terminalTupleResult = false,
            TerminalTupleCaseEntry? tupleEntry = null
        )
        {
            if (decision.Clauses is not
                [
                { Guard: null, Patterns: [MapPattern { Fields.Count: > 0 } map] } matching,
                { Guard: null, Patterns: [var remainingPattern] } remaining
                ]
                || parameters.Length + map.Fields.Count >= FirstReservedXRegister)
            {
                return false;
            }

            bool fallbackOnNonMap = remainingPattern is global::Erlang.Pattern.Variable
            { Name: VariableScopeNames.Wildcard };
            int dynamicKey = -1;
            if (fallbackOnNonMap)
            {
                if (map.Fields is not [{ Key: Expr.Variable key }] || key.Name == VariableScopeNames.Wildcard
                    || (dynamicKey = Array.IndexOf(parameters, key.Name)) < 0)
                {
                    return false;
                }
            }
            else if (remainingPattern is not MapPattern { Fields.Count: 0 })
            {
                return false;
            }

            var names = parameters.ToHashSet(StringComparer.Ordinal);
            var keys = new HashSet<Term>();
            var fields = new List<(Term? Key, string Binding)>();
            foreach (var field in map.Fields)
            {
                if (field.Value is not Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding
                    || !names.Add(binding.Name))
                {
                    return false;
                }

                Term? constant = null;
                if (dynamicKey < 0)
                {
                    if (!AttributeLiteral.TryNormalizeConstant(field.Key, out Term normalized) || !keys.Add(normalized))
                    {
                        return false;
                    }

                    constant = normalized;
                }

                fields.Add((constant, binding.Name));
            }

            if (dynamicKey < 0)
            {
                fields.Sort((left, right) => TermOrder.Compare(left.Key!, right.Key!, exact: true));
            }
            string[] withFields = [.. parameters, .. fields.Select(field => field.Binding)];
            var matched = LowerTerminalCaseArm(null, matching.Body, withFields);
            var fallback = LowerTerminalCaseArm(
                null,
                remaining.Body,
                parameters,
                allowFunctionValueCalls: fallbackOnNonMap
            );
            if (matched is null || fallback is null || !matched.RequiresFrame && !fallback.RequiresFrame)
            {
                return false;
            }

            int successRegister = parameters.Length;
            if (terminalTupleResult && dynamicKey >= 0
                && !matched.Operations.Any(operation => operation.Uses.Contains(selector)))
            {
                // With a single extraction, failure cannot have published a
                // result. Success can reuse the dead map operand's X home.
                string[] reused = parameters.ToArray();
                reused[selector] = fields[0].Binding;
                if (LowerTerminalCaseArm(null, matching.Body, reused) is not { } compact)
                {
                    return false;
                }

                withFields = reused;
                matched = compact;
                successRegister = selector;
            }

            bool knownMap = tupleEntry?.Input != selector
                && owner.HasMapEntryFact((function.Name, function.Arity), selector);
            const int entry = 0;
            const int readFields = 1;
            const int success = 2;
            const int missingKey = 3;
            const int nonMap = 4;
            var blocks = new List<BeamFrameBlock>
            {
                new(
                    success,
                    [],
                    [],
                    RequiresFrame: matched.RequiresFrame
                ),
                new(
                    missingKey,
                    [],
                    [],
                    RequiresFrame: fallback.RequiresFrame
                )
            };
            if (knownMap)
            {
                blocks.Add(new(
                    entry,
                    [missingKey, success],
                    [],
                    RequiresFrame: false
                ));
            }
            else
            {
                blocks.Add(new(
                    entry,
                    [fallbackOnNonMap ? missingKey : nonMap, readFields],
                    [],
                    RequiresFrame: false
                ));
                blocks.Add(new(
                    readFields,
                    [missingKey, success],
                    [],
                    RequiresFrame: false
                ));
                if (!fallbackOnNonMap)
                {
                    blocks.Add(new(
                        nonMap,
                        [],
                        [],
                        RequiresFrame: false
                    ));
                }
            }

            var frames = BeamFramePlacement.Place(entry, blocks);
            if (frames is null || frames.Owners[entry] is not null
                || AllocateTerminalCaseRegisters(matched, withFields) is not { } accepted
                || AllocateTerminalCaseRegisters(fallback, parameters) is not { } alternative)
            {
                return false;
            }

            int missing = Label();
            int failed = knownMap ? 0 : fallbackOnNonMap ? missing : Label();
            int entryMiss = 0;
            if (tupleEntry is { } checkedEntry)
            {
                entryMiss = Label();
                owner.Op(IsTaggedTuple, LabelTag, entryMiss);
                owner.Operand(XRegisterTag, checkedEntry.Input);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.LiteralOperand(checkedEntry.Tag);
                EmitTerminalProjection(new(
                    checkedEntry.Input,
                    XRegisterTag,
                    checkedEntry.Input,
                    GetTupleElement,
                    1
                ));
            }

            if (!knownMap)
            {
                owner.Op(IsMap, LabelTag, failed);
                owner.Operand(XRegisterTag, selector);
            }

            owner.Op(GetMapElements, LabelTag, missing);
            owner.EmitTypedRegister(selector, MapTypeBits);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, fields.Count * 2);
            for (int index = 0; index < fields.Count; index++)
            {
                if (dynamicKey >= 0)
                {
                    owner.Operand(XRegisterTag, dynamicKey);
                }
                else
                {
                    owner.LiteralOperand(fields[index].Key!);
                }
                owner.Operand(XRegisterTag, successRegister + index);
            }

            EmitTerminalCaseArm(accepted, frames.Owners[success] is not null);
            Mark(missing);
            EmitTerminalCaseArm(alternative, frames.Owners[missingKey] is not null);
            if (!knownMap && !fallbackOnNonMap)
            {
                Mark(failed);
                BeamSourceLocation previous = location;
                owner.EmitArithmeticLine(decision.SourceLine, ref previous);
                owner.Op(CaseEnd, XRegisterTag, selector);
            }

            if (tupleEntry is { } original)
            {
                Mark(entryMiss);
                owner.MoveToX0(XRegisterTag, original.Input);
                owner.instructions.WriteByte(Return);
            }

            return true;
        }
    }
}
