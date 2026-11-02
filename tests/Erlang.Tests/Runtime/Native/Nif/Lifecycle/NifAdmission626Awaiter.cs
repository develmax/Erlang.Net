using System.Runtime.CompilerServices;

namespace Erlang.Tests;

// Instrument only the actual caller GetResult, not an intermediary Task owner.
internal sealed class NifAdmission626Awaiter : ICriticalNotifyCompletion
{
    private readonly NifAllocationDomainHostAdmission628 actual;
    private readonly Action result;
    private readonly Action? registered;
    internal object Raw { get; private set; } = null!;

    internal NifAdmission626Awaiter(
        Type type,
        Action result,
        Action? registered = null
    )
    {
        actual = new(type);
        this.result = result;
        this.registered = registered;
    }

    internal void Bind(object value)
    {
        Raw = value;
        actual.Bind(value);
    }

    public NifAdmission626Awaiter GetAwaiter() => this;

    public bool IsCompleted => actual.IsCompleted;

    public IDisposable GetResult()
    {
        result();

        return actual.GetResult();
    }

    public void OnCompleted(Action body)
    {
        actual.OnCompleted(body);
        registered?.Invoke();
    }

    public void UnsafeOnCompleted(Action body)
    {
        actual.UnsafeOnCompleted(body);
        registered?.Invoke();
    }
}
