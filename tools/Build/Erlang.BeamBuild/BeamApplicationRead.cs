namespace Erlang.BeamBuild;

internal static class BeamApplicationRead
{
    internal static byte[] Bytes(string path, int limit)
    {
        BeamApplicationPaths.RequireActualCase(path);
        using var input = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
        );
        if (input.Length > limit)
            throw new InvalidDataException(BeamApplicationNames.Invalid);
        using var output = new MemoryStream();
        byte[] buffer = new byte[BeamApplicationNames.ReadBufferBytes];
        int count;
        while ((count = input.Read(buffer)) != 0)
        {
            if (output.Length + count > limit)
                throw new InvalidDataException(BeamApplicationNames.Invalid);
            output.Write(buffer, 0, count);
        }
        BeamDeclaredPaths.RequireNoReparse(path);

        return output.ToArray();
    }
}
