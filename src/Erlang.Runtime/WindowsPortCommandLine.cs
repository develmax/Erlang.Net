using System.Text;

namespace Erlang;

internal static class WindowsPortCommandLine
{
    public static StringBuilder Create(PortOpenOptions options)
    {
        var command = new StringBuilder();
        foreach (string argument in new[] { options.ArgumentZero ?? options.Start.FileName }.Concat(options.Start.ArgumentList))
        {
            if (command.Length != 0)
                command.Append(NativePortNames.ArgumentSeparator);
            Quote(command, argument);
        }

        return command;
    }

    private static void Quote(StringBuilder command, string argument)
    {
        if (argument.Length != 0 && !argument.Any(character => char.IsWhiteSpace(character) || character == NativePortNames.ArgumentQuote))
        {
            command.Append(argument);

            return;
        }
        command.Append(NativePortNames.ArgumentQuote);
        int slashes = 0;
        foreach (char character in argument)
        {
            if (character == NativePortNames.ArgumentEscape)
            {
                slashes++;
                continue;
            }
            command.Append(NativePortNames.ArgumentEscape, character == NativePortNames.ArgumentQuote ? slashes * 2 + 1 : slashes);
            command.Append(character);
            slashes = 0;
        }
        command.Append(NativePortNames.ArgumentEscape, slashes * 2);
        command.Append(NativePortNames.ArgumentQuote);
    }
}
