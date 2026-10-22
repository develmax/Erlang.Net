namespace Erlang.Compiler;

internal sealed class CompiledComprehensionFrames(
    ComprehensionSource source,
    ModuleDefinition module,
    ProcessContext context,
    bool tail
)
{
    private readonly List<ComprehensionLogicalFrame> pending = [];
    private int currentLine;
    private int currentGenerator;
    private long emittedCount;
    private readonly Dictionary<int, ComprehensionGeneratorFrames> generators = [];
    private readonly IReadOnlyDictionary<int, GeneratedComprehensionFunction> functions = source.Functions.ToDictionary(value => value.QualifierIndex);

    internal bool TerminalBody => source.TerminalBody && !SourceReturnSafety.HasNativeOverrides(module, context);

    internal async ValueTask<Term> RunTerminal(Func<ValueTask<Term>> operation)
    {
        using var sourceLocation = context.EnterSourceLocation(SourceCallLocations.For(module, source.Line));

        return await operation();
    }

    internal void EnterGenerator(int index)
    {
        if (!functions.TryGetValue(index, out var function))
        {
            return;
        }
        currentLine = function.SourceLine;
        if (pending.Count == 0)
        {
            pending.Add(new(function, function.EntryContinuation));
        }
        else
        {
            pending[^1] = new(function, pending[^1].Continuation);
        }
        currentGenerator = index;
        generators[index] = new(pending.ToArray(), emittedCount);
    }

    internal void Resume(int index)
    {
        if (!generators.TryGetValue(index, out var saved))
        {
            return;
        }
        pending.Clear();
        pending.AddRange(saved.Frames);
        currentGenerator = index;
        currentLine = functions[index].SourceLine;
        if (saved.EmittedCount != emittedCount)
        {
            AddBodyContinuation();
        }
        Save();
    }

    internal void SetLocation(int line)
    {
        currentLine = line;
    }

    internal void Emitted()
    {
        if (pending.Count == 0)
        {
            return;
        }
        emittedCount++;
        AddBodyContinuation();
        Save();
    }

    private void AddBodyContinuation()
    {
        if (pending.Count == 0)
        {
            return;
        }
        var active = pending[^1];
        if (active.Function.DiscardConsContinuation)
        {
            return;
        }
        object continuation = active.Function.RecursiveContinuation;
        if (pending.Count < 2 || !ReferenceEquals(pending[^2].Continuation, continuation))
        {
            pending.Insert(pending.Count - 1, new(active.Function, continuation));
            if (pending.Count > BacktraceDepthParameters.MaximumDepth)
            {
                pending.RemoveAt(0);
            }
        }
    }

    private void Save()
    {
        generators[currentGenerator] = new(pending.ToArray(), emittedCount);
    }

    internal IDisposable Materialize()
    {
        var scopes = new List<IDisposable>(pending.Count + 1);
        if (context.EnterSourceLocation(SourceCallLocations.For(module, source.Line)) is { } ownerLocation)
        {
            scopes.Add(ownerLocation);
        }
        for (int index = 0; index < pending.Count; index++)
        {
            var frame = pending[index];
            scopes.Add(context.EnterGeneratedFunction(
                module.Name,
                frame.Function.Function,
                frame.Function.Arity,
                SourceCallLocations.For(
                    module,
                    index == pending.Count - 1 && currentLine > 0
                    ? currentLine : frame.Function.SourceLine > 0 ? frame.Function.SourceLine : source.Line
                ),
                frame.Continuation,
                index == 0 && tail
            ));
        }

        return new ComprehensionFrameScope(scopes);
    }

    internal async ValueTask<T> Run<T>(Func<ValueTask<T>> operation)
    {
        using var scope = Materialize();

        return await operation();
    }

    internal async ValueTask Reduce()
    {
        using var scope = Materialize();
        await context.ReduceAsync();
    }

    internal ErlangException Capture(ErlangException error)
    {
        using var scope = Materialize();

        return context.CaptureSourceFailure(error);
    }
}
