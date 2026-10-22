namespace Erlang.Compiler;

public sealed class ComprehensionSource
{
    public string Owner { get; private set; }
    public int OwnerArity { get; private set; }
    public int Line { get; }
    public int Offset { get; }
    public bool TerminalBody { get; private set; }
    public IReadOnlyList<GeneratedComprehensionFunction> Functions { get; private set; }

    public ComprehensionSource(int line, int offset)
        : this(
            string.Empty,
            0,
            line,
            offset,
            []
        )
    {
    }

    public ComprehensionSource(
        string owner,
        int ownerArity,
        int line,
        int offset,
        IReadOnlyList<GeneratedComprehensionFunction> functions
    )
        : this(
            owner,
            ownerArity,
            line,
            offset,
            functions,
            false
        )
    {
    }

    public ComprehensionSource(
        string owner,
        int ownerArity,
        int line,
        int offset,
        IReadOnlyList<GeneratedComprehensionFunction> functions,
        bool terminalBody
    )
    {
        TerminalBody = terminalBody;
        Owner = owner;
        OwnerArity = ownerArity;
        Line = line;
        Offset = offset;
        Functions = functions;
    }

    internal void Bind(
        string owner,
        int arity,
        IReadOnlyList<GeneratedComprehensionFunction> functions,
        bool terminalBody = false
    )
    {
        TerminalBody = terminalBody;
        Owner = owner;
        OwnerArity = arity;
        Functions = functions;
    }
}
