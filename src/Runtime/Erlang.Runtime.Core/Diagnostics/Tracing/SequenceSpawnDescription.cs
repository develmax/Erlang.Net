namespace Erlang;

internal sealed record SequenceSpawnDescription(
    Term Mfa,
    Term Arguments,
    Term Options,
    Term? ReplyTag = null
)
{
    public SequenceSpawnDescription Asynchronous()
    {
        var mfa = (TupleTerm)Mfa;

        return this with
        {
            Mfa = Term.Tuple(
                Term.A(SequenceSpawnProtocolNames.InternalModule),
                Term.A(SequenceSpawnProtocolNames.Initializer),
                Term.I(SequenceSpawnProtocolNames.InitializerArity)
            ),
            Arguments = Term.List(Term.Tuple(mfa.Items[0], mfa.Items[1], Arguments))
        };
    }

    public static Term DefaultOptions(string function) => function switch
    {
        ErlangModuleNames.SpawnLink => Term.List(Term.A(ProcessSpawnOptionNames.Link)),
        ErlangModuleNames.SpawnMonitor => Term.List(Term.A(ProcessSpawnOptionNames.Monitor)),
        _ => Nil.Value
    };

    public static SequenceSpawnDescription Callback(Term callback, Term? options = null) => new(
        Term.Tuple(Term.A(ErlangModuleNames.Module), Term.A(ErlangBasicBifNames.Apply), Term.I(2)),
        Term.List(callback, Nil.Value),
        options ?? Nil.Value
    );

    public static SequenceSpawnDescription Module(
        Atom module,
        Atom function,
        Term arguments,
        Term? options = null
    ) => new(
        Term.Tuple(module, function, Term.I(Cons.Items(arguments).Count())),
        arguments,
        options ?? Nil.Value
    );
}
