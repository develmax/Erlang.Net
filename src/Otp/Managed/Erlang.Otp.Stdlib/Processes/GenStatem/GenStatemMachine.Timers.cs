// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 2016-2026. All Rights Reserved.
// Modified: OTP-29.1.1 timer cancellation, update and zero-event queue ported to C#.
namespace Erlang;

internal sealed partial class GenStatemMachine
{
    private async ValueTask Cancel(Term type)
    {
        if (!timers.Remove(type, out Term? value))
        {
            return;
        }
        Term reference = ((TupleTerm)value).Items[0];
        if (Is(await Call(GenStatemDependencyNames.Erlang, GenStatemFunctionNames.CancelTimer, reference), GenStatemProtocolAtoms.False))
        {
            timers[reference] = Nil.Value;
        }
    }

    private async ValueTask Timeouts(List<TupleTerm> actions)
    {
        var seen = new HashSet<Term>();
        var zeros = new LinkedList<Term>();
        // OTP evaluates the reversed action list: the last action for each type wins.
        for (int i = actions.Count - 1; i >= 0; i--)
        {
            TupleTerm action = actions[i];
            Term type = action.Items[0];
            if (!seen.Add(type))
            {
                continue;
            }
            Term time = action.Items[1];
            Term content = action.Items[2];
            if (Is(time, GenStatemProtocolAtoms.Infinity))
            {
                await Cancel(type);
                continue;
            }
            if (Is(time, GenStatemProtocolAtoms.Update) && timers.TryGetValue(type, out Term? current))
            {
                timers[type] = Term.Tuple(((TupleTerm)current).Items[0], content);
                continue;
            }
            Term reference;
            if (Is(time, GenStatemProtocolAtoms.Update) || time is Integer zero && zero.Value.IsZero)
            {
                reference = await Call(GenStatemDependencyNames.Erlang, GenStatemFunctionNames.MakeRef);
                zeros.AddFirst(Term.Tuple(type, reference));
            }
            else
            {
                Term options = action.Items.Count == 4 ? Cons.From(Listify(action.Items[3])) : Nil.Value;
                reference = await Call(
                    GenStatemDependencyNames.Erlang,
                    GenStatemFunctionNames.StartTimer,
                    time,
                    context.Self,
                    type,
                    options
                );
            }
            await Cancel(type);
            timers[type] = Term.Tuple(reference, content);
            if (!Is(time, GenStatemProtocolAtoms.Update))
            {
                await Debug(Term.Tuple(
                    Term.A(GenStatemProtocolAtoms.StartTimer),
                    Term.Tuple(
                        type,
                        time,
                        content,
                        action.Items.Count == 4 ? Cons.From(Listify(action.Items[3])) : Nil.Value
                    ),
                    State
                ));
            }
        }
        foreach (Term zero in zeros)
        {
            zeroQueue.Enqueue(zero);
        }
    }

    private Term ListTimeouts() => Cons.From(new MapTerm(timers).Entries
        .Where(pair => pair.Value is TupleTerm { Items.Count: 2 })
        .Select(pair => Term.Tuple(pair.Key, ((TupleTerm)pair.Value).Items[1])));
}
