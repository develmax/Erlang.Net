// Modified: OTP-29.1.1 gen_server callback return/throw/action validation adapted to CLR.
namespace Erlang.Otp;

public sealed class ErlangGenServer(string module) : IGenServer
{
    public string Module { get; } = module;

    private async ValueTask<Term> Invoke(ProcessContext context, string function, params Term[] arguments)
    {
        try
        {
            return await context.Runtime.Modules.Call(
                context,
                Module,
                function,
                arguments
            );
        }
        catch (ErlangException exception) when (exception.ExceptionClass == ErlangExceptionClasses.Throw)
        {
            return exception.Reason;
        }
    }

    public async ValueTask<Term> Init(ProcessContext context, Term arguments)
    {
        var result = await Initialize(context, arguments);
        if (result.Error is not null)
            context.Exit(result.Error);
        if (result.State is null)
            throw Bad(Term.A(BehaviourAtoms.Ignore));

        return result.State;
    }

    public async ValueTask<ServerInitialization> Initialize(ProcessContext context, Term arguments)
    {
        Term result = await Invoke(context, BehaviourCallbacks.Init, arguments);
        if (result is Atom { Name: BehaviourAtoms.Ignore })
            return new(null, Ignore: true);
        if (result is TupleTerm tuple)
        {
            if (tuple.Items.Count == 2 && tuple.Items[0] is Atom { Name: BehaviourAtoms.Stop or BehaviourAtoms.Error })
                return new(
                    null,
                    Error: tuple.Items[1],
                    StartupExitReason: tuple.Items[0] is Atom { Name: BehaviourAtoms.Error } ? Term.A(ProcessExitReasons.Normal) : tuple.Items[1]
                );
            if (tuple.Items.Count is 2 or 3 && tuple.Items[0] is Atom { Name: BehaviourAtoms.Ok })
            {
                var action = tuple.Items.Count == 3 ? Action(tuple.Items[2], result) : (null, null);

                return new(tuple.Items[1], Timeout: action.Item1, Continue: action.Item2);
            }
        }

        throw Bad(result);
    }

    public async ValueTask<ServerResult> HandleCall(
        ProcessContext context,
        Term request,
        ServerFrom from,
        Term state
    ) => Parse(await Invoke(
        context,
        BehaviourCallbacks.HandleCall,
        request,
        Term.Tuple(from.Pid, from.Reference),
        state
    ), true);

    public async ValueTask<ServerResult> HandleCast(ProcessContext context, Term request, Term state) => Parse(await Invoke(
        context,
        BehaviourCallbacks.HandleCast,
        request,
        state
    ), false);

    public async ValueTask<ServerResult> HandleInfo(ProcessContext context, Term message, Term state)
    {
        if (!context.Runtime.Modules.Exports.Contains((Module, BehaviourCallbacks.HandleInfo, 2)))
            return new(state);

        return Parse(await Invoke(
            context,
            BehaviourCallbacks.HandleInfo,
            message,
            state
        ), false);
    }

    public async ValueTask<ServerResult> HandleContinue(ProcessContext context, Term continuation, Term state) => Parse(await Invoke(
        context,
        BehaviourCallbacks.HandleContinue,
        continuation,
        state
    ), false);

    public async ValueTask Terminate(ProcessContext context, Term reason, Term state)
    {
        if (context.Runtime.Modules.Exports.Contains((Module, BehaviourCallbacks.Terminate, 2)))
            await context.Runtime.Modules.Call(
                context,
                Module,
                BehaviourCallbacks.Terminate,
                reason,
                state
            );
    }

    private static ServerResult Parse(Term result, bool call)
    {
        if (result is TupleTerm tuple)
        {
            if (call && tuple.Items.Count is 3 or 4 && tuple.Items[0] is Atom { Name: BehaviourAtoms.Reply })
            {
                var action = tuple.Items.Count == 4 ? Action(tuple.Items[3], result) : (null, null);

                return new(
                    tuple.Items[2],
                    tuple.Items[1],
                    Timeout: action.Item1,
                    Continue: action.Item2
                );
            }
            if (tuple.Items.Count is 2 or 3 && tuple.Items[0] is Atom { Name: BehaviourAtoms.NoReply })
            {
                var action = tuple.Items.Count == 3 ? Action(tuple.Items[2], result) : (null, null);

                return new(tuple.Items[1], Timeout: action.Item1, Continue: action.Item2);
            }
            if (call && tuple.Items.Count == 4 && tuple.Items[0] is Atom { Name: BehaviourAtoms.Stop })
                return new(tuple.Items[3], tuple.Items[2], tuple.Items[1]);
            if (tuple.Items.Count == 3 && tuple.Items[0] is Atom { Name: BehaviourAtoms.Stop })
                return new(tuple.Items[2], StopReason: tuple.Items[1]);
        }

        throw Bad(result);
    }

    private static (TimeSpan?, Term?) Action(Term action, Term result)
    {
        if (action is Atom { Name: BehaviourAtoms.Infinity or BehaviourAtoms.Hibernate })
            return (null, null);
        if (action is Integer n && n.Value >= 0 && n.Value <= int.MaxValue)
            return (TimeSpan.FromMilliseconds((int)n.Value), null);
        if (action is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0] is Atom { Name: BehaviourAtoms.Continue })
            return (null, tuple.Items[1]);

        throw Bad(result);
    }

    private static ErlangException Bad(Term result) => new(Term.Tuple(Term.A(BehaviourAtoms.BadReturnValue), result), ErlangExceptionClasses.Exit);
}
