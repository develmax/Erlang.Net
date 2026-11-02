namespace Erlang;

public sealed partial class ModuleCodeImage
{
    private ModuleLiteralDomain800? literalDomain819;
    private PreparedSourceLiteralView819? sourceLiteralView819;
    internal ModuleLiteralDomain800? LiteralDomain819 => Volatile.Read(ref literalDomain819);
    internal PreparedSourceLiteralView819? SourceLiteralView819 => Volatile.Read(ref sourceLiteralView819);

    internal void BindLiteralDomain819(ModuleLiteralDomain800 domain)
    {
        if (Interlocked.CompareExchange(ref literalDomain819, domain, null) is not null)
            throw new InvalidOperationException(SourceLiteral819Names.Invalid);
    }

    internal void BindSourceLiteralView819(PreparedSourceLiteralView819 view)
    {
        if (LiteralDomain819 is null || Interlocked.CompareExchange(ref sourceLiteralView819, view, null) is not null)
            throw new InvalidOperationException(SourceLiteral819Names.Invalid);
    }
}
