namespace Erlang;

internal sealed record ProcessGarbageCollectionOptions(bool Async, Term RequestId, Term Type)
{
    public static ProcessGarbageCollectionOptions Parse(Term options)
    {
        bool async = false;
        Term requestId = Nil.Value;
        Term type = Term.A(ProcessGarbageCollectionNames.Major);
        while (options is Cons item)
        {
            if (item.Head is not TupleTerm { Items.Count: 2 } option)
                throw new ErlangException(ProcessGarbageCollectionNames.BadOption);
            switch (option.Items[0])
            {
                case Atom { Name: ProcessGarbageCollectionNames.AsyncOption }:
                    async = true;
                    requestId = option.Items[1];
                    break;
                case Atom { Name: ProcessGarbageCollectionNames.TypeOption }:
                    type = option.Items[1];
                    break;
                default:
                    throw new ErlangException(ProcessGarbageCollectionNames.BadOption);
            }
            options = item.Tail;
        }
        if (options is not Nil)
            throw new ErlangException(ProcessGarbageCollectionNames.BadOption);

        return new(async, requestId, type);
    }
}
