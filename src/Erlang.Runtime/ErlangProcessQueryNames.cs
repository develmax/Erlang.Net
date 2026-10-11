namespace Erlang;

public static class ErlangProcessQueryNames
{
    public const string Node = "node";
    public const string Processes = "processes";
    public const string Registered = "registered";
    public const string Alive = "is_process_alive";
    public const string Yield = "yield";
    public const string Bump = "bump_reductions";
    public static IReadOnlyList<(string Function, int Arity)> Guards { get; } = [(Node, 0), (Node, 1)];
}
