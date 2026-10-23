namespace Erlang;

public static class ErlangBasicBifNames
{
    public const string Absolute = "abs";
    public const string Float = "float";
    public const string Round = "round";
    public const string Truncate = "trunc";
    public const string Floor = "floor";
    public const string Ceiling = "ceil";
    public const string Minimum = "min";
    public const string Maximum = "max";
    public const string IsBoolean = "is_boolean";
    public const string IsFunction = "is_function";
    public const string IsPort = "is_port";
    public const string IsReference = "is_reference";
    public const string Size = "size";
    public const string SetElement = "setelement";
    public const string AppendElement = "append_element";
    public const string InsertElement = "insert_element";
    public const string DeleteElement = "delete_element";
    public const string MakeTuple = "make_tuple";
    public const string TupleToList = "tuple_to_list";
    public const string ListToTuple = "list_to_tuple";
    public const string Append = "append";
    public const string Subtract = "subtract";
    public const string Apply = "apply";
    public const string GetKeys = "get_keys";
    public const string Send = "send";

    public static IReadOnlyList<(string, int)> Guards { get; } =
    [
        (Absolute, 1), (Float, 1), (Round, 1), (Truncate, 1), (Floor, 1), (Ceiling, 1),
        (Minimum, 2), (Maximum, 2), (IsBoolean, 1), (IsFunction, 1), (IsFunction, 2),
        (IsPort, 1), (IsReference, 1), (Size, 1), (ErlangModuleNames.IsInteger, 3)
    ];
}
