namespace Erlang;

internal sealed record RuntimeNodeQuery(
    bool This,
    bool Known,
    bool Info,
    bool ConnectionId,
    bool NodeType
)
{
    public static RuntimeNodeQuery Parse(Term types, Term? options)
    {
        bool includeThis = false;
        bool includeKnown = false;
        Term remaining = types is Atom ? Term.List(types) : types;
        while (remaining is Cons cell)
        {
            switch (cell.Head)
            {
                case Atom { Name: RuntimeNodeQueryNames.This }:
                    includeThis = true;
                    break;
                case Atom { Name: RuntimeNodeQueryNames.Known }:
                    includeThis = includeKnown = true;
                    break;
                case Atom { Name: RuntimeNodeQueryNames.Visible or RuntimeNodeQueryNames.Hidden or RuntimeNodeQueryNames.Connected }:
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            remaining = cell.Tail;
        }
        if (remaining is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        bool connectionId = false;
        bool nodeType = false;
        if (options is not null)
        {
            if (options is not MapTerm map)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            foreach (var pair in map.Entries)
            {
                if (pair.Key is not Atom key || pair.Value is not Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False } value)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                bool enabled = value.Name == ErlangBooleanAtoms.True;
                switch (key.Name)
                {
                    case RuntimeNodeQueryNames.ConnectionId:
                        connectionId = enabled;
                        break;
                    case RuntimeNodeQueryNames.NodeType:
                        nodeType = enabled;
                        break;
                    default:
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                }
            }
        }

        return new(
            includeThis,
            includeKnown,
            options is not null,
            connectionId,
            nodeType
        );
    }
}
