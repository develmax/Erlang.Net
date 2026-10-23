using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Erlang;

internal static unsafe class NifNativeAtomCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint Make(nint env, byte* name)
    {
        try
        {
            return MakeCore(name, ZeroTerminatedLength(name, NifNativeTextNames.MaximumAtomCharacters));
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeLength(nint env, byte* name, nuint length)
    {
        try
        {
            return MakeCore(name, length);
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    private static nuint MakeCore(byte* name, nuint length)
    {
        var owner = NifNativeCallEnvironment.CurrentInvocation();
        if (length > NifNativeTextNames.MaximumAtomCharacters)
        {
            owner.Raise(owner.Add(NifNativeNames.BadArgument));

            return 0;
        }
        if (length != 0 && name == null)
            throw new InvalidOperationException(NifNativeTextNames.NullInput);
        string text = Encoding.Latin1.GetString(new ReadOnlySpan<byte>(name, (int)length));

        return owner.Add(Term.A(text));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Existing(
        nint env,
        byte* name,
        nuint* output,
        int encoding
    )
    {
        try
        {
            return ExistingCore(
                name,
                ZeroTerminatedLength(name, NifNativeTextNames.MaximumAtomUtf8Bytes),
                output,
                encoding
            );
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int ExistingLength(
        nint env,
        byte* name,
        nuint length,
        nuint* output,
        int encoding
    )
    {
        try
        {
            return ExistingCore(
                name,
                length,
                output,
                encoding
            );
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    private static int ExistingCore(
        byte* name,
        nuint length,
        nuint* output,
        int encoding
    )
    {
        if (encoding is not (NifNativeTextNames.Latin1 or NifNativeTextNames.Utf8) ||
            length > (nuint)(encoding == NifNativeTextNames.Latin1 ? NifNativeTextNames.MaximumAtomCharacters : NifNativeTextNames.MaximumAtomUtf8Bytes))
            return 0;
        if (length != 0 && name == null)
            throw new InvalidOperationException(NifNativeTextNames.NullInput);
        string text;
        try
        {
            var bytes = new ReadOnlySpan<byte>(name, (int)length);
            text = encoding == NifNativeTextNames.Latin1 ? Encoding.Latin1.GetString(bytes) : NifNativeTextNames.StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return 0;
        }
        if (!AtomNameRegistry.Exists(text))
            return 0;
        if (output == null)
            throw new InvalidOperationException(NifNativeTextNames.NullOutput);
        *output = NifNativeCallEnvironment.CurrentInvocation().Add(Term.A(text));

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Get(
        nint env,
        nuint value,
        byte* output,
        uint capacity,
        int encoding
    )
    {
        try
        {
            if (capacity == 0)
                return 0;
            Term term = NifNativeCallEnvironment.ResolveTermOwner(value, env).Get(value);
            if (term is not Atom atom || !TryBytes(atom, encoding, out byte[] bytes) || (uint)bytes.Length >= capacity)
                return 0;
            if (output == null)
                throw new InvalidOperationException(NifNativeTextNames.NullOutput);
            bytes.CopyTo(new Span<byte>(output, bytes.Length));
            output[bytes.Length] = 0;

            return bytes.Length + 1;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Length(
        nint env,
        nuint value,
        uint* length,
        int encoding
    )
    {
        try
        {
            Term term = NifNativeCallEnvironment.ResolveTermOwner(value, env).Get(value);
            if (term is not Atom atom || !TryBytes(atom, encoding, out byte[] bytes))
                return 0;
            if (length == null)
                throw new InvalidOperationException(NifNativeTextNames.NullOutput);
            *length = (uint)bytes.Length;

            return 1;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    private static bool TryBytes(Atom atom, int encoding, out byte[] bytes)
    {
        bytes = [];
        if (encoding == NifNativeTextNames.Utf8)
        {
            bytes = NifNativeTextNames.StrictUtf8.GetBytes(atom.Name);

            return true;
        }
        if (encoding != NifNativeTextNames.Latin1 || atom.Name.Any(character => character > byte.MaxValue))
            return false;
        bytes = Encoding.Latin1.GetBytes(atom.Name);

        return true;
    }

    private static nuint ZeroTerminatedLength(byte* name, int maximum)
    {
        if (name == null)
            throw new InvalidOperationException(NifNativeTextNames.NullInput);
        for (int index = 0; index <= maximum; index++)
            if (name[index] == 0)
                return (nuint)index;

        // Greater lengths already fail the atom limit; do not scan unbounded host memory.
        return (nuint)maximum + 1;
    }
}
