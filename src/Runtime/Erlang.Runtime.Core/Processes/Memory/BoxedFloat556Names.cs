namespace Erlang;

internal static class BoxedFloat556Names
{
    internal const int HeaderWords = 1;
    internal const int PayloadWords = 1;
    internal const int HeaderArityShift = 6;
    internal const nuint FloatSubtag = 0x18;
    internal const nuint Header = ((nuint)PayloadWords << HeaderArityShift) | FloatSubtag;
}
