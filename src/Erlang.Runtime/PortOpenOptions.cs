using System.Diagnostics;
using System.Text;

namespace Erlang;

internal sealed class PortOpenOptions
{
    public ProcessStartInfo Start { get; } = new() { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true };
    public string Name { get; private set; } = string.Empty;
    public bool IsDescriptor { get; private set; }
    public int InputDescriptor { get; private set; }
    public int OutputDescriptor { get; private set; }
    public bool Binary { get; private set; }
    public bool Eof { get; private set; }
    public bool ExitStatus { get; private set; }
    public int PacketBytes { get; private set; }
    public int LineBytes { get; private set; }
    public ulong HighWatermark { get; private set; } = PortParameters.DefaultHighWatermark;
    public ulong LowWatermark { get; private set; } = PortParameters.DefaultLowWatermark;
    public bool Parallelism { get; private set; }
    public bool UseStdio { get; private set; } = true;
    public bool MergeStandardError { get; private set; }
    public bool OverlappedIo { get; private set; }
    public string? ArgumentZero { get; private set; }
    public PortDirectionFlags Directions { get; private set; }
    public bool Read => (Directions & PortDirectionFlags.Read) != 0;
    public bool Write => (Directions & PortDirectionFlags.Write) != 0;
    public bool NativeRequired => !Read || !Write || MergeStandardError || ArgumentZero is not null ||
        !UseStdio || OperatingSystem.IsWindows() && OverlappedIo;
    public ulong MessageHighWatermark { get; private set; } = PortMessageParameters.DefaultHighWatermark;
    public ulong MessageLowWatermark { get; private set; } = PortMessageParameters.DefaultLowWatermark;

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
                case Atom { Name: PortLineNames.Option }:
                    options.LineBytes = PortParameters.DefaultLineBytes;
                    break;
                case Atom { Name: PortNames.UseStdio }:
                    options.UseStdio = true;
                    break;
                case Atom { Name: PortNames.NoUseStdio }:
                    options.UseStdio = false;
                    break;
                case Atom { Name: PortNames.ReadOnlyOption }:
                    options.Directions |= PortDirectionFlags.Read;
                    break;
                case Atom { Name: PortNames.WriteOnlyOption }:
                    options.Directions |= PortDirectionFlags.Write;
                    break;
                case Atom { Name: PortNames.MergeStandardError }:
                    options.MergeStandardError = true;
                    break;
                case Atom { Name: PortNames.OverlappedIo }:
                    options.OverlappedIo = true;
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
                        case Atom { Name: PortNames.Parallelism } when tuple.Items[1] is Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False } boolean:
                            options.Parallelism = boolean.Name == ErlangBooleanAtoms.True;
                            break;
                        case Atom { Name: PortNames.BusyLimitsPort }:
                            (options.LowWatermark, options.HighWatermark) = Watermarks(tuple.Items[1]);
                            break;
                        case Atom { Name: PortNames.ArgumentZero }:
                            options.ArgumentZero = Text(tuple.Items[1]);
                            break;
                        case Atom { Name: PortNames.BusyLimitsMessageQueue }:
                            (options.MessageLowWatermark, options.MessageHighWatermark) = Watermarks(tuple.Items[1]);
                            break;
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
        if (options.Directions == PortDirectionFlags.None)
            options.Directions = PortDirectionFlags.Both;
        if (options.LineBytes != 0 && options.PacketBytes != 0 || options.MergeStandardError && !options.UseStdio)
            throw BadOption();
        if (specification is TupleTerm { Items.Count: 3 } descriptor && descriptor.Items[0] is Atom { Name: PortDescriptorNames.Kind })
        {
            if (options.ExitStatus || arguments is not null || options.ArgumentZero is not null ||
                descriptor.Items[1] is not Integer input || descriptor.Items[2] is not Integer output ||
                input.Value < DescriptorPortParameters.MinimumSmallInteger || input.Value > DescriptorPortParameters.MaximumSmallInteger ||
                output.Value < DescriptorPortParameters.MinimumSmallInteger || output.Value > DescriptorPortParameters.MaximumSmallInteger ||
                OperatingSystem.IsLinux() && (options.Read && (input.Value < 0 || input.Value > int.MaxValue) || options.Write && (output.Value < 0 || output.Value > int.MaxValue)))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            options.IsDescriptor = true;
            options.InputDescriptor = unchecked((int)(long)input.Value);
            options.OutputDescriptor = unchecked((int)(long)output.Value);
            options.Name = FormattableString.Invariant($"{options.InputDescriptor}/{options.OutputDescriptor}");

            return options;
        }
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
        else if (name.Items[0] is Atom { Name: PortNames.Spawn } && arguments is null && options.ArgumentZero is null)
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

    private static (ulong Low, ulong High) Watermarks(Term term)
    {
        if (term is Atom { Name: PortNames.Disabled })
            return (ulong.MaxValue, ulong.MaxValue);
        if (term is not TupleTerm { Items.Count: 2 } pair ||
            pair.Items[0] is not Integer low || pair.Items[1] is not Integer high ||
            low.Value < 1 || high.Value < 1 || low.Value >= ulong.MaxValue || high.Value >= ulong.MaxValue)
            throw BadOption();

        return ((ulong)System.Numerics.BigInteger.Min(low.Value, high.Value), (ulong)high.Value);
    }

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
