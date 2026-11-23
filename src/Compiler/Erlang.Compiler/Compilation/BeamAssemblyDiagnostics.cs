namespace Erlang.Compiler;

internal static class BeamAssemblyDiagnostics
{
    internal static CompileException InvalidModule =>
        new(CompilerDiagnosticCodes.Syntax, "The assembly module, filename and abstract companion must name the same module.", 0);

    internal static CompileException InvalidHeader =>
        new(CompilerDiagnosticCodes.Syntax, "Expected a BEAM assembly module, export list, attributes and label count.", 0);

    internal static CompileException InvalidFunctions =>
        new(CompilerDiagnosticCodes.Syntax, "The BEAM assembly function entries, labels or exports are inconsistent.", 0);

    internal static CompileException InvalidTerm(int offset) =>
        new(CompilerDiagnosticCodes.Syntax, "Expected a literal BEAM assembly term.", offset);

    internal static NotSupportedException Instruction(string name, int arity) =>
        new(FormattableString.Invariant($"Unsupported BEAM assembly instruction {name}/{arity}."));

    internal static NotSupportedException Operand(Term operand) =>
        new($"Unsupported BEAM assembly operand {operand}.");

    internal static CompileException InvalidImage(string detail) =>
        new(CompilerDiagnosticCodes.Syntax, $"Invalid BEAM assembly: {detail}", 0);
}
