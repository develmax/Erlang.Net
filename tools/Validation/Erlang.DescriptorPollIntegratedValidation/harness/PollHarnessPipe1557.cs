using System.Runtime.InteropServices;

namespace Erlang;

// The fixture owns both exact FDs; engine registrations only borrow them.
internal sealed class PollHarnessPipe1557 : IDisposable
{
    internal int ReadFd { get; private set; }
    internal int WriteFd { get; private set; }

    internal PollHarnessPipe1557()
    {
        int[] descriptors = [-1, -1];
        if (LinuxPortNative.pipe2(descriptors, LinuxPortParameters.NonBlocking | LinuxPortParameters.CloseOnExec) != 0)
            throw new NativePortIOException(Marshal.GetLastPInvokeError());
        ReadFd = descriptors[PollHarnessNames1557.PipeReadIndex];
        WriteFd = descriptors[PollHarnessNames1557.PipeWriteIndex];
    }

    internal unsafe void WriteByte()
    {
        byte value = PollHarnessNames1557.DataByte;
        nint result = LinuxPortNative.write(WriteFd, (nint)(&value), 1);
        if (result != 1) throw new NativePortIOException(Marshal.GetLastPInvokeError());
    }

    internal unsafe int ReadOne(out byte value)
    {
        byte actual = 0;
        nint result = LinuxPortNative.read(ReadFd, (nint)(&actual), 1);
        if (result < 0) throw new NativePortIOException(Marshal.GetLastPInvokeError());
        value = actual;

        return (int)result;
    }

    internal void ReplaceOwnedReadFrom(PollHarnessPipe1557 source)
    {
        // Destination is STILL our open owned FD, not an arbitrary reused
        // integer. Caller first joins old engine owner retirement.
        if (PollHarnessNative1557.dup3(source.ReadFd, ReadFd, LinuxPortParameters.CloseOnExec) != ReadFd)
            throw new NativePortIOException(Marshal.GetLastPInvokeError());
    }

    internal void CloseWrite()
    {
        int descriptor = WriteFd;
        WriteFd = -1;
        if (descriptor >= 0 && LinuxDescriptorPollNative.close(descriptor) != 0)
            throw new NativePortIOException(Marshal.GetLastPInvokeError());
    }

    public void Dispose()
    {
        var errors = new List<Exception>();
        int descriptor = ReadFd;
        ReadFd = -1;
        if (descriptor >= 0 && LinuxDescriptorPollNative.close(descriptor) != 0)
            errors.Add(new NativePortIOException(Marshal.GetLastPInvokeError()));
        try
        {
            CloseWrite();
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        if (errors.Count != 0) throw new AggregateException(errors);
    }
}
