using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedGuardedTailCall(Clause clause)
        {
            if (!owner.SsaTypeInferenceEnabled || clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Case
                {
                    Value: Expr.Call selector,
                    Clauses:
                    [
                    {
                        Patterns: [Pattern.Tuple
                        {
                            Items: [Pattern.Literal { Value: Atom tag }, Pattern.Tuple payload]
                        }],
                        Guard: Expr.Binary
                        {
                            Operator: ErlangOperators.ExactEqual,
                            Left: Expr.Binary
                            {
                                Operator: ErlangOperators.BitwiseAnd,
                                Left: Expr.Variable tested,
                                Right: var mask
                            },
                            Right: var expected
                        },
                        Body: Expr.Call tail
                    },
                    {
                        Patterns: [Pattern.Tuple
                        {
                            Items: [Pattern.Literal { Value: Atom fallbackTag },
                                    Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                        }],
                        Guard: null,
                        Body: var fallbackValue
                    },
                    {
                        Patterns: [Pattern.Variable { Name: not VariableScopeNames.Wildcard } fallback],
                        Guard: null,
                        Body: Expr.Variable returned
                    }
                    ]
                }
                || !tag.Equals(fallbackTag) || returned.Name != fallback.Name
                || !HasTupleTag(payload, out Atom payloadTag)
                || !AttributeLiteral.TryNormalizeConstant(mask, out Term maskValue)
                || maskValue is not Integer { Value.Sign: < 0 }
                || !AttributeLiteral.TryNormalizeConstant(expected, out Term expectedValue)
                || expectedValue is not Integer
                || !AttributeLiteral.TryNormalizeConstant(fallbackValue, out Term rejected))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            if (parameters.Contains(fallback.Name) || ReadEntryCall(selector, parameters) is not { } entry
                || ReadTaggedTailFields(payload, tail, parameters) is not { Count: >= 2 } fields
                || fields.FirstOrDefault(field => field.Value == tested.Name) is not { Key: > 0 } testedField
                || ReadNestedGuardArgumentRegisters(fields, testedField, tail) is not { } registers
                || ReadEntryCall(tail, registers.Names) is not { } prepared)
            {
                return false;
            }

            int badPayload = Label();
            int unrecognized = Label();
            EmitEntryCallWithEmptyFrame(entry, parameters);
            EmitDetailRecordTest(
                0,
                2,
                tag,
                unrecognized,
                RegisterCaseSelectorType(entry, IsTuple)
            );
            owner.EmitTuplePart(0, 1, 0);
            EmitDetailRecordTest(
                0,
                payload.Items.Count,
                payloadTag,
                badPayload
            );
            owner.EmitTuplePart(0, testedField.Key, 1);
            // The record in X0 and its guard field in X1 are the only roots.
            owner.Op(GcBifTwo, LabelTag, badPayload);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.BitwiseAnd, 2));
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(maskValue);
            owner.Operand(XRegisterTag, 2);
            owner.Op(ExactEqualTest, LabelTag, badPayload);
            owner.EmitTypedRegister(2, IntegerTypeBits);
            owner.LiteralOperand(expectedValue);

            foreach (var (field, register) in registers.Projections)
            {
                owner.EmitTuplePart(0, field, register);
            }
            EmitEmptyFrameTail(prepared, CallLocation(selector, location));
            Mark(badPayload);
            owner.EmitConstant(rejected, 0);
            owner.EmitFrameReturn(0);
            Mark(unrecognized);
            owner.EmitFrameReturn(0);

            return true;
        }

        private static (string[] Names, (int Field, int Register)[] Projections)? ReadNestedGuardArgumentRegisters(
            Dictionary<int, string> fields,
            KeyValuePair<int, string> tested,
            Expr.Call tail
        )
        {
            int count = Math.Max(tail.Arguments.Count, fields.Count + 1);
            if (count > FirstReservedXRegister)
            {
                return null;
            }

            var destinations = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < tail.Arguments.Count; index++)
            {
                if (tail.Arguments[index] is Expr.Variable variable && !destinations.TryAdd(variable.Name, index))
                {
                    return null;
                }
            }

            var names = new string[count];
            names[1] = tested.Value;
            var rest = fields.Where(field => field.Key != tested.Key).OrderBy(field => field.Key).ToArray();
            var projections = new (int Field, int Register)[rest.Length];
            for (int index = 0; index < rest.Length; index++)
            {
                var (field, name) = rest[index];
                int register = destinations[name];
                if (index == rest.Length - 1)
                {
                    // The last projection can overwrite its source record.
                    register = 0;
                }
                else if (register == 0 || names[register] is not null)
                {
                    register = Array.FindIndex(names, 2, binding => binding is null);
                }

                names[register] = name;
                projections[index] = (field, register);
            }

            return (names, projections);
        }
    }
}
