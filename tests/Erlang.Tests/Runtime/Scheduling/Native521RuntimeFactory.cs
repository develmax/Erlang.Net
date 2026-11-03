using System.Reflection;

namespace Erlang.Tests;

internal static class Native521RuntimeFactory
{
    internal static ProcessRuntime Create(bool owned)
    {
        var options = new ProcessRuntimeOptions { DirtyIoSchedulerCount = DirtyIoConcurrency522Names.ProfileIoWorkers };
        var property = typeof(ProcessRuntimeOptions).GetProperty(
            OwnedGuest506Names.PrivateOption,
            BindingFlags.Instance | BindingFlags.NonPublic
        ) ?? throw new InvalidOperationException(OwnedGuest506Names.FactoryFailure);
        property.SetValue(options, owned);

        return new ProcessRuntime(options);
    }
}
