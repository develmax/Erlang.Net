

namespace Erlang.Compiler;

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
