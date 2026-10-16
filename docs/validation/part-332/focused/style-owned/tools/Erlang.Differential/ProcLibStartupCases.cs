namespace Erlang.Differential;

public static class ProcLibStartupCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        Case(ProcLibStartupCaseNames.StartThree, ProcLibStartupCaseSources.StartThree, Atom(ProcLibStartupCaseNames.Ready)),
        Case(ProcLibStartupCaseNames.StartFour, ProcLibStartupCaseSources.StartFour, Atom(ProcLibStartupCaseNames.Ready)),
        Case(ProcLibStartupCaseNames.LinkThree, ProcLibStartupCaseSources.LinkThree, Atom(ProcLibStartupCaseNames.Ready)),
        Case(ProcLibStartupCaseNames.LinkFour, ProcLibStartupCaseSources.LinkFour, Atom(ProcLibStartupCaseNames.True)),
        Case(ProcLibStartupCaseNames.FailTwo, ProcLibStartupCaseSources.FailTwo, Atom(ProcLibStartupCaseNames.True)),
        Case(ProcLibStartupCaseNames.FailThree, ProcLibStartupCaseSources.FailThree, Atom(ProcLibStartupCaseNames.True)),
        Case(ProcLibStartupCaseNames.RejectError, ProcLibStartupCaseSources.RejectError, Ok),
        Case(ProcLibStartupCaseNames.RejectExit, ProcLibStartupCaseSources.RejectExit, Ok),
        Case(ProcLibStartupCaseNames.RejectThrow, ProcLibStartupCaseSources.RejectThrow, Ok),
        Case(ProcLibStartupCaseNames.ZeroTimeout, ProcLibStartupCaseSources.ZeroTimeout, Ok),
        Case(ProcLibStartupCaseNames.LinkTimeout, ProcLibStartupCaseSources.LinkTimeout, Ok),
        Case(ProcLibStartupCaseNames.FiniteTimeout, ProcLibStartupCaseSources.FiniteTimeout, Ok),
        Case(ProcLibStartupCaseNames.Mailbox, ProcLibStartupCaseSources.Mailbox, Ok),
        Case(ProcLibStartupCaseNames.Registered, ProcLibStartupCaseSources.Registered, Atom(ProcLibStartupCaseNames.Ready)),
        Case(ProcLibStartupCaseNames.AtomTimeout, ProcLibStartupCaseSources.AtomTimeout, InvalidTimeout),
        Case(ProcLibStartupCaseNames.NegativeTimeout, ProcLibStartupCaseSources.NegativeTimeout, InvalidTimeout),
        Case(ProcLibStartupCaseNames.LargeTimeout, ProcLibStartupCaseSources.LargeTimeout, InvalidTimeout),
        Case(ProcLibStartupCaseNames.FloatTimeout, ProcLibStartupCaseSources.FloatTimeout, InvalidTimeout),
        Case(ProcLibStartupCaseNames.InvalidMfa, ProcLibStartupCaseSources.InvalidMfa, Ok),
        Case(ProcLibStartupCaseNames.ImproperArgs, ProcLibStartupCaseSources.ImproperArgs, Ok),
        Case(ProcLibStartupCaseNames.MissingMfa, ProcLibStartupCaseSources.MissingMfa, Ok),
        Case(ProcLibStartupCaseNames.NormalBeforeAck, ProcLibStartupCaseSources.NormalBeforeAck, Ok),
        Case(
            ProcLibStartupCaseNames.ErrorReturn,
            ProcLibStartupCaseSources.ErrorReturn,
            Term.Tuple(
                Atom(ProcLibStartupCaseNames.True),
                Term.Tuple(Atom(ProcLibStartupCaseNames.Error), Atom(ProcLibStartupCaseNames.Payload))
            )
        ),
        Case(ProcLibStartupCaseNames.FlushMonitor, ProcLibStartupCaseSources.FlushMonitor, Ok),
        Case(ProcLibStartupCaseNames.FailStack, ProcLibStartupCaseSources.FailStack, Atom(ProcLibStartupCaseNames.True)),
        Case(ProcLibStartupCaseNames.FailBadClass, ProcLibStartupCaseSources.FailBadClass, Atom(ProcLibStartupCaseNames.True)),
        Case(ProcLibStartupCaseNames.FailMalformed, ProcLibStartupCaseSources.FailMalformed, Atom(ProcLibStartupCaseNames.True)),
        Case(ProcLibStartupCaseNames.FailMissingParent, ProcLibStartupCaseSources.FailMissingParent, Ok),
        Case(ProcLibStartupCaseNames.LinkedReject, ProcLibStartupCaseSources.LinkedReject, Ok),
        Case(ProcLibStartupCaseNames.MaximumTimeout, ProcLibStartupCaseSources.MaximumTimeout, Atom(ProcLibStartupCaseNames.Ready)),
        Case(ProcLibStartupCaseNames.TimeoutMailbox, ProcLibStartupCaseSources.TimeoutMailbox, Ok)
    ];

    private static Atom Atom(string value) => Term.A(value);

    private static Atom Ok => Atom(ProcLibStartupCaseNames.Ok);

    private static Term InvalidTimeout => Term.Tuple(Atom(ProcLibStartupCaseNames.Caught), Atom(ProcLibStartupCaseNames.False));

    private static CompiledModuleCase Case(string name, string source, Term expected) => new(name, source, Term.Tuple(Atom(OracleOutcomeTags.Success), expected));
}
