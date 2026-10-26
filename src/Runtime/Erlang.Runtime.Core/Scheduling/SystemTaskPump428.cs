namespace Erlang;

internal sealed class SystemTaskPump428(Func<ValueTask<Term>> body)
{
    internal Func<ValueTask<Term>> Body { get; } = body;
}
