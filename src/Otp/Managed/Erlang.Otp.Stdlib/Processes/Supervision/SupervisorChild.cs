// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2026. All Rights Reserved.
// Modified: immutable OTP child record view.
namespace Erlang;

internal sealed record SupervisorChild(
    Term Pid,
    Term Id,
    TupleTerm Start,
    Term Restart,
    Term Significant,
    Term Shutdown,
    Term Type,
    Term Modules
)
{
    internal bool Temporary => Restart.Equals(Term.A(SupervisorProtocol.Temporary));

    internal TupleTerm ToTerm() => Term.Tuple(
        Term.A(SupervisorProtocol.Child),
        Pid,
        Id,
        Start,
        Restart,
        Significant,
        Shutdown,
        Type,
        Modules
    );

    internal static SupervisorChild FromTerm(Term value)
    {
        if (value is not TupleTerm { Items.Count: 9 } child || !child.Items[0].Equals(Term.A(SupervisorProtocol.Child)))
        {
            throw new ErlangException(SupervisorProtocol.FunctionClause);
        }
        var a = child.Items;

        return new(
            a[1],
            a[2],
            (TupleTerm)a[3],
            a[4],
            a[5],
            a[6],
            a[7],
            a[8]
        );
    }

    internal MapTerm ToSpec() => new(new Dictionary<Term, Term>
    {
        [Term.A(SupervisorProtocol.Id)] = Id,
        [Term.A(SupervisorProtocol.Start)] = Start,
        [Term.A(SupervisorProtocol.Restart)] = Restart,
        [Term.A(SupervisorProtocol.Significant)] = Significant,
        [Term.A(SupervisorProtocol.Shutdown)] = Shutdown,
        [Term.A(SupervisorProtocol.Type)] = Type,
        [Term.A(SupervisorProtocol.Modules)] = Modules
    });
}
