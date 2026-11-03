using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Erlang;

internal static unsafe class NifNativeResourceType527Callbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nint Open(
        nint env,
        byte* module,
        byte* name,
        nint destructor,
        int flags,
        int* tried
    )
    {
        try
        {
            return OpenCore(
                env,
                name,
                new(destructor),
                flags,
                tried
            );
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nint OpenX(
        nint env,
        byte* name,
        nint init,
        int flags,
        int* tried
    )
    {
        try
        {
            if (!Loading(
                env,
                flags,
                tried,
                out var transaction
            )) return 0;

            return OpenPrepared(
                env,
                name,
                NativeResourceCallbacks527.Read527(init, 3),
                flags,
                tried,
                transaction!
            );
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nint Init(
        nint env,
        byte* name,
        nint init,
        int flags,
        int* tried
    )
    {
        try
        {
            if (init == 0) throw new ArgumentNullException(nameof(init));
            int count = ((NativeResourceCallbacks527*)init)->Members;
            if (!Loading(
                env,
                flags,
                tried,
                out var transaction
            )) return 0;

            return OpenPrepared(
                env,
                name,
                NativeResourceCallbacks527.Read527(init, count),
                flags,
                tried,
                transaction!
            );
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return 0;
        }
    }

    private static nint OpenCore(
        nint env,
        byte* name,
        NativeResourceCallbacks527 callbacks,
        int flags,
        int* tried
    )
    {
        if (!Loading(
            env,
            flags,
            tried,
            out var transaction
        )) return 0;

        return OpenPrepared(
            env,
            name,
            callbacks,
            flags,
            tried,
            transaction!
        );
    }

    private static bool Loading(
        nint env,
        int flags,
        int* tried,
        out NativeResourceLoadTransaction527? transaction
    )
    {
        if (tried != null) *tried = flags;
        NifNativeCallEnvironment owner = NifNativeCallEnvironment.Resolve(env);
        transaction = !owner.IsIndependent && NativeResourceImages524.TryGet527(owner.Image, out var image)
            ? image!.ResourceLoad527 : null;

        return transaction is { Loading527: true };
    }

    private static nint OpenPrepared(
        nint env,
        byte* name,
        NativeResourceCallbacks527 callbacks,
        int flags,
        int* tried,
        NativeResourceLoadTransaction527 transaction
    )
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        int length = 0;
        while (name[length] != 0) length = checked(length + 1);
        string text = Encoding.Latin1.GetString(new ReadOnlySpan<byte>(name, length));
        NifNativeCallEnvironment.Resolve(env).Runtime.ObserveIncomingTerm(Term.A(text));
        NativeResourceType524? type = transaction.Open527(
            text,
            callbacks,
            flags,
            out int operation
        );
        if (tried != null) *tried = operation;

        return type?.Token ?? 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int DynamicCall(
        nint env,
        nuint module,
        nuint name,
        nuint resource,
        nint data
    )
    {
        try
        {
            NifNativeCallEnvironment owner = NifNativeCallEnvironment.Resolve(env);
            if (owner.Get(resource) is not ResourceReferenceTerm reference
                || reference.ResourceKind != ReferenceResourceKind.NativeResource524) return 1;
            NativeResourcePayload524 payload = reference.Entry.Read<NativeResourcePayload524>();
            if (!ReferenceEquals(owner.Runtime, payload.Runtime)) return 1;
            if (owner.Get(module) is not Atom moduleName || owner.Get(name) is not Atom typeName
                || moduleName.Name != payload.Type.Module527 || typeName.Name != payload.Type.Name) return 1;

            return payload.Type.DynamicCall527(owner, payload.Pointer524, data) ? 0 : 1;
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return 1;
        }
    }
}
