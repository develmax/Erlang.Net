using System.Security.Cryptography;
namespace Erlang.Tests;

internal static class DistributionProfile429Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DistributionProfile429Names.Failure);
    }

    private static ValueTask<Term> Read(ProcessContext c) => c.Runtime.Modules.Call(c, DistributionProfile429Names.Module, DistributionProfile429Names.Function);

    internal static async Task Binding()
    {
        await using var runtime = new ProcessRuntime();
        Require(!runtime.Modules.HasExport(DistributionProfile429Names.Module, DistributionProfile429Names.Function, 0));
        await runtime.InitializeModulesAsync(registry =>
      {
          RuntimeBootstrapDistributionProfile429.Register(runtime, registry);
          RuntimeBootstrapDistributionProfile429.Register(runtime, registry);
      });
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(registry => RuntimeBootstrapDistributionProfile429.Register(foreign, registry));
        bool rejected = false;
        try
        {
            RuntimeBootstrapDistributionProfile429.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        Require(rejected);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        Term first = await actor.InvokeAsync(async c => await Read(c));
        Term again = await actor.InvokeAsync(async c => await Read(c));
        Term elsewhere = await other.InvokeAsync(async c => await Read(c));
        Require(ReferenceEquals(first, again) && !ReferenceEquals(first, elsewhere) && first.Equals(elsewhere));
        Require(first is TupleTerm { Items.Count: 6 } t && t.Items[0].Equals(Term.A(DistributionProfile429Names.Record)));
        Require(runtime.Modules.SourceOwner(DistributionProfile429Names.Module, DistributionProfile429Names.Function, 0) is null);
    }

    internal static async Task Codec()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDistributionProfile429.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async c =>
        {
            TupleTerm profile = (TupleTerm)await Read(c);
            ulong flags = (ulong)((Integer)profile.Items[1]).Value;
            Require(flags == DistributionCapabilityProfile429.Preferred && (flags & DistributionPassThroughFlags.Unsupported) == 0);
            var message = new DistributionPassThroughMessage(Term.Tuple(Term.I(DistributionControlOpcodes.SendSender), c.Self, c.Self), Term.A(DistributionProfile429Names.Payload));
            byte[] packet = DistributionPassThroughCodec.Encode(message, negotiatedFlags: flags);
            DistributionPassThroughMessage decoded = DistributionPassThroughCodec.Decode(packet, negotiatedFlags: flags);
            Require(decoded.Control.Equals(message.Control) && decoded.Payload.Equals(message.Payload));
            foreach (ulong invalid in new[] { flags & ~DistributionPassThroughFlags.Utf8Atoms, flags | DistributionPassThroughFlags.AtomCacheHeader, flags | DistributionPassThroughFlags.Fragments, flags & ~DistributionPassThroughFlags.SendSender })
            {
                bool denied = false;
                try
                {
                    DistributionPassThroughCodec.Encode(message, negotiatedFlags: invalid);
                }
                catch (NotSupportedException)
                {
                    denied = true;
                }
                Require(denied);
            }

            return true;
        });
    }

    internal static async Task OriginalReaders()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapDistributionProfile429.Register(runtime, registry);
            using var input = typeof(DistributionProfile429Checks).Assembly.GetManifestResourceStream(DistributionProfile429Names.Resource)!;
            using var buffer = new MemoryStream();
            input.CopyTo(buffer);
            byte[] bytes = buffer.ToArray();
            Require(Convert.ToHexString(SHA256.HashData(bytes)) == DistributionProfile429Names.Hash);
            PreparedBeamImage original = registry.PrepareBeam(bytes, new BeamDependencySelection(DistributionProfile429Names.Imports));
            Require(original.NativePrimitiveExports.Count == 0 && registry.PublishPreparedBeam([original]).Status == BeamPublicationStatus.Published);
            Require(ReferenceEquals(registry.SourceOwner(DistributionProfile429Names.DistUtil, DistributionProfile429Names.Strict, 0), original));
        });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async c =>
        {
            TupleTerm value = (TupleTerm)await Read(c);
            Require((await runtime.Modules.Call(c, DistributionProfile429Names.DistUtil, DistributionProfile429Names.Strict)).Equals(value.Items[5]));
            Require((await runtime.Modules.Call(c, DistributionProfile429Names.DistUtil, DistributionProfile429Names.Reject)).Equals(value.Items[4]));

            return true;
        });
    }
}
