namespace Erlang;

internal sealed record ProcessSuspensionOptions(bool Async, bool UnlessSuspending, Term? Tag)
{
    public static ProcessSuspensionOptions Parse(Term value)
    {
        bool async = false;
        bool unless = false;
        Term? tag = null;
        while (value is Cons item)
        {
            switch (item.Head)
            {
                case Atom { Name: ProcessSuspensionNames.Asynchronous }:
                    async = true;
                    break;
                case Atom { Name: ProcessSuspensionNames.UnlessSuspending }:
                    unless = true;
                    break;
                case TupleTerm pair when pair.Items.Count == 2 && pair.Items[0].Equals(Term.A(ProcessSuspensionNames.Asynchronous)):
                    async = true;
                    tag = pair.Items[1];
                    break;
                default:
                    throw new ErlangException(ProcessSuspensionNames.BadOption);
            }
            value = item.Tail;
        }
        if (value is not Nil)
            throw new ErlangException(ProcessSuspensionNames.BadOption);

        return new(async, unless, tag);
    }
}
