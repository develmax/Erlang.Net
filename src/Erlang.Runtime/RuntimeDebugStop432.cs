namespace Erlang;

public sealed class RuntimeDebugStop432
{
    private readonly RuntimeDebugController432 owner;
    private IReadOnlyList<Term> arguments;
    private ModuleCodeImage? image;
    private readonly TaskCompletionSource<bool> resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource settled = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal RuntimeDebugStop432(
        RuntimeDebugController432 owner,
        Pid process,
        ModuleCodeImage image,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        this.owner = owner;
        Process = process;
        this.image = image;
        Function = function;
        Arity = arguments.Count;
        this.arguments = Array.AsReadOnly(arguments.ToArray());
    }

    public Pid Process { get; }
    public string Function { get; }
    public int Arity { get; }
    public ModuleCodeImage? Image => Volatile.Read(ref image);
    public IReadOnlyList<Term> Arguments => Volatile.Read(ref arguments);
    public bool IsSettled => settled.Task.IsCompleted;

    public void Resume() => owner.Resume(this);

    internal RuntimeDebugController432 Owner => owner;
    internal RuntimeDebugStop432? ScopeNext { get; set; }

    internal void ClearArguments() => Volatile.Write(ref arguments, Array.Empty<Term>());

    internal Task<bool> ResumeTask => resume.Task;
    internal Task Settled => settled.Task;
    internal LinkedListNode<RuntimeDebugStop432>? QueueNode { get; set; }

    internal void RequestResume() => resume.TrySetResult(true);

    internal void RequestClose() => resume.TrySetResult(false);

    internal void Complete()
    {
        Volatile.Write(ref arguments, Array.Empty<Term>());
        Volatile.Write(ref image, null);
        settled.TrySetResult();
    }
}
