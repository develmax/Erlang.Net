namespace Erlang.Compiler;

internal static class NativeRecordTypeLint
{
    private static readonly HashSet<string> Nullary = ["any", "arity", "atom", "binary", "bitstring", "bool", "boolean", "byte", "char", "dynamic", "float", "function", "identifier", "integer", "iodata", "iolist", "list", "map", "maybe_improper_list", "mfa", "module", "neg_integer", "nil", "no_return", "node", "non_neg_integer", "none", "nonempty_binary", "nonempty_bitstring", "nonempty_list", "nonempty_maybe_improper_list", "nonempty_string", "number", "pid", "port", "pos_integer", "record", "reference", "string", "term", "timeout", "tuple"];

    public static void Validate(string name, int arity, int offset)
    {
        bool valid = arity == 0 && Nullary.Contains(name) || arity == 1 && name is "list" or "nonempty_list" || arity == 2 && name is "maybe_improper_list" or "nonempty_improper_list" or "nonempty_maybe_improper_list";
        if (!valid)
            throw new CompileException(CompilerDiagnosticCodes.RecordDefinition, NativeRecordDiagnostics.UndefinedType(name, arity), offset);
    }
}
