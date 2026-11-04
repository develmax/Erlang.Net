// Derived from lib/stdlib/src/timer.erl, Erlang/OTP 29.1.1, Apache-2.0.
// Copyright Ericsson AB 1996-2025. Original notices remain in the retained source.
using static Erlang.TimerLibraryNames;

namespace Erlang;

/// <summary>The explicitly selected C# implementation of the pinned timer library.</summary>
public static partial class TimerLibrary
{
    public const string Name = Module;

    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (TimerLibraryFunctions.ApplyAfter, 2), (TimerLibraryFunctions.ApplyAfter, 3), (TimerLibraryFunctions.ApplyAfter, 4),
        (TimerLibraryFunctions.SendAfter, 2), (TimerLibraryFunctions.SendAfter, 3), (TimerLibraryFunctions.ExitAfter, 2), (TimerLibraryFunctions.ExitAfter, 3),
        (TimerLibraryFunctions.KillAfter, 1), (TimerLibraryFunctions.KillAfter, 2),
        (TimerLibraryFunctions.ApplyInterval, 2), (TimerLibraryFunctions.ApplyInterval, 3), (TimerLibraryFunctions.ApplyInterval, 4),
        (TimerLibraryFunctions.ApplyRepeatedly, 2), (TimerLibraryFunctions.ApplyRepeatedly, 3), (TimerLibraryFunctions.ApplyRepeatedly, 4),
        (TimerLibraryFunctions.SendInterval, 2), (TimerLibraryFunctions.SendInterval, 3), (TimerLibraryFunctions.Cancel, 1), (TimerLibraryFunctions.Sleep, 1),
        (TimerLibraryFunctions.TimeCall, 1), (TimerLibraryFunctions.TimeCall, 2), (TimerLibraryFunctions.TimeCall, 3), (TimerLibraryFunctions.TimeCall, 4), (TimerLibraryFunctions.NowDifference, 2),
        (TimerLibraryFunctions.Seconds, 1), (TimerLibraryFunctions.Minutes, 1), (TimerLibraryFunctions.Hours, 1), (TimerLibraryFunctions.HoursMinutesSeconds, 3),
        (TimerLibraryFunctions.Start, 0), (TimerLibraryFunctions.StartLink, 0), (TimerLibraryFunctions.Initialize, 1), (TimerLibraryFunctions.HandleCall, 3),
        (TimerLibraryFunctions.HandleInfo, 2), (TimerLibraryFunctions.HandleCast, 2), (TimerLibraryFunctions.Terminate, 2), (TimerLibraryFunctions.CodeChange, 3)
    });

    public static void Register(ModuleRegistry registry)
        => registry.InstallPreloaded(target =>
        {
            foreach (var export in Exports)
            {
                string function = export.Name;
                target.RegisterReviewedManagedLibrary789(
                    Module,
                    function,
                    export.Arity,
                    (context, arguments) => Invoke(context, function, arguments)
                );
            }
        });

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        if (!Exports.Contains((function, arguments.Count)))
        {
            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
        using var tail = context.EnterSourceTailCall(Module, function);
        Term a = arguments.Count == 0 ? Nil.Value : arguments[0];
        switch (function)
        {
            case TimerLibraryFunctions.ApplyAfter:
            case TimerLibraryFunctions.ApplyInterval:
            case TimerLibraryFunctions.ApplyRepeatedly:
                return await Apply(context, function, arguments);
            case TimerLibraryFunctions.SendAfter:
            case TimerLibraryFunctions.SendInterval:
                return await Send(
                    context,
                    function,
                    a,
                    arguments.Count == 2 ? context.Self : arguments[1],
                    arguments[^1]
                );
            case TimerLibraryFunctions.ExitAfter:
            case TimerLibraryFunctions.KillAfter:
                {
                    bool kill = function == TimerLibraryFunctions.KillAfter;
                    Term target = arguments.Count == (kill ? 1 : 2) ? context.Self : arguments[1];

                    return await Apply(
                        context,
                        TimerLibraryFunctions.ApplyAfter,
                        [a, Term.A(ErlangModuleNames.Module), Term.A(ErlangModuleNames.Exit),
                    Term.List(target, kill ? Term.A(ProcessExitSignals.Kill) : arguments[^1])]
                    );
                }
            case TimerLibraryFunctions.Cancel:
                if (a is TupleTerm { Items.Count: TimerProtocolArities.Reference } reference && reference.Items[1] is ReferenceTerm)
                {
                    switch (reference.Items[0])
                    {
                        case Atom { Name: TimerReferenceKinds.Instant }:
                            return Cancelled;
                        case Atom { Name: TimerReferenceKinds.LocalSend }:
                            _ = await Bif(context, TimerModuleNames.CancelTimer, reference.Items[1]);

                            return Cancelled;
                        case Atom { Name: TimerReferenceKinds.Once or TimerReferenceKinds.Interval }:
                            return await Request(context, TimerRequestNames.Cancel, a);
                    }
                }

                return BadArgument;
            case TimerLibraryFunctions.Sleep:
                while (a is Integer time && time.Value > TimerTimeParameters.MaximumReceiveAfterMilliseconds)
                {
                    await context.ReceiveErlangAsync<Term>(_ => null, Term.I(TimerTimeParameters.MaximumReceiveAfterMilliseconds));
                    a = new Integer(time.Value - TimerTimeParameters.MaximumReceiveAfterMilliseconds);
                }
                await TimerSource.Run(
                    context,
                    TimerLibraryFunctions.Sleep,
                    1,
                    TimerSourceLines.SleepReceive,
                    async () =>
                {
                    await context.ReceiveErlangAsync<Term>(_ => null, a);

                    return Ok;
                }
                );

                return Ok;
            case TimerLibraryFunctions.TimeCall:
                return await TimeCall(context, arguments);
            case TimerLibraryFunctions.Seconds:
                return await Multiply(
                    context,
                    TimerLibraryFunctions.Seconds,
                    a,
                    TimerTimeParameters.MillisecondsPerSecond,
                    TimerSourceLines.SecondsMultiply
                );
            case TimerLibraryFunctions.Minutes:
                return await Multiply(
                    context,
                    TimerLibraryFunctions.Minutes,
                    a,
                    TimerTimeParameters.MillisecondsPerSecond * TimerTimeParameters.SecondsPerMinute,
                    TimerSourceLines.MinutesMultiply
                );
            case TimerLibraryFunctions.Hours:
                return await Multiply(
                    context,
                    TimerLibraryFunctions.Hours,
                    a,
                    TimerTimeParameters.MillisecondsPerSecond * TimerTimeParameters.SecondsPerMinute * TimerTimeParameters.MinutesPerHour,
                    TimerSourceLines.HoursMultiply
                );
            case TimerLibraryFunctions.HoursMinutesSeconds:
                return await TimerSource.Run(
                    context,
                    TimerLibraryFunctions.HoursMinutesSeconds,
                    3,
                    TimerSourceLines.HoursMinutesSeconds,
                    async () =>
                {
                    Term hours = await Multiply(
                        context,
                        TimerLibraryFunctions.Hours,
                        a,
                        TimerTimeParameters.MillisecondsPerSecond * TimerTimeParameters.SecondsPerMinute * TimerTimeParameters.MinutesPerHour,
                        TimerSourceLines.HoursMultiply
                    );
                    Term minutes = await Multiply(
                        context,
                        TimerLibraryFunctions.Minutes,
                        arguments[1],
                        TimerTimeParameters.MillisecondsPerSecond * TimerTimeParameters.SecondsPerMinute,
                        TimerSourceLines.MinutesMultiply
                    );
                    Term sum = await Bif(
                        context,
                        ErlangOperators.Plus,
                        hours,
                        minutes
                    );
                    Term seconds = await Multiply(
                        context,
                        TimerLibraryFunctions.Seconds,
                        arguments[2],
                        TimerTimeParameters.MillisecondsPerSecond,
                        TimerSourceLines.SecondsMultiply
                    );

                    return await Bif(
                        context,
                        ErlangOperators.Plus,
                        sum,
                        seconds
                    );
                }
                );
            case TimerLibraryFunctions.NowDifference:
                if (a is TupleTerm { Items.Count: TimerProtocolArities.Timestamp } later && arguments[1] is TupleTerm { Items.Count: TimerProtocolArities.Timestamp } earlier)
                {
                    return await TimerSource.Run(
                        context,
                        TimerLibraryFunctions.NowDifference,
                        2,
                        TimerSourceLines.NowDifferenceArithmetic,
                        async () =>
                    {
                        Term delta = await Bif(
                            context,
                            ErlangOperators.Minus,
                            later.Items[TimerTimestampLayout.Megaseconds],
                            earlier.Items[TimerTimestampLayout.Megaseconds]
                        );
                        Term major = await Bif(
                            context,
                            ErlangOperators.Multiply,
                            delta,
                            Term.I(TimerTimeParameters.MicrosecondsPerSecond)
                        );
                        Term middle = await Bif(
                            context,
                            ErlangOperators.Plus,
                            major,
                            later.Items[TimerTimestampLayout.Seconds]
                        );
                        middle = await Bif(
                            context,
                            ErlangOperators.Minus,
                            middle,
                            earlier.Items[TimerTimestampLayout.Seconds]
                        );
                        Term micros = await Bif(
                            context,
                            ErlangOperators.Multiply,
                            middle,
                            Term.I(TimerTimeParameters.MicrosecondsPerSecond)
                        );
                        micros = await Bif(
                            context,
                            ErlangOperators.Plus,
                            micros,
                            later.Items[TimerTimestampLayout.Microseconds]
                        );

                        return await Bif(
                            context,
                            ErlangOperators.Minus,
                            micros,
                            earlier.Items[TimerTimestampLayout.Microseconds]
                        );
                    }
                    );
                }

                throw TimerSource.Clause(
                    context,
                    function,
                    arguments,
                    TimerSourceLines.NowDifferenceClause
                );
            case TimerLibraryFunctions.Start:
                {
                    Term started = await Start(context);
                    RequireOk(started);

                    return Ok;
                }
            case TimerLibraryFunctions.StartLink:
                return await Call(
                    context,
                    TimerDependencyNames.GenServer,
                    TimerDependencyFunctions.ServerStartLink,
                    Term.Tuple(Term.A(Erlang.Otp.BehaviourAtoms.Local), Term.A(Server)),
                    Term.A(Module),
                    Nil.Value,
                    Nil.Value
                );
            default:
                return await Callback(context, function, arguments);
        }
    }

    private static ValueTask<Term> Multiply(
        ProcessContext context,
        string function,
        Term value,
        int factor,
        int line
    )
        => TimerSource.Run(
            context,
            function,
            1,
            line,
            () => Bif(
                context,
                ErlangOperators.Multiply,
                value,
                Term.I(factor)
            )
        );

    private static bool IsList(Term value) => value is Cons or Nil;

    private static bool ProperLength(Term value, out int count)
    {
        count = 0;
        while (value is Cons cell)
        {
            count++;
            value = cell.Tail;
        }

        return value is Nil;
    }

    private static bool IsDestination(Term value)
        => value is Pid or Atom || value is TupleTerm { Items.Count: TimerProtocolArities.Destination } tuple && tuple.Items.All(item => item is Atom);

    private static bool IsTime(Term value) => value is Integer time && time.Value >= 0;

    private static ValueTask<Term> Call(
        ProcessContext context,
        string module,
        string function,
        params Term[] arguments
    )
        => context.Runtime.Modules.Call(
            context,
            module,
            function,
            arguments
        );

    private static ValueTask<Term> Bif(ProcessContext context, string function, params Term[] arguments)
        => Call(
            context,
            ErlangModuleNames.Module,
            function,
            arguments
        );

    private static async ValueTask<Term> Apply(ProcessContext context, string operation, IReadOnlyList<Term> arguments)
    {
        Term time = arguments[0];
        Term module;
        Term function;
        Term args;
        if (arguments.Count < TimerApplyArities.ModuleFunctionArguments)
        {
            args = arguments.Count == TimerApplyArities.FunctionOnly ? Nil.Value : arguments[2];
            if (!ProperLength(args, out int length) || arguments[1] is not FunctionTerm callback || callback.Arity != length)
            {
                return BadArgument;
            }
            module = Term.A(ErlangModuleNames.Module);
            function = Term.A(ErlangBasicBifNames.Apply);
            args = Term.List(callback, args);
        }
        else
        {
            module = arguments[1];
            function = arguments[2];
            args = arguments[3];
            if (module is not Atom || function is not Atom || !IsList(args))
            {
                return BadArgument;
            }
        }
        if (!IsTime(time))
        {
            return BadArgument;
        }
        Term mfa = Term.Tuple(module, function, args);
        if (operation == TimerLibraryFunctions.ApplyAfter && time is Integer instant && instant.Value.IsZero)
        {
            _ = await DoApply(context, mfa, false);

            return Term.Tuple(Ok, Term.Tuple(Term.A(TimerReferenceKinds.Instant), await Bif(context, ErlangModuleNames.MakeReference)));
        }
        Term started = await Clock(context);

        return operation == TimerLibraryFunctions.ApplyAfter
            ? await Request(context, TimerRequestNames.ApplyOnce, Term.Tuple(started, time, mfa))
            : await Request(
                context,
                operation == TimerLibraryFunctions.ApplyInterval ? TimerRequestNames.ApplyInterval : TimerRequestNames.ApplyRepeatedly,
                Term.Tuple(
                    started,
                    time,
                    context.Self,
                    mfa
                )
            );
    }

    private static async ValueTask<Term> Send(
        ProcessContext context,
        string operation,
        Term time,
        Term target,
        Term message
    )
    {
        if (!IsDestination(target))
        {
            return BadArgument;
        }
        if (operation == TimerLibraryFunctions.SendAfter)
        {
            if (time is Integer instant && instant.Value.IsZero)
            {
                _ = await Bif(
                    context,
                    ErlangBasicBifNames.Send,
                    target,
                    message
                );

                return Term.Tuple(Ok, Term.Tuple(Term.A(TimerReferenceKinds.Instant), await Bif(context, ErlangModuleNames.MakeReference)));
            }
            if (IsTime(time) && target is Pid && (await Bif(context, ErlangProcessQueryNames.Node, target)).Equals(await Bif(context, ErlangProcessQueryNames.Node)))
            {
                return Term.Tuple(Ok, Term.Tuple(Term.A(TimerReferenceKinds.LocalSend), await Bif(
                    context,
                    TimerModuleNames.SendAfter,
                    time,
                    target,
                    message
                )));
            }

            return await Apply(
                context,
                TimerLibraryFunctions.ApplyAfter,
                [time, Term.A(Module), Term.A(TimerLibraryFunctions.Send), Term.List(target, message)]
            );
        }
        if (!IsTime(time))
        {
            return BadArgument;
        }

        return await Request(
            context,
            TimerRequestNames.ApplyInterval,
            Term.Tuple(
                await Clock(context),
                time,
                target,
                Term.Tuple(Term.A(Module), Term.A(TimerLibraryFunctions.Send), Term.List(target, message))
            )
        );
    }

    private static async ValueTask<Term> TimeCall(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        int count = arguments.Count;
        bool listSecond = count == 2 && IsList(arguments[1]);
        bool listThird = count == 3 && IsList(arguments[2]);
        Term unit = count == 1 || listSecond || listThird ? Term.A(ErlangClockNames.Microsecond) : arguments[^1];
        Term before = await Bif(context, ErlangClockNames.MonotonicTime);
        Term result;
        int sourceArity = count == 1 || count == 2 && !listSecond ? TimerTimeCallArities.FunctionOnly : count == 2 || count == 3 && !listThird ? TimerTimeCallArities.FunctionArguments : TimerTimeCallArities.ModuleFunctionArguments;
        int callLine = sourceArity == TimerTimeCallArities.FunctionOnly ? TimerSourceLines.FunctionCallInvoke : sourceArity == TimerTimeCallArities.FunctionArguments ? TimerSourceLines.ArgumentCallInvoke : TimerSourceLines.ModuleCallInvoke;
        result = await TimerSource.Run(
            context,
            TimerLibraryFunctions.TimeCall,
            sourceArity,
            callLine,
            async () =>
        {
            if (sourceArity == TimerTimeCallArities.FunctionOnly)
            {
                if (arguments[0] is not FunctionTerm function)
                {
                    throw TimerSource.CallFailure(context, Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), arguments[0]));
                }
                if (function.Arity != 0)
                {
                    throw TimerSource.CallFailure(context, Term.Tuple(Term.A(ErlangErrorReasons.BadArity), Term.Tuple(function, Nil.Value)));
                }

                return await context.InvokeGuestFunction507(function, []);
            }

            if (sourceArity == TimerTimeCallArities.FunctionArguments && arguments[0] is FunctionTerm callback && ProperLength(arguments[1], out int length)
                && callback.Arity != length)
            {
                throw TimerSource.CallFailure(context, Term.Tuple(Term.A(ErlangErrorReasons.BadArity), Term.Tuple(callback, arguments[1])));
            }

            return sourceArity == TimerTimeCallArities.FunctionArguments ? await Bif(
                context,
                ErlangBasicBifNames.Apply,
                arguments[0],
                arguments[1]
            )
                : await Bif(
                    context,
                    ErlangBasicBifNames.Apply,
                    arguments[0],
                    arguments[1],
                    arguments[2]
                );
        }
        );
        Term after = await Bif(context, ErlangClockNames.MonotonicTime);
        Term elapsed = await TimerSource.Run(
            context,
            TimerLibraryFunctions.TimeCall,
            sourceArity,
            sourceArity == TimerTimeCallArities.FunctionOnly ? TimerSourceLines.FunctionCallConvert
                : sourceArity == TimerTimeCallArities.FunctionArguments ? TimerSourceLines.ArgumentCallConvert : TimerSourceLines.ModuleCallConvert,
            () =>
            Bif(
                context,
                ErlangClockNames.Convert,
                CoreModules.Arithmetic(ErlangOperators.Minus, after, before),
                Term.A(ErlangClockNames.Native),
                unit
            )
        );

        return Term.Tuple(elapsed, result);
    }

    private static async ValueTask<Term> Clock(ProcessContext context)
    {
        var micros = (Integer)await Bif(context, ErlangClockNames.MonotonicTime, Term.A(ErlangClockNames.Microsecond));

        return new Integer((micros.Value + TimerTimeParameters.MicrosecondsPerMillisecond - 1) / TimerTimeParameters.MicrosecondsPerMillisecond);
    }

    private static void RequireOk(Term result)
    {
        if (result is not TupleTerm { Items.Count: TimerProtocolArities.Result } tuple || !tuple.Items[0].Equals(Ok))
        {
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), result));
        }
    }
}
