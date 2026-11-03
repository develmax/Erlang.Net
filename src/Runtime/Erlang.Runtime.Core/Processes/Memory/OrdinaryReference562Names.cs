namespace Erlang;

internal static class OrdinaryReference562Names
{
    internal const int HeaderWords = 1;
    internal const int DataWords = 2;
    internal const int NumberCount = 3;
    internal const int NumberBits = 32;
    internal const int HeaderArityShift = 6;
    internal const nuint ReferenceSubtag = 0x10;
    internal const nuint Marker = uint.MaxValue;
    internal static nuint Header => ((nuint)DataWords << HeaderArityShift) | ReferenceSubtag;
    internal const string Platform = "Ordinary reference storage requires the pinned little-endian 64-bit profile.";
    internal const string Provenance = "Owned ordinary reference requires this VM's actual ordinary factory identity.";
}
