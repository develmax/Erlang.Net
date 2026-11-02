namespace Erlang.Tests;

internal static class PreparedBeamFixtureImages825
{
    internal static async Task PrepareAndPublishAsync(
        ProcessRuntime runtime,
        IEnumerable<byte[]> modules,
        BeamDependencySelection dependencies,
        Action<ModuleRegistry, PreparedBeamImage[]> publish
    )
    {
        var images = new List<PreparedBeamImage>();
        Exception? original = null;
        try
        {
            foreach (byte[] module in modules)
            {
                images.EnsureCapacity(checked(images.Count + 1));
                images.Add(runtime.Modules.PrepareBeam(module, dependencies));
            }
            PreparedBeamImage[] prepared = images.ToArray();
            await runtime.InitializeModulesAsync(registry => publish(registry, prepared));
        }
        catch (Exception error)
        {
            original = error;
            throw;
        }
        finally
        {
            var failures = new List<Exception>();
            foreach (PreparedBeamImage image in images)
            {
                try
                {
                    image.Dispose();
                }
                catch (Exception cleanup)
                {
                    failures.Add(cleanup);
                }
            }
            if (failures.Count != 0)
            {
                if (original is not null)
                    failures.Insert(0, original);
                throw new AggregateException(failures);
            }
        }
    }
}
