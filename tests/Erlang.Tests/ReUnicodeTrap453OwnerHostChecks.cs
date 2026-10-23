using System.Text;
using Erlang;

namespace Erlang.Tests;

internal static class ReUnicodeTrap453OwnerHostChecks
{
    internal static async Task Run()
    {
        // Independent fresh owners: no source image first; explicit non-BEAM stand-ins second.
        await Refused(false);
        await Refused(true);
    }

    private static async Task Refused(bool substitute)
    {
        await using var runtime = new ProcessRuntime();
        RuntimeRegexEngine411 engine = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(
            runtime, runtime.Modules, RegexNative411Access.Admission());
        int wrapperCalls = 0;
        if (substitute)
            await runtime.InitializeModulesAsync(registry =>
            {
                registry.Register(
                    ReUnicodeOriginal453Names.Re,
                    ReUnicodeOriginal453Names.UnicodeCompile,
                    2,
                    (_, _) =>
                    {
                        Interlocked.Increment(ref wrapperCalls);

                        return ValueTask.FromResult<Term>(Term.A(ReUnicodeOriginal453Names.UnexpectedWrapper));
                    }
                );
                registry.Register(
                    ReUnicodeOriginal453Names.Re,
                    ReUnicodeOriginal453Names.UnicodeRun,
                    3,
                    (_, _) =>
                    {
                        Interlocked.Increment(ref wrapperCalls);

                        return ValueTask.FromResult<Term>(Term.A(ReUnicodeOriginal453Names.UnexpectedWrapper));
                    }
                );
                ReUnicodeOriginal453Access.Require(registry.SourceOwner(ReUnicodeOriginal453Names.Re,
                    ReUnicodeOriginal453Names.UnicodeCompile, 2) is not PreparedBeamImage);
                ReUnicodeOriginal453Access.Require(registry.SourceOwner(ReUnicodeOriginal453Names.Re,
                    ReUnicodeOriginal453Names.UnicodeRun, 3) is not PreparedBeamImage);
            });
        try
        {
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(async context =>
            {
                Term unicode = Term.List(Term.A(RegexNative411Names.Unicode));
                var beforeCompile = engine.Report;
                bool compileRefused = false;
                try
                {
                    await engine.CompileAsync(context, Term.String(ReUnicodeOriginal453Names.BinaryPattern), unicode);
                }
                catch (NotSupportedException)
                {
                    compileRefused = true;
                }
                var afterCompile = engine.Report;
                ReUnicodeOriginal453Access.Require(compileRefused && afterCompile.Active == 0
                    && afterCompile.CodesCreated == beforeCompile.CodesCreated
                    && afterCompile.MatchDataCreated == beforeCompile.MatchDataCreated);
                Term pattern = RegexNative411Access.Pattern(await engine.CompileAsync(context,
                    new BitString(Encoding.ASCII.GetBytes(ReUnicodeOriginal453Names.BinaryPattern)), unicode));
                var beforeRun = engine.Report;
                bool runRefused = false;
                try
                {
                    await engine.RunAsync(
                        context,
                        Term.String(ReUnicodeOriginal453Names.BinarySubject),
                        pattern,
                        Nil.Value
                    );
                }
                catch (NotSupportedException)
                {
                    runRefused = true;
                }
                var afterRun = engine.Report;
                ReUnicodeOriginal453Access.Require(runRefused && afterRun.Active == 0
                    && afterRun.RetainedCodes == beforeRun.RetainedCodes
                    && afterRun.CodesCreated == beforeRun.CodesCreated
                    && afterRun.CodesFreed == beforeRun.CodesFreed
                    && afterRun.MatchDataCreated == beforeRun.MatchDataCreated);
                Term usable = await engine.RunAsync(
                    context,
                    new BitString(Encoding.ASCII.GetBytes(ReUnicodeOriginal453Names.BinarySubject)),
                    pattern,
                    Nil.Value
                );
                ReUnicodeOriginal453Access.Require(usable.Equals(RegexNative411Access.Match(0, 3)) && wrapperCalls == 0);

                return true;
            }).WaitAsync(ReUnicodeOriginal453Names.Deadline);
        }
        finally
        {
            await runtime.DisposeAsync().AsTask().WaitAsync(ReUnicodeOriginal453Names.Deadline);
        }
        RegexNative411Access.Empty(engine);
    }
}
