namespace Erlang.Tests;

internal static class RegexNative411ConcurrencyChecks
{
    internal static async Task LargeUnicodeResume()
    {
        await using var runtime = new ProcessRuntime();
        var engine = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules, RegexNative411Access.Admission());
        try
        {
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(async context =>
            {
                var pattern = RegexNative411Access.Pattern(await RegexNative411Access.Call(
                    context,
                    RegexNative411Names.Compile,
                    RegexNative411Names.Bytes(RegexNative411Names.UnicodeAllPattern),
                    Term.List(Term.A(RegexNative411Names.Unicode))
                ));
                var bytes = RegexNative411Names.Bytes(new string('a', 1000000));
                var result = await RegexNative411Access.Call(
                    context,
                    RegexNative411Names.Run,
                    bytes,
                    pattern
                );
                RegexNative411Access.Require(result.Equals(RegexNative411Access.Match(0, 1000000)) && engine.Report.Yields > 0);

                return true;
            });
        }
        finally
        {
            await runtime.DisposeAsync();
        }
        RegexNative411Access.Empty(engine);
    }

    internal static async Task ConcurrentRunsAndHeldCapability()
    {
        await using var runtime = new ProcessRuntime();
        var engine = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules, RegexNative411Access.Admission());
        try
        {
            await using var first = new RuntimeNodeFoundationHostActor(runtime);
            await using var second = new RuntimeNodeFoundationHostActor(runtime);
            Term pattern = await first.InvokeAsync(async context => RegexNative411Access.Pattern(await RegexNative411Access.Call(context, RegexNative411Names.Compile, RegexNative411Names.Bytes(RegexNative411Names.SingleValue))));
            var results = await Task.WhenAll(
                first.InvokeAsync(async context => await RegexNative411Access.Call(
                    context,
                    RegexNative411Names.Run,
                    RegexNative411Names.Bytes(RegexNative411Names.OffsetSubject),
                    pattern
                )),
                second.InvokeAsync(async context => await RegexNative411Access.Call(
                    context,
                    RegexNative411Names.Run,
                    RegexNative411Names.Bytes(RegexNative411Names.RepeatedSubject),
                    pattern
                ))
            );
            RegexNative411Access.Require(results[0].Equals(RegexNative411Access.Match(1, 1)) && results[1].Equals(RegexNative411Access.Match(0, 1)));
            await first.InvokeAsync(async context =>
            {
                try
                {
                    await RegexNative411Access.Call(
                        context,
                        RegexNative411Names.Compile,
                        RegexNative411Names.Bytes(RegexNative411Names.SingleValue),
                        Term.List(Term.A(RegexNative411Names.Caseless))
                    );
                    throw new InvalidOperationException(RegexNative411Names.Failed);
                }
                catch (NotSupportedException) { }

                return true;
            });
            RegexNative411Access.Require(engine.Report.MatchDataCreated == 2 && engine.Report.MatchDataFreed == 2);
        }
        finally
        {
            await runtime.DisposeAsync();
        }
        RegexNative411Access.Empty(engine);
    }
}
