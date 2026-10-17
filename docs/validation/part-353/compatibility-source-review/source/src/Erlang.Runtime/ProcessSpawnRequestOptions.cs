namespace Erlang;

internal sealed class ProcessSpawnRequestOptions
{
    public Term Tag { get; private set; } = Term.A(ProcessSpawnRequestNames.DefaultTag);
    public bool SuccessReply { get; private set; } = true;
    public bool ErrorReply { get; private set; } = true;
    public bool BadOptions { get; private set; }
    public ProcessSpawnOptions Spawn { get; private set; } = ProcessSpawnOptions.Parse(Nil.Value);

    public static ProcessSpawnRequestOptions Parse(IReadOnlyList<Term> values, bool disconnected, int configuredSchedulers = int.MaxValue)
    {
        var result = new ProcessSpawnRequestOptions();
        var spawn = new List<Term>();
        foreach (Term option in values)
        {
            if (option is TupleTerm { Items: [Atom { Name: ProcessSpawnRequestNames.ReplyTag }, var tag] })
                result.Tag = tag;
            else if (option is TupleTerm { Items: [Atom { Name: ProcessSpawnRequestNames.Reply }, Atom mode] })
            {
                switch (mode.Name)
                {
                    case ProcessSpawnRequestNames.Yes:
                        result.SuccessReply = true;
                        result.ErrorReply = true;
                        break;
                    case ProcessSpawnRequestNames.No:
                        result.SuccessReply = false;
                        result.ErrorReply = false;
                        break;
                    case ProcessSpawnRequestNames.ErrorOnly:
                        result.SuccessReply = false;
                        result.ErrorReply = true;
                        break;
                    case ProcessSpawnRequestNames.SuccessOnly:
                        result.SuccessReply = true;
                        result.ErrorReply = false;
                        break;
                    default:
                        result.BadOptions = true;
                        break;
                }
            }
            else if (disconnected && option is TupleTerm { Items: [Atom { Name: ProcessSpawnRequestNames.Reply }, _] })
                result.BadOptions = true;
            else if (!disconnected || option is Atom { Name: ProcessSpawnOptionNames.Link or ProcessSpawnOptionNames.Monitor } or TupleTerm { Items: [Atom { Name: ProcessSpawnOptionNames.Link or ProcessSpawnOptionNames.Monitor }, _] })
                spawn.Add(option);
            if (disconnected)
            {
                // The sender validates only reply/link/monitor controls. Other options
                // reach the remote creator only after connection succeeds.
                try
                {
                    result.Spawn = ProcessSpawnOptions.ParseRemote(Cons.From(spawn));
                }
                catch (ErlangException)
                {
                    result.BadOptions = true;
                }
                if (result.BadOptions)
                    break;
            }
        }
        try
        {
            result.Spawn = disconnected
                ? ProcessSpawnOptions.ParseRemote(Cons.From(spawn))
                : ProcessSpawnOptions.Parse(Cons.From(spawn), configuredSchedulers);
        }
        catch (ErlangException)
        {
            result.BadOptions = true;
        }

        return result;
    }
}
