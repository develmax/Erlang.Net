namespace Erlang;

// Carries the original request; preparation never runs its Fill/Publish body.
internal sealed class PendingProcessInfoAdmission817
{
    internal ProcessRuntime Runtime { get; }
    internal ProcessContext Owner { get; }
    internal Pid Target { get; }
    internal ProcessInfoRequest Request { get; }
    internal Func<bool?> Callback { get; }

    internal PendingProcessInfoAdmission817(
        ProcessRuntime runtime,
        ProcessContext owner,
        Pid target,
        ProcessInfoRequest request
    )
    {
        Runtime = runtime;
        Owner = owner;
        Target = target;
        Request = request;
        Callback = Invoke;
    }

    private bool? Invoke() => Runtime.AdmitProcessInfo817(this);
}
