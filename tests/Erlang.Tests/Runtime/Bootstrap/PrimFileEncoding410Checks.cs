using Erlang;
using static Erlang.Tests.PrimFileEncoding410Names;
using static Erlang.Tests.PrimFileEncoding410Access;

namespace Erlang.Tests;

internal static class PrimFileEncoding410Checks
{
    internal static async Task Routing()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var foreign = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(foreign);
        foreach (string name in new[] { ToNative, FromNative, Normalize, Translatable })
        {
            EtsFoundation405Access.Require(Capture(runtime, name) is not null);
            EtsFoundation405Access.Require(Capture(runtime, name, 0) is null);
            EtsFoundation405Access.Require(!runtime.Modules.HasExport(ModuleName, name, 1));
        }
        EtsFoundation405Access.Require(Capture(runtime, Missing) is null);
        ErlangFunction body = Capture(runtime, ToNative)!;
        await actor.InvokeAsync(async context =>
        {
            bool refused = false;
            try
            {
                _ = await body(context, [Nil.Value]);
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            EtsFoundation405Access.Require(refused);

            return true;
        });
    }

    internal static async Task Words()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ErlangFunction to = Capture(runtime, ToNative)!, from = Capture(runtime, FromNative)!, translatable = Capture(runtime, Translatable)!;
        await actor.InvokeAsync(async context =>
        {
            byte[] expected = OperatingSystem.IsWindows() ? [65, 0, 66, 0, 0, 0] : [65, 66, 0];
            EtsFoundation405Access.Require((await to(context, [Term.List(Term.I(65), Term.List(Term.I(66)))])).Equals(new BitString(expected)));
            await BadArgument(() => to(context, [Term.A(Missing)]));
            await BadArgument(() => to(context, [Term.List(new BitString(new byte[] { 65 }))]));
            await BadArgument(() => to(context, [new Cons(Term.I(65), Term.I(66))]));
            await BadArgument(() => to(context, [Term.List(Term.I(0))]));
            await BadArgument(() => from(context, [Nil.Value]));
            EtsFoundation405Access.Require((await translatable(context, [new BitString(new byte[] { 255 })])).Equals(Term.A(ErlangBooleanAtoms.False)));
            if (OperatingSystem.IsWindows())
            {
                EtsFoundation405Access.Require((await from(context, [new BitString(new byte[] { 65, 0, 255 })])).Equals(Term.List(Term.I(65), Term.I(255))));
                EtsFoundation405Access.Require((await from(context, [new BitString(new byte[] { 0, 216, 0, 0 })])).Equals(Term.List(Term.I(55296), Term.I(0))));
                EtsFoundation405Access.Require((await to(context, [Term.List(Term.I(55296))])).Equals(new BitString(new byte[] { 0, 216, 0, 0 })));
                await BadArgument(() => to(context, [Term.List(Term.I(128512))]));
            }

            return true;
        });
    }

    internal static async Task NormalizeSource()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ErlangFunction normalize = Capture(runtime, Normalize)!;
        await actor.InvokeAsync(async context =>
        {
            EtsFoundation405Access.Require((await normalize(context, [new BitString(new byte[] { 101, 204, 129 })])).Equals(Term.List(Term.I(233))));
            EtsFoundation405Access.Require((await normalize(context, [new BitString(new byte[] { 225, 132, 128, 225, 133, 161 })])).Equals(Term.List(Term.I(4352), Term.I(4449))));
            EtsFoundation405Access.Require((await normalize(context, [new BitString(new byte[] { 240, 159, 152, 128 })])).Equals(Term.List(Term.I(128512))));
            await BadArgument(() => normalize(context, [new BitString(new byte[] { 255 })]));
            await BadArgument(() => normalize(context, [Term.List(Term.I(65))]));

            return true;
        });
    }
}
