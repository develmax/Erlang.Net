namespace Erlang;

internal sealed class RePattern411(RuntimeRegexEngine411 owner, ReCode411 code)
{
    internal RuntimeRegexEngine411 Owner { get; } = owner;
    internal ReCode411 Code { get; } = code;

    ~RePattern411() => Owner.ReleaseGuest(Code);
}
