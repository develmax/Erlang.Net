using System.Globalization;

internal sealed record BeamCorpusOptions(string[] Paths, string? ModulesFile, int Jobs)
{
    internal static BeamCorpusOptions? Parse(string[] arguments)
    {
        var paths = new List<string>();
        string? modulesFile = null;
        int? jobs = null;
        for (int index = 0; index < arguments.Length; index++)
        {
            string argument = arguments[index];
            switch (argument)
            {
                case "--modules-file" when modulesFile is null && index + 1 < arguments.Length:
                    modulesFile = arguments[++index];
                    break;
                case "--jobs" when jobs is null && index + 1 < arguments.Length:
                    if (!int.TryParse(
                        arguments[++index],
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out int workers
                    ) || workers < 1)
                    {
                        return null;
                    }

                    jobs = workers;
                    break;
                default:
                    if (argument.StartsWith("--", StringComparison.Ordinal))
                    {
                        return null;
                    }

                    paths.Add(argument);
                    break;
            }
        }

        return paths.Count is >= 2 and <= 4 ? new(paths.ToArray(), modulesFile, jobs ?? 1) : null;
    }
}
