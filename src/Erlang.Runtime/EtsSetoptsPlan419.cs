namespace Erlang;

internal sealed record EtsSetoptsPlan419(
    bool ChangeHeir,
    Pid? Heir,
    Term? Gift,
    RuntimeEtsProtection? Protection
)
{
    internal static EtsSetoptsPlan419 Parse(ProcessRuntime runtime, Term options, CancellationToken cancellation)
    {
        bool heirSeen = false, protectionSeen = false;
        Pid? heir = null;
        Term? gift = null;
        RuntimeEtsProtection? protection = null;
        Term rest = options is TupleTerm tuple ? Term.List(tuple) : options;
        while (rest is Cons cell)
        {
            cancellation.ThrowIfCancellationRequested();
            if (cell.Head is not TupleTerm option || option.Items.Count < 2 || option.Items[0] is not Atom name) throw Bad();
            if (name.Name == EtsHeirNames419.Heir)
            {
                if (heirSeen) throw Bad();
                heirSeen = true;
                if (option.Items.Count == 2 && option.Items[1] is Atom { Name: EtsHeirNames419.None })
                {
                    heir = null;
                    gift = null;
                }
                else if (option.Items.Count is 2 or 3 && option.Items[1] is Pid pid && runtime.IsEtsInternalPid419(pid))
                {
                    heir = pid;
                    gift = option.Items.Count == 3 ? option.Items[2] : null;
                }
                else throw Bad();
            }
            else if (name.Name == EtsHeirNames419.Protection)
            {
                if (protectionSeen || option.Items.Count != 2) throw Bad();
                protectionSeen = true;
                protection = option.Items[1] switch
                {
                    Atom { Name: RuntimeEtsCoreNames407.Private } => RuntimeEtsProtection.Private,
                    Atom { Name: RuntimeEtsCoreNames407.Protected } => RuntimeEtsProtection.Protected,
                    Atom { Name: RuntimeEtsCoreNames407.Public } => RuntimeEtsProtection.Public,
                    _ => throw Bad()
                };
            }
            else throw Bad();
            rest = cell.Tail;
        }
        if (rest is not Nil) throw Bad();

        return new(
            heirSeen,
            heir,
            gift,
            protection
        );
    }

    private static RuntimeEtsCoreFailure407 Bad() => new(RuntimeEtsCoreCause407.None);
}
