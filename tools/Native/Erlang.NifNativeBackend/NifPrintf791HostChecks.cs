using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Erlang.NifNativeBackend;

public static class NifPrintf791HostChecks
{
    public static Task FixedTermRenderAndCleanup() => NifNativeTypeHostChecks.Run(Check);

    private static unsafe void Check(ProcessContext context)
    {
        var failure = new NifNativeFailureState();
        var allocations = new NifNativeAllocationOwner();
        using var scope = new NifNativeCallbackScope(allocations, failure);
        using var environment = new NifNativeCallEnvironment(
            context,
            new ModuleCodeImage(NifPrintf791HostNames.Module),
            0,
            failure
        );
        nuint tuple = environment.Add(Term.Tuple(Term.A(NifPrintf791HostNames.Ok), Term.I(42)));
        string actualTuple = Render(tuple, NifPrintf791HostNames.DefaultTermBudget);
        if (actualTuple != NifPrintf791HostNames.TupleText)
            throw new InvalidOperationException(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                NifPrintf791HostNames.TextMismatch,
                NifPrintf791HostNames.TupleText,
                actualTuple
            ));
        Require(Render(tuple, 0) == NifPrintf791HostNames.TruncatedText);
        Require(Render(tuple, 1) == NifPrintf791HostNames.FirstNodeText);
        Require(Render(environment.Add(Term.A(NifPrintf791HostNames.QuoteAtom)), NifPrintf791HostNames.DefaultTermBudget)
            == NifPrintf791HostNames.QuoteText);
        Require(Render(environment.Add(Term.List(Term.I('a'), Term.I('b'), Term.I('c'))), NifPrintf791HostNames.DefaultTermBudget)
            == NifPrintf791HostNames.PrintableText);
        var render = (delegate* unmanaged[Cdecl]<nuint, long, nint, nint, int>)&NifNativePrintf791Callbacks.RenderTerm;
        var reject = (delegate* unmanaged[Cdecl]<nint, byte*, nuint, int>)&Reject;
        Require(render(
            tuple,
            NifPrintf791HostNames.DefaultTermBudget,
            (nint)reject,
            0
        ) == -NifNativePrintf791Names.InvalidArgument);
        failure.ThrowPending();
        Require(allocations.Count == 0);
        NifPrintf791StreamingChecks.Check(context);
    }

    private static unsafe string Render(nuint value, long precision)
    {
        var capture = new NifPrintf791Capture();
        GCHandle handle = GCHandle.Alloc(capture);
        try
        {
            var render = (delegate* unmanaged[Cdecl]<nuint, long, nint, nint, int>)&NifNativePrintf791Callbacks.RenderTerm;
            var write = (delegate* unmanaged[Cdecl]<nint, byte*, nuint, int>)&Write;
            int returned = render(
                value,
                precision,
                (nint)write,
                GCHandle.ToIntPtr(handle)
            );
            Require(capture.Failure is null && returned == capture.Bytes.Count);

            return Encoding.UTF8.GetString(capture.Bytes.ToArray());
        }
        finally
        {
            handle.Free();
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int Write(nint state, byte* bytes, nuint count)
    {
        NifPrintf791Capture? capture = null;
        try
        {
            capture = (NifPrintf791Capture)GCHandle.FromIntPtr(state).Target!;
            capture.Bytes.AddRange(new ReadOnlySpan<byte>(bytes, checked((int)count)).ToArray());

            return checked((int)count);
        }
        catch (Exception failure)
        {
            if (capture is not null) capture.Failure = failure;

            return -NifNativePrintf791Names.InvalidArgument;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int Reject(nint state, byte* bytes, nuint count)
        => -NifNativePrintf791Names.InvalidArgument;

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(NifPrintf791HostNames.Failure);
    }
}
