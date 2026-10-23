namespace Erlang;

internal static class ErlangFunctionInfoNames
{
    public const string Info = "fun_info";
    public const string Mfa = "fun_info_mfa";
    public const string ToList = "fun_to_list";
    public const string Type = "type";
    public const string Environment = "env";
    public const string Arity = "arity";
    public const string Name = "name";
    public const string OldUnique = "uniq";
    public const string Index = "index";
    public const string NewUnique = "new_uniq";
    public const string NewIndex = "new_index";
    public const string Module = "module";
    public const string Pid = "pid";
    public const string References = "refc";
    public const string Local = "local";
    public const string External = "external";
    public const string Undefined = "undefined";

    public static IReadOnlyList<string> Keys { get; } = Array.AsReadOnly(new[] { Type, Environment, Arity, Name, OldUnique, Index, NewUnique, NewIndex, Module, Pid });
}
