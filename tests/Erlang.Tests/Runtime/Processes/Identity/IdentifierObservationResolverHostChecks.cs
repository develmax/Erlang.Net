using Erlang;
using System.Linq.Expressions;
using System.Reflection;

namespace Erlang.Tests;

internal static class IdentifierObservationResolverHostChecks
{
    private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    internal static async Task ClosedPublicationRace()
    {
        foreach (bool nullableResult in new[] { false, true })
        {
            await using var runtime = new ProcessRuntime();
            object? identity = nullableResult ? null : typeof(ProcessRuntime).GetMethod(
                IdentifierObservationShutdownHostNames.GetIdentityMethod, InstanceFlags)!.Invoke(runtime,
                new object[] { Term.A(IdentifierObservationShutdownHostNames.Remote) });
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            int calls = 0;
            object known = BuildKnown(
                runtime,
                _ =>
            {
                Interlocked.Increment(ref calls);
                entered.Set();
                if (!release.Wait(IdentifierObservationShutdownHostNames.Deadline))
                    throw new TimeoutException();

                return identity;
            }
            );
            Term key = new Pid(IdentifierObservationShutdownHostNames.Remote, 1, 1);
            Task observation = Task.Run(() => Observe(known, key));
            try
            {
                Check(entered.Wait(IdentifierObservationShutdownHostNames.Deadline));
                known.GetType().GetMethod(IdentifierObservationShutdownHostNames.CloseObservedMethod,
                    InstanceFlags)!.Invoke(known, null);
            }
            finally
            {
                release.Set();
            }
            await observation.WaitAsync(IdentifierObservationShutdownHostNames.Deadline);
            Check(!HasObserved(known, key));
            Observe(known, key);
            Check(Volatile.Read(ref calls) == 1 && !HasObserved(known, key));
            await runtime.DisposeAsync();
            Check(typeof(ProcessRuntime).GetMethod(IdentifierObservationShutdownHostNames.TryGetIdentityMethod,
                InstanceFlags)!.Invoke(runtime, new object[] { Term.A(IdentifierObservationShutdownHostNames.Remote) }) is null);
        }
    }

    internal static async Task UnexpectedOpenResolverError()
    {
        await using var runtime = new ProcessRuntime();
        var expected = new InvalidOperationException(IdentifierObservationShutdownHostNames.UnexpectedResolverFailure);
        object known = BuildKnown(runtime, _ => throw expected);
        bool propagated = false;
        try
        {
            Observe(known, new Pid(IdentifierObservationShutdownHostNames.Remote, 1, 1));
        }
        catch (TargetInvocationException error) when (ReferenceEquals(error.InnerException, expected))
        {
            propagated = true;
        }
        Check(propagated);
    }

    private static object BuildKnown(ProcessRuntime runtime, Func<Atom, object?> callback)
    {
        Type knownType = typeof(ProcessRuntime).Assembly.GetType(IdentifierObservationShutdownHostNames.KnownNodesType)!;
        ConstructorInfo constructor = knownType.GetConstructors(InstanceFlags).Single();
        Type resolverType = constructor.GetParameters()[1].ParameterType;
        Type resultType = resolverType.GetGenericArguments()[1];
        var atom = Expression.Parameter(typeof(Atom));
        var body = Expression.Convert(Expression.Invoke(Expression.Constant(callback), atom), resultType);
        Delegate resolver = Expression.Lambda(resolverType, body, atom).Compile();
        object localIdentity = typeof(ProcessRuntime).GetProperty(
            IdentifierObservationShutdownHostNames.LocalIdentityProperty, InstanceFlags)!.GetValue(runtime)!;

        Action<string> observeAtom = name =>
        {
            using var atomScope = runtime.EnterAtomScope();
            AtomNameRegistry.Observe(name);
        };

        return constructor.Invoke(new object[] { localIdentity, resolver, observeAtom });
    }

    private static void Observe(object known, Term term)
        => known.GetType().GetMethod(IdentifierObservationShutdownHostNames.ObserveMethod,
            InstanceFlags)!.Invoke(known, new object[] { term });

    private static bool HasObserved(object known, Term term)
    {
        object table = known.GetType().GetField(IdentifierObservationShutdownHostNames.ObservedField,
            InstanceFlags)!.GetValue(known)!;

        return (bool)table.GetType().GetMethod(IdentifierObservationShutdownHostNames.TryGetValueMethod)!
            .Invoke(table, new object?[] { term, null })!;
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(IdentifierObservationShutdownHostNames.Failure);
    }
}
