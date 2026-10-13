using System.Diagnostics;
using System.Text;

namespace Erlang;

internal sealed class PortOpenOptions
{
    public ProcessStartInfo Start { get; } = new() { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true };
    public string Name { get; private set; } = string.Empty;
    public bool Binary { get; private set; }
    public bool Eof { get; private set; }
    public bool ExitStatus { get; private set; }
    public int PacketBytes { get; private set; }
    public int LineBytes { get; private set; }

    public static PortOpenOptions Parse(Term specification, Term settings)
    {
        var options = new PortOpenOptions();
        string[]? arguments = null;
        foreach (Term setting in Settings(settings))
        {
            switch (setting)
            {
                case Atom { Name: PortNames.Binary }:
                    options.Binary = true;
                    break;
                case Atom { Name: PortNames.Stream }:
                    options.PacketBytes = 0;
                    break;
                case Atom { Name: PortNames.UseStdio }:
                    break;
                case Atom { Name: PortNames.Hide }:
                    options.Start.CreateNoWindow = true;
                    break;
                case Atom { Name: PortNames.Eof }:
                    options.Eof = true;
                    break;
                case Atom { Name: PortNames.ExitStatus }:
                    options.ExitStatus = true;
                    break;
                case TupleTerm { Items.Count: 2 } tuple:
                    switch (tuple.Items[0])
                    {
                        case Atom { Name: PortLineNames.Option } when tuple.Items[1] is Integer line && line.Value > 0:
                            if (line.Value > PortLineParameters.MaximumBufferBytes)
                                throw new ErlangException(ErlangErrorReasons.SystemLimit);
                            options.LineBytes = (int)line.Value;
                            break;
                        case Atom { Name: PortNames.Packet } when tuple.Items[1] is Integer width && (width.Value == PortParameters.PacketOne || width.Value == PortParameters.PacketTwo || width.Value == PortParameters.PacketFour):
                            options.PacketBytes = (int)width.Value;
                            break;
                        case Atom { Name: PortNames.Args }:
                            arguments = Settings(tuple.Items[1]).Select(Text).ToArray();
                            break;
                        case Atom { Name: PortNames.WorkingDirectory }:
                            options.Start.WorkingDirectory = Text(tuple.Items[1]);
                            break;
                        case Atom { Name: PortNames.Environment }:
                            options.Start.Environment.Clear();
                            foreach (System.Collections.DictionaryEntry entry in System.Environment.GetEnvironmentVariables())
                                options.Start.Environment[(string)entry.Key] = (string?)entry.Value;
                            foreach (Term entry in Settings(tuple.Items[1]))
                            {
                                if (entry is not TupleTerm { Items.Count: 2 } pair)
                                    throw BadOption();
                                string key = Text(pair.Items[0]);
                                if (pair.Items[1] is Nil or Atom { Name: ErlangBooleanAtoms.False })
                                    options.Start.Environment.Remove(key);
                                else
                                    options.Start.Environment[key] = Text(pair.Items[1]);
                            }
                            break;
                        default:
                            throw BadOption();
                    }
                    break;
                default:
                    throw BadOption();
            }
        }
        if (options.LineBytes != 0 && options.PacketBytes != 0)
            throw BadOption();
        if (specification is not TupleTerm { Items.Count: 2 } name)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        options.Name = Text(name.Items[1]);
        if (name.Items[0] is Atom { Name: PortNames.SpawnExecutable })
        {
            options.Start.FileName = options.Name;
            if (arguments is not null)
                foreach (string argument in arguments)
                    options.Start.ArgumentList.Add(argument);
        }
        else if (name.Items[0] is Atom { Name: PortNames.Spawn } && arguments is null)
        {
            options.Start.FileName = OperatingSystem.IsWindows() ? PortParameters.WindowsShell : PortParameters.UnixShell;
            options.Start.ArgumentList.Add(OperatingSystem.IsWindows() ? PortParameters.WindowsShellSwitch : PortParameters.UnixShellSwitch);
            options.Start.ArgumentList.Add(options.Name);
        }
        else
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return options;
    }

    private static string Text(Term term)
    {
        string text = term switch
        {
            Atom atom => atom.Name,
            BitString { IsBinary: true } binary => Encoding.UTF8.GetString(binary.ToArray()),
            _ => string.Concat(Cons.Items(term).Select(item => item is Integer number && number.Value >= 0 && number.Value <= PortParameters.MaximumUnicodeCodepoint && Rune.IsValid((int)number.Value) ? new Rune((int)number.Value).ToString() : throw BadOption()))
        };
        if (text.Contains('\0'))
            throw BadOption();

        return text;
    }

    private static ErlangException BadOption() => new(PortNames.BadOption);

    private static Term[] Settings(Term term)
    {
        try
        {
            return Cons.Items(term).ToArray();
        }
        catch (ErlangException)
        {
            throw BadOption();
        }
    }
}
