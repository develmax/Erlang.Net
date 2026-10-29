using System.Buffers.Binary;

namespace Erlang;

public sealed partial class RuntimeRegexEngine411
{
    private static uint ExportChecksum413(ProcessContext context, ReadOnlySpan<byte> bytes)
    {
        uint crc = uint.MaxValue;
        foreach (byte item in bytes)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            crc ^= item;
            for (int bit = 0; bit < ReExtendedNames413.BitsPerByte; bit++)
                crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0 : ReExtendedNames413.CrcPolynomial);
        }

        return ~crc;
    }

    private Term PublishImported413(ProcessContext context, ReCode411 code)
    {
        var payload = new RePattern411(this, code);
        var reference = runtime.ReferenceResources.CreateRegex411(payload);
        reference = context.AdmitRegexProducer597(reference, code.Backing597);

        try
        {
            return Term.Tuple(
                Term.A(ReNames411.Pattern),
                Term.I(code.Captures),
                Term.I(code.UnicodeMetadata),
                Term.I(code.Crlf ? 1 : 0),
                reference
            );
        }
        catch (Exception original)
        {
            try
            {
                reference.RegexHost597?.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }

    private async ValueTask<Term> Import413Async(ProcessContext context, Term exported)
    {
        Enter(context);
        ReCode411? owned = null;
        ReMagicHost597? failedHost597 = null;
        Exception? primary597 = null;
        try
        {
            if (exported is not TupleTerm { Items.Count: 5 } tuple
                || !tuple.Items[0].Equals(Term.A(ReExtendedNames413.ExportedPattern))
                || tuple.Items[1] is not BitString { IsBinary: true } header
                || header.ByteSpan.Length < ReExtendedNames413.HeaderBytes
                || !header.ByteSpan[..ReExtendedNames413.HeaderTitleBytes].SequenceEqual(ReExtendedNames413.HeaderTitle))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            bool fallback = header.ByteSpan[ReExtendedNames413.EncodingOffset] != ReExtendedNames413.EncodingVersion;
            if (!fallback)
            {
                if (tuple.Items[4] is not BitString { IsBinary: true } encoded)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                byte[] bytes = encoded.ByteSpan.ToArray();
                uint expected = BinaryPrimitives.ReadUInt32BigEndian(header.ByteSpan[ReExtendedNames413.ChecksumOffset..]);
                if (ExportChecksum413(context, bytes) != expected)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                fallback = ReSerializedLayout413.RequiresFallback(bytes);
                if (!fallback)
                {
                    using var input = new ReBuffer411(bytes);
                    int result = api.Decode(
                        out IntPtr code,
                        1,
                        input.Pointer,
                        IntPtr.Zero
                    );
                    if (result == 1)
                        owned = AdoptCode413(code, header.ByteSpan[ReExtendedNames413.UnicodeOffset]);
                    else if (result is ReNativeConstants411.BadMagic or ReNativeConstants411.BadMode)
                        fallback = true;
                    else
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                }
            }
            if (fallback)
            {
                if (tuple.Items[2] is not BitString)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                Term options;
                try
                {
                    options = Cons.From(Cons.Items(tuple.Items[3]).Where(option => !option.Equals(Term.A(ReExtendedNames413.ExportOption))));
                }
                catch (ErlangException)
                {
                    throw new ReBadOption411();
                }
                bool unicode = ReOptions411.Compile(options);
                var compiled = CompileCode(await Bytes(context, tuple.Items[2], unicode).ConfigureAwait(false), unicode);
                if (compiled.Code is null)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                owned = compiled.Code;
            }
            Check(context);
            var resultTerm = PublishImported413(context, owned!);
            failedHost597 = ((ResourceReferenceTerm)((TupleTerm)resultTerm).Items[ReMagicNames597.PatternReferenceIndex]).RegexHost597;
            ReleaseGuest(owned!);
            owned = null;

            failedHost597 = null;

            return resultTerm;
        }
        catch (Exception error)
        {
            primary597 = error;
            throw;
        }
        finally
        {
            CleanupPublication597(
                context,
                owned,
                failedHost597,
                primary597
            );
        }
    }
}
