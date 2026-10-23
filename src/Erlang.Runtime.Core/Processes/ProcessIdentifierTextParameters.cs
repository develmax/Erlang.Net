namespace Erlang;

internal static class ProcessIdentifierTextParameters
{
    public const int PidMaximumInputBytes = 64;
    public const int PortMaximumInputBytes = 38;
    public const int StoredPidSerialShift = 32;
    public const ulong StoredPidNumberMask = uint.MaxValue;
    public const ulong MaximumLocalPidNumber = (1UL << 28) - 1;
    public const ulong MaximumLocalPidSerial = uint.MaxValue;
    public const ulong MaximumLocalPortNumber = (1UL << 60) - 1;
    public const ulong LocalChannel = 0;
    public const ulong DecimalRadix = 10;
}
