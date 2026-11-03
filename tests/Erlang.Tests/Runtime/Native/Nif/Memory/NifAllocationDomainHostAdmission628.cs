using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

// A transparent awaiter adapter for the actual internal admission. In particular,
// GetResult executes in the caller's continuation and transfers real custody once.
internal sealed class NifAllocationDomainHostAdmission628 : ICriticalNotifyCompletion
{
    private object admission = null!;
    private readonly PropertyInfo completed;
    private readonly MethodInfo result;
    private ICriticalNotifyCompletion continuation = null!;

    internal NifAllocationDomainHostAdmission628(Type type)
    {
        if (!typeof(ICriticalNotifyCompletion).IsAssignableFrom(type))
            throw new InvalidOperationException(NifAllocationDomainHostNames.ReflectionContract);
        completed = type.GetProperty(nameof(IsCompleted), NifAllocationDomainHostNames.Members)
            ?? throw new InvalidOperationException(NifAllocationDomainHostNames.ReflectionContract);
        result = type.GetMethod(nameof(GetResult), NifAllocationDomainHostNames.Members)
            ?? throw new InvalidOperationException(NifAllocationDomainHostNames.ReflectionContract);
    }

    internal void Bind(object value)
    {
        admission = value;
        continuation = (ICriticalNotifyCompletion)value;
    }

    public NifAllocationDomainHostAdmission628 GetAwaiter() => this;

    public bool IsCompleted => (bool)completed.GetValue(admission)!;

    public IDisposable GetResult()
    {
        try
        {
            return (IDisposable)result.Invoke(admission, null)!;
        }
        catch (TargetInvocationException error) when (error.InnerException is { } failure)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
            throw;
        }
    }

    public void OnCompleted(Action body) => continuation.OnCompleted(body);

    public void UnsafeOnCompleted(Action body) => continuation.UnsafeOnCompleted(body);
}
