namespace Erlang.BeamBuild;

internal sealed class BeamApplicationPathSet
{
    private readonly Dictionary<string, (string Spelling, bool File)> entries = new(StringComparer.OrdinalIgnoreCase);

    internal void AddFile(string path)
    {
        BeamApplicationPaths.RequireLogical(path);
        string[] parts = path.Split(BeamApplicationNames.LogicalSeparatorCharacter);
        string prefix = string.Empty;
        for (int index = 0; index < parts.Length; index++)
        {
            prefix = prefix.Length == 0 ? parts[index] : prefix + BeamApplicationNames.LogicalSeparator + parts[index];
            bool file = index == parts.Length - 1;
            if (entries.TryGetValue(prefix, out var prior))
            {
                if (prior.Spelling != prefix || prior.File || file)
                    throw new InvalidDataException(BeamApplicationNames.Invalid);
            }
            else
                entries.Add(prefix, (prefix, file));
        }
    }
}
