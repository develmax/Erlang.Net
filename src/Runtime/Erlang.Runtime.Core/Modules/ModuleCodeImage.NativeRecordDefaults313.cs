namespace Erlang;

public sealed partial class ModuleCodeImage
{
    private PreparedNativeRecordDefaults313? nativeRecordDefaults313;

    internal void BindNativeRecordDefaults313(PreparedNativeRecordDefaults313 defaults)
    {
        if (LiteralDomain819 is null || Interlocked.CompareExchange(ref nativeRecordDefaults313, defaults, null) is not null)
            throw new InvalidOperationException(SourceLiteral819Names.Invalid);
    }

    internal bool HasPreparedNativeRecordDefinition323(NativeRecordDefinition definition, SourceCodeLease lease)
    {
        if (!ReferenceEquals(lease.Image, this) || LiteralDomain819 is null || lease.LiteralLease800 is not { } literalLease)
            return false;
        if (SourceLiteralView819 is { } source)
            return source.OwnsNativeRecordDefinition323(definition, this, literalLease);

        return Volatile.Read(ref nativeRecordDefaults313)?.OwnsDefinition323(definition, this, literalLease) == true;
    }

    internal Term ReadNativeRecordDefault313(Term template, ModuleLiteralLease800? lease)
    {
        if (LiteralDomain819 is null) return template;
        if (lease is null) throw new InvalidOperationException(SourceLiteral819Names.Invalid);
        if (SourceLiteralView819 is { } source) return source.Read819(template, this, lease);
        PreparedNativeRecordDefaults313 defaults = Volatile.Read(ref nativeRecordDefaults313)
            ?? throw new InvalidOperationException(SourceLiteral819Names.Invalid);

        return defaults.Read(template, this, lease);
    }
}
