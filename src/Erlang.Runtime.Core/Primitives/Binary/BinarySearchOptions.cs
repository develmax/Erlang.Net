namespace Erlang;

internal readonly record struct BinarySearchOptions(
    int Start,
    int End,
    bool Global,
    bool Trim,
    bool TrimAll
);
