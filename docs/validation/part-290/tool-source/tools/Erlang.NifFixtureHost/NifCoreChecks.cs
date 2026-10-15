using Erlang;

namespace Erlang.NifFixtureHost;

internal static unsafe class NifCoreChecks
{
    internal static void Run(
        ProcessContext caller,
        ModuleCodeImage image,
        string[] arguments
    )
    {
        object owner = new();
        using var source = caller.EnterSourceCode(image, owner);
        CheckEnvironment(caller, image);
        CheckGeneration(caller, image, owner);
        if (arguments.Length == NifFixtureNames.NativeArgumentCount)
            CheckNative(
                caller,
                image,
                owner,
                arguments[0],
                arguments[1]
            );
        else if (arguments.Length != NifFixtureNames.InternalArgumentCount)
            throw new ArgumentException(NifHostDiagnostics.Usage);
    }

    private static void CheckEnvironment(ProcessContext caller, ModuleCodeImage image)
    {
        int baseline = NifCallEnvironment.ActiveCount;
        var allocations = new NifNativeAllocationOwner();
        using (var scope = new NifCallbackScope(allocations))
        using (var first = new NifCallEnvironment(caller, image, NifFixtureNames.FirstPrivateData))
        using (var second = new NifCallEnvironment(caller, image, NifFixtureNames.SecondPrivateData))
        {
            nuint value = first.Add(Term.I(NifFixtureNames.EnvironmentInputInteger));
            Expect<InvalidOperationException>(() => second.Get(value));
            int extracted = 0;
            var get = (delegate* unmanaged[Cdecl]<nint, nuint, int*, int>)&NifPrimitiveCallbacks.GetInt;
            Check(get(first.Pointer, value, &extracted) == 1 && extracted == NifFixtureNames.EnvironmentInputInteger);
            var make = (delegate* unmanaged[Cdecl]<nint, int, nuint>)&NifPrimitiveCallbacks.MakeInt;
            Check(first.Get(make(first.Pointer, NifFixtureNames.EnvironmentConstructedInteger)).Equals(Term.I(NifFixtureNames.EnvironmentConstructedInteger)));
            nuint atom = first.Add(Term.A(NifFixtureNames.NonInteger));
            Check(get(first.Pointer, atom, &extracted) == 0);
            var allocate = (delegate* unmanaged[Cdecl]<nuint, nint>)&NifPrimitiveCallbacks.Allocate;
            var free = (delegate* unmanaged[Cdecl]<nint, void>)&NifPrimitiveCallbacks.Free;
            nint memory = allocate(NifFixtureNames.AllocationBytes);
            Check(memory != 0 && allocations.Count == 1);
            free(memory);
            Check(allocations.Count == 0);
            NifCallbackScope.ThrowPending();
            var badarg = (delegate* unmanaged[Cdecl]<nint, nuint>)&NifPrimitiveCallbacks.BadArgument;
            Check(badarg(first.Pointer) == 0);
            Expect<ErlangException>(first.ThrowPending);
            first.Dispose();
            Expect<ObjectDisposedException>(() => first.Add(Term.I(0)));
        }
        Check(NifCallEnvironment.ActiveCount == baseline);
    }

    private static void CheckGeneration(
        ProcessContext caller,
        ModuleCodeImage image,
        object owner
    )
    {
        var generations = new NifGenerationLeaseTable();
        int unloads = 0;
        generations.Bind(image, owner, () => unloads++);
        Check(generations.RetainedGenerationCount == 1);
        using (caller.EnterSourceCode(image, new object()))
            Expect<InvalidOperationException>(() => generations.Acquire(caller, image));
        IDisposable active = generations.Acquire(caller, image);
        generations.Retire(image);
        Check(unloads == 0 && generations.RetainedGenerationCount == 1);
        Expect<ObjectDisposedException>(() => generations.Acquire(caller, image));
        active.Dispose();
        active.Dispose();
        generations.Retire(image);
        Check(unloads == 1 && generations.RetainedGenerationCount == 0);
        Expect<ObjectDisposedException>(() => generations.Acquire(caller, image));
        Expect<ObjectDisposedException>(() => generations.Bind(image, owner, () => unloads++));
        generations.Retire(image);
        Check(unloads == 1 && generations.RetainedGenerationCount == 0);
    }

    private static void CheckNative(
        ProcessContext caller,
        ModuleCodeImage image,
        object owner,
        string path,
        string sha
    )
    {
        var generations = new NifGenerationLeaseTable();
        WindowsNifFixtureLibrary library = WindowsNifFixtureLibrary.LoadAllowlistedFixture(
            path,
            sha,
            generations,
            image,
            owner,
            caller,
            Term.I(NifFixtureNames.LoadMarker)
        );
        try
        {
            Check(library.OwnedAllocationCount == 1);
            Check(library.Invoke(caller, NifFixtureNames.Increment, Term.I(0)).Equals(Term.I(1)));
            Check(library.Invoke(caller, NifFixtureNames.Increment, Term.I(-1)).Equals(Term.I(0)));
            Check(library.Invoke(caller, NifFixtureNames.Increment, Term.I(int.MaxValue - 1)).Equals(Term.I(int.MaxValue)));
            Expect<ErlangException>(() => library.Invoke(caller, NifFixtureNames.Increment, Term.I(int.MaxValue)));
            Expect<ErlangException>(() => library.Invoke(caller, NifFixtureNames.Increment, Term.A(NifFixtureNames.NonInteger)));
            Check(Term.Text(library.Invoke(caller, NifFixtureNames.Greeting)) == NifFixtureNames.GreetingText);
        }
        finally
        {
            library.Dispose();
        }
        Check(library.ObservedUnloadCount == 1 && library.OutstandingAfterNativeUnload == 0 && library.OwnedAllocationCount == 0);
        Expect<ObjectDisposedException>(() => library.Invoke(caller, NifFixtureNames.Greeting));
        Check(NifCallEnvironment.ActiveCount == 0);
        var vetoed = new NifGenerationLeaseTable();
        Expect<InvalidOperationException>(() => WindowsNifFixtureLibrary.LoadAllowlistedFixture(
            path,
            sha,
            vetoed,
            image,
            owner,
            caller,
            Term.I(NifFixtureNames.RejectedLoadMarker)
        ));
        Check(NifCallEnvironment.ActiveCount == 0);
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifHostDiagnostics.AssertionFailed);
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException(NifHostDiagnostics.ExpectedException(typeof(T)));
    }
}
