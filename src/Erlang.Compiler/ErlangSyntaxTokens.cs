namespace Erlang.Compiler;

internal static class ErlangSyntaxTokens
{
    public const string OpenParenthesis = "(";
    public const string CloseParenthesis = ")";
    public const string OpenList = "[";
    public const string CloseList = "]";
    public const string OpenTuple = "{";
    public const string CloseTuple = "}";
    public const string Comma = ",";
    public const string Semicolon = ";";
    public const string FunctionArrow = "->";
    public const string ConditionalMatch = "?=";
    public const string ComprehensionSeparator = "||";
    public const string ListGenerator = "<-";
    public const string StrictListGenerator = "<:-";
    public const string BinaryGenerator = "<=";
    public const string StrictBinaryGenerator = "<:=";
    public const string FormTerminator = ".";
    public const string MapPrefix = "#";
    public const string MapAssociation = "=>";
    public const string MapExactField = ":=";
    public const string BinaryOpen = "<<";
    public const string BinaryClose = ">>";
    public const string ListTail = "|";
    public const string ModuleQualifier = ":";
    public const string AttributeIntroducer = "-";
    public const string AritySeparator = "/";
}
