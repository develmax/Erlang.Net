namespace Erlang;

// Internal immutable capability. Default production constructors pass null.
// It cannot complete Ready/Started/Exited, mutate workers, or replace Thread.Start.
internal sealed class RuntimeWorkerCreationPolicy
{
    private readonly Action<string, int>? beforeAccounting;
    private readonly Action<string, int>? beforeStart;
    private readonly Action<string, int>? enteredBeforeReady;
    private readonly Action<string, object>? constructorCheckpoint;

    internal RuntimeWorkerCreationPolicy(Action<string, int>? beforeStart,
        Action<string, int>? enteredBeforeReady, Action<string, object>? constructorCheckpoint)
        : this(
            beforeStart,
            enteredBeforeReady,
            constructorCheckpoint,
            null
        )
    {
    }

    internal RuntimeWorkerCreationPolicy(
        Action<string, int>? beforeStart,
        Action<string, int>? enteredBeforeReady,
        Action<string, object>? constructorCheckpoint,
        Action<string, int>? beforeAccounting
    )
    {
        this.beforeAccounting = beforeAccounting;
        this.beforeStart = beforeStart;
        this.enteredBeforeReady = enteredBeforeReady;
        this.constructorCheckpoint = constructorCheckpoint;
    }

    internal void BeforeAccounting(string kind, int identity) => beforeAccounting?.Invoke(kind, identity);

    internal void BeforeStart(string kind, int identity) => beforeStart?.Invoke(kind, identity);

    internal void EnteredBeforeReady(string kind, int identity) => enteredBeforeReady?.Invoke(kind, identity);

    internal void ConstructorCheckpoint(string phase, object owner) => constructorCheckpoint?.Invoke(phase, owner);
}
