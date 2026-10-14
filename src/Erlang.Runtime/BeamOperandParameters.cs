namespace Erlang;

internal static class BeamOperandParameters
{
    public const int MinimumCompatibleOpcode = 169;
    public const int EndOfCodeOpcode = 3;
    public const int MaximumRegister = 1024;
    public const int MaximumManagedArguments = 1 << 20;
    public const int AtomTag = 2;
    public const int XTag = 3;
    public const int YTag = 4;
    public const int LabelTag = 5;
    public const int ExtendedTag = 7;
    public const int ArgumentList = 1;
    public const int FloatRegister = 2;
    public const int AllocationList = 3;
    public const int Literal = 4;
    public const int TypedRegister = 5;
    public const string Unsigned = "unsigned";
    public const string Integer = "integer";
    public const string Atom = "atom";
    public const string Nil = "nil";
    public const string X = "x";
    public const string Y = "y";
    public const string Label = "label";
    public const string Failure = "failure";
    public const string Float = "float-register";
    public const string LiteralValue = "literal";
    public const string Count = "argument-count";
    public const string Unsupported = "Unsupported or invalid generic BEAM operand";
    public const string InvalidCode = "Invalid generic BEAM opcode or operand boundary";
}
