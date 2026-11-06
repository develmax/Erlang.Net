// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2026. All Rights Reserved.
// Modified: per-callback copy of the OTP state tuple; guest state remains immutable.
namespace Erlang;

internal sealed class SupervisorState
{
    private readonly Term[] fields;
    internal List<SupervisorChild> Children { get; } = [];
    internal Dictionary<Term, Term> Dynamics { get; } = [];
    internal Term DynamicKind { get; set; } = Term.A(SupervisorProtocol.Undefined);

    internal SupervisorState(Term value)
    {
        if (value is not TupleTerm { Items.Count: SupervisorStateLayout.Arity } state || !state.Items[0].Equals(Term.A(SupervisorProtocol.State)))
        {
            throw new ErlangException(SupervisorProtocol.FunctionClause);
        }
        fields = state.Items.ToArray();
        var children = (TupleTerm)fields[SupervisorStateLayout.Children];
        var db = (MapTerm)children.Items[1];
        foreach (Term id in Cons.Items(children.Items[0]))
        {
            if (!db.TryGet(id, out Term? child))
            {
                throw new ErlangException(SupervisorProtocol.Badkey);
            }
            Children.Add(SupervisorChild.FromTerm(child!));
        }
        if (fields[SupervisorStateLayout.Dynamics] is TupleTerm dynamic)
        {
            DynamicKind = dynamic.Items[0];
            foreach (var entry in ((MapTerm)dynamic.Items[1]).Entries)
            {
                Dynamics.Add(entry.Key, entry.Value);
            }
        }
    }

    internal SupervisorState(
        Term name,
        Term module,
        Term args,
        Term tag
    )
    {
        fields = [Term.A(SupervisorProtocol.State), name, Term.A(SupervisorProtocol.OneForOne), Term.Tuple(Nil.Value, new MapTerm([])), Term.A(SupervisorProtocol.Undefined),
            Term.I(SupervisorDefaults.RestartIntensity), Term.I(SupervisorDefaults.RestartPeriodSeconds), Nil.Value, Term.I(0), Term.I(0), Term.A(SupervisorProtocol.Never), Term.A(SupervisorProtocol.Infinity), Term.A(SupervisorProtocol.False), tag, module, args];
    }

    internal Term Name => fields[SupervisorStateLayout.Name];
    internal Term Strategy { get => fields[SupervisorStateLayout.Strategy]; set => fields[SupervisorStateLayout.Strategy] = value; }
    internal Term Intensity { get => fields[SupervisorStateLayout.Intensity]; set => fields[SupervisorStateLayout.Intensity] = value; }
    internal Term Period { get => fields[SupervisorStateLayout.Period]; set => fields[SupervisorStateLayout.Period] = value; }
    internal Term Restarts { get => fields[SupervisorStateLayout.Restarts]; set => fields[SupervisorStateLayout.Restarts] = value; }
    internal Term NRestarts { get => fields[SupervisorStateLayout.RestartCount]; set => fields[SupervisorStateLayout.RestartCount] = value; }
    internal Term DynamicRestarts { get => fields[SupervisorStateLayout.DynamicRestarts]; set => fields[SupervisorStateLayout.DynamicRestarts] = value; }
    internal Term AutoShutdown { get => fields[SupervisorStateLayout.AutoShutdown]; set => fields[SupervisorStateLayout.AutoShutdown] = value; }
    internal Term HibernateAfter { get => fields[SupervisorStateLayout.HibernateAfter]; set => fields[SupervisorStateLayout.HibernateAfter] = value; }
    internal Term Hibernating { get => fields[SupervisorStateLayout.Hibernating]; set => fields[SupervisorStateLayout.Hibernating] = value; }
    internal Term Tag => fields[SupervisorStateLayout.Tag];
    internal Term Module => fields[SupervisorStateLayout.Module];
    internal Term Args => fields[SupervisorStateLayout.Arguments];
    internal bool Simple => Strategy.Equals(Term.A(SupervisorProtocol.SimpleOneForOne));

    internal TupleTerm ChildrenTerm() => Term.Tuple(Cons.From(Children.Select(c => c.Id)), new MapTerm(Children.Select(c => new KeyValuePair<Term, Term>(c.Id, c.ToTerm()))));

    internal TupleTerm ToTerm()
    {
        fields[SupervisorStateLayout.Children] = ChildrenTerm();
        fields[SupervisorStateLayout.Dynamics] = DynamicKind.Equals(Term.A(SupervisorProtocol.Undefined)) ? DynamicKind : Term.Tuple(DynamicKind, new MapTerm(Dynamics));

        return Term.Tuple(fields.ToArray());
    }
}
