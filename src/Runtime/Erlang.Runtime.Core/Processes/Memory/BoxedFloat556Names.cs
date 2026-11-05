namespace Erlang;

internal static class BoxedFloat556Names
{
    internal const int HeaderWords = 1;
    internal static int PayloadWords => NativeWordLayout.DoubleWords;
    internal const int HeaderArityShift = 6;
    internal const nuint FloatSubtag = 0x18;
    internal static nuint Header => ((nuint)PayloadWords << HeaderArityShift) | FloatSubtag;
}
