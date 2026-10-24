namespace Erlang.Tests;

internal static class RegexNative411Checks
{
    internal static async Task NativeAndErrors()
    {
        await using var runtime = new ProcessRuntime();
        var engine = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules, RegexNative411Access.Admission());
        try
        {
            RegexNative411Access.Require(runtime.NativePrimitives.Bindings.Count(b => b.Module == RegexNative411Names.Module) == 5);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(async context =>
            {
                var pattern = RegexNative411Access.Pattern(await RegexNative411Access.Call(
                    context,
                    RegexNative411Names.Compile,
                    RegexNative411Names.Bytes(RegexNative411Names.PrefixPattern),
                    Term.List(Term.A(RegexNative411Names.Unicode))
                ));
                RegexNative411Access.Require((await RegexNative411Access.Call(
                    context,
                    RegexNative411Names.Run,
                    RegexNative411Names.Bytes(RegexNative411Names.PrefixSubject),
                    pattern
                )).Equals(RegexNative411Access.Match(0, 3)));
                RegexNative411Access.Require((await RegexNative411Access.Call(
                    context,
                    RegexNative411Names.Run,
                    RegexNative411Names.Bytes(RegexNative411Names.NonMatchSubject),
                    pattern,
                    Nil.Value
                )).Equals(Term.A(RegexNative411Names.NoMatch)));
                var syntax = await RegexNative411Access.Call(context, RegexNative411Names.Compile, RegexNative411Names.Bytes(RegexNative411Names.InvalidPattern));
                RegexNative411Access.Require(syntax is TupleTerm result && result.Items[0].Equals(Term.A(RegexNative411Names.ErrorAtom)));
                var version = await RegexNative411Access.Call(context, RegexNative411Names.Version);
                RegexNative411Access.Require(version.Equals(RegexNative411Names.Bytes(RegexNative411Names.ExpectedVersion)));
                try
                {
                    await RegexNative411Access.Call(
                        context,
                        RegexNative411Names.Compile,
                        Term.A(RegexNative411Names.InvalidAtom),
                        Nil.Value
                    );
                    throw new InvalidOperationException(RegexNative411Names.Failed);
                }
                catch (ErlangException error)
                {
                    RegexNative411Access.Require(error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
                    var frame = (TupleTerm)Cons.Items(error.StackTraceTerm).First();
                    RegexNative411Access.Require(frame.Items.Count == 4 && frame.Items[0].Equals(Term.A(RegexNative411Names.Module)) && frame.Items[1].Equals(Term.A(RegexNative411Names.Compile))
                        && frame.Items[2].Equals(Term.List(Term.A(RegexNative411Names.InvalidAtom), Nil.Value)));
                }

                return true;
            });
        }
        finally
        {
            await runtime.DisposeAsync();
        }
        RegexNative411Access.Empty(engine);
    }

    internal static async Task OwnershipAndRollback()
    {
        await using var runtime = new ProcessRuntime();
        var engine = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(runtime, runtime.Modules, RegexNative411Access.Admission());
        try
        {
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            Term? retained = null;
            await actor.InvokeAsync(async context =>
            {
                retained = RegexNative411Access.Pattern(await RegexNative411Access.Call(
                    context,
                    RegexNative411Names.Compile,
                    RegexNative411Names.Bytes(RegexNative411Names.SingleValue),
                    Nil.Value
                ));
                var tuple = (TupleTerm)retained;
                Term forged = Term.Tuple(
                    tuple.Items[0],
                    Term.I(100),
                    tuple.Items[2],
                    tuple.Items[3],
                    tuple.Items[4]
                );
                try
                {
                    await RegexNative411Access.Call(
                        context,
                        RegexNative411Names.Run,
                        RegexNative411Names.Bytes(RegexNative411Names.SingleValue),
                        forged
                    );
                    throw new InvalidOperationException(RegexNative411Names.Failed);
                }
                catch (NotSupportedException) { }

                return true;
            });
            await using var foreign = new ProcessRuntime();
            await using var other = new RuntimeNodeFoundationHostActor(foreign);
            await other.InvokeAsync(async context =>
            {
                try
                {
                    await engine.RunAsync(
                        context,
                        RegexNative411Names.Bytes(RegexNative411Names.SingleValue),
                        retained!,
                        Nil.Value
                    );
                    throw new InvalidOperationException(RegexNative411Names.Failed);
                }
                catch (ArgumentException) { }

                return true;
            });
            await runtime.DisposeAsync();
            RegexNative411Access.Empty(engine);
            GC.KeepAlive(retained); // External guest resource remains alive but cannot keep native code/DLL live after settled close.
        }
        finally
        {
            await runtime.DisposeAsync();
        }
        await using var nested = new ProcessRuntime();
        bool nestedRefused = false;
        try
        {
            await nested.InitializeModulesAsync(registry => RuntimeNativePrimitiveProviders.RegisterRegexAsync411(nested, registry, RegexNative411Access.Admission()).AsTask().GetAwaiter().GetResult());
        }
        catch (InvalidOperationException)
        {
            nestedRefused = true;
        }
        RegexNative411Access.Require(nestedRefused && !nested.Modules.IsModuleLoaded(RegexNative411Names.Module));
        await using var displaced = new ProcessRuntime();
        displaced.Modules.Register(
            RegexNative411Names.Module,
            RegexNative411Names.Version,
            0,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        bool refused = false;
        try
        {
            await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(displaced, displaced.Modules, RegexNative411Access.Admission());
        }
        catch (InvalidOperationException)
        {
            refused = true;
        }
        RegexNative411Access.Require(refused && !System.Diagnostics.Process.GetCurrentProcess().Modules.Cast<System.Diagnostics.ProcessModule>().Any(module => Path.GetFileName(module.FileName).Equals(RegexNative411Names.BeamImage, StringComparison.OrdinalIgnoreCase)));
        RegexNative411Access.Require(displaced.NativePrimitives.Bindings.All(binding => binding.Module != RegexNative411Names.Module));
    }
}
