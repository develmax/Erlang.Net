namespace Erlang.Compiler;

internal static class CompilerDiagnosticCodes
{
    public const string InvalidLiteral = "ERL001";
    public const string Syntax = "ERL002";
    public const string UnsupportedSyntax = "ERL003";
    public const string InvalidPattern = "ERL004";
    public const string FunctionDefinition = "ERL005";
    public const string VariableBinding = "ERL006";
    public const string IllegalGuard = "ERL007";
    public const string EmbeddedExpression = "ERL008";
}

internal static class LexerDiagnostics
{
    public const string UnsupportedEscape = "Hex, control and octal escapes are not implemented yet";
    public const string UnterminatedQuotedLiteral = "Unterminated quoted literal";
    public const string IllegalQuotedUnicode = "Illegal Unicode character in quoted literal";
}

internal static class ParserDiagnostics
{
    public const string ExpectedAtom = "Expected atom";
    public const string UnexpectedTrailingToken = "Unexpected trailing token";
    public const string ExpectedArity = "Expected arity";
    public const string ClauseNameMismatch = "Function clauses must have the same name";
    public const string ClauseArityMismatch = "Function clauses must have the same arity";
    public const string MissingModuleAttribute = "Missing -module attribute";
    public const string EmptyCase = "case needs a clause";
    public const string DynamicModuleCall = "Dynamic module calls are not supported yet";
    public const string UnaryBitSize = "Unary bit segment sizes must be parenthesized";
    public const string InvalidBitUnit = "Bit segment unit must be an integer from 1 through 256";
    public const string NumericUnitRequiresSize = "An explicit numeric segment unit requires a size";
    public const string UtfSizeOrUnit = "UTF segments must not specify a size or unit";
    public const string StringSegmentModifiers = "String segment modifiers are not supported yet";
    public const string ExpectedMapFieldOperator = "Expected '=>' or ':=' in map field";
    public const string InvalidPattern = "Invalid or unsupported pattern";

    public static string ExpectedToken(string value, string actualText) => $"Expected '{value}', found '{actualText}'";
    public static string UnsupportedAttribute(string attribute) => $"Attribute '{attribute}' is not supported yet";
    public static string UnsupportedExpression(string text) => $"Unsupported expression '{text}'";
    public static string ConflictingBitSpecifier(string category) => $"Conflicting bit segment {category} specifiers";
    public static string UnsupportedBitSpecifier(string specifier) => $"Bit segment specifier '{specifier}' is not supported yet";
}

internal static class SemanticDiagnostics
{
    public const string DuplicateFunction = "Duplicate function definition";
    public const string MapConstructionOperator = "Map construction requires '=>' fields; ':=' is for updates or patterns";
    public const string GuardOperator = "Operator is not legal in a guard";
    public const string GuardMatch = "Match is not legal in a guard";
    public const string GuardCase = "case is not legal in a guard";
    public const string GuardReceive = "receive is not legal in a guard";
    public const string GuardFun = "fun is not legal in a guard";
    public const string FunClauseArityMismatch = "fun clauses must have equal arity";
    public const string GuardDynamicCall = "Dynamic calls are not legal in guards";

    public static string UndefinedExport(string name, int arity) => $"Undefined export {name}/{arity}";
    public static string UnsafePatternVariable(string name) => $"Unsafe pattern variable '{name}'";
    public static string UnboundOrUnsafeVariable(string name) => $"Unbound or unsafe variable '{name}'";
    public static string IllegalGuardCall(string function, int arity) => $"Illegal guard call '{function}/{arity}'";
    public static string UnsafeMatchVariable(string name) => $"Unsafe match variable '{name}'";
}

internal static class BitPatternDiagnostics
{
    public const string UnsizedBinaryNotLast = "Unsized binary pattern segment must be last";
    public const string FloatLiteralOutOfRange = "Float pattern literal is outside the finite double range";
    public const string UnsupportedSegmentValue = "Bit pattern segments support variables and numeric literals only";
}

internal static class HybridDiagnostics
{
    public const string MissingExpressionTerminator = "Embedded Erlang expressions must end with 'end.'";
}
