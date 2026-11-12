// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 2016-2026. All Rights Reserved.
// Modified: OTP-29.1.1 callback results, action ordering and transitions ported to C#.
namespace Erlang;

internal sealed partial class GenStatemMachine
{
    private async ValueTask<bool> Result(GenStatemTransition transition, Term result, bool stateCall)
    {
        bool enter = false;
        Term actions = Nil.Value;
        if (result is Atom atom)
        {
            switch (atom.Name)
            {
                case GenStatemProtocolAtoms.KeepStateAndData:
                    return false;
                case GenStatemProtocolAtoms.RepeatStateAndData:
                    return true;
                case GenStatemProtocolAtoms.Stop:
                    StateData = Term.Tuple(transition.State, transition.Data);
                    await Terminate(new ErlangException(Term.A(GenStatemProtocolAtoms.Normal), ErlangExceptionClasses.Exit));

                    return false;
                default:
                    throw InvalidResult(transition, result, false);
            }
        }
        if (result is not TupleTerm tuple || tuple.Items.Count < 2 || tuple.Items[0] is not Atom tag)
        {
            throw InvalidResult(transition, result, false);
        }
        switch (tag.Name)
        {
            case GenStatemProtocolAtoms.NextState when tuple.Items.Count is 3 or 4:
                enter = !tuple.Items[1].Equals(transition.State);
                if (enter && !stateCall)
                {
                    throw InvalidResult(transition, result, true);
                }
                transition.State = tuple.Items[1];
                transition.Data = tuple.Items[2];
                actions = tuple.Items.Count == 4 ? tuple.Items[3] : Nil.Value;
                break;
            case GenStatemProtocolAtoms.KeepState or GenStatemProtocolAtoms.RepeatState when tuple.Items.Count is 2 or 3:
                transition.Data = tuple.Items[1];
                enter = tag.Name == GenStatemProtocolAtoms.RepeatState;
                actions = tuple.Items.Count == 3 ? tuple.Items[2] : Nil.Value;
                break;
            case GenStatemProtocolAtoms.KeepStateAndData or GenStatemProtocolAtoms.RepeatStateAndData when tuple.Items.Count == 2:
                enter = tag.Name == GenStatemProtocolAtoms.RepeatStateAndData;
                actions = tuple.Items[1];
                break;
            case GenStatemProtocolAtoms.Stop when tuple.Items.Count is 2 or 3:
                transition.Data = tuple.Items.Count == 3 ? tuple.Items[2] : transition.Data;
                StateData = Term.Tuple(transition.State, transition.Data);
                await Terminate(new ErlangException(tuple.Items[1], ErlangExceptionClasses.Exit));

                return false;
            case GenStatemProtocolAtoms.StopAndReply when tuple.Items.Count is 3 or 4:
                transition.Data = tuple.Items.Count == 4 ? tuple.Items[3] : transition.Data;
                StateData = Term.Tuple(transition.State, transition.Data);
                foreach (Term reply in Listify(tuple.Items[2]))
                {
                    if (reply is not TupleTerm { Items.Count: 3 } r || !Is(r.Items[0], GenStatemProtocolAtoms.Reply) || !IsFrom(r.Items[1]))
                    {
                        throw new ErlangException(Term.Tuple(Term.A(GenStatemProtocolAtoms.BadReplyActionFromStateFunction), reply));
                    }
                    await Reply(r.Items[1], r.Items[2]);
                }
                await Terminate(new ErlangException(tuple.Items[1], ErlangExceptionClasses.Exit));

                return false;
            default:
                throw InvalidResult(transition, result, false);
        }
        await Actions(transition, actions, stateCall);

        return enter;
    }

    private ErlangException InvalidResult(GenStatemTransition transition, Term result, bool stateEnterReturn)
    {
        StateData = Term.Tuple(transition.State, transition.Data);

        return GenStatemSource.Failure(
            context,
            Term.Tuple(
                Term.A(stateEnterReturn
            ? GenStatemProtocolAtoms.BadStateEnterReturnFromStateFunction : GenStatemProtocolAtoms.BadReturnFromStateFunction),
                result
            ),
            GenStatemFunctionNames.LoopStateCallbackResult,
            11,
            stateEnterReturn ? 3805 : 3895
        );
    }

    private ErlangException InvalidAction(GenStatemTransition transition, Term action, bool enter = false)
    {
        StateData = Term.Tuple(transition.State, transition.Data);
        string function = GenStatemFunctionNames.LoopActionsList;
        int arity = 13;
        int line = 4214;
        if (action is TupleTerm tuple)
        {
            if (tuple.Items.Count == 3 && Is(tuple.Items[0], GenStatemProtocolAtoms.Reply))
            {
                function = GenStatemFunctionNames.LoopActionsReply;
                arity = 14;
                line = 4244;
            }
            else if (tuple.Items.Count == 3 && Is(tuple.Items[0], GenStatemProtocolAtoms.NextEvent))
            {
                function = GenStatemFunctionNames.LoopActionsNextEventBad;
                arity = 9;
                line = 4295;
            }
            else if (tuple.Items.Count == 2 && tuple.Items[0] is Atom { Name: GenStatemProtocolAtoms.ChangeCallbackModule or GenStatemProtocolAtoms.PushCallbackModule } && enter)
            {
                arity = 12;
                line = 4030;
            }
            else if (tuple.Items.Count == 4)
            {
                line = IsTimeoutType(tuple.Items[0]) ? 4130 : 4141;
            }
            else if (tuple.Items.Count == 3)
            {
                line = 4166;
            }
            else if (tuple.Items.Count == 2 && Is(tuple.Items[1], GenStatemProtocolAtoms.Cancel))
            {
                line = 4190;
            }
        }
        else if (Is(action, GenStatemProtocolAtoms.Postpone) && enter)
        {
            arity = 12;
            line = 3983;
        }
        else if (Is(action, GenStatemProtocolAtoms.PopCallbackModule) && enter)
        {
            arity = 12;
            line = 4064;
        }

        return GenStatemSource.Failure(
            context,
            Term.Tuple(
                Term.A(enter
            ? GenStatemProtocolAtoms.BadStateEnterActionFromStateFunction : GenStatemProtocolAtoms.BadActionFromStateFunction),
                action
            ),
            function,
            arity,
            line
        );
    }

    private static bool IsFrom(Term value) => value is TupleTerm { Items.Count: 2 } from && from.Items[0] is Pid;

    private static bool IsTimeoutType(Term type) => type is Atom { Name: GenStatemProtocolAtoms.Timeout or GenStatemProtocolAtoms.StateTimeout }
        || type is TupleTerm { Items.Count: 2 } tuple && Is(tuple.Items[0], GenStatemProtocolAtoms.Timeout);

    private static bool IsEventType(Term type) => type is Atom { Name: GenStatemProtocolAtoms.Cast or GenStatemProtocolAtoms.Info or GenStatemProtocolAtoms.Internal }
        || type is TupleTerm { Items.Count: 2 } call && Is(call.Items[0], GenStatemProtocolAtoms.Call) && IsFrom(call.Items[1])
        || IsTimeoutType(type);

    private async ValueTask Reply(Term from, Term value)
    {
        await Call(
            GenStatemDependencyNames.Gen,
            GenStatemFunctionNames.Reply,
            from,
            value
        );
        await Debug(Term.Tuple(Term.A(GenStatemProtocolAtoms.Out), value, from));
    }

    private async ValueTask Actions(GenStatemTransition transition, Term actions, bool stateCall)
    {
        foreach (Term action in Listify(actions))
        {
            await context.ReduceAsync();
            if (Is(action, GenStatemProtocolAtoms.Hibernate))
            {
                transition.Hibernate = true;
                continue;
            }
            if (Is(action, GenStatemProtocolAtoms.Postpone))
            {
                if (!stateCall)
                {
                    throw InvalidAction(transition, action, true);
                }
                transition.Postpone = true;
                continue;
            }
            if (Is(action, GenStatemProtocolAtoms.PopCallbackModule) && modules.Count > 1)
            {
                if (!stateCall)
                {
                    throw InvalidAction(transition, action, true);
                }
                await ChangeModules(modules.Skip(1).ToList(), transition, 4040);
                await Debug(Term.Tuple(Term.A(GenStatemProtocolAtoms.Module), modules[0], transition.State));
                continue;
            }
            if (action is TupleTerm tuple)
            {
                if (tuple.Items.Count == 3 && Is(tuple.Items[0], GenStatemProtocolAtoms.Reply))
                {
                    if (!IsFrom(tuple.Items[1]))
                    {
                        throw InvalidAction(transition, action);
                    }
                    await Reply(tuple.Items[1], tuple.Items[2]);
                    continue;
                }
                if (tuple.Items.Count == 2 && Is(tuple.Items[0], GenStatemProtocolAtoms.Hibernate) && tuple.Items[1] is Atom { Name: GenStatemProtocolAtoms.True or GenStatemProtocolAtoms.False })
                {
                    transition.Hibernate = Is(tuple.Items[1], GenStatemProtocolAtoms.True);
                    continue;
                }
                if (tuple.Items.Count == 2 && Is(tuple.Items[0], GenStatemProtocolAtoms.Postpone)
                    && tuple.Items[1] is Atom { Name: GenStatemProtocolAtoms.True or GenStatemProtocolAtoms.False } && (stateCall || Is(tuple.Items[1], GenStatemProtocolAtoms.False)))
                {
                    transition.Postpone = Is(tuple.Items[1], GenStatemProtocolAtoms.True);
                    continue;
                }
                if (tuple.Items.Count == 3 && Is(tuple.Items[0], GenStatemProtocolAtoms.NextEvent))
                {
                    if (!IsEventType(tuple.Items[1]) || !stateCall)
                    {
                        throw InvalidAction(transition, action, !stateCall);
                    }
                    Term next = Term.Tuple(tuple.Items[1], tuple.Items[2]);
                    transition.NextEvents.Add(next);
                    await Debug(Term.Tuple(Term.A(GenStatemProtocolAtoms.In), next, transition.State));
                    continue;
                }
                if (tuple.Items.Count == 2 && tuple.Items[0] is Atom { Name: GenStatemProtocolAtoms.ChangeCallbackModule or GenStatemProtocolAtoms.PushCallbackModule } tag && tuple.Items[1] is Atom)
                {
                    if (!stateCall)
                    {
                        throw InvalidAction(transition, action, true);
                    }
                    List<Term> proposed = modules.ToList();
                    if (tag.Name == GenStatemProtocolAtoms.ChangeCallbackModule)
                    {
                        proposed[0] = tuple.Items[1];
                    }
                    else
                    {
                        proposed.Insert(0, tuple.Items[1]);
                    }
                    await ChangeModules(proposed, transition, 4007);
                    await Debug(Term.Tuple(Term.A(GenStatemProtocolAtoms.Module), modules[0], transition.State));
                    continue;
                }
                if (tuple.Items.Count == 2 && IsTimeoutType(tuple.Items[0]) && Is(tuple.Items[1], GenStatemProtocolAtoms.Cancel))
                {
                    transition.Timeouts.Add((TupleTerm)Term.Tuple(tuple.Items[0], Term.A(GenStatemProtocolAtoms.Infinity), Term.A(GenStatemProtocolAtoms.Undefined)));
                    continue;
                }
                if (tuple.Items.Count is 3 or 4 && IsTimeoutType(tuple.Items[0]))
                {
                    bool absolute = false;
                    if (tuple.Items.Count == 4)
                    {
                        foreach (Term option in Listify(tuple.Items[3]))
                        {
                            if (option is not TupleTerm { Items.Count: 2 } abs || !Is(abs.Items[0], GenStatemProtocolAtoms.Abs)
                                || abs.Items[1] is not Atom { Name: GenStatemProtocolAtoms.True or GenStatemProtocolAtoms.False })
                            {
                                throw InvalidAction(transition, action);
                            }
                            absolute = Is(abs.Items[1], GenStatemProtocolAtoms.True);
                        }
                    }
                    Term time = tuple.Items[1];
                    bool valid = Is(time, GenStatemProtocolAtoms.Infinity) || time is Integer integer && (absolute || integer.Value >= 0)
                        || tuple.Items.Count == 3 && Is(time, GenStatemProtocolAtoms.Update);
                    if (!valid)
                    {
                        throw InvalidAction(transition, action);
                    }
                    transition.Timeouts.Add(absolute ? tuple : (TupleTerm)Term.Tuple(tuple.Items[0], time, tuple.Items[2]));
                    continue;
                }
            }
            if (Is(action, GenStatemProtocolAtoms.Infinity) || action is Integer n && n.Value >= 0)
            {
                transition.Timeouts.Add((TupleTerm)Term.Tuple(Term.A(GenStatemProtocolAtoms.Timeout), action, action));
                continue;
            }
            throw InvalidAction(transition, action);
        }
    }

    private async ValueTask ChangeModules(List<Term> proposed, GenStatemTransition transition, int sourceLine)
    {
        List<Term> previous = modules;
        Term previousMode = callbackMode;
        bool previousEnter = stateEnter;
        modules = proposed;
        try
        {
            using var source = GenStatemSource.Scope(
                context,
                GenStatemFunctionNames.LoopActionsList,
                12,
                sourceLine
            );
            await SetCallbackMode();
        }
        catch (ErlangException)
        {
            modules = previous;
            callbackMode = previousMode;
            stateEnter = previousEnter;
            StateData = Term.Tuple(transition.State, transition.Data);
            throw;
        }
    }

    private async ValueTask Complete(GenStatemTransition transition, Term currentEvent, bool enter)
    {
        Term oldState = State;
        while (enter && stateEnter)
        {
            Term result = await StateCallback(transition, Term.A(GenStatemProtocolAtoms.Enter), oldState);
            enter = await Result(transition, result, false);
        }
        if (transition.Postpone)
        {
            postponed.Add(currentEvent);
        }
        bool changed = !transition.State.Equals(oldState);
        await Debug(Term.Tuple(
            Term.A(transition.Postpone ? GenStatemProtocolAtoms.Postpone : GenStatemProtocolAtoms.Consume),
            currentEvent,
            oldState,
            transition.State
        ));
        if (changed)
        {
            for (int i = postponed.Count - 1; i >= 0; i--)
            {
                events.AddFirst(postponed[i]);
            }
            postponed.Clear();
            await Cancel(Term.A(GenStatemProtocolAtoms.StateTimeout));
        }
        await Cancel(Term.A(GenStatemProtocolAtoms.Timeout));
        StateData = Term.Tuple(transition.State, transition.Data);
        hibernate = transition.Hibernate;
        await Timeouts(transition.Timeouts);
        for (int i = transition.NextEvents.Count - 1; i >= 0; i--)
        {
            events.AddFirst(transition.NextEvents[i]);
        }
    }
}
