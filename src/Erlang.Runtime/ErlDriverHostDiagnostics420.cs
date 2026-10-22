namespace Erlang;

// Retain bounded real child diagnostics while continuously draining its pipes.
internal sealed class ErlDriverHostDiagnostics420
{
    private const int RetainedCharacters = 32768;
    private readonly char[] retained = new char[RetainedCharacters];
    private readonly StreamReader reader;
    private int written;
    private int start;

    internal ErlDriverHostDiagnostics420(StreamReader reader) => this.reader = reader;

    internal string Text
    {
        get
        {
            var text = new char[written];
            for (int index = 0; index < written; index++)
                text[index] = retained[(start + index) % retained.Length];

            return new(text);
        }
    }

    internal async Task DrainAsync()
    {
        var buffer = new char[2048];
        int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory()).ConfigureAwait(false)) != 0)
        {
            for (int index = 0; index < count; index++)
            {
                if (written == retained.Length)
                {
                    retained[start] = buffer[index];
                    start = (start + 1) % retained.Length;
                }
                else retained[(start + written++) % retained.Length] = buffer[index];
            }
        }
    }
}
