namespace HelloHybrid;

public interface IBox<out T>
{
    T Value
    {
        get;
    }
}
public sealed record Box<T>(T Value) : IBox<T>;
public readonly record struct Coordinate(int X, int Y);

public static class CSharpFeatures
{
    private static int fun(int value) => value;

    public static async Task Verify()
    {
        // receive {hello, X} -> X end. is prose in ordinary C#.
        string raw = """case anything of {hello, X} -> X end.""";
        string interpolated = $"receive {fun(42)}";
        Box<string?> box = new(null);
        Coordinate point = new(1, 2);
        Func<int, int> identity = value => fun(value);
        int selected;
        switch (point.X)
        {
            case 1:
                selected = identity(42);
                break;
            case 2:
                selected = 0;
                break;
            default:
                selected = -1;
                break;
        }
        await Task.Yield();
        if (selected != 42 || box is not { Value: null } || !raw.StartsWith("case", StringComparison.Ordinal) || interpolated != "receive 42")
            throw new InvalidOperationException("Ordinary C# semantics changed by preprocessing");
    }
}
