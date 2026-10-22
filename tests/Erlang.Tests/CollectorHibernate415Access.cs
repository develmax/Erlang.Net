using System.Collections;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class CollectorHibernate415Access
{
    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(CollectorHibernate415Names.Failure);
    }

    internal static object? Field(object value, string name) => ManagedCollectors413Access.Field(value, name);

    internal static object? Member(object value, string name) => ManagedCollectors413Access.Property(value, name);

    internal static async Task Initialize(ProcessRuntime runtime)
    {
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(
            ManagedCollectors413Access.Resource(ManagedCollectors413Names.Manifest))!;
        Require(hashes.Count == 2);
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapFileSystem.Register(runtime, registry);
            var images = new List<PreparedBeamImage>();
            foreach (string module in new[] { ManagedCollectors413Names.Literal, ManagedCollectors413Names.Trace })
            {
                byte[] bytes = ManagedCollectors413Access.Resource(module + StandardBootOriginal412Names.Beam);
                Require(Convert.ToHexString(SHA256.HashData(bytes)) == hashes[module + StandardBootOriginal412Names.Beam]);
                PreparedBeamImage image = registry.PrepareBeam(bytes, new BeamDependencySelection([ManagedCollectors413Names.Erlang, CollectorHibernate415Names.Erts]));
                Require(image.Name == module && image.NativePrimitiveExports.Count == 2);
                images.Add(image);
            }
            Require(registry.PublishPreparedBeam(images.ToArray()).Status == BeamPublicationStatus.Published);
        });
    }

    internal static CollectorHibernate415State? Capture(ProcessRuntime runtime, string module, string slotName)
    {
        object? owner = runtime.Modules.SourceOwner(module, ManagedCollectors413Names.Start, 0);
        lock (Field(runtime, CollectorHibernate415Names.Gate)!)
        {
            object slot = Field(runtime, slotName)!;
            if (Member(slot, ManagedCollectors413Names.Actor) is not ProcessContext context
                || Member(slot, ManagedCollectors413Names.Source) is not PreparedBeamImage source
                || !ReferenceEquals(source, owner) || !ReferenceEquals(context.Runtime, runtime)) return null;
            object execution = Member(source, CollectorHibernate415Names.ExecutionIdentity)!;
            var image = (ModuleCodeImage)Member(execution, CollectorHibernate415Names.Image)!;

            return new(
                module,
                slot,
                context,
                source,
                image
            );
        }
    }

    internal static bool Lease(CollectorHibernate415State state)
    {
        lock (Field(state.Context, CollectorHibernate415Names.CodeGate)!)
            return ((IDictionary)Field(state.Context, CollectorHibernate415Names.ActiveCode)!).Contains(state.Image);
    }

    internal static bool Alive(ProcessRuntime runtime, CollectorHibernate415State state)
    {
        lock (Field(runtime, CollectorHibernate415Names.Gate)!)
        {
            var done = (TaskCompletionSource<Term>)Member(state.Context, CollectorHibernate415Names.Done)!;
            bool system = (bool)ManagedCollectors413Access.Invoke(
                runtime,
                typeof(ProcessRuntime),
                CollectorHibernate415Names.System,
                state.Context.Self
            )!;

            return !done.Task.IsCompleted && system
                && ReferenceEquals(Member(state.Slot, ManagedCollectors413Names.Actor), state.Context)
                && ReferenceEquals(Member(state.Slot, ManagedCollectors413Names.Source), state.Source);
        }
    }

    internal static bool Waiting(CollectorHibernate415State state)
        => (bool)Field(state.Context, CollectorHibernate415Names.Waiting)!;

    internal static async Task Until(Func<bool> condition, TimeSpan deadline)
    {
        DateTime end = DateTime.UtcNow + deadline;
        while (!condition())
        {
            Require(DateTime.UtcNow < end);
            await Task.Delay(CollectorHibernate415Names.PollInterval);
        }
    }

    internal static void Phase(string output, string phase, object detail)
    {
        string value = JsonSerializer.Serialize(new { Phase = phase, Detail = detail });
        File.WriteAllText(Path.Combine(output, CollectorHibernate415Names.Phase), value);
        File.WriteAllText(Path.Combine(output, CollectorHibernate415Names.JournalPrefix + phase + CollectorHibernate415Names.Json), value);
    }
}
