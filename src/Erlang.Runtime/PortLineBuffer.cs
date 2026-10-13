namespace Erlang;

internal sealed class PortLineBuffer(int capacity)
{
    private readonly List<byte> bytes = new();
    private PortLineState state;

    public IReadOnlyList<PortLineResult> Read(ReadOnlySpan<byte> input)
    {
        var results = new List<PortLineResult>();
        int index = 0;
        while (index < input.Length)
        {
            byte current = input[index];
            if (current == PortLineParameters.LineFeed)
            {
                state = PortLineState.Main;
                index++;
                results.Add(Take(true));
                continue;
            }
            switch (state)
            {
                case PortLineState.Main:
                    if (bytes.Count == capacity)
                        state = PortLineState.Full;
                    else if (current == PortLineParameters.CarriageReturn)
                    {
                        index++;
                        state = PortLineState.CarriageReturnInside;
                    }
                    else
                        bytes.Add(input[index++]);
                    break;
                case PortLineState.Full:
                    if (current == PortLineParameters.CarriageReturn)
                    {
                        index++;
                        state = PortLineState.CarriageReturnAfter;
                    }
                    else
                    {
                        results.Add(Take(false));
                        state = PortLineState.Main;
                    }
                    break;
                case PortLineState.CarriageReturnInside:
                    bytes.Add(PortLineParameters.CarriageReturn);
                    state = PortLineState.Main;
                    break;
                case PortLineState.CarriageReturnAfter:
                    results.Add(Take(false));
                    state = PortLineState.CarriageReturnInside;
                    break;
            }
        }

        return results;
    }

    public IReadOnlyList<PortLineResult> Flush()
    {
        var results = new List<PortLineResult>();
        do
        {
            if (state == PortLineState.CarriageReturnInside)
                bytes.Add(PortLineParameters.CarriageReturn);
            bool deferred = state == PortLineState.CarriageReturnAfter;
            state = deferred ? PortLineState.CarriageReturnInside : PortLineState.Main;
            if (bytes.Count != 0)
                results.Add(Take(false));
        }
        while (state == PortLineState.CarriageReturnInside);

        return results;
    }

    private PortLineResult Take(bool eol)
    {
        byte[] payload = bytes.ToArray();
        bytes.Clear();

        return new(payload, eol);
    }
}
