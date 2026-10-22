using Erlang;
using System.Runtime.CompilerServices;

namespace Erlang.Tests;

internal static class IdentifierLifetimeHostChecks
{
    internal static Task PidLifetime() => CheckLifetime(IdentifierLifetimeHostNames.Pid);

    internal static Task PortLifetime() => CheckLifetime(IdentifierLifetimeHostNames.Port);

    internal static Task ReferenceLifetime() => CheckLifetime(IdentifierLifetimeHostNames.Reference);

    internal static Task NestedFunctionLifetime() => CheckLifetime(IdentifierLifetimeHostNames.Function);

    private static async Task CheckLifetime(string kind)
    {
        await using var fixture = await IdentifierLifetimeHostFixture.Create();
        Term root = await fixture.AdoptAndImport(kind);
        IdentifierLifetimeHostAccess.Collect();
        await fixture.StopKernel();
        IdentifierLifetimeHostAccess.Check((await fixture.ReadCreation()).Equals(new Integer(IdentifierLifetimeHostNames.RemoteCreation)));
        Term key = IdentifierLifetimeHostFixture.Key(root);
        object? retained = IdentifierLifetimeHostAccess.Observed(fixture.Runtime, key);
        IdentifierLifetimeHostAccess.Check(IdentifierLifetimeHostAccess.IsIdentity(retained));
        IdentifierLifetimeHostAccess.Check(ReferenceEquals(retained, IdentifierLifetimeHostAccess.Current(fixture.Runtime)));
        IdentifierLifetimeHostAccess.Check((uint)retained!.GetType().GetProperty(IdentifierLifetimeHostNames.Creation,
            IdentifierLifetimeHostNames.InstanceFlags)!.GetValue(retained)! == IdentifierLifetimeHostNames.RemoteCreation);
        if (root is FunctionTerm function)
            IdentifierLifetimeHostAccess.Check((await fixture.Reader.InvokeAsync(async context =>
                await function.Invoke(context, Array.Empty<Term>()))).Equals(function.Environment[0]));
        GC.KeepAlive(root);
    }

    internal static async Task LocalForeignAndClosedRoots()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        using (runtime.ReserveExternalTerm(runtime.InitializationPid))
            IdentifierLifetimeHostAccess.Check(IdentifierLifetimeHostAccess.Observed(runtime, runtime.InitializationPid) is null);
        bool refused = false;
        try
        {
            using var invalid = runtime.ReserveExternalTerm(foreign.InitializationPid);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        IdentifierLifetimeHostAccess.Check(refused);
        var resource = foreign.ReferenceResources.CreateBinary(new byte[] { 1 });
        IdentifierLifetimeHostAccess.Observe(runtime, resource);
        IdentifierLifetimeHostAccess.Check(IdentifierLifetimeHostAccess.Observed(runtime, resource)?.GetType() == typeof(object));
        Term held = runtime.ImportExternalTerm(ExternalTermFormat.Encode(new Pid(IdentifierLifetimeHostNames.Remote, 1, 1))).Term;
        IdentifierLifetimeHostAccess.Check(IdentifierLifetimeHostAccess.IsIdentity(IdentifierLifetimeHostAccess.Observed(runtime, held)));
        await runtime.DisposeAsync();
        IdentifierLifetimeHostAccess.Check(IdentifierLifetimeHostAccess.Observed(runtime, held) is null);
        IdentifierLifetimeHostAccess.Check(IdentifierLifetimeHostAccess.Current(runtime) is null && IdentifierLifetimeHostAccess.IdentityCount(runtime) == 0);
        using var lease = foreign.ReferenceResources.AcquireBinary(resource);
        IdentifierLifetimeHostAccess.Check(lease is not null);
        GC.KeepAlive(held);
        GC.KeepAlive(resource);
    }

    internal static async Task ConcurrentIdentityAndImportChurn()
    {
        await using var runtime = new ProcessRuntime();
        Term same = new Pid(IdentifierLifetimeHostNames.Remote, 1, 1);
        await Task.WhenAll(Enumerable.Range(0, IdentifierLifetimeHostNames.ConcurrentCount)
            .Select(_ => Task.Run(() => IdentifierLifetimeHostAccess.Observe(runtime, same))));
        object? identity = IdentifierLifetimeHostAccess.Observed(runtime, same);
        IdentifierLifetimeHostAccess.Check(IdentifierLifetimeHostAccess.IsIdentity(identity));
        Term[] keys = Enumerable.Range(1, IdentifierLifetimeHostNames.ConcurrentCount)
            .Select(index => (Term)new Pid(IdentifierLifetimeHostNames.Remote, (uint)index, 1)).ToArray();
        await Task.WhenAll(keys.Select(key => Task.Run(() => IdentifierLifetimeHostAccess.Observe(runtime, key))));
        IdentifierLifetimeHostAccess.Check(keys.All(key => ReferenceEquals(identity, IdentifierLifetimeHostAccess.Observed(runtime, key))));
        Churn(runtime, 0, IdentifierLifetimeHostNames.ChurnFirst);
        IdentifierLifetimeHostAccess.Collect();
        Churn(runtime, IdentifierLifetimeHostNames.ChurnFirst, IdentifierLifetimeHostNames.ChurnSecond);
        IdentifierLifetimeHostAccess.Check(IdentifierLifetimeHostAccess.IdentityCount(runtime) <= IdentifierLifetimeHostNames.ChurnBound);
        IdentifierLifetimeHostAccess.Check(runtime.InitializeDistributionPending(IdentifierLifetimeHostNames.PendingCapacity).MaximumNodes == IdentifierLifetimeHostNames.PendingCapacity);
        GC.KeepAlive(keys);
        GC.KeepAlive(same);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Churn(ProcessRuntime runtime, int start, int count)
    {
        for (int index = start; index < start + count; index++)
            IdentifierLifetimeHostAccess.Observe(runtime, new Pid(IdentifierLifetimeHostNames.ChurnPrefix + index + IdentifierLifetimeHostNames.ChurnSuffix, 1, 1));
    }
}
