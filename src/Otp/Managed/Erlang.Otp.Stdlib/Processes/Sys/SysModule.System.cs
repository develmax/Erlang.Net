// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2025. All Rights Reserved.
// Modified: OTP system-message loop and callbacks adapted to .NET.
namespace Erlang;

public static partial class SysModule
{
    private static async ValueTask<Term> HandleSystem(
        ProcessContext context,
        Term systemState,
        Term message,
        Term from,
        Term parent,
        Term module,
        Term debug,
        Term misc,
        Term hibernate
    )
    {
        while (true)
        {
            Term reply = A(SysProtocolNames.Ok);
            Term? terminating = null;
            using (SysSource.Scope(
                context,
                SysApiNames.HandleSystemMsg,
                8,
                SysSourceLines.HandleCommand
            ))
            using (SysSource.Scope(
                context,
                SysSourceNames.DoCmd,
                6,
                SysSource.CommandLine(message)
            ))
            {
                if (Is(message, SysApiNames.Suspend))
                {
                    systemState = A(SysProtocolNames.Suspended);
                }
                else if (Is(message, SysApiNames.Resume))
                {
                    systemState = A(SysProtocolNames.Running);
                }
                else if (Is(message, SysApiNames.GetState))
                {
                    reply = await GetState(context, module, misc);
                }
                else if (message is TupleTerm { Items: [Atom { Name: SysApiNames.ReplaceState }, var transform] })
                {
                    (reply, misc) = await ReplaceState(
                        context,
                        transform,
                        module,
                        misc
                    );
                }
                else if (Is(message, SysApiNames.GetStatus))
                {
                    reply = await Status(
                        context,
                        systemState,
                        parent,
                        module,
                        debug,
                        misc
                    );
                }
                else if (message is TupleTerm { Items: [Atom { Name: SysProtocolNames.Debug }, var what] })
                {
                    (reply, debug) = await DebugCommand(context, what, debug);
                }
                else if (message is TupleTerm { Items: [Atom { Name: SysApiNames.Terminate }, var reason] })
                {
                    terminating = reason;
                }
                else if (Is(systemState, SysProtocolNames.Suspended) && message is TupleTerm { Items: [Atom { Name: SysApiNames.ChangeCode }, var changedModule, var version, var extra] })
                {
                    (reply, misc) = await ChangeCode(
                        context,
                        module,
                        changedModule,
                        version,
                        extra,
                        misc
                    );
                }
                else
                {
                    reply = Term.Tuple(A(SysProtocolNames.Error), Term.Tuple(A(SysProtocolNames.UnknownSystemMsg), message));
                }
            }
            using (SysSource.Scope(
                context,
                SysApiNames.HandleSystemMsg,
                8,
                terminating is not null ? SysSourceLines.HandleTerminateReply
                    : Is(systemState, SysProtocolNames.Running) ? SysSourceLines.HandleContinueReply : SysSourceLines.HandleSuspendReply
            ))
            {
                await Call(
                    context,
                    SysDependencyNames.Gen,
                    SysDependencyFunctions.Reply,
                    from,
                    reply
                );
            }
            if (terminating is not null)
            {
                return await CallModule(
                    context,
                    module,
                    SysCallbackNames.SystemTerminate,
                    terminating,
                    parent,
                    debug,
                    misc
                );
            }
            if (Is(systemState, SysProtocolNames.Running))
            {
                return await CallModule(
                    context,
                    module,
                    SysCallbackNames.SystemContinue,
                    parent,
                    debug,
                    misc
                );
            }
            bool poll = Is(hibernate, SysProtocolNames.True);
            TupleTerm? next;
            using (SysSource.Scope(
                context,
                poll ? SysApiNames.SuspendLoopHib : SysSourceNames.SuspendLoop,
                6,
                poll ? SysSourceLines.SuspendHibernateReceive : SysSourceLines.SuspendReceive
            ))
            {
                next = await ReceiveSystem(context, parent, poll);
            }
            if (next is null)
            {
                return await Hibernate(
                    context,
                    systemState,
                    parent,
                    module,
                    debug,
                    misc,
                    hibernate
                );
            }
            if (Is(next.Items[0], SysProtocolNames.Exit))
            {
                return await CallModule(
                    context,
                    module,
                    SysCallbackNames.SystemTerminate,
                    next.Items[2],
                    parent,
                    debug,
                    misc
                );
            }
            from = next.Items[1];
            message = next.Items[2];
        }
    }

    private static async ValueTask<Term> Suspend(
        ProcessContext context,
        Term state,
        Term parent,
        Term module,
        Term debug,
        Term misc,
        Term hibernate,
        bool poll
    )
    {
        TupleTerm? next;
        using (SysSource.Scope(
            context,
            SysApiNames.SuspendLoopHib,
            6,
            SysSourceLines.SuspendHibernateReceive
        ))
        {
            next = await ReceiveSystem(context, parent, poll);
        }
        if (next is null)
        {
            return await Hibernate(
                context,
                state,
                parent,
                module,
                debug,
                misc,
                hibernate
            );
        }
        if (Is(next.Items[0], SysProtocolNames.Exit))
        {
            return await CallModule(
                context,
                module,
                SysCallbackNames.SystemTerminate,
                next.Items[2],
                parent,
                debug,
                misc
            );
        }

        return await HandleSystem(
            context,
            state,
            next.Items[2],
            next.Items[1],
            parent,
            module,
            debug,
            misc,
            hibernate
        );
    }

    private static ValueTask<TupleTerm?> ReceiveSystem(ProcessContext context, Term parent, bool poll)
        => context.ReceiveAsync(
            value => value is TupleTerm { Items: [var tag, var sender, _] } tuple &&
            (Is(tag, SysProtocolNames.System) || Is(tag, SysProtocolNames.Exit) && sender.Equals(parent)) ? tuple : null,
            poll ? TimeSpan.Zero : null
        );

    private static ValueTask<Term> Hibernate(
        ProcessContext context,
        Term state,
        Term parent,
        Term module,
        Term debug,
        Term misc,
        Term hibernate
    )
        => Call(
            context,
            SysDependencyNames.ProcLib,
            SysDependencyFunctions.Hibernate,
            A(Name),
            A(SysApiNames.SuspendLoopHib),
            Term.List(
                state,
                parent,
                module,
                debug,
                misc,
                hibernate
            )
        );

    private static ValueTask<Term> CallModule(
        ProcessContext context,
        Term module,
        string function,
        params Term[] arguments
    )
        => module is Atom atom ? Call(
            context,
            atom.Name,
            function,
            arguments
        )
            : Call(
                context,
                SysDependencyNames.Erlang,
                SysDependencyFunctions.Apply,
                module,
                A(function),
                Term.List(arguments)
            );

    private static async ValueTask<bool> Exported(
        ProcessContext context,
        Term module,
        string function,
        int arity
    )
        => Is(
            await Call(
                context,
                SysDependencyNames.Erlang,
                SysDependencyFunctions.FunctionExported,
                module,
                A(function),
                Term.I(arity)
            ),
            SysProtocolNames.True
        );

    private static Term CallbackFailure(Term callback, Term failure)
        => Term.Tuple(A(SysProtocolNames.Error), Term.Tuple(A(SysProtocolNames.CallbackFailed), callback, failure));

    private static async ValueTask<Term> GetState(ProcessContext context, Term module, Term misc)
    {
        bool exported;
        using (SysSource.Scope(
            context,
            SysSourceNames.DoGetState,
            2,
            SysSourceLines.GetStateExported
        ))
        {
            exported = await Exported(
                context,
                module,
                SysCallbackNames.SystemGetState,
                1
            );
        }
        if (!exported)
        {
            return Term.Tuple(A(SysProtocolNames.Ok), misc);
        }
        Term callback = Term.Tuple(module, A(SysCallbackNames.SystemGetState));
        try
        {
            Term result = await CallModule(
                context,
                module,
                SysCallbackNames.SystemGetState,
                misc
            );

            return result is TupleTerm { Items: [Atom { Name: SysProtocolNames.Ok }, _] }
                ? result : CallbackFailure(callback, Term.Tuple(A(SysProtocolNames.BadReturn), result));
        }
        catch (ErlangException error)
        {
            return CallbackFailure(callback, Term.Tuple(A(error.ExceptionClass), error.Reason));
        }
    }

    private static async ValueTask<(Term Reply, Term Misc)> ReplaceState(
        ProcessContext context,
        Term transform,
        Term module,
        Term misc
    )
    {
        bool exported;
        using (SysSource.Scope(
            context,
            SysSourceNames.DoReplaceState,
            3,
            SysSourceLines.ReplaceStateExported
        ))
        {
            exported = await Exported(
                context,
                module,
                SysCallbackNames.SystemReplaceState,
                2
            );
        }
        Term callback = exported ? Term.Tuple(module, A(SysCallbackNames.SystemReplaceState)) : transform;
        try
        {
            Term result = exported ? await CallModule(
                context,
                module,
                SysCallbackNames.SystemReplaceState,
                transform,
                misc
            )
                : await Apply(context, transform, misc);
            if (!exported)
            {
                return (Term.Tuple(A(SysProtocolNames.Ok), result), result);
            }

            return result is TupleTerm { Items: [Atom { Name: SysProtocolNames.Ok }, var state, var next] }
                ? (Term.Tuple(A(SysProtocolNames.Ok), state), next)
                : (CallbackFailure(callback, Term.Tuple(A(SysProtocolNames.BadReturn), result)), misc);
        }
        catch (ErlangException error)
        {
            return (CallbackFailure(callback, Term.Tuple(A(error.ExceptionClass), error.Reason)), misc);
        }
    }

    private static async ValueTask<Term> Status(
        ProcessContext context,
        Term state,
        Term parent,
        Term module,
        Term debug,
        Term misc
    )
    {
        Term dictionary = await Call(context, SysDependencyNames.Erlang, SysDependencyFunctions.Get);
        Term formatted = misc;
        bool exported;
        using (SysSource.Scope(
            context,
            SysApiNames.GetStatus,
            5,
            SysSourceLines.StatusExported
        ))
        {
            exported = await Exported(
                context,
                module,
                SysCallbackNames.FormatStatus,
                2
            );
        }
        if (exported)
        {
            using var scope = SysSource.Scope(
                context,
                SysApiNames.GetStatus,
                5,
                SysSourceLines.StatusCallback
            );
            formatted = await CallModule(
                context,
                module,
                SysCallbackNames.FormatStatus,
                A(SysProtocolNames.Normal),
                Term.List(
                    dictionary,
                    state,
                    parent,
                    debug,
                    misc
                )
            );
        }

        return Term.Tuple(
            A(SysProtocolNames.Status),
            context.Self,
            Term.Tuple(A(SysProtocolNames.Module), module),
            Term.List(
                dictionary,
                state,
                parent,
                debug,
                formatted
            )
        );
    }

    private static async ValueTask<(Term Reply, Term Misc)> ChangeCode(
        ProcessContext context,
        Term module,
        Term changedModule,
        Term version,
        Term extra,
        Term misc
    )
    {
        Term result;
        try
        {
            using var scope = SysSource.Scope(
                context,
                SysSourceNames.DoChangeCode,
                5,
                SysSourceLines.CodeChangeCallback
            );
            result = await CallModule(
                context,
                module,
                SysCallbackNames.SystemCodeChange,
                misc,
                changedModule,
                version,
                extra
            );
        }
        catch (ErlangException error)
        {
            if (error.ExceptionClass == ErlangExceptionClasses.Throw)
            {
                result = error.Reason;
            }
            else
            {
                Term exit = error.ExceptionClass == ErlangExceptionClasses.Error
                    ? Term.Tuple(error.Reason, error.StackTraceTerm ?? Nil.Value) : error.Reason;

                return (Term.Tuple(A(SysProtocolNames.Error), Term.Tuple(A(SysProtocolNames.Exit), exit)), misc);
            }
        }

        return result is TupleTerm { Items: [Atom { Name: SysProtocolNames.Ok }, var next] }
            ? (A(SysProtocolNames.Ok), next) : (Term.Tuple(A(SysProtocolNames.Error), result), misc);
    }
}
