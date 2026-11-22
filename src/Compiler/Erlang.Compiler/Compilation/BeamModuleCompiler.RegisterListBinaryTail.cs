using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record RegisterListBinaryTail(
            Expr.Call Call,
            Expr.Bits Bits,
            List<BinaryConstructionSegment> Segments
        );

        private RegisterListBinaryTail? ReadRegisterListBinaryTail(Expr body, Pattern.Variable[] parameters)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || parameters.Length is < 3 or > 5
                || parameters.Any(parameter => parameter.Name == VariableScopeNames.Wildcard)
                || body is not Expr.Call
                {
                    Module: null, DynamicRemote: false, Arguments: [Expr.Bits bits, ..]
                } call || call.Arguments.Count != parameters.Length - 1
                || !entries.ContainsKey((call.Function, call.Arguments.Count))
                || bits.Segments is not [var first, var second]
                || !ReadSegment(first, out string firstName) || !ReadSegment(second, out string secondName)
                || firstName == secondName
                || !call.Arguments.Skip(1).SequenceEqual(parameters.Skip(2).Select(parameter => new Expr.Variable(parameter.Name))))
            {
                return null;
            }

            return new(call, bits, ReadBinaryConstruction(bits));

            bool ReadSegment(BitSegment segment, out string name)
            {
                if (segment is
                    {
                        Type: BitSegmentTypes.Binary, Size: null, Unit: BitSyntaxDefaults.BinaryUnit, Endian: BitByteOrders.Big,
                        Signed: false, IsStringLiteral: false, Value: Expr.Variable value
                    } && (value.Name == parameters[0].Name || value.Name == parameters[1].Name))
                {
                    name = value.Name;

                    return true;
                }

                name = string.Empty;

                return false;
            }
        }

        private void EmitRegisterListBinaryTail(
            RegisterListBinaryTail plan,
            IReadOnlyDictionary<string, int> registers,
            int live
        )
        {
            EmitBinaryConstructionHeader(
                plan.Bits,
                plan.Segments,
                live,
                null
            );
            foreach (var segment in plan.Segments)
            {
                EmitBinaryConstructionSegment(segment);
                owner.Operand(XRegisterTag, registers[((Expr.Variable)segment.Value).Name]);
                owner.LiteralOperand(((Expr.Literal)segment.Size).Value);
            }

            // The proper-list cursor owns X2. All forwarded heads start at X4,
            // above their outgoing destinations, so the copies are independent.
            var forwarded = plan.Call.Arguments.Skip(1).Cast<Expr.Variable>()
                .Select((variable, index) => (Destination: index + 1, Source: registers[variable.Name])).ToList();
            owner.EmitOrderedTailMoves(forwarded, []);
            EmitRegisterTailTarget(plan.Call, plan.Call.Arguments.Count);
        }
    }
}
