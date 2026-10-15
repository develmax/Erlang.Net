using Erlang;

namespace NifPrototype;

internal static unsafe class NifCoreChecks
{
    internal static void Run(ProcessContext caller, ModuleCodeImage image, string[] arguments)
    {
        object owner = new();
        using var source = caller.EnterSourceCode(image, owner);
        CheckEnvironment(caller, image);
        CheckGeneration(caller, image, owner);
        if (arguments.Length == 2)
            CheckNative(caller, image, owner, arguments[0], arguments[1]);
        else if (arguments.Length != 0)
            throw new ArgumentException("Pass no arguments for internal checks, or absolute DLL path plus trusted fixture SHA256.");
    }

    private static void CheckEnvironment(ProcessContext caller, ModuleCodeImage image)
    {
        int baseline = NifCallEnvironment.ActiveCount;
        var allocations = new NifNativeAllocationOwner();
        using (var scope = new NifCallbackScope(allocations))
        using (var first = new NifCallEnvironment(caller, image, 123))
        using (var second = new NifCallEnvironment(caller, image, 456))
        {
            nuint value = first.Add(Term.I(41));
            Expect<InvalidOperationException>(() => second.Get(value));
            int extracted = 0;
            var get = (delegate* unmanaged[Cdecl]<nint, nuint, int*, int>)&NifPrimitiveCallbacks.GetInt;
            Check(get(first.Pointer, value, &extracted) == 1 && extracted == 41);
            var make = (delegate* unmanaged[Cdecl]<nint, int, nuint>)&NifPrimitiveCallbacks.MakeInt;
            Check(first.Get(make(first.Pointer, 42)).Equals(Term.I(42)));
            nuint atom = first.Add(Term.A("not_an_integer"));
            Check(get(first.Pointer, atom, &extracted) == 0);
            var allocate = (delegate* unmanaged[Cdecl]<nuint, nint>)&NifPrimitiveCallbacks.Allocate;
            var free = (delegate* unmanaged[Cdecl]<nint, void>)&NifPrimitiveCallbacks.Free;
            nint memory = allocate(32);
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

    private static void CheckGeneration(ProcessContext caller, ModuleCodeImage image, object owner)
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

    private static void CheckNative(ProcessContext caller, ModuleCodeImage image, object owner, string path, string sha)
    {
        var generations = new NifGenerationLeaseTable();
        WindowsNifFixtureLibrary library = WindowsNifFixtureLibrary.LoadAllowlistedFixture(path, sha, generations, image, owner, caller, Term.I(42));
        try
        {
            Check(library.OwnedAllocationCount == 1);
            Check(library.Invoke(caller, "increment", Term.I(0)).Equals(Term.I(1)));
            Check(library.Invoke(caller, "increment", Term.I(-1)).Equals(Term.I(0)));
            Check(library.Invoke(caller, "increment", Term.I(int.MaxValue - 1)).Equals(Term.I(int.MaxValue)));
            Expect<ErlangException>(() => library.Invoke(caller, "increment", Term.I(int.MaxValue)));
            Expect<ErlangException>(() => library.Invoke(caller, "increment", Term.A("not_an_integer")));
            Check(Term.Text(library.Invoke(caller, "greeting")) == "native hello");
        }
        finally
        {
            library.Dispose();
        }
        Check(library.ObservedUnloadCount == 1 && library.OutstandingAfterNativeUnload == 0 && library.OwnedAllocationCount == 0);
        Expect<ObjectDisposedException>(() => library.Invoke(caller, "greeting"));
        Check(NifCallEnvironment.ActiveCount == 0);
        var vetoed = new NifGenerationLeaseTable();
        Expect<InvalidOperationException>(() => WindowsNifFixtureLibrary.LoadAllowlistedFixture(path, sha, vetoed, image, owner, caller, Term.I(17)));
        Check(NifCallEnvironment.ActiveCount == 0);
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("NIF prerequisite assertion failed.");
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }

        throw new InvalidOperationException("Expected exception was not observed: " + typeof(T).Name);
    }
}
