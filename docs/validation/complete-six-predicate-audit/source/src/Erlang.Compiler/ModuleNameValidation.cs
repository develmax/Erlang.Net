using System.Text;

namespace Erlang.Compiler;

internal static class ModuleNameValidation
{
    public static void Validate(string name)
    {
        int[] characters = name.EnumerateRunes().Select(rune => rune.Value).ToArray();
        string? error;
        if (characters.Length == 0)
            error = ModuleNameDiagnostics.Empty;
        else if (characters.All(value => value is ModuleNameParameters.Space or ModuleNameParameters.NonBreakingSpace or ModuleNameParameters.SoftHyphen))
            error = ModuleNameDiagnostics.Blank;
        else if (characters.Any(value => value > ModuleNameParameters.MaximumLatin1CodePoint))
            error = ModuleNameDiagnostics.NonLatin1;
        else if (characters.Any(value => value < ModuleNameParameters.FirstControlLimit || value >= ModuleNameParameters.SecondControlStart && value < ModuleNameParameters.SecondControlLimit))
            error = ModuleNameDiagnostics.Control;
        else
            error = null;
        if (error is not null)
            throw new CompileException(CompilerDiagnosticCodes.ModuleDefinition, error, 0);
    }
}
