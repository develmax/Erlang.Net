using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.BinaryArena598Names;

namespace Erlang.Tests;

internal sealed class BinaryArena598Fixture
{
    internal ProcessRuntime Runtime { get; } = OwnedGuest506RuntimeFactory.Create();
    internal RuntimeNodeFoundationHostActor? Actor { get; private set; }
    internal object HeapOwner { get; private set; } = null!;
    internal object Program { get; private set; } = null!;
    internal object BackingOwner { get; private set; } = null!;
    internal ResourceReferenceTerm Reference { get; private set; } = null!;
    private readonly List<IDisposable> frames = new();
    private readonly List<Action> restores = new();
    private bool released;

    internal object Frame()
    {
        frames.EnsureCapacity(checked(frames.Count + 1));
        var frame = (IDisposable)Call(HeapOwner, Enter)!;
        frames.Add(frame);

        return frame;
    }

    internal async Task Initialize(Term input)
    {
        Actor = new RuntimeNodeFoundationHostActor(Runtime);
        ProcessContext context = await Actor.Ready.WaitAsync(Deadline);
        HeapOwner = Property(context, Heap);
        Program = await Actor.InvokeAsync(async active =>
        {
            object valueTask = typeof(ProcessRuntime).Assembly.GetType(BinaryArena598Names.Compiler, true)!
                .GetMethod(Compile, StaticMembers)!.Invoke(null, [active, input])!;
            Task task = (Task)valueTask.GetType().GetMethod(nameof(ValueTask.AsTask))!.Invoke(valueTask, null)!;
            await task;

            return task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task)!;
        });
        BackingOwner = Property(Program, Backing);
        Reference = (ResourceReferenceTerm)Call(Runtime.ReferenceResources, Create, Program)!;
    }

    internal void Restore(Action action) => restores.Add(action);

    internal void Release()
    {
        if (released || BackingOwner is null) return;
        released = true;
        Call(BackingOwner, ReleaseProducer);
    }

    internal static async Task Run(Term input, Func<BinaryArena598Fixture, Task> body)
    {
        var fixture = new BinaryArena598Fixture();
        Exception? original = null;
        var errors = new List<Exception>();
        try
        {
            await fixture.Initialize(input).WaitAsync(Deadline);
            await body(fixture).WaitAsync(Deadline);
        }
        catch (Exception error)
        {
            original = error;
        }
        foreach (var restore in fixture.restores.AsEnumerable().Reverse())
            try
            {
                restore();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        foreach (var frame in fixture.frames.AsEnumerable().Reverse())
            try
            {
                frame.Dispose();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        try
        {
            fixture.Release();
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        try
        {
            if (fixture.Actor is not null) await fixture.Actor.DisposeAsync().AsTask().WaitAsync(Deadline);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        try
        {
            await fixture.Runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        if (original is not null && errors.Count == 0) ExceptionDispatchInfo.Capture(original).Throw();
        if (original is not null) errors.Insert(0, original);
        if (errors.Count != 0) throw new AggregateException(errors);
    }
}
