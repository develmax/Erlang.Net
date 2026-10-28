using System.Collections.Frozen;

namespace Erlang;

// Dynamic loader literals belong to this exact code image. Runtime maps and
// equal static LitT tuples never enter this loader-only interning domain.
internal static class BeamLoaderMapKeys565
{
    internal static FrozenDictionary<int, TupleTerm> Capture(BeamLinkedCode code)
    {
        var result = new Dictionary<int, TupleTerm>();
        var dynamicPool = new List<TupleTerm>();
        foreach (BeamInstruction instruction in code.Instructions)
        {
            var operands = instruction.Operands;
            if (instruction.Definition.Name != BeamGeneralOpcodeNames.MapAssociate
                || operands.Count < PairStart || (operands.Count - PairStart) % PairWidth != 0
                || operands[4].Value != operands.Count - PairStart
                || operands[1].Kind != BeamOperandParameters.LiteralValue
                || operands[1].Value < 0 || operands[1].Value >= code.Structure.Literals.Count
                || code.Structure.Literals[(int)operands[1].Value] is not MapTerm { Entries.Count: 0 })
                continue;
            int count = (operands.Count - PairStart) / PairWidth;
            if (count > HamtViewLayout.ReleaseFlatMapLimit)
                continue;
            var entries = new List<KeyValuePair<Term, Term>>(count);
            bool literal = true;
            for (int pair = PairStart; pair < operands.Count; pair += PairWidth)
            {
                BeamOperand key = operands[pair];
                if (key.Kind is not (BeamOperandParameters.Atom or BeamOperandParameters.Integer
                    or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue))
                {
                    literal = false;
                    break;
                }
                entries.Add(new(BeamRegisterInterpreter.Read(code, null!, key), Nil.Value));
            }
            if (!literal)
                continue;
            // Canonical generic BEAM produced by OTP has unique literal keys.
            // A malformed duplicate-key instruction is not reclassified here.
            var shape = new MapTerm(entries);
            if (shape.Entries.Count != count)
                continue;
            TupleTerm keys = count == 0 ? (TupleTerm)Term.Tuple() : shape.FlatKeys426!;
            TupleTerm? interned = dynamicPool.FirstOrDefault(existing => existing.Equals(keys));
            if (interned is null)
                dynamicPool.Add(interned = keys);
            result.Add(instruction.Offset, interned);
        }

        return result.ToFrozenDictionary();
    }

    private const int PairStart = 5;
    private const int PairWidth = 2;
}
