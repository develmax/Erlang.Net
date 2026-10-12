namespace Erlang.Compiler;

internal static class LexerWhitespace
{
    private const char ControlMinimum = '\0';
    private const char ControlMaximum = ' ';
    private const char ExtendedMinimum = '\u0080';
    private const char ExtendedMaximum = '\u00a0';

    public static bool Is(char value) =>
        value is >= ControlMinimum and <= ControlMaximum or >= ExtendedMinimum and <= ExtendedMaximum;
}
