namespace Erlang;

internal sealed class RuntimeSystemProfile(ProcessRuntime runtime, object gate, Dictionary<Pid, ProcessContext> processes)
{
    private Term? receiver;
    private SystemProfileOptions options = new(SystemProfileNames.Timestamp, false);

    internal string Timestamp
    {
        get
        {
            lock (gate)
                return options.Timestamp;
        }
    }

    public Term Get()
    {
        lock (gate)
            return Settings();
    }

    public Term Configure(Term target, Term optionList)
    {
        lock (gate)
        {
            runtime.ThrowIfHalted();
            if (target is Atom { Name: SystemProfileNames.Undefined } || optionList is Nil)
            {
                Term previous = Settings();
                Clear();

                return previous;
            }
            if (optionList is not Cons)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            bool live = target switch
            {
                Pid pid => pid.Node == runtime.Node && processes.ContainsKey(pid),
                PortTerm port => port.Node == runtime.Node && runtime.Ports.Alive(port),
                _ => false
            };
            if (!live)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            SystemProfileOptions wanted = SystemProfileOptions.Read(optionList);
            if (wanted.HasEvents)
                throw new ErlangException(SystemProfileNames.NotSupported);
            Term result = Settings();
            receiver = target;
            options = wanted;

            return result;
        }
    }

    public void Terminated(Term target)
    {
        lock (gate)
        {
            if (receiver?.Equals(target) == true)
                Clear();
        }
    }

    internal void ClearForHalt()
    {
        lock (gate)
            Clear();
    }

    private Term Settings() => receiver is null ? Term.A(SystemProfileNames.Undefined) : Term.Tuple(receiver, Nil.Value);

    private void Clear()
    {
        receiver = null;
        options = new(SystemProfileNames.Timestamp, false);
    }
}
