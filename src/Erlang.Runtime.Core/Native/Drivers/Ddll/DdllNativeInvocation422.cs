namespace Erlang;

// Factory is private session infrastructure. It does not register an incomplete
// public module or pretend the exact seven-stub original image is ready before
// the physical host/session and native port frontend have committed.
internal sealed class DdllNativeInvocation422
{
    private readonly ProcessRuntime runtime;
    private readonly RuntimeDdllParent422 owner;
    private readonly string function;
    private readonly int arity;

    internal DdllNativeInvocation422(
        ProcessRuntime runtime,
        RuntimeDdllParent422 owner,
        string function,
        int arity
    )
    {
        this.runtime = runtime;
        this.owner = owner;
        this.function = function;
        this.arity = arity;
        Body = Invoke;
    }

    internal ErlangFunction Body { get; }

    private async ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        runtime.RequireDriverFrontendReady422(context);
        if (arguments.Count != arity) throw new ArgumentException(DriverParentWireNames422.Invalid);
        try
        {
            return function switch
            {
                DdllNativeSignatures422.Load => await owner.TryLoad(
                    context,
                    arguments[0],
                    arguments[1],
                    arguments[2]
                ).ConfigureAwait(false),
                DdllNativeSignatures422.Unload => await owner.TryUnload(context, arguments[0], arguments[1]).ConfigureAwait(false),
                DdllNativeSignatures422.Info => await owner.Info(context, arguments[0], arguments[1]).ConfigureAwait(false),
                DdllNativeSignatures422.Monitor => await owner.Monitor(context, arguments[0], arguments[1]).ConfigureAwait(false),
                DdllNativeSignatures422.Demonitor => await owner.Demonitor(context, arguments[0]).ConfigureAwait(false),
                DdllNativeSignatures422.Loaded => await owner.LoadedDrivers(context).ConfigureAwait(false),
                _ => throw new InvalidOperationException(DriverParentWireNames422.Invalid)
            };
        }
        catch (ErlangException error)
        {
            throw context.CaptureNativeFailure(
                error,
                DdllParentAtoms422.Module,
                function,
                arguments
            );
        }
    }

    internal static IReadOnlyDictionary<(string Function, int Arity), ErlangFunction> Create(
        ProcessRuntime runtime, RuntimeDdllParent422 owner)
    {
        var bodies = new Dictionary<(string Function, int Arity), ErlangFunction>();
        foreach (var signature in DdllNativeSignatures422.Stateful)
            bodies.Add(signature, new DdllNativeInvocation422(
                runtime,
                owner,
                signature.Function,
                signature.Arity
            ).Body);
        // Genuine existing Windows formatter; source capture can privately bind
        // it together with six real stateful bodies, with no public partial export.
        bodies.Add((DdllErrorNames416.Function, 1), new DdllErrorBinding416(runtime).Body);

        return bodies;
    }
}
