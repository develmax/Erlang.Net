using System.Collections;
using System.Reflection;
using static Erlang.Tests.TrustedCapabilities507Names;

namespace Erlang.Tests;

internal static class TrustedCapabilities507Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task RepeatedGuestOnlyPreparation()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        byte[] bytes = (byte[])typeof(BeamTryHostAssets).GetMethod(ReadResource,
            BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null,
            [BeamTryHostNames.GuestResource, BeamTryHostNames.GuestSha256])!;
        // ToFrozenDictionary may return its shared Empty singleton. An empty
        // policy needs no CWT registration, including repeated preparation.
        for (int index = 0; index < 4; index++)
        {
            PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
            Check(image.NativePrimitiveExports.Count == 0);
            object execution = Get(image, Identity);
            Check(Entries(execution, NativePrimitives).Length == 0);
            Check(Entries(execution, NativeCapabilities).Length == 0);
        }
    }

    internal static async Task FactoryInheritedAndPublicOverride()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var other = OwnedGuest506RuntimeFactory.Create();
        await runtime.InitializeModulesAsync(registry =>
        {
            Check(!registry.HasExport(DebugSame420HostNames.Module, DebugSame420HostNames.Function, 2));
            var first = registry.PrepareBeam(DebugSame420HostAccess.Beam(),
                new BeamDependencySelection([DebugSame420HostNames.Runtime]));
            ErlangFunction primitive = RequireSingleCapability(registry, first);
            Check(BorrowKind(other.Modules, first, primitive) == Opaque);
            Check(registry.PublishPreparedBeam(first).Status == BeamPublicationStatus.Published);
            // The second capture must inherit the exact captured primitive and
            // its precise image policy, without re-registering a global delegate.
            var inherited = registry.PrepareBeam(DebugSame420HostAccess.Beam(),
                new BeamDependencySelection([DebugSame420HostNames.Runtime]));
            ErlangFunction inheritedPrimitive = RequireSingleCapability(registry, inherited);
            Check(ReferenceEquals(primitive, inheritedPrimitive));
            Check(!ReferenceEquals(Get(Get(first, Identity), NativePrimitives),
                Get(Get(inherited, Identity), NativePrimitives)));
            Check(BorrowKind(registry, first, primitive) == PureRead);
            Check(BorrowKind(registry, inherited, primitive) == PureRead);
            // Public republication of the same CLR delegate must not acquire the
            // internal capability of either already admitted image.
            registry.DeleteModule(DebugSame420HostNames.Module);
            registry.Register(
                DebugSame420HostNames.Module,
                DebugSame420HostNames.Function,
                2,
                primitive
            );
            PreparedBeamImage? opaque;
            try
            {
                opaque = registry.PrepareBeam(DebugSame420HostAccess.Beam(),
                    new BeamDependencySelection([DebugSame420HostNames.Runtime]));
            }
            catch (NotSupportedException)
            {
                // A public non-native entry can be rejected at preparation.
                return;
            }
            Check(Entries(Get(opaque, Identity), NativeCapabilities).Length == 0);
            Check(BorrowKind(registry, opaque, primitive) == Opaque);
        });
    }

    private static ErlangFunction RequireSingleCapability(ModuleRegistry registry, PreparedBeamImage image)
    {
        object execution = Get(image, Identity);
        object[] primitives = Entries(execution, NativePrimitives);
        object[] capabilities = Entries(execution, NativeCapabilities);
        Check(primitives.Length == 1 && capabilities.Length == 1);
        var body = (ErlangFunction)Get(primitives[0], Value);
        Check(ReferenceEquals(body, Get(capabilities[0], Key)));
        Check(Get(capabilities[0], Value).ToString() == PureRead);
        Check(BorrowKind(registry, image, body) == PureRead);

        return body;
    }

    private static object[] Entries(object target, string property) => ((IEnumerable)Get(target, property)).Cast<object>().ToArray();

    private static object Get(object target, string property) => target.GetType().GetProperty(property, Members)!.GetValue(target)!;

    private static string BorrowKind(ModuleRegistry registry, PreparedBeamImage image, ErlangFunction body)
        => registry.GetType().GetMethod(Kind, Members)!.Invoke(registry, [Get(image, Identity), body])!.ToString()!;

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
