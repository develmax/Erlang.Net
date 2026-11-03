using static Erlang.Tests.BuildProfile521Names;

namespace Erlang.Tests;

internal static class BuildProfile521Checks
{
    internal static async Task OwnBuildProfile()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term debug = await runtime.Modules.Call(
                context,
                ErlangModule,
                SystemInfo,
                Term.A(DebugCompiled)
            );
            Term counted = await runtime.Modules.Call(
                context,
                ErlangModule,
                SystemInfo,
                Term.A(LockCounting)
            );
            Term type = await runtime.Modules.Call(
                context,
                ErlangModule,
                SystemInfo,
                Term.A(BuildType)
            );
            Term emulatorType = await runtime.Modules.Call(
                context,
                ErlangModule,
                SystemInfo,
                Term.A(EmulatorType)
            );
            string expected = debug.Equals(Term.A(ErlangBooleanAtoms.True)) ? Debug
                : counted.Equals(Term.A(ErlangBooleanAtoms.True)) ? Counted : Optimized;
            Check(type.Equals(Term.A(expected)) && type.Equals(emulatorType));

            return true;
        });
    }

    internal static async Task SmpAtOneOnline()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term previous = await runtime.Modules.Call(
                context,
                ErlangModule,
                SystemFlag,
                Term.A(Online),
                Term.I(1)
            );
            try
            {
                Term online = await runtime.Modules.Call(
                    context,
                    ErlangModule,
                    SystemInfo,
                    Term.A(Online)
                );
                Term smp = await runtime.Modules.Call(
                    context,
                    ErlangModule,
                    SystemInfo,
                    Term.A(SmpSupport)
                );
                Check(online.Equals(Term.I(1)) && smp.Equals(Term.A(ErlangBooleanAtoms.True)));
            }
            finally
            {
                _ = await runtime.Modules.Call(
                    context,
                    ErlangModule,
                    SystemFlag,
                    Term.A(Online),
                    previous
                );
            }

            return true;
        });
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
