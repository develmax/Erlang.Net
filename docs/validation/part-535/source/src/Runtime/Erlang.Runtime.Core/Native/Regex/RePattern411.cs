namespace Erlang;

// Identity adapter only. Actual process headers/Host carriers own backing.
internal sealed class RePattern411(RuntimeRegexEngine411 owner, ReCode411 code)
{
    internal RuntimeRegexEngine411 Owner { get; } = owner;
    internal ReCode411 Code { get; } = code;
    internal ReMagicBacking597 Backing597 => Code.Backing597;
}
