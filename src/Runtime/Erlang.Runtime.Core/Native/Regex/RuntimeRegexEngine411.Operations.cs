using System.Runtime.InteropServices;
using System.Text;

namespace Erlang;

public sealed partial class RuntimeRegexEngine411
{
    private async ValueTask<byte[]> Bytes(ProcessContext context, Term input, bool unicode)
    {
        if (unicode && (input is Cons or Nil)) throw new NotSupportedException(ReNames411.Unsupported); // Requires actual original ucompile/urun wrappers, not a managed substitute.
        var value = await BinaryWrapperOutput.Flatten(context, input).ConfigureAwait(false);
        Check(context);

        return value.ToArray();
    }

    private (ReCode411? Code, Term? Error) CompileCode(byte[] pattern, bool unicode)
    {
        using var bytes = new ReBuffer411(pattern);
        var pointer = api.Compile(
            bytes.Pointer,
            (nuint)pattern.Length,
            unicode ? ReNames411.Utf : 0,
            out var error,
            out var offset,
            IntPtr.Zero
        );
        if (pointer == IntPtr.Zero)
        {
            using var message = new ReBuffer411(new byte[ReNativeConstants411.ErrorMessageBufferBytes]);
            if (api.Message(error, message.Pointer, ReNativeConstants411.ErrorMessageBufferBytes) < 0) throw new InvalidOperationException(ReDiagnosticNames411.NativeRegexErrorTextUnavailable);
            string text = Marshal.PtrToStringUTF8(message.Pointer)!;

            return (null, Term.Tuple(Term.A(ReAtomNames411.Error), Term.Tuple(Term.String(text), Term.I(checked((long)offset)))));
        }

        return (AdoptCode413(pointer, unicode ? (byte)1 : (byte)0), null);
    }

    private ReCode411 AdoptCode413(IntPtr pointer, byte unicode)
    {
        Interlocked.Increment(ref createdCodes);
        try
        {
            using var captures = new ReBuffer411(new byte[sizeof(uint)]);
            using var newline = new ReBuffer411(new byte[sizeof(uint)]);
            if (api.Info(pointer, ReNativeConstants411.CaptureCount, captures.Pointer) != 0 || api.Info(pointer, ReNativeConstants411.Newline, newline.Pointer) != 0) throw new InvalidOperationException(ReDiagnosticNames411.NativeRegexMetadataFailed);
            uint count = unchecked((uint)Marshal.ReadInt32(captures.Pointer));
            uint nl = unchecked((uint)Marshal.ReadInt32(newline.Pointer));
            var code = new ReCode411(
                pointer,
                count,
                unicode,
                nl is ReNativeConstants411.NewlineCrLf
                or ReNativeConstants411.NewlineAny or ReNativeConstants411.NewlineAnyCrLf
            );
            lock (gate)
            {
                if (closed) throw new ReOwnerClosing411(runtime);
                codes.Add(code);
            }

            return code;
        }
        catch
        {
            api.FreeCode(pointer);
            Interlocked.Increment(ref freedCodes);
            throw;
        }
    }

    public async ValueTask<Term> CompileAsync(ProcessContext context, Term pattern, Term options)
    {
        Enter(context);
        ReCode411? owned = null;
        try
        {
            bool unicode = ReOptions411.Compile(options);
            if (unicode && pattern is not BitString)
                return await UnicodeTrap453(
                    context,
                    ReUnicodeTrap453Names.Compile,
                    pattern,
                    options
                ).ConfigureAwait(false);

            var compiled = CompileCode(await Bytes(context, pattern, unicode).ConfigureAwait(false), unicode);
            if (compiled.Code is null) return compiled.Error!;
            owned = compiled.Code;
            Check(context);
            var payload = new RePattern411(this, owned);
            ResourceReferenceTerm reference;
            try
            {
                reference = runtime.ReferenceResources.CreateRegex411(payload);
            }
            catch
            {
                ReleaseGuest(owned);
                owned = null;
                throw;
            }
            var mp = Term.Tuple(
                Term.A(ReNames411.Pattern),
                Term.I(owned.Captures),
                Term.I(unicode ? 1 : 0),
                Term.I(owned.Crlf ? 1 : 0),
                reference
            );
            owned = null;

            return Term.Tuple(Term.A(ReAtomNames411.Ok), mp);
        }
        finally
        {
            if (owned is not null) ReleaseGuest(owned);
            Exit(context);
        }
    }

    public Term Version(ProcessContext context)
    {
        Enter(context);
        try
        {
            return new BitString(Encoding.ASCII.GetBytes(api.Version()));
        }
        finally
        {
            Exit(context);
        }
    }

    private static bool SmallMetadata(Term value) => value is Integer number && number.Value >= ErlangIntegerParseParameters.MinimumNativeSmall && number.Value <= ErlangIntegerParseParameters.MaximumNativeSmall;

    private ReCode411 Authenticate(Term expression, bool canonicalMetadata = true)
    {
        if (expression is not TupleTerm { Items.Count: 5 } mp || !mp.Items[0].Equals(Term.A(ReNames411.Pattern)) || mp.Items[4] is not ReferenceTerm reference)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        using var lease = runtime.ReferenceResources.AcquireRegex411(reference);
        if (lease is null) throw new ErlangException(ErlangErrorReasons.BadArgument);
        RePattern411 payload;
        try
        {
            payload = lease.Value;
        }
        catch (ObjectDisposedException)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        if (!ReferenceEquals(payload.Owner, this)) throw new ErlangException(ErlangErrorReasons.BadArgument);
        var code = payload.Code;
        if (!SmallMetadata(mp.Items[1]) || !SmallMetadata(mp.Items[2]) || !SmallMetadata(mp.Items[3])) throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (canonicalMetadata && (!mp.Items[1].Equals(Term.I(code.Captures)) || !mp.Items[2].Equals(Term.I(code.UnicodeMetadata)) || !mp.Items[3].Equals(Term.I(code.Crlf ? 1 : 0))))
            throw new NotSupportedException(ReNames411.Unsupported); // Pinned raw tuple metadata is not universally authenticated; altered metadata is a held capability, never fake badarg.
        Read(code);
        GC.KeepAlive(payload); // Reader ownership now protects native code if the last guest payload subsequently dies.

        return code;
    }

    public async ValueTask<Term> RunAsync(
        ProcessContext context,
        Term subject,
        Term expression,
        Term options
    ) => await RunCore413Async(
        context,
        subject,
        expression,
        options,
        true
    ).ConfigureAwait(false);

    private async ValueTask<Term> RunCore413Async(
        ProcessContext context,
        Term subject,
        Term expression,
        Term options,
        bool validateUtf,
        bool ownAdmission = true
    )
    {
        if (ownAdmission) Enter(context);
        ReCode411? code = null;
        bool temporary = false;
        try
        {
            bool binaryCaptures = ReOptions411.Run(options);
            if (expression is TupleTerm) code = Authenticate(expression);
            else
            {
                var result = CompileCode(await Bytes(context, expression, false).ConfigureAwait(false), false);
                if (result.Code is null) throw new ErlangException(ErlangErrorReasons.BadArgument);
                var candidate = result.Code;
                try
                {
                    Read(candidate);
                }
                catch
                {
                    ReleaseGuest(candidate);
                    throw;
                }
                code = candidate;
                temporary = true;
            }
            if (code.Unicode && subject is not BitString)
                return await UnicodeTrap453(
                    context,
                    ReUnicodeTrap453Names.Run,
                    subject,
                    expression,
                    options
                ).ConfigureAwait(false);

            BitString? captureSource = (subject as BitString)?.PinRegexCaptureSource();
            byte[] input = await Bytes(context, subject, code.Unicode).ConfigureAwait(false);

            return await MatchAsync(
                context,
                code,
                input,
                validateUtf,
                binaryCaptures,
                captureSource
            ).ConfigureAwait(false);
        }
        finally
        {
            if (code is not null)
            {
                EndRead(code);
                if (temporary) ReleaseGuest(code);
            }
            if (ownAdmission) Exit(context);
        }
    }

    private async ValueTask<Term> MatchAsync(
        ProcessContext context,
        ReCode411 code,
        byte[] input,
        bool validateUtf,
        bool binaryCaptures,
        BitString? originalSubject
    )
    {
        using var subject = new ReBuffer411(input);
        using var restart = new ReBuffer411(new byte[IntPtr.Size]);
        var data = api.CreateData(code.Pointer, IntPtr.Zero);
        if (data == IntPtr.Zero) throw new OutOfMemoryException();
        Interlocked.Increment(ref createdData);
        try
        {
            if (api.SetRestart(data, restart.Pointer) != 0 || api.SetFlags(data, 0) != 0) throw new InvalidOperationException(ReDiagnosticNames411.RegexRestartInitializationFailed);
            bool first = true;
            bool resumed = false;
            int result;
            do
            {
                Check(context);
                api.SetLoops(data, ReNames411.LoopBudget);
                api.SetFlags(data, 0);
                if (!first) resumed = true;
                result = first ? api.Match(
                    code.Pointer,
                    subject.Pointer,
                    (nuint)input.Length,
                    0,
                    validateUtf ? 0 : ReExtendedNames413.NoUtfCheck,
                    data,
                    IntPtr.Zero
                )
                    : api.Match(
                        IntPtr.Zero,
                        IntPtr.Zero,
                        0,
                        0,
                        0,
                        data,
                        IntPtr.Zero
                    );
                first = false;
                if (result == ReNames411.LoopLimit)
                {
                    Interlocked.Increment(ref matchYields);
                    if (Marshal.ReadIntPtr(restart.Pointer) == IntPtr.Zero) throw new InvalidOperationException(ReDiagnosticNames411.RegexYieldedWithoutNativeRestartState);
                    await context.ReduceAsync();
                    await Task.Yield();
                }
            } while (result == ReNames411.LoopLimit);
            Check(context);
            if (result == ReNames411.Utf8Yield) throw new NotSupportedException(ReNames411.Unsupported); // Public pinned match converts internal UTF8 yield to LOOP_LIMIT; refuse ABI drift, never fake guest badarg.
            if (result is 0 or ReNames411.NoMatch or ReNativeConstants411.MatchLimit or ReNativeConstants411.DepthLimit
                || (!resumed && (result is ReNativeConstants411.HeapLimit or ReNativeConstants411.RecurseLoop)))
                return Term.A(ReAtomNames411.Nomatch);
            if (result <= ReNativeConstants411.Utf8ErrorFirst && result >= ReNativeConstants411.Utf8ErrorLast
                || result is ReNativeConstants411.BadUtfOffset or ReNativeConstants411.BadOffset or ReNativeConstants411.BadMagic or ReNativeConstants411.BadMode)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (result < 0) throw new InvalidOperationException(ReDiagnosticNames411.RegexCaptureVectorInvalid);
            var vector = api.Vector(data);
            if (vector == IntPtr.Zero || result > code.Captures + 1) throw new InvalidOperationException(ReDiagnosticNames411.RegexCaptureVectorInvalid);
            var captures = new Term[result];
            for (int i = 0; i < result; i++)
            {
                long start = Marshal.ReadInt64(vector, checked(i * ReNativeConstants411.CapturePairBytes)),
                    end = Marshal.ReadInt64(vector, checked(i * ReNativeConstants411.CapturePairBytes + ReNativeConstants411.CaptureElementBytes));
                if (binaryCaptures)
                {
                    if (start == -1)
                        captures[i] = new BitString([]);
                    else
                    {
                        int offset = checked((int)start), count = checked((int)(end - start));
                        captures[i] = originalSubject is null
                            ? new BitString(input.AsSpan(offset, count))
                            : originalSubject.SliceBits(checked(offset * ReCaptureNames485.BitsPerByte), checked(count * ReCaptureNames485.BitsPerByte));
                    }
                }
                else
                    captures[i] = start == -1 ? Term.Tuple(Term.I(-1), Term.I(0)) : Term.Tuple(Term.I(start), Term.I(checked(end - start)));
            }

            return Term.Tuple(Term.A(ReAtomNames411.Match), Term.List(captures));
        }
        finally
        {
            try
            {
                if (Marshal.ReadIntPtr(restart.Pointer) != IntPtr.Zero) api.FreeRestart(data);
            }
            finally
            {
                api.FreeData(data);
                Interlocked.Increment(ref freedData);
            }
        }
    }
}
