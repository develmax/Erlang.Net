namespace Erlang;

internal static class ConnectedSpawnWireOptions
{
    internal static Term Normalize(Term original)
    {
        bool link = false;
        bool monitor = false;
        var retained = new List<Term>();
        foreach (Term option in Cons.Items(original))
        {
            if (option is TupleTerm { Items: [Atom key, _] })
            {
                if (key.Name == ProcessSpawnOptionNames.Link)
                    link = true;
                else if (key.Name == ProcessSpawnOptionNames.Monitor)
                    monitor = true;
                else if (key.Name is not (ProcessSpawnRequestNames.Reply or ProcessSpawnRequestNames.ReplyTag))
                    retained.Add(option);
            }
            else
                retained.Add(option);
        }
        if (monitor)
            retained.Insert(0, Term.A(ProcessSpawnOptionNames.Monitor));
        if (link)
            retained.Insert(0, Term.A(ProcessSpawnOptionNames.Link));

        return Cons.From(retained);
    }
}
