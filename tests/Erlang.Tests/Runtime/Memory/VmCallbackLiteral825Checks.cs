using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static class VmCallbackLiteral825Checks
{
    internal const string OrdinaryTest = "runtime/beam/vm-callback-literal825/ordinary-zeroarg-captured-literal";
    internal const string PilotTest = "runtime/beam/vm-callback-literal825/pilot-zeroarg-captured-literal";
    private const string Module = "beam_vm_callback_literal825";
    private const string Resource = "Erlang.Tests.VmCallbackLiteral825.beam_vm_callback_literal825.beam";

    internal static Task Ordinary() => Run(false);

    internal static Task Pilot() => Run(true);

    private static async Task Run(bool pilot)
    {
        byte[] bytes;
        using (Stream input = typeof(VmCallbackLiteral825Checks).Assembly.GetManifestResourceStream(Resource)
            ?? throw new InvalidOperationException("Missing genuine callback literal825 BEAM resource"))
        using (var data = new MemoryStream())
        {
            input.CopyTo(data);
            bytes = data.ToArray();
        }
        var runtime = pilot ? OwnedGuest506RuntimeFactory.Create() : new ProcessRuntime();
        try
        {
            await runtime.InitializeModulesAsync(Erlang.Otp.OtpModules.Register);
            bool actualPilot = (bool)typeof(ProcessRuntime).GetProperty(
                "OwnedTermsPilot506", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
            Require(actualPilot == pilot);
            using PreparedBeamImage image = runtime.Modules.PrepareBeam(
                bytes,
                new BeamDependencySelection([Module, "erlang"])
            );
            await runtime.InitializeModulesAsync(registry =>
            {
                Require(registry.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
                Require(ReferenceEquals(registry.SourceOwner(Module, "run", 0), image));
                Require(ReferenceEquals(registry.SourceOwner(Module, "literal", 0), image));
            });
            await using (var actor = new RuntimeNodeFoundationHostActor(runtime))
            {
                Term actual = await actor.InvokeAsync(async context =>
                    await runtime.Modules.Call(context, Module, "run"));
                Term expected = Term.Tuple(Term.A("retained"), new MapTerm([new(Term.A("a"), Term.A("b"))]));
                Require(actual.Equals(expected));
                using Stream original = typeof(VmCallbackLiteral825Checks).Assembly.GetManifestResourceStream(
                    "Erlang.Tests.VmCallbackLiteral825.original.etf")!;
                using var originalBytes = new MemoryStream();
                original.CopyTo(originalBytes);
                Require(ExternalTermFormat.Encode(actual).AsSpan().SequenceEqual(originalBytes.ToArray()));
                // The Erlang function returned only after report plus matching normal DOWN.
                await runtime.InitializeModulesAsync(registry =>
                {
                    Require(registry.DeleteModule(Module).Equals(Term.A(ErlangBooleanAtoms.True)));
                    Require(!registry.IsModuleLoaded(Module) && registry.HasOldCode(Module));
                });
                _ = await actor.InvokeAsync(async context =>
                    await runtime.Modules.Call(
                        context,
                        "erlang",
                        "purge_module",
                        Term.A(Module)
                    ));
                Require(!runtime.Modules.HasOldCode(Module));
                Require(!runtime.Modules.IsModuleLoaded(Module));
            }
        }
        finally
        {
            // Await the actual runtime's complete owned/native/module/actor retirement.
            await runtime.DisposeAsync();
        }
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException("Genuine zeroarg callback literal825 regression failed");
    }
}
