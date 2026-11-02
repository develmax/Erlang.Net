namespace Erlang;

public sealed partial class ModuleRegistry
{
    private Dictionary<(string Module, string Function, int Arity), ErlangFunction> CaptureStartupPreparation802(
        RuntimeLibrarySelection request,
        Dictionary<string, ModuleCodeImage> bootstrap
    )
    {
        RequireAsyncSourceCaller();
        owningRuntime?.RequireModuleLiteralPreparation800();
        lock (gate)
        {
            ThrowIfSourceClosing();
            var bindings = new Dictionary<(string Module, string Function, int Arity), ErlangFunction>();
            foreach (RuntimeModuleSelection choice in request.Modules)
            {
                ModuleCodeImage? image = RequireStartupOwner(choice);
                if (image is not null) bootstrap.Add(choice.Module, image);
                foreach (var entry in functions.Where(entry => entry.Key.Module == choice.Module))
                    bindings.Add(entry.Key, entry.Value);
            }

            return bindings;
        }
    }

    private void ValidateStartupPreparation802(RuntimeLibraryPreparedSelection plan)
    {
        lock (gate)
        {
            ThrowIfSourceClosing();
            foreach (RuntimeModuleSelection choice in plan.Request.Modules)
            {
                ModuleCodeImage? current = RequireStartupOwner(choice);
                ModuleCodeImage? expected = plan.BootstrapImages.GetValueOrDefault(choice.Module);
                if (!ReferenceEquals(current, expected))
                    throw new InvalidOperationException(StartupPreparation802Names.Changed);
                var actualBindings = functions.Where(entry => entry.Key.Module == choice.Module).ToArray();
                var expectedBindings = plan.BootstrapBindings802.Where(entry => entry.Key.Module == choice.Module).ToArray();
                if (actualBindings.Length != expectedBindings.Length || actualBindings.Any(entry =>
                    !plan.BootstrapBindings802.TryGetValue(entry.Key, out ErlangFunction? body) || !ReferenceEquals(body, entry.Value)))
                    throw new InvalidOperationException(StartupPreparation802Names.Changed);
            }
        }
    }
}
