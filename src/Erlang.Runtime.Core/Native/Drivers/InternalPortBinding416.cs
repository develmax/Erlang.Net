namespace Erlang;

internal sealed class InternalPortBinding416
{
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }
    private readonly string function;
    private readonly int arity;

    internal InternalPortBinding416(ProcessRuntime runtime, string function, int arity)
    {
        Runtime = runtime;
        this.function = function;
        this.arity = arity;
        if (!InternalPortNames416.Signatures.Contains((function, arity)))
            throw new ArgumentException(InternalPortNames416.Foreign, nameof(function));
        Body = Invoke;
    }

    private async ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Runtime.RequireInternalPortActor416(context);
        if (arguments.Count != arity)
            throw new ArgumentException(InternalPortNames416.Foreign, nameof(arguments));
        Term result;
        try
        {
            // These are the real port-owner operations. Internal entries return
            // their native status atoms; the original erlang wrappers raise errors.
            result = function switch
            {
                PortNames.Open => Runtime.Ports.Open(context, arguments[0], arguments[1]),
                PortNames.Command => await Runtime.Ports.Command(
                    arguments[0],
                    arguments[1],
                    arguments[2],
                    context
                ),
                PortNames.Connect => await Connect(context, arguments),
                PortNames.Close => await Runtime.Ports.CloseOrdered(
                    arguments[0],
                    context.Cancellation,
                    context
                ),
                PortNames.Info => await Info(context, arguments),
                _ => throw new InvalidOperationException(InternalPortNames416.Conflict)
            };
        }
        catch (ErlangException error) when (!error.ExplicitStackTrace)
        {
            // Native /1 uses badarg for a non-port type; /2 distinguishes
            // badtype so the original wrapper can attach its cause field.
            result = function == PortNames.Info && arity == 1
                && error.Reason is Atom { Name: PortNames.BadType }
                ? Term.A(ErlangErrorReasons.BadArgument) : error.Reason;
        }
        // Keep signal admission outside status translation: an actor exit must
        // unwind the actor rather than turn into an ordinary native return value.
        Runtime.AdmitSignals(context);

        return result;
    }

    private async ValueTask<Term> Info(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        return await Runtime.Ports.InfoAsync454(context, arguments[0], arity == 1 ? null : arguments[1]);
    }

    private async ValueTask<Term> Connect(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (Runtime.DriverFrontendForPort422(arguments[0]) is null)
            await Runtime.Ports.AwaitCommands(arguments[0], context.Cancellation, context);

        return await Runtime.Ports.ConnectOrdered455(context, arguments[0], arguments[1]);
    }
}
