using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitComputedLiteralDispatch(FunctionDefinition function)
        {
            var clauses = function.Clauses;
            if (function.Arity != 1 || clauses.Count < 3
                || clauses[^1] is not { Guard: null, Patterns: [Erlang.Pattern.Variable] } fallback
                || fallback.Body is Expr.Variable
                || fallback.Body is Expr.Call error && IsConstantErrorCall(error)
                || AttributeLiteral.TryNormalizeConstant(fallback.Body, out _)
                || TryReadTupleError(fallback, out _, out _) || ReadArgumentListError(fallback) is not null
                || !HasLiteralDispatchKeys(clauses.SkipLast(1)))
            {
                return false;
            }

            int info = owner.nextLabel - 2;
            owner.EmitLiteralDispatch(
                clauses,
                location,
                emitComputedFallback: () =>
                {
                    if (!TryEmitBinaryPrefixCall(fallback) && !TryEmitFramelessDispatchFallback(fallback))
                    {
                        EmitGeneralFunction(function with { Clauses = [fallback] }, info);
                    }
                }
            );

            return true;
        }

        private bool TryEmitFramelessDispatchFallback(Clause clause)
        {
            if (clause.Body is Expr.Tuple tuple && tuple.Items.Any(item => item is Expr.Variable)
                && tuple.Items.All(item => item is Expr.Variable or Expr.Literal { Value: Atom or Nil }
                    || item is Expr.Literal { Value: Integer integer } && integer.Value >= int.MinValue && integer.Value <= int.MaxValue)
                && owner.TryEmitConstructor(clause))
            {
                owner.instructions.WriteByte(Return);

                return true;
            }

            if (clause.Body is not Expr.Call call || clause.Patterns is not [Erlang.Pattern.Variable parameter])
            {
                return false;
            }

            if (ReadEntryCall(call, [parameter.Name], allowControlTransfer: true) is { } entry)
            {
                EmitEntryCallArguments(entry);
                owner.EmitCallLine(call, location);
                int arity = call.Arguments.Count;
                if (entry.Module is { } module)
                {
                    owner.Op(TailExternalCall, BeamWire.UnsignedTag, arity);
                    owner.Operand(BeamWire.UnsignedTag, owner.Import(module, call.Function, arity));
                }
                else
                {
                    owner.Op(TailLocalCall, BeamWire.UnsignedTag, arity);
                    owner.Operand(LabelTag, entries[(call.Function, arity)]);
                }

                return true;
            }

            var inputs = new Dictionary<string, int>(StringComparer.Ordinal);
            if (parameter.Name != VariableScopeNames.Wildcard)
            {
                inputs.Add(parameter.Name, 0);
            }

            return TryEmitRegisterConstructedArgument(call, inputs, allowControlTransfer: true);
        }

        private bool TryEmitBinaryPrefixCall(Clause clause)
        {
            if (clause.Patterns is not [Erlang.Pattern.Variable parameter]
                || clause.Body is not Expr.Bits bits
                || bits.Segments.Count < 2
                || bits.Segments[^1].Value is not Expr.Call
                {
                    Module: null or ErlangModuleNames.Module,
                    DynamicRemote: false,
                    Function: AutoImportNames.AtomToBinary or AutoImportNames.IntegerToBinary
                        or AutoImportNames.ListToBinary or AutoImportNames.IolistToBinary,
                    Arguments: [Expr.Variable argument]
                } call
                || argument.Name != parameter.Name || parameter.Name == VariableScopeNames.Wildcard
                || call.Module is null && (entries.ContainsKey((call.Function, 1))
                    || owner.ResolveExternalModule(call.Function, 1) != ErlangModuleNames.Module))
            {
                return false;
            }

            var segments = ReadBinaryConstruction(bits);
            if (segments.Count < 2 || segments[^1] is not
                {
                    Kind: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit,
                    Size: Expr.Literal { Value: Atom { Name: BitSizeAtoms.All } }
                } suffix || segments.Take(segments.Count - 1).Any(segment => segment.Bytes is null))
            {
                return false;
            }

            byte[] prefix = segments.Take(segments.Count - 1).SelectMany(segment => segment.Bytes!).ToArray();
            var combined = segments[0] with
            {
                Bytes = prefix,
                Size = new Expr.Literal(Term.I(prefix.Length))
            };
            EmitFrameAllocation(1, 0);
            owner.EmitCallLine(call, location);
            owner.EmitStaticRemoteCall(call with { Module = ErlangModuleNames.Module });
            EmitBinaryConstructionHeader(
                bits,
                [combined, suffix with { Number = 2 }],
                1,
                failure: null,
                previousLine: CallLocation(call, location)
            );
            EmitBinaryConstructionSegment(combined);
            EmitBinaryConstructionSegment(suffix with { Number = 2 });
            owner.EmitTypedRegister(0, MatchableTypeBits, unit: BitSyntaxDefaults.BinaryUnit);
            owner.LiteralOperand(Term.A(BitSizeAtoms.All));
            owner.EmitFrameReturn(0);

            return true;
        }
    }
}
