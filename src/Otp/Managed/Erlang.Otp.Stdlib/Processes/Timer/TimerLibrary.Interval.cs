// Derived from Erlang/OTP 29.1.1 timer.erl, Apache-2.0; Ericsson AB 1996-2025.
using static Erlang.TimerLibraryNames;

namespace Erlang;

public static partial class TimerLibrary
{
    private static async ValueTask<Term> StartInterval(ProcessContext context, IReadOnlyList<Term> arguments, bool waitComplete)
    {
        Term tag = await Bif(context, ErlangModuleNames.MakeReference);
        Term server = context.Self;
        Term started = arguments[0];
        Term time = arguments[1];
        Term target = arguments[2];
        Term mfa = arguments[3];
        var recipe = new TimerIntervalRecipe(context.Runtime.Functions);
        FunctionTerm worker = recipe.Rebind([server, target, tag, started, time, mfa, CoreModules.Boolean(waitComplete)]);
        var spawned = (TupleTerm)await Bif(context, ErlangModuleNames.SpawnMonitor, worker);

        return Term.Tuple(spawned.Items[1], spawned.Items[0], tag);
    }

    internal static async ValueTask<Term> RunInterval(ProcessContext context, IReadOnlyList<Term> environment)
    {
        Term serverMonitor = await Bif(
            context,
            ErlangModuleNames.Monitor,
            Term.A(TimerProtocolAtoms.Process),
            environment[TimerIntervalEnvironment.Server]
        );
        Term targetMonitor = await Bif(
            context,
            ErlangModuleNames.Monitor,
            Term.A(TimerProtocolAtoms.Process),
            environment[TimerIntervalEnvironment.Target]
        );
        Term timer = await Schedule(
            context,
            environment[TimerIntervalEnvironment.Started],
            environment[TimerIntervalEnvironment.Time],
            environment[TimerIntervalEnvironment.Action]
        );

        return await IntervalLoop(
            context,
            serverMonitor,
            targetMonitor,
            environment[TimerIntervalEnvironment.Tag],
            CoreModules.Bool(environment[TimerIntervalEnvironment.WaitComplete]),
            timer
        );
    }

    private static async ValueTask<Term> IntervalLoop(
        ProcessContext context,
        Term serverMonitor,
        Term targetMonitor,
        Term tag,
        bool waitComplete,
        Term timer
    )
    {
        bool Stop(TupleTerm message)
            => message.Items.Count == TimerProtocolArities.CancelMessage && message.Items[0] is Atom { Name: TimerProtocolAtoms.Cancel } && message.Items[1].Equals(tag)
                || message.Items.Count == TimerProtocolArities.DownMessage && message.Items[0] is Atom { Name: TimerProtocolAtoms.Down }
                && message.Items[2] is Atom { Name: TimerProtocolAtoms.Process }
                && (message.Items[1].Equals(serverMonitor) || message.Items[1].Equals(targetMonitor));

        while (true)
        {
            TupleTerm message = (await context.ReceiveAsync(value => value is TupleTerm tuple
                && (Stop(tuple) || tuple.Items.Count == TimerProtocolArities.TimeoutMessage && tuple.Items[0] is Atom { Name: TimerProtocolAtoms.Timeout }
                    && tuple.Items[1].Equals(timer) && tuple.Items[2] is TupleTerm { Items.Count: TimerProtocolArities.IntervalAction } payload
                    && payload.Items[0] is Atom { Name: TimerRequestNames.ApplyInterval }) ? tuple : null))!;
            if (Stop(message))
            {
                return await CancelTimer(context, timer);
            }
            var action = (TupleTerm)message.Items[2];
            Term outcome = await DoApply(context, action.Items[3], waitComplete);
            if (outcome is TupleTerm { Items.Count: TimerProtocolArities.Result } success && success.Items[0].Equals(Ok)
                && success.Items[1] is TupleTerm { Items.Count: TimerProtocolArities.SpawnAction } spawned && spawned.Items[0] is Atom { Name: TimerActionKinds.Spawn })
            {
                TupleTerm finished = (await context.ReceiveAsync(value => value is TupleTerm tuple
                    && (Stop(tuple) || tuple.Items.Count == TimerProtocolArities.DownMessage && tuple.Items[0] is Atom { Name: TimerProtocolAtoms.Down }
                        && tuple.Items[2] is Atom { Name: TimerProtocolAtoms.Process } && tuple.Items[1].Equals(spawned.Items[1])) ? tuple : null))!;
                if (Stop(finished))
                {
                    return Ok;
                }
            }
            timer = await Schedule(
                context,
                action.Items[1],
                action.Items[2],
                action.Items[3]
            );
            await context.BumpReductionsAsync(1);
        }
    }

    private static async ValueTask<Term> Schedule(
        ProcessContext context,
        Term current,
        Term time,
        Term mfa
    )
    {
        Term next = CoreModules.Arithmetic(ErlangOperators.Plus, current, time);
        Term payload = Term.Tuple(
            Term.A(TimerRequestNames.ApplyInterval),
            next,
            time,
            mfa
        );
        if (TermOrder.Compare(next, await Clock(context)) <= 0)
        {
            Term reference = await Bif(context, ErlangModuleNames.MakeReference);
            _ = await Bif(
                context,
                ErlangBasicBifNames.Send,
                context.Self,
                Term.Tuple(Term.A(TimerProtocolAtoms.Timeout), reference, payload)
            );

            return reference;
        }

        return await Bif(
            context,
            TimerModuleNames.StartTimer,
            next,
            context.Self,
            payload,
            Absolute
        );
    }

    private static async ValueTask<Term> DoApply(ProcessContext context, Term value, bool waitComplete)
    {
        if (value is not TupleTerm { Items.Count: TimerProtocolArities.Mfa } mfa)
        {
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        }
        Term module = mfa.Items[TimerMfaLayout.Module];
        Term function = mfa.Items[TimerMfaLayout.Function];
        Term args = mfa.Items[TimerMfaLayout.Arguments];
        if (module is Atom { Name: Module } && function is Atom { Name: TimerLibraryFunctions.Send })
        {
            try
            {
                Term[] pair = Cons.Items(args).ToArray();
                if (pair.Length != TimerProtocolArities.SendArguments)
                {
                    throw new ErlangException(ErlangErrorReasons.FunctionClause);
                }
                _ = await Bif(
                    context,
                    ErlangBasicBifNames.Send,
                    pair[0],
                    pair[1]
                );

                return Term.Tuple(Ok, Term.A(TimerActionKinds.Send));
            }
            catch (ErlangException)
            {
                return Error;
            }
        }
        if (module is Atom { Name: ErlangModuleNames.Module } && function is Atom { Name: ErlangModuleNames.Exit }
            && args is Cons { Tail: Cons { Tail: Nil } second } first)
        {
            try
            {
                Term pid = first.Head;
                if (pid is Atom { Name: not TimerProtocolAtoms.Undefined })
                {
                    pid = await Bif(context, ErlangModuleNames.WhereIs, pid);
                }
                if (pid is not Pid)
                {
                    pid = Term.A(TimerProtocolAtoms.Undefined);
                }
                _ = await Bif(
                    context,
                    ErlangModuleNames.Exit,
                    pid,
                    second.Head
                );

                return Term.Tuple(Ok, Term.A(TimerActionKinds.Exit));
            }
            catch (ErlangException)
            {
                return Error;
            }
        }
        try
        {
            Term spawned = await Bif(
                context,
                waitComplete ? ErlangModuleNames.SpawnMonitor : ErlangModuleNames.Spawn,
                module,
                function,
                args
            );

            return Term.Tuple(
                Ok,
                waitComplete
                ? Term.Tuple(Term.A(TimerActionKinds.Spawn), ((TupleTerm)spawned).Items[1]) : Term.A(TimerActionKinds.Spawn)
            );
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason is Atom { Name: ErlangErrorReasons.BadArgument })
        {
            return Error;
        }
    }
}
