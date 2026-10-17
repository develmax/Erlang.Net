using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Erlang;

internal static unsafe class NifNativeStringCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeLength(
        nint env,
        byte* input,
        nuint length,
        int encoding
    )
        => MakeCore(
            env,
            input,
            length,
            encoding
        );

    internal static nuint MakeZeroTerminated(nint env, byte* input, int encoding)
    {
        try
        {
            if (input == null)
                throw new InvalidOperationException(NifNativeTextNames.NullInput);
            // Original enif_make_string delegates to make_string_len after strlen.
            // A valid input has accessible storage through its first NUL; metadata
            // read limits are unrelated to this caller-owned string contract.
            nuint length = 0;
            while (input[length] != 0)
                length = checked(length + 1);

            return MakeCore(
                env,
                input,
                length,
                encoding
            );
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    private static nuint MakeCore(
        nint env,
        byte* input,
        nuint length,
        int encoding
    )
    {
        try
        {
            var owner = NifNativeCallEnvironment.Resolve(env);
            if (encoding is not (NifNativeTextNames.Latin1 or NifNativeTextNames.Utf8))
                return BadArgument(owner);
            if (length > (nuint)Array.MaxLength)
            {
                owner.Raise(owner.Add(Term.A(ErlangErrorReasons.SystemLimit)));

                return 0;
            }
            if (length != 0 && input == null)
                throw new InvalidOperationException(NifNativeTextNames.NullInput);
            var bytes = new ReadOnlySpan<byte>(input, (int)length);
            string text;
            try
            {
                text = encoding == NifNativeTextNames.Latin1 ? Encoding.Latin1.GetString(bytes) : NifNativeTextNames.StrictUtf8.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return BadArgument(owner);
            }

            return owner.Add(Term.String(text));
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
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
            // OTP refuses a zero-sized destination without reading the term or buffer.
            if (capacity == 0)
                return 0;
            if (encoding is not (NifNativeTextNames.Latin1 or NifNativeTextNames.Utf8))
                return 0;
            Term list = NifNativeCallEnvironment.ResolveTermOwner(value, env).Get(value);
            if (output == null)
                throw new InvalidOperationException(NifNativeTextNames.NullOutput);

            return encoding == NifNativeTextNames.Latin1
                ? GetLatin1(list, output, capacity)
                : GetUtf8(list, output, capacity);
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    private static int GetLatin1(Term list, byte* output, uint capacity)
    {
        uint written = 0;
        while (list is not Nil)
        {
            if (list is not Cons cell || cell.Head is not Integer number || number.Value < byte.MinValue || number.Value > byte.MaxValue)
            {
                output[written] = 0;

                return 0;
            }
            output[written++] = (byte)number.Value;
            if (written >= capacity)
            {
                output[written - 1] = 0;

                return -checked((int)capacity);
            }
            list = cell.Tail;
        }
        output[written] = 0;

        return checked((int)written + 1);
    }

    private static int GetUtf8(Term list, byte* output, uint capacity)
    {
        uint written = 0;
        uint characters = 0;
        Span<byte> bytes = stackalloc byte[NifNativeTextNames.MaximumUtf8ScalarBytes];
        while (list is not Nil)
        {
            if (list is not Cons cell || cell.Head is not Integer number || number.Value < 0 ||
                number.Value > NifNativeTextNames.MaximumUnicodeScalar || !Rune.IsValid((int)number.Value))
            {
                // The original UTF8 path resets byte zero on failure, retaining other prefix bytes.
                output[0] = 0;

                return 0;
            }
            if (characters++ >= capacity)
            {
                output[written] = 0;

                return -checked((int)written + 1);
            }
            var rune = new Rune((int)number.Value);
            int count = rune.EncodeToUtf8(bytes);
            if ((ulong)written + (uint)count > capacity - 1)
            {
                output[written] = 0;

                return -checked((int)written + 1);
            }
            for (int index = 0; index < count; index++)
                output[written++] = bytes[index];
            list = cell.Tail;
        }
        output[written] = 0;

        return checked((int)written + 1);
    }

    private static nuint BadArgument(NifNativeCallEnvironment owner)
    {
        owner.Raise(owner.Add(NifNativeNames.BadArgument));

        return 0;
    }
}
