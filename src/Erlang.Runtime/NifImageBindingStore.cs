using System.Runtime.CompilerServices;

namespace Erlang;

internal static class NifImageBindingStore
{
    private static readonly ConditionalWeakTable<ModuleCodeImage, NifImageBindings> bindings = new();

    internal static NifImageBindings Get(ModuleCodeImage image) => bindings.GetValue(image, static owner => new(owner));

    internal static bool IsCreated(ModuleCodeImage image) => bindings.TryGetValue(image, out _);
}
