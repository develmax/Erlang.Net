namespace Erlang;

internal sealed class ReNativeBinding413
{
    internal RuntimeRegexEngine411 Engine { get; }
    internal ErlangFunction Body { get; }
    private readonly (string Function, int Arity) signature;

    internal ReNativeBinding413(RuntimeRegexEngine411 engine, (string Function, int Arity) signature)
    {
        Engine = engine;
        this.signature = signature;
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
        => Engine.Invoke(context, signature, arguments);
}
