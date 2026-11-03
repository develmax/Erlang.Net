namespace Erlang;

internal sealed partial class ProcessTermFrame506
{
    private readonly Dictionary<int, ModuleLiteralRoots801> literalSlots801 = new();

    // Enumerated only while the actual owning heap gate is held. This is not
    // code admission and cannot create a module-image lease from a bare Term.
    internal IEnumerable<ModuleLiteralRoots801> ModuleLiteralAuthorities801 => literalSlots801.Values;

    private void ClearLiteralSlot801(int slot)
    {
        if (literalSlots801.Remove(slot, out ModuleLiteralRoots801? old)) old.Dispose();
    }

    private void ClearLiteralRoots801()
    {
        ModuleLiteralRoots801[] old = literalSlots801.Values.ToArray();
        literalSlots801.Clear();
        List<Exception>? failures = null;
        foreach (ModuleLiteralRoots801 item in old)
            try
            {
                item.Dispose();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
        if (failures is not null) throw new AggregateException(failures);
    }
}
