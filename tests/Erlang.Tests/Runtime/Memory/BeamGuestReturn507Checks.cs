using System.Reflection;
using Erlang.Differential;
using static Erlang.Tests.BeamGuestReturn507Names;

namespace Erlang.Tests;

internal static class BeamGuestReturn507Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task AliasAndHost()
    {
        using var guest = GeneratedModuleCompiler.Compile(Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await DebugSame420HostAccess.Initialize(runtime);
        using (Stream input = typeof(BeamGuestReturn507Checks).Assembly.GetManifestResourceStream(Resource)!)
        using (var buffer = new MemoryStream())
        {
            input.CopyTo(buffer);
            byte[] original = buffer.ToArray();
            Check(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(original)) == OriginalSha256);
            var image = runtime.Modules.PrepareBeam(original, new BeamDependencySelection([ErLang, OriginalModule]));
            Check(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        }
        guest.Definition.Register(runtime.Modules);
        Term? retainedHost = null;
        runtime.Modules.Register(
            Probe,
            Opaque,
            1,
            async (context, arguments) =>
        {
            Check(Portable(arguments[0]));
            retainedHost = await runtime.Modules.Call(
                context,
                OriginalModule,
                Identity,
                arguments[0]
            );
            Check(Portable(retainedHost));

            return retainedHost;
        }
        );
        runtime.Modules.Register(
            Probe,
            Collect,
            0,
            (context, _) =>
        {
            CollectOwned(context);

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term result = await actor.InvokeAsync(async context =>
            await runtime.Modules.Call(
                context,
                Guest,
                Run,
                Term.I(507)
            ));
        Check(Portable(result));
        Check(result.Equals(Term.Tuple(Term.I(507), Term.List(Term.I(507)))));
        Term opaque = await actor.InvokeAsync(async context =>
            await runtime.Modules.Call(
                context,
                Guest,
                HostRoute,
                Term.I(508)
            ));
        Check(opaque.Equals(Term.A(ErlangBooleanAtoms.False)));
        ProcessContext owner = await actor.Ready;
        CollectOwned(owner);
        Check(retainedHost is not null && Portable(retainedHost));
        Check(retainedHost!.Equals(Term.Tuple(Term.I(508), Term.List(Term.I(508)))));
        Check(result.Equals(Term.Tuple(Term.I(507), Term.List(Term.I(507)))));
    }

    private static bool Portable(Term value) => value is not TupleTerm tuple
        || tuple.GetType().GetProperty(Storage, Members)!.GetValue(tuple) is null;

    private static void CollectOwned(ProcessContext context)
    {
        object heap = context.GetType().GetProperty(Heap, Members)!.GetValue(context)!;
        heap.GetType().GetMethod(HeapCollect, Members)!.Invoke(heap, null);
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
