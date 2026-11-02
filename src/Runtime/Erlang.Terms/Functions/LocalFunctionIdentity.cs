namespace Erlang;

public sealed record LocalFunctionIdentity(
    Atom Module,
    Atom? Name,
    int Index,
    int OldUnique,
    BitString Unique,
    int? LegacyIndex = null
)
{
    private BitString unique255 = Unique is null ? null! : Unique.CopyIdentityValue255();

    // ERTS stores uniq bytes in the function entry, not a GC term edge. Keep
    // the public C# extension's full bit length while removing source custody.
    public BitString Unique
    {
        get => unique255;
        init => unique255 = value is null ? null! : value.CopyIdentityValue255();
    }

    public int OldIndex => LegacyIndex ?? Index;
}
