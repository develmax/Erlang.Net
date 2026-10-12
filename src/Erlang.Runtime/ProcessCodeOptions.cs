namespace Erlang;

internal sealed record ProcessCodeOptions(bool Async, Term RequestId)
{
    public static ProcessCodeOptions Parse(Term options)
    {
        bool async = false;
        Term requestId = Nil.Value;
        while (options is Cons item)
        {
            if (item.Head is not TupleTerm { Items.Count: 2 } option)
                throw new ErlangException(ProcessCodeCheckNames.BadOption);
            switch (option.Items[0])
            {
                case Atom { Name: ProcessCodeCheckNames.AsyncOption }:
                    async = true;
                    requestId = option.Items[1];
                    break;
                case Atom { Name: ProcessCodeCheckNames.AllowGcOption } when option.Items[1] is Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False }:
                    break;
                default:
                    throw new ErlangException(ProcessCodeCheckNames.BadOption);
            }
            options = item.Tail;
        }
        if (options is not Nil)
            throw new ErlangException(ProcessCodeCheckNames.BadOption);

        return new ProcessCodeOptions(async, requestId);
    }
}
