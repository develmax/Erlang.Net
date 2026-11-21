using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedBinaryMeasurement(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Case decision } clause]
                || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            Expr.Case? outer = null;
            Atom? outerKey = null;
            if (owner.SsaTypeInferenceEnabled && parameters.Length > 1 && decision is
                {
                    Value: Expr.Variable outerSelector,
                    Clauses:
                    [
                        {
                            Guard: null,
                            Patterns: [Pattern.Literal { Value: Atom key }],
                            Body: Expr.Case inner
                        }
                    ]
                }
                && parameters[0] == outerSelector.Name)
            {
                outer = decision;
                outerKey = key;
                decision = inner;
            }

            int selector = decision.Value is Expr.Variable selected ? Array.IndexOf(parameters, selected.Name) : -1;
            if (selector <= 0)
            {
                return false;
            }

            if (outer is not null && outerKey is not null && selector == 1
                && TryEmitGuardedBinaryConversion(
                    outer,
                    outerKey,
                    decision,
                    parameters[selector],
                    parameters
                ))
            {
                return true;
            }

            if (decision.Clauses is not
                [
                {
                    Patterns: [Pattern.Tuple
                    {
                        Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable payload]
                    }],
                    Guard: Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module,
                        Function: AutoImportNames.IsBinary,
                        DynamicRemote: false,
                        Arguments: [Expr.Variable tested]
                    } guard,
                    Body: Expr.Tuple
                    {
                        Items:
                            [
                                Expr.Variable returned,
                                Expr.Call
                            {
                                Module: null or ErlangModuleNames.Module,
                                Function: AutoImportNames.ByteSize,
                                DynamicRemote: false,
                                Arguments: [Expr.Variable measured]
                            } measure
                            ]
                    }
                }
                ]
                || !DistinctBindings(payload)
                || parameters.Contains(payload.Name, StringComparer.Ordinal)
                || payload.Name != tested.Name || payload.Name != returned.Name || payload.Name != measured.Name
                || !owner.ResolvesErlangBif(guard) || !owner.ResolvesErlangBif(measure))
            {
                return false;
            }

            // Keep the caller's selector intact until the guard has succeeded.
            // The projected binary and its size then occupy the two result registers.
            int failure = Label();
            int outerFailure = outerKey is null ? 0 : EmitBinaryCaseSelection(outerKey);
            EmitDetailRecordTest(
                selector,
                2,
                tag,
                failure
            );
            owner.EmitTuplePart(selector, 1, 0);
            owner.Op(IsBinary, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.EmitCallLine(measure, location);
            owner.Op(GcBifOne, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, measure.Function, 1));
            owner.EmitTypedRegister(0, MatchableTypeBits, unit: BitSyntaxDefaults.BinaryUnit);
            owner.Operand(XRegisterTag, 1);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.instructions.WriteByte(Return);

            EmitBinaryCaseFailure(decision, failure, selector);
            if (outer is not null)
            {
                EmitBinaryCaseFailure(outer, outerFailure, 0);
            }

            return true;
        }

        private bool TryEmitGuardedBinaryConversion(
            Expr.Case outer,
            Atom outerKey,
            Expr.Case decision,
            string selected,
            string[] parameters
        )
        {
            if (decision.Clauses is not
                [
                {
                    Patterns: [Pattern.Variable payload],
                    Guard: Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module,
                        Function: AutoImportNames.IsBinary,
                        DynamicRemote: false,
                        Arguments: [Expr.Variable tested]
                    } guard,
                    Body: Expr.Tuple
                    {
                        Items: [Expr.Literal { Value: Atom preservedTag }, Expr.Variable returned]
                    }
                },
                {
                    Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Guard: null,
                    Body: Expr.Tuple
                    {
                        Items:
                            [
                                Expr.Literal { Value: Atom convertedTag },
                                Expr.Call
                            {
                                Module: null,
                                DynamicRemote: false,
                                Arguments: [Expr.Variable input]
                            } convert
                            ]
                    }
                }
                ]
                || !DistinctBindings(payload)
                || parameters.Contains(payload.Name, StringComparer.Ordinal)
                || payload.Name != tested.Name || payload.Name != returned.Name
                || selected != input.Name
                || !preservedTag.Equals(convertedTag)
                || !owner.ResolvesErlangBif(guard)
                || !entries.TryGetValue((convert.Function, 1), out int target))
            {
                return false;
            }

            int conversion = Label();
            int outerFailure = EmitBinaryCaseSelection(outerKey);
            owner.Op(IsBinary, LabelTag, conversion);
            owner.Operand(XRegisterTag, 1);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(preservedTag);
            owner.Operand(XRegisterTag, 1);
            owner.instructions.WriteByte(Return);

            // Only the fallback needs a continuation across the ordinary call.
            Mark(conversion);
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Op(Move, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitCallLine(convert, location);
            owner.EmitFieldCall(1, target);
            EmitTaggedRegisterResult(convertedTag);
            owner.EmitFrameReturn(0);

            EmitBinaryCaseFailure(outer, outerFailure, 0);

            return true;
        }

        private int EmitBinaryCaseSelection(Atom key)
        {
            int failure = Label();
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(key);

            return failure;
        }

        private void EmitBinaryCaseFailure(Expr.Case decision, int failure, int selector)
        {
            Mark(failure);
            if (decision.SourceLine != 0)
            {
                owner.EmitLine(new BeamSourceLocation(decision.PreprocessorFile ?? location.File, decision.SourceLine));
            }

            owner.Op(CaseEnd, XRegisterTag, selector);
        }
    }
}
