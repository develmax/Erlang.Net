namespace Erlang;

internal sealed unsafe partial class ErlDriverPort420
{
    internal void OutputDistribution457(byte[] bytes)
        => Invoke(
            0,
            true,
            () =>
        {
            if (!started) throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);
            var vector = Entry.Callback<ErlDriverEntryTypes420.Binary>(ErlDriverEntryNames420.OutputVector);
            if (vector is not null)
            {
                using var input = new ErlDriverDistributionVector457(memory, layout457, bytes);
                vector(data, input.Pointer);
            }
            else
            {
                var output = Entry.Callback<ErlDriverEntryTypes420.Output>(ErlDriverEntryNames420.Output);
                if (output is not null)
                    fixed (byte* input = bytes) output(data, (nint)input, checked((nuint)bytes.Length));
            }

            return true;
        }
        );
}
