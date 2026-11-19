using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private HashSet<int> ReadLocationNeutralExternalImports()
    {
        if (!atoms.TryGetValue(ErlangModuleNames.Module, out int erlang))
        {
            return [];
        }

        var atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();

        return imports.Where(entry => entry.Key.Module == erlang
            && IsSafeExternalBif(atomNames[entry.Key.Function - 1], entry.Key.Arity))
            .Select(entry => entry.Value).ToHashSet();
    }

    private bool CanDropSafeCallLine(
        IReadOnlyList<BeamInstruction> body,
        int call,
        HashSet<int> safeImports
    )
    {
        if (!IsSafeExternalBifCall(body[call], safeImports))
        {
            return false;
        }

        if (body[call].Definition.Opcode != ExternalCall)
        {
            return true;
        }

        // An adjacent throwing call may have reused this location. Retain it unless
        // the fallthrough has a fresh line or reaches return through safe instructions.
        for (int next = call + 1; next < body.Count; next++)
        {
            BeamInstruction instruction = body[next];
            switch (instruction.Definition.Opcode)
            {
                case Line or Return:
                    return true;
                case Move or Swap or TestHeap or PutList or PutTuple or InitYRegisters or Deallocate:
                    continue;
                case PutMapAssoc when instruction.Operands is
                    [
                    { Kind: BeamOperandParameters.Failure },
                    { Kind: BeamOperandParameters.LiteralValue } map,
                        ..,
                    ] && ExternalTermFormat.Decode(literalBytes[(int)map.Value]) is MapTerm:
                    continue;
                case ExternalCall when IsSafeExternalBifCall(instruction, safeImports):
                    continue;
                case TailExternalCall or TailExternalCallWithFrame:
                    return IsSafeExternalBifCall(instruction, safeImports);
                default:
                    return false;
            }
        }

        return false;
    }

    private static bool IsSafeExternalBifCall(BeamInstruction instruction, HashSet<int> safeImports) =>
        instruction.Definition.Opcode is (ExternalCall or TailExternalCall or TailExternalCallWithFrame)
        && safeImports.Contains((int)instruction.Operands[1].Value);

    // The nonthrowing BIFs from OTP 29.1.1 erl_bifs:is_safe/3.
    private static bool IsSafeExternalBif(string name, int arity) => (name, arity) switch
    {
        (AutoImportNames.Date or AutoImportNames.Get or ErlangCookieNames.Get or AutoImportNames.GroupLeader
            or AutoImportNames.IsAlive or AutoImportNames.MakeRef or AutoImportNames.Node or AutoImportNames.Nodes
            or PortNames.Ports or AutoImportNames.PreLoaded or AutoImportNames.Processes or AutoImportNames.Registered
            or AutoImportNames.Self or AutoImportNames.Time, 0) => true,
        (AutoImportNames.Get or AutoImportNames.GetKeys or AutoImportNames.IsAtom or AutoImportNames.IsBoolean
            or AutoImportNames.IsBinary or ErlangModuleNames.IsBitString or AutoImportNames.IsFloat
            or AutoImportNames.IsFunction or AutoImportNames.IsInteger or AutoImportNames.IsList
            or AutoImportNames.IsMap or AutoImportNames.IsNumber or AutoImportNames.IsPid or AutoImportNames.IsPort
            or AutoImportNames.IsRecord or AutoImportNames.IsReference or AutoImportNames.IsTuple
            or AutoImportNames.TermToBinary, 1) => true,
        (ErlangOperators.NumericNotEqual or ErlangOperators.Less or ErlangOperators.ExactNotEqual
            or ErlangOperators.ExactEqual or ErlangOperators.LessOrEqual or ErlangOperators.NumericEqual
            or ErlangOperators.Greater or ErlangOperators.GreaterOrEqual or AutoImportNames.Max
            or AutoImportNames.Min, 2) => true,
        _ => false,
    };
}
