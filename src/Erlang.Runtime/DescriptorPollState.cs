using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct DescriptorPollState
{
    public int Descriptor;
    public short Events;
    public short Returned;
}
