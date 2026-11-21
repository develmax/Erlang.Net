using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedBinaryTailCase(FunctionDefinition function)
        {
            if (!CanUseEntryTypeFacts() || recursiveClosure is not null
                || owner.IsDeclaredNative((function.Name, function.Arity))
                || function.Clauses.Count is not (1 or 2)
                || function.Clauses[0] is not
                {
                    Patterns: [Pattern.Variable first, Pattern.Variable second],
                    Body: Expr.Case
                    {
                        Value: Expr.Variable binary,
                        Clauses:
                        [
                        {
                            Guard: null,
                            Patterns:
                                [
                                    BitPattern
                                {
                                    Segments:
                                        [
                                        {
                                            Value: Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                            Specification:
                                            {
                                                Type: BitSegmentTypes.Binary,
                                                Unit: BitSyntaxDefaults.BinaryUnit,
                                                Size: Expr.Variable count
                                            }
                                        },
                                        {
                                            Value: Pattern.Variable tail,
                                            Specification:
                                            {
                                                Type: BitSegmentTypes.Binary,
                                                Unit: BitSyntaxDefaults.BinaryUnit,
                                                Size: null
                                            }
                                        }
                                        ]
                                }
                                ],
                            Body: Expr.Variable returned
                        },
                        {
                            Guard: null,
                            Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                            Body: var onFailure
                        }
                        ]
                    }
                } clause
                || !HasUnconditionalParameters(clause) || !DistinctBindings(first, second, tail)
                || tail.Name != returned.Name
                || !AttributeLiteral.TryNormalizeConstant(onFailure, out Term value)
                || value is not BitString { BitLength: 0 } empty
                || ReadEntryTypeGuard(clause, clause.Guard) is not { Opcode: IsBinary, Register: var input })
            {
                return false;
            }

            string[] parameters = [first.Name, second.Name];
            int size = Array.IndexOf(parameters, count.Name);
            if (binary.Name != parameters[input] || size < 0 || size == input)
            {
                return false;
            }

            EntryCall? fallback = null;
            if (function.Clauses.Count == 2)
            {
                Clause other = function.Clauses[1];
                if (other is not
                    {
                        Patterns: [Pattern.Variable a, Pattern.Variable b],
                        Body: Expr.Call { Arguments: [Expr.Variable left, Expr.Variable right] } call
                    } || !HasUnconditionalParameters(other)
                    || ReadEntryTypeGuard(other, other.Guard) != (IsList, input)
                    || left.Name != a.Name || right.Name != b.Name
                    || ReadEntryCall(call, [a.Name, b.Name]) is not { Module: null } target)
                {
                    return false;
                }

                fallback = target;
            }

            int functionInfo = owner.nextLabel - 2;
            int shortInput = Label();
            int nonbinary = fallback is null ? functionInfo : Label();
            owner.Op(IsBinary, LabelTag, nonbinary);
            owner.Operand(XRegisterTag, input);

            // A successful binary guard proves byte alignment. The wildcard
            // prefix consumes a multiple of a byte and has no materialized value.
            owner.Op(StartBinaryMatchKnown, AtomTag, owner.Intern(BeamBinaryNames.NoFail));
            owner.Operand(BeamWire.UnsignedTag, function.Arity);
            owner.Operand(XRegisterTag, input);
            owner.Operand(XRegisterTag, input);
            owner.Op(SkipBinaryBits, LabelTag, shortInput);
            owner.EmitTypedRegister(input, MatchableTypeBits, unit: BitSyntaxDefaults.BitStringUnit);
            var signature = (function.Name, function.Arity);
            if (owner.ReadIntegerEntryRange(signature, size) is { } bounds)
            {
                owner.EmitTypedRegister(
                    size,
                    IntegerTypeBits,
                    bounds.Lower,
                    bounds.Upper
                );
            }
            else if (owner.HasIntegerEntryFact(signature, size))
            {
                owner.EmitTypedRegister(size, IntegerTypeBits);
            }
            else
            {
                owner.Operand(XRegisterTag, size);
            }

            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BinaryUnit);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Op(GetBinaryTail, XRegisterTag, input);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, input + 1);
            owner.instructions.WriteByte(Return);

            Mark(shortInput);
            owner.EmitConstant(empty, 0);
            owner.instructions.WriteByte(Return);

            if (fallback is not null)
            {
                Mark(nonbinary);
                owner.Op(IsList, LabelTag, functionInfo);
                owner.Operand(XRegisterTag, input);
                EmitRegisterTailCall(fallback);
            }

            return true;
        }
    }
}
