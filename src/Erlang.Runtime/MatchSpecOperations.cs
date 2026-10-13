namespace Erlang;

public static class MatchSpecOperations
{
    public const string And = "and";
    public const string Or = "or";
    public const string AndAlso = "andalso";
    public const string OrElse = "orelse";
    public const string AndThen = "andthen";
    public const string ControlWordGet = "get_tcw";
    public const string ControlWordSet = "set_tcw";
    public const string IsSequentialTrace = "is_seq_trace";
    public const string Display = "display";
    public static IReadOnlySet<(string Name, int Arity)> Pure { get; } = new HashSet<(string, int)>
    {
        ("is_atom", 1), ("is_float", 1), ("is_integer", 1), ("is_integer", 3),
        ("is_list", 1), ("is_number", 1), ("is_pid", 1), ("is_port", 1),
        ("is_reference", 1), ("is_tuple", 1), ("is_map", 1), ("is_binary", 1),
        ("is_bitstring", 1), ("is_boolean", 1), ("is_function", 1), ("is_function", 2),
        ("is_record", 3), ("abs", 1), ("element", 2), ("hd", 1), ("length", 1),
        ("max", 2), ("min", 2), ("node", 0), ("node", 1), ("round", 1),
        ("size", 1), ("map_size", 1), ("map_get", 2), ("is_map_key", 2),
        ("bit_size", 1), ("byte_size", 1), ("tuple_size", 1), ("binary_part", 2),
        ("binary_part", 3), ("tl", 1), ("trunc", 1), ("float", 1), ("ceil", 1), ("floor", 1)
    };

    public static IReadOnlySet<string> Unary { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "+", "-", "bnot", "not"
    };

    public static IReadOnlySet<string> Binary { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "+", "-", "*", "/", "div", "rem", "band", "bor", "bxor", "bsl", "bsr",
        ">", ">=", "<", "=<", "==", "=:=", "/=", "=/=", "xor"
    };

    public static IReadOnlyDictionary<string, IReadOnlySet<int>> Trace { get; } = new Dictionary<string, IReadOnlySet<int>>(StringComparer.Ordinal)
    {
        [MatchSpecNames.Message] = new HashSet<int> { 1 },
        [MatchSpecNames.ReturnTrace] = new HashSet<int> { 0 },
        [MatchSpecNames.ExceptionTrace] = new HashSet<int> { 0 },
        [MatchSpecNames.Caller] = new HashSet<int> { 0 },
        ["caller_line"] = new HashSet<int> { 0 },
        ["current_stacktrace"] = new HashSet<int> { 0, 1 },
        ["is_seq_trace"] = new HashSet<int> { 0 },
        ["get_seq_token"] = new HashSet<int> { 0 },
        ["set_seq_token"] = new HashSet<int> { 2 },
        ["silent"] = new HashSet<int> { 1 },
        ["display"] = new HashSet<int> { 1 },
        ["process_dump"] = new HashSet<int> { 0 },
        ["enable_trace"] = new HashSet<int> { 1, 2 },
        ["disable_trace"] = new HashSet<int> { 1, 2 },
        ["trace"] = new HashSet<int> { 2, 3 },
        ["get_tcw"] = new HashSet<int> { 0 },
        ["set_tcw"] = new HashSet<int> { 1 }
    };

    public static bool IsBoolean(string name) => name is And or Or or AndAlso or OrElse or AndThen;

    public static bool IsPure(string name, int arity) => Pure.Contains((name, arity)) || arity == 1 && Unary.Contains(name) || arity == 2 && Binary.Contains(name);

    public static bool TestSupports(string name) => name is MatchSpecNames.Message or MatchSpecNames.ReturnTrace or MatchSpecNames.ExceptionTrace or MatchSpecNames.Caller;
}
