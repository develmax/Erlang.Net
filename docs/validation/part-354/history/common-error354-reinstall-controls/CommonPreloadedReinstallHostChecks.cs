using System.Collections;
using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static class CommonPreloadedReinstallHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var registry = new ModuleRegistry();
        ErlangException firstError = new(Term.A(CommonPreloadedReinstallHostNames.Fault));
        ErlangException secondError = new(Term.A(CommonPreloadedReinstallHostNames.Fault));
        ErlangFunction firstBody = (_, _) => throw firstError;
        ErlangFunction secondBody = (_, _) => throw secondError;
        registry.InstallPreloaded(target => target.Register(CommonPreloadedReinstallHostNames.Module, CommonPreloadedReinstallHostNames.Function, 0, firstBody));
        ModuleCodeImage firstImage = registry.CurrentCodeImage(CommonPreloadedReinstallHostNames.Module)!;
        object firstBinding = Bindings(registry)[Key()]!;
        await Check(actor, registry, firstError);
        try
        {
            registry.Apply(outer =>
            {
                Require(outer.DeleteModule(CommonPreloadedReinstallHostNames.Module).Equals(Term.A("true")));
                Require(!Bindings(outer).Contains(Key()));
                outer.InstallPreloaded(inner => inner.Register(CommonPreloadedReinstallHostNames.Module, CommonPreloadedReinstallHostNames.Function, 0, secondBody));
                throw new InvalidOperationException(CommonPreloadedReinstallHostNames.Rollback);
            });
            throw new Exception(CommonPreloadedReinstallHostNames.MissingRollback);
        }
        catch (InvalidOperationException error) when (error.Message == CommonPreloadedReinstallHostNames.Rollback)
        {
        }
        Require(ReferenceEquals(registry.CurrentCodeImage(CommonPreloadedReinstallHostNames.Module), firstImage));
        Require(ReferenceEquals(Bindings(registry)[Key()], firstBinding));
        Require(!registry.HasOldCode(CommonPreloadedReinstallHostNames.Module));
        await Check(actor, registry, firstError);
        Require(registry.DeleteModule(CommonPreloadedReinstallHostNames.Module).Equals(Term.A("true")));
        Require(!Bindings(registry).Contains(Key()));
        object old = OldCode(registry)[CommonPreloadedReinstallHostNames.Module]!;
        Require(ReferenceEquals(Property(old, "Image"), firstImage));
        object entries = Property(old, "Entries");
        object entry = ((IEnumerable)entries).Cast<object>().Single();
        Require(ReferenceEquals(entry.GetType().GetField("Item2")!.GetValue(entry), firstBody));
        registry.InstallPreloaded(target => target.Register(CommonPreloadedReinstallHostNames.Module, CommonPreloadedReinstallHostNames.Function, 0, secondBody));
        ModuleCodeImage secondImage = registry.CurrentCodeImage(CommonPreloadedReinstallHostNames.Module)!;
        object secondBinding = Bindings(registry)[Key()]!;
        Require(!ReferenceEquals(firstImage, secondImage));
        Require(ReferenceEquals(Property(secondBinding, "Image"), secondImage));
        Require(ReferenceEquals(Property(secondBinding, "Body"), secondBody));
        Require(ReferenceEquals(OldCode(registry)[CommonPreloadedReinstallHostNames.Module], old));
        await Check(actor, registry, secondError);
        Require(firstError.StackTraceTerm is Nil && !firstError.AutomaticStackTraceCaptured);
        Require(secondError.StackTraceTerm is Nil && !secondError.AutomaticStackTraceCaptured);
    }

    private static async Task Check(
        RuntimeNodeFoundationHostActor actor,
        ModuleRegistry registry,
        ErlangException shared
    )
    {
        await actor.InvokeAsync(async context =>
        {
            try
            {
                await registry.Call(context, CommonPreloadedReinstallHostNames.Module, CommonPreloadedReinstallHostNames.Function);
            }
            catch (ErlangException error)
            {
                Require(!ReferenceEquals(error, shared) && error.AutomaticStackTraceCaptured && !error.ExplicitStackTrace);
                Require(error.Reason.Equals(shared.Reason));
                Term[] frames = Cons.Items(error.StackTraceTerm).ToArray();
                Require(frames.Length == 1 && frames[0].Equals(Term.Tuple(Term.A(CommonPreloadedReinstallHostNames.Module), Term.A(CommonPreloadedReinstallHostNames.Function), Term.I(0), Nil.Value)));
                return Nil.Value;
            }
            throw new InvalidOperationException(CommonPreloadedReinstallHostNames.MissingFailure);
        });
    }

    private static object Key()
        => (CommonPreloadedReinstallHostNames.Module, CommonPreloadedReinstallHostNames.Function, 0);

    private static IDictionary Bindings(ModuleRegistry registry)
        => (IDictionary)typeof(ModuleRegistry).GetField("runtimeErrorBindings", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(registry)!;

    private static IDictionary OldCode(ModuleRegistry registry)
        => (IDictionary)typeof(ModuleRegistry).GetField("oldCode", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(registry)!;

    private static object Property(object value, string name)
        => value.GetType().GetProperty(name)!.GetValue(value)!;

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(CommonPreloadedReinstallHostNames.Broken);
    }
}
