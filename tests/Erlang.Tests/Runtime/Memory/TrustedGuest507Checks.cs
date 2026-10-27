using System.Reflection;
using Erlang.Differential;
using static Erlang.Tests.TrustedGuest507Names;

namespace Erlang.Tests;

internal static class TrustedGuest507Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    internal static async Task Sharing()
    {
        using var guest = GeneratedModuleCompiler.Compile(Source);
        using var target = GeneratedModuleCompiler.Compile(TargetSource);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await DebugSame420HostAccess.Initialize(runtime);
        guest.Definition.Register(runtime.Modules);
        target.Definition.Register(runtime.Modules);
        RegisterProbes(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term value = await actor.InvokeAsync(async context =>
            await runtime.Modules.Call(
                context,
                Guest,
                Run,
                Term.I(507)
            ));
        Check(value.Equals(Term.List(Enumerable.Repeat<Term>(Term.A(ErlangBooleanAtoms.True), 5).ToArray())));
        ProcessContext owner = await actor.Ready;
        Invoke(Property(owner, Heap), OwnedGuest506Names.Collect);
        Check(WordsOf(owner) == 0);
        // Public export is detached and remains readable after its owner collects.
        Check(Cons.Items(value).Count() == 5);
    }

    internal static async Task AllocationAndSlots()
    {
        using var guest = GeneratedModuleCompiler.Compile(Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        guest.Definition.Register(runtime.Modules);
        RegisterProbes(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term result = await actor.InvokeAsync(async context =>
            await runtime.Modules.Call(
                context,
                Guest,
                Allocation,
                Term.I(507)
            ));
        Check(result.Equals(Term.A(ErlangBooleanAtoms.True)));
        ProcessContext owner = await actor.Ready;
        Check(owner.Dictionary.Count == 0);
        Invoke(Property(owner, Heap), OwnedGuest506Names.Collect);
        Check(WordsOf(owner) == 0);
    }

    internal static async Task OpaqueOverride()
    {
        using var guest = GeneratedModuleCompiler.Compile(Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        guest.Definition.Register(runtime.Modules);
        Term? escaped = null;
        // Identical module/function names never grant the internal capability.
        runtime.Modules.DeleteModule(ErLang);
        runtime.Modules.Register(
            ErLang,
            Get,
            1,
            (context, arguments) =>
        {
            escaped = arguments[0];
            Check(escaped is TupleTerm);
            Check(PropertyOrNull(escaped, Storage) is null);

            return ValueTask.FromResult(escaped);
        }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term result = await actor.InvokeAsync(async context =>
            await runtime.Modules.Call(
                context,
                Guest,
                Override,
                Term.I(507)
            ));
        ProcessContext owner = await actor.Ready;
        Invoke(Property(owner, Heap), OwnedGuest506Names.Collect);
        Check(escaped is not null && result.Equals(escaped));
        Check(result.Equals(Term.Tuple(Term.I(507), Term.List(Term.I(507)))));
    }

    private static void RegisterProbes(ProcessRuntime runtime)
    {
        runtime.Modules.Register(
            Probe,
            Words,
            0,
            (context, _) => ValueTask.FromResult<Term>(Term.I(WordsOf(context)))
        );
        runtime.Modules.Register(
            Probe,
            RootIds,
            0,
            (context, _) =>
        {
            object frame = typeof(ProcessDictionary).GetField(Frame, Members)!.GetValue(context.Dictionary)!;

            return ValueTask.FromResult<Term>(Term.I((int)Property(frame, RootIdProperty)));
        }
        );
        runtime.Modules.Register(
            Probe,
            Collect,
            0,
            (context, _) =>
        {
            Invoke(Property(context, Heap), OwnedGuest506Names.Collect);
            Check(WordsOf(context) > 0);

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        );
    }

    private static long WordsOf(ProcessContext context) => (long)Property(Property(Property(context, Heap), Snapshot), Payload);

    private static object Property(object target, string name) => PropertyOrNull(target, name) ?? throw new InvalidOperationException(Failure);

    private static object? PropertyOrNull(object target, string name) => target.GetType().GetProperty(name, Members)!.GetValue(target);

    private static void Invoke(object target, string name) => target.GetType().GetMethod(name, Members)!.Invoke(target, null);

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}
