using System.Reflection;
using Erlang.Differential;

namespace Erlang.Tests;

// Root enrollment: Test("runtime/exception828/explicit-native-counter-still-transfers", PortableExceptionIdentity828Checks.NativeCounter);
internal static class PortableExceptionIdentity828Checks
{
    internal static async Task NativeCounter()
    {
        foreach (bool pilot in new[] { false, true })
        {
            await using ProcessRuntime runtime = OwnedGuest506RuntimeFactory.Create(pilot);
            using PreparedBeamImage counters = CountersOriginal421Preparation.Prepare(runtime, runtime.Modules);
            ErlangException? original = null;
            var hosts = new HashSet<IDisposable>(ReferenceEqualityComparer.Instance);
            try
            {
                await runtime.InitializeModulesAsync(registry =>
                {
                    Require(registry.PublishPreparedBeam(counters).Status == BeamPublicationStatus.Published);
                    registry.Register(
                        "exception_host828",
                        "raise",
                        1,
                        (_, values) =>
                    {
                        original = new ErlangException(values[0]) { ExplicitStackTrace = true, StackTraceTerm = Nil.Value };
                        throw original;
                    }
                    );
                });
                using var compiled = GeneratedModuleCompiler.Compile(
                    "-module(exception_native828). -export([run/0]). run()->R=erts_internal:counters_new(1),ok=erts_internal:counters_put(R,1,828),exception_host828:raise(R).");
                compiled.Definition.Register(runtime.Modules);
                await using var actor = new RuntimeNodeFoundationHostActor(runtime);
                ErlangException caught = await actor.InvokeAsync(async context =>
                {
                    try
                    {
                        _ = await runtime.Modules.Call(context, "exception_native828", "run");
                    }
                    catch (ErlangException error)
                    {
                        return error;
                    }
                    throw new InvalidOperationException("Expected actual explicit native counter exception");
                });
                Require(original is not null && !ReferenceEquals(original, caught));
                Require(caught.ExplicitStackTrace && caught.StackTraceTerm is Nil);

                IDisposable Host(Term reference) => (IDisposable)reference.GetType().GetProperty(
                    "CountersHost596", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(reference)!;
                IDisposable sourceHost = Host(original!.Reason), escapedHost = Host(caught.Reason);
                hosts.Add(sourceHost);
                hosts.Add(escapedHost);
                Require(!ReferenceEquals(sourceHost, escapedHost));
                sourceHost.Dispose();
                // Completion joins actual source-process heap retirement.
                await actor.DisposeAsync();
                await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
                Term value = await receiver.InvokeAsync(async context =>
                    await runtime.Modules.Call(
                        context,
                        "erts_internal",
                        "counters_get",
                        caught.Reason,
                        Term.I(1)
                    ));
                Require(value.Equals(Term.I(828)));
            }
            finally
            {
                foreach (IDisposable host in hosts) host.Dispose();
            }
        }
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException("Explicit native exception lost real independent custody");
    }
}
