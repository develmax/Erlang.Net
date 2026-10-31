using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
namespace Erlang.NifNativeBackend;

internal static unsafe class NifLinuxMutex795AuthorityChecks
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static object Namespace(ProcessRuntime runtime) => typeof(ProcessRuntime).GetField(NifLinuxMutex795Names.Namespace, Fields)!.GetValue(runtime)!;

    internal static nint Library(ProcessRuntime runtime) => (nint)Namespace(runtime).GetType().GetField(NifLinuxMutex795Names.Library, Fields)!.GetValue(Namespace(runtime))!;

    private static NifNativeAllocationOwner Owner(ProcessRuntime runtime) => (NifNativeAllocationOwner)typeof(NifNativeLibraryOwner)
        .GetField(NifLinuxMutex795Names.Allocations, Fields)!.GetValue(NifMutexLifecycle795Checks.CaptureOwner(runtime.Modules.CurrentCodeImage(NifLinuxMutex795Names.Module)!))!;

    internal static string Witness(ProcessRuntime runtime) => (string)Namespace(runtime).GetType()
        .GetProperty(NifLinuxMutex795Names.PthreadWitness, Fields)!.GetValue(Namespace(runtime))!;

    internal static string CheckedWitness(ProcessRuntime runtime, ulong minimumCreates)
    {
        string value = Witness(runtime);
        using var document = JsonDocument.Parse(value);
        foreach (var binding in document.RootElement.GetProperty(NifLinuxMutex795Names.Bindings).EnumerateArray())
        {
            using var rows = JsonDocument.Parse(binding.GetString()!);
            foreach (var row in rows.RootElement.EnumerateArray())
                Require(row.GetProperty(NifLinuxMutex795Names.LookupMatches).GetBoolean());
        }
        using var calls = JsonDocument.Parse(document.RootElement.GetProperty(NifLinuxMutex795Names.Calls).GetString()!);
        var values = calls.RootElement.EnumerateArray().ToDictionary(
            row => row.GetProperty(NifLinuxMutex795Names.Name).GetString()!,
            row => row.GetProperty(NifLinuxMutex795Names.Calls).GetUInt64()
        );
        Require(values[NifNativeLinuxPthreadControlNames.Symbols[0]] >= minimumCreates
            && values[NifNativeLinuxPthreadControlNames.Symbols[1]] >= minimumCreates);

        return value;
    }

    internal static void Check(ProcessRuntime a, ProcessRuntime b)
    {
        nint ca = NifNativeLinuxNamespaceApi.Symbol(Library(a), NifLinuxMutex795Names.Create);
        nint da = NifNativeLinuxNamespaceApi.Symbol(Library(a), NifLinuxMutex795Names.Destroy);
        nint ta = NifNativeLinuxNamespaceApi.Symbol(Library(a), NifLinuxMutex795Names.TryLock);
        nint ua = NifNativeLinuxNamespaceApi.Symbol(Library(a), NifLinuxMutex795Names.Unlock);
        nint cb = NifNativeLinuxNamespaceApi.Symbol(Library(b), NifLinuxMutex795Names.Create);
        nint db = NifNativeLinuxNamespaceApi.Symbol(Library(b), NifLinuxMutex795Names.Destroy);
        int baseline = NifNativeAllocationOwner.LiveMutexCount795;
        nint left = 0, right = 0;
        byte[] text = Encoding.UTF8.GetBytes(NifLinuxMutex795Names.ControlName + '\0');
        try
        {
            // Genuine extension calls with NO ambient NIF scope: namespace
            // capability, not thread-local context, supplies exact VM authority.
            fixed (byte* name = text)
            {
                left = ((delegate* unmanaged[Cdecl]<byte*, nint>)ca)(name);
                right = ((delegate* unmanaged[Cdecl]<byte*, nint>)cb)(name);
            }
            Require(left != 0 && right != 0);
            Require(ReferenceEquals(NifNativeAllocationOwner.ResolveMutex795(left).Owner.Arena528, a.NativeAllocationDomain)
                && ReferenceEquals(NifNativeAllocationOwner.ResolveMutex795(right).Owner.Arena528, b.NativeAllocationDomain));
            var beforeA = a.NativeAllocationSnapshot;
            var beforeB = b.NativeAllocationSnapshot;
            var wrongCreate = new NifNativeFailureState();
            using (var scope = new NifNativeCallbackScope(Owner(b), wrongCreate))
            {
                fixed (byte* name = text) Require(((delegate* unmanaged[Cdecl]<byte*, nint>)ca)(name) == 0);
                ExpectRejected(wrongCreate);
            }
            var wrongOperation = new NifNativeFailureState();
            using (var scope = new NifNativeCallbackScope(Owner(b), wrongOperation))
            {
                Require(((delegate* unmanaged[Cdecl]<nint, int>)ta)(left) == NifNativeMutex795Names.Invalid);
                ExpectRejected(wrongOperation);
            }
            var foreignHandle = new NifNativeFailureState();
            using (var scope = new NifNativeCallbackScope(Owner(a), foreignHandle))
            {
                Require(((delegate* unmanaged[Cdecl]<nint, int>)ta)(right) == NifNativeMutex795Names.Invalid);
                ExpectRejected(foreignHandle);
            }
            Require(a.NativeAllocationSnapshot == beforeA && b.NativeAllocationSnapshot == beforeB);
            var positive = new NifNativeFailureState();
            nint stale;
            using (var scope = new NifNativeCallbackScope(Owner(a), positive))
            {
                Require(((delegate* unmanaged[Cdecl]<nint, int>)ta)(left) == 0);
                ((delegate* unmanaged[Cdecl]<nint, void>)ua)(left);
                ((delegate* unmanaged[Cdecl]<nint, void>)da)(left);
                positive.ThrowPending();
                stale = left;
                left = 0;
            }
            var destroyed = new NifNativeFailureState();
            using (var scope = new NifNativeCallbackScope(Owner(a), destroyed))
            {
                Require(((delegate* unmanaged[Cdecl]<nint, int>)ta)(stale) == NifNativeMutex795Names.Invalid);
                ExpectRejected(destroyed);
            }
            Require(NifNativeAllocationOwner.LiveMutexCount795 == baseline + 1);
        }
        finally
        {
            if (left != 0) ((delegate* unmanaged[Cdecl]<nint, void>)da)(left);
            if (right != 0) ((delegate* unmanaged[Cdecl]<nint, void>)db)(right);
        }
        Require(NifNativeAllocationOwner.LiveMutexCount795 == baseline);
    }

    private static void ExpectRejected(NifNativeFailureState failure)
    {
        try
        {
            failure.ThrowPending();
        }
        catch (InvalidOperationException error) when (error.Message == NifNativeMutex795Names.InvalidHandle)
        {
            return;
        }
        throw new InvalidOperationException(NifLinuxMutex795Names.Failure);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(NifLinuxMutex795Names.Failure);
    }
}
