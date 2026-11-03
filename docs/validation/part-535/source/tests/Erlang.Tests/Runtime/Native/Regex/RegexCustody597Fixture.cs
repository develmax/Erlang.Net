using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.RegexCustody597Names;

namespace Erlang.Tests;

internal sealed class RegexCustody597Fixture
{
    internal ProcessRuntime Runtime { get; } = OwnedGuest506RuntimeFactory.Create();
    internal RuntimeRegexEngine411? Engine { get; private set; }
    internal RuntimeNodeFoundationHostActor? Actor { get; private set; }
    internal ProcessContext Context { get; private set; } = null!;
    internal object Heap { get; private set; } = null!;
    internal object Code { get; private set; } = null!;
    internal ResourceReferenceTerm Reference { get; private set; } = null!;
    internal object? Backing => Code?.GetType().GetProperty(RegexCustody597Names.Backing, Members)?.GetValue(Code);
    private readonly List<IDisposable> frames = new();
    private readonly List<Action> restores = new();
    private bool producerReleased;

    internal async Task Initialize(bool originalRe = false)
    {
        Engine = await RuntimeNativePrimitiveProviders.RegisterRegexAsync411(Runtime, Runtime.Modules, RegexNative411Access.Admission());
        if (originalRe)
            await Runtime.InitializeModulesAsync(registry =>
            {
                Erlang.Otp.OtpModules.Register(registry);
                var dependencies = new BeamDependencySelection(RegexThreeOriginal413Names.OriginalModules
                    .Concat([RegexThreeOriginal413Names.Erlang]));
                PreparedBeamImage[] images = RegexThreeOriginal413Names.OriginalModules.Select(module => registry.PrepareBeam(
                    RegexThreeOriginal413Access.Read(module + LibraryDependencyOriginalNames.Beam), dependencies
                )).ToArray();
                if (images.Single(image => image.Name == RegexThreeOriginal413Names.Module).NativePrimitiveExports.Count != RegexThreeOriginal413Names.NativeBodies
                    || registry.PublishPreparedBeam(images).Status != BeamPublicationStatus.Published)
                    throw new InvalidOperationException(RegexThreeOriginal413Names.Failure);
                foreach (PreparedBeamImage image in images)
                    foreach (var export in registry.Exports.Where(export => export.Module == image.Name))
                        if (!ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image))
                            throw new InvalidOperationException(RegexThreeOriginal413Names.Failure);
            });
        Actor = new RuntimeNodeFoundationHostActor(Runtime);
        Context = await Actor.Ready.WaitAsync(Deadline);
        Heap = Property(Context, RegexCustody597Names.Heap);
        object compiled = Call(
            Engine,
            CompileCode,
            System.Text.Encoding.ASCII.GetBytes(Word),
            false
        )!;
        Code = compiled.GetType().GetField(Item1)!.GetValue(compiled)!;
        object payload = Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(PatternType, true)!,
            Members,
            null,
            [Engine, Code],
            null
        )!;
        Reference = (ResourceReferenceTerm)Call(Runtime.ReferenceResources, CreateReference, payload)!;
    }

    internal object Frame()
    {
        frames.EnsureCapacity(checked(frames.Count + 1));
        var frame = (IDisposable)Call(Heap, Enter)!;
        frames.Add(frame);

        return frame;
    }

    internal void Restore(Action action) => restores.Add(action);

    internal void Release()
    {
        if (!producerReleased && Engine is not null && Code is not null)
        {
            producerReleased = true;
            Call(Engine, ReleaseProducer, Code);
        }
    }

    internal static async Task Run(Func<RegexCustody597Fixture, Task> body, bool originalRe = false)
    {
        var fixture = new RegexCustody597Fixture();
        Exception? original = null;
        var errors = new List<Exception>();
        try
        {
            await fixture.Initialize(originalRe).WaitAsync(Deadline);
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
        try
        {
            if (fixture.Engine is not null) RegexNative411Access.Empty(fixture.Engine);
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
