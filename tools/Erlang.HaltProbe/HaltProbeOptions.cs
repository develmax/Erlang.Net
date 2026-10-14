namespace Erlang.HaltProbe;

internal sealed record HaltProbeOptions(
    IReadOnlyList<string> Sources,
    string? Module,
    string Entry,
    bool Compiled,
    string Evidence,
    string? Dump
)
{
    public static HaltProbeOptions Parse(string[] args)
    {
        var sources = new List<string>();
        string? module = null, evidence = null, dump = null;
        string entry = HaltProbeNames.DefaultEntry;
        bool compiled = false;
        for (int index = 0; index < args.Length; index += 2)
        {
            if (index + 1 == args.Length)
                throw new ArgumentException(args[index]);
            string value = args[index + 1];
            switch (args[index])
            {
                case HaltProbeNames.Source: sources.Add(Path.GetFullPath(value)); break;
                case HaltProbeNames.Module: module = value; break;
                case HaltProbeNames.Entry: entry = value; break;
                case HaltProbeNames.Evidence: evidence = Path.GetFullPath(value); break;
                case HaltProbeNames.Dump: dump = Path.GetFullPath(value); break;
                case HaltProbeNames.Backend:
                    if (value is not (HaltProbeNames.Compiled or HaltProbeNames.Interpreted))
                        throw new ArgumentException(value);
                    compiled = value == HaltProbeNames.Compiled;
                    break;
                default: throw new ArgumentException(args[index]);
            }
        }
        if (sources.Count == 0 || evidence is null)
            throw new ArgumentException(HaltProbeNames.Source);

        return new(
            sources.AsReadOnly(),
            module,
            entry,
            compiled,
            evidence,
            dump
        );
    }
}
