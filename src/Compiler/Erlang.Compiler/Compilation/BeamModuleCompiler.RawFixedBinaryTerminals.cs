using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRawFixedBinaryTerminals(FunctionDefinition function)
        {
            if (!CanUseEntryTypeFacts() || recursiveClosure is not null
                || owner.SourceModule?.DeclaredNifs is { Count: > 0 }
                || function.Clauses is not
                [
                { Guard: null, Patterns: [BitPattern { Segments.Count: > 0 } pattern, ..] } hot,
                { Guard: null } cold
                ] || !HasUnconditionalParameters(cold)
                || hot.Patterns.Skip(1).Any(parameter => parameter is not global::Erlang.Pattern.Variable))
            {
                return false;
            }

            string[] hotInputs =
            [
                VariableScopeNames.Wildcard,
                .. hot.Patterns.Skip(1).Cast<Pattern.Variable>().Select(binding => binding.Name)
            ];
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (hotInputs.Skip(1).Where(name => name != VariableScopeNames.Wildcard).Any(name => !names.Add(name)))
            {
                return false;
            }

            var captures = new Dictionary<string, int>(StringComparer.Ordinal);
            int bits = 0;
            for (int index = 0; index < pattern.Segments.Count; index++)
            {
                BitPatternSegment segment = pattern.Segments[index];
                if (index == pattern.Segments.Count - 1 && segment is
                    {
                        Value: Pattern.Variable { Name: VariableScopeNames.Wildcard },
                        Specification: { Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BitStringUnit, Size: null }
                    })
                {
                    break;
                }

                BitSegment field = segment.Specification;
                if (field.Type != BitSegmentTypes.Integer || field.Endian is not (BitByteOrders.Big or BitByteOrders.Little)
                    || FixedSize(field) is not int size || size == 0 || size * field.Unit > int.MaxValue - bits)
                {
                    return false;
                }

                int width = size * field.Unit;
                bits += width;
                if (segment.Value is Pattern.Variable binding)
                {
                    if (!IsWildcard(binding))
                    {
                        int destination = function.Arity + captures.Count;
                        if (destination >= FirstReservedXRegister || !names.Add(binding.Name))
                        {
                            return false;
                        }

                        captures.Add(binding.Name, destination);
                    }
                }
                else if (ReadFixedBitEquality(segment, width) is null)
                {
                    return false;
                }
            }

            if (bits == 0)
            {
                return false;
            }

            hotInputs = [.. hotInputs, .. captures.OrderBy(capture => capture.Value).Select(capture => capture.Key)];
            Expr result = hot.Body is Expr.RecordConstruction record ? RecordExpressions.Create(record) : hot.Body;
            EntryConstructedArgument? resultConstruction = null;
            if (!AttributeLiteral.TryNormalizeConstant(result, out Term hotLiteral))
            {
                if (result is not Expr.Tuple
                    || ReadEntryConstructedArgument(result, 0, hotInputs) is not { IsTuple: true } tuple)
                {
                    return false;
                }

                resultConstruction = tuple;
            }

            EntryCall? raised = null;
            if (!AttributeLiteral.TryNormalizeConstant(cold.Body, out Term coldLiteral))
            {
                string[] coldInputs =
                [
                    VariableScopeNames.Wildcard,
                    .. cold.Patterns.Skip(1).Cast<Pattern.Variable>().Select(binding => binding.Name)
                ];
                if (cold.Body is not Expr.Call { Function: AutoImportNames.Throw, Arguments.Count: 1 } call
                    || !owner.ResolvesErlangBif(call)
                    || ReadEntryCall(
                        call,
                        coldInputs,
                        allowConstructedArgument: true,
                        allowControlTransfer: true
                    )
                        is not { Module: ErlangModuleNames.Module } throwCall)
                {
                    return false;
                }

                raised = throwCall;
            }

            if (!HasBinaryLocalArgument(function, binaryRecursion: null, materializedBitstringOnly: true))
            {
                return false;
            }

            int failure = Label();
            EmitFixedBitPattern(
                pattern,
                0,
                failure,
                captures,
                rawEntryLive: function.Arity
            );
            if (resultConstruction is null)
            {
                owner.EmitConstant(hotLiteral, 0);
            }
            else
            {
                EmitTerminalBitTuple(resultConstruction);
            }

            owner.instructions.WriteByte(Return);
            Mark(failure);
            if (raised is null)
            {
                owner.EmitConstant(coldLiteral, 0);
                owner.instructions.WriteByte(Return);
            }
            else
            {
                if (raised.ConstructedArgument is { } reason)
                {
                    EmitTerminalBitHeap(reason);
                }

                EmitEntryCallArguments(raised);
                EmitRegisterTailTarget(raised.Call with { Module = raised.Module }, raised.Call.Arguments.Count);
            }

            return true;
        }

        private void EmitTerminalBitTuple(EntryConstructedArgument tuple)
        {
            EmitTerminalBitHeap(tuple);
            EmitEntryConstruction(tuple);
        }

        private void EmitTerminalBitHeap(EntryConstructedArgument term)
        {
            int live = term.Operands.Max(operand => operand.Source.GetValueOrDefault(-1)) + 1;
            owner.Op(TestHeap, BeamWire.UnsignedTag, term.HeapWords);
            owner.Operand(BeamWire.UnsignedTag, live);
        }
    }
}
