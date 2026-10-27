using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static class OwnedGuest506RuntimeFactory
{
    internal static ProcessRuntime Create(bool enabled = true, RuntimeNativeMetadataImage? nativeMetadata = null)
    {
        var options = new ProcessRuntimeOptions { NativeMetadataImage = nativeMetadata };
        var property = typeof(ProcessRuntimeOptions).GetProperty(
            OwnedGuest506Names.PrivateOption,
            BindingFlags.Instance | BindingFlags.NonPublic
        ) ?? throw new InvalidOperationException(OwnedGuest506Names.FactoryFailure);
        property.SetValue(options, enabled);

        return new ProcessRuntime(options);
    }
}
