using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.EmptyTuple803Names;

namespace Erlang.Tests;

public static class EmptyTuple803Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static async Task PhysicalHeaderAndLifetime()
    {
        ProcessRuntime runtime = OwnedGuest506RuntimeFactory.Create();
        RuntimeNodeFoundationHostActor? first = null, second = null;
        IDisposable? roots = null, otherRoots = null, read = null, held = null;
        Task? closing = null;
        try
        {
            await runtime.InitializeModulesAsync(_ => { });
            object domain = Get(runtime, Domain);
            var canonical = (TupleTerm)Get(domain, Value);
            object storage = Get(canonical, Storage);
            object cells = Field(domain, Cells);
            Require((int)Get(cells, HeaderCount) == 1 && (int)Get(cells, TermCount) == 0);
            Require((int)Get(cells, NativeLength) == 2 && (int)Get(cells, PadCount) == 1);
            Require((nuint)Call(cells, ReadPad, 1)! == 0);
            Require((nuint)Call(cells, ReadHeader, 0)! == 0 && canonical.Items.Count == 0);
            PreparedBeamImage image = runtime.Modules.PrepareBeam(EmptyTuple803Bytes.Image());
            Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
            object literals = Get(image, LiteralDomain);
            var map = (MapTerm)((IReadOnlyList<Term>)Get(literals, Literals))[0];
            Require(ReferenceEquals(Get(map, FlatKeys), canonical));
            object templates = Get(Get(image, Code), Templates);
            var actualTemplates = ((IEnumerable)Get(templates, TemplateValues)).Cast<TupleTerm>().ToArray();
            // This literal move has no generated template. The real hidden
            // empty-map key edge above is the actual native-storage witness.
            Require(actualTemplates.Length == 0);

            // Under the exact intrinsic metadata gate no concurrent reader can
            // enter; require no reader was already active. Corruption is local
            // to this controlled VM and restored before releasing the gate.
            lock (Field(domain, DomainGate))
            {
                Require((int)Field(domain, ReaderCount) == 0);
                Call(
                    cells,
                    WriteHeaders,
                    new nuint[] { 1 },
                    0
                );
                try
                {
                    bool rejected = false;
                    try
                    {
                        _ = ((TupleTerm)Get(map, FlatKeys)).Items.Count;
                    }
                    catch (InvalidOperationException error) when (error.Message == InvalidHeader)
                    {
                        rejected = true;
                    }
                    Require(rejected);
                }
                finally
                {
                    Call(
                        cells,
                        WriteHeaders,
                        new nuint[] { 0 },
                        0
                    );
                }
            }
            first = new(runtime);
            second = new(runtime);
            TupleTerm? ordinary = null;
            await first.InvokeAsync(async context =>
            {
                await context.ExecutionCheckpoint();
                object heap = Get(context, Heap);
                roots = (IDisposable)Call(heap, Enter)!;
                Term constructed = (Term)Call(
                    roots,
                    EmptyTuple803Names.Tuple,
                    Call(roots, Reserve)!,
                    Array.Empty<Term>()
                )!;
                Require(ReferenceEquals(constructed, canonical) && ReferenceEquals(Get(constructed, Storage), storage));
                Term escaped = (Term)Call(
                    heap,
                    Export,
                    roots,
                    constructed
                )!;
                Require(escaped is TupleTerm { Items.Count: 0 } && GetNullable(escaped, Storage) is null);

                return Nil.Value;
            });
            await second.InvokeAsync(context =>
            {
                object heap = Get(context, Heap);
                otherRoots = (IDisposable)Call(heap, Enter)!;
                Term constructed = (Term)Call(
                    otherRoots,
                    EmptyTuple803Names.Tuple,
                    Call(otherRoots, Reserve)!,
                    Array.Empty<Term>()
                )!;
                Require(ReferenceEquals(constructed, canonical));
                ordinary = (TupleTerm)Call(
                    otherRoots,
                    EmptyTuple803Names.Tuple,
                    Call(otherRoots, Reserve)!,
                    new Term[] { Term.A(ForeignAtom) }
                )!;

                return Task.FromResult<Term>(Nil.Value);
            });
            await first.InvokeAsync(context =>
            {
                RejectForeign(() => Call(
                    roots!,
                    SetLocal,
                    Call(roots!, Reserve)!,
                    ordinary!
                ));

                return Task.FromResult<Term>(Nil.Value);
            });
            await using (ProcessRuntime foreignRuntime = OwnedGuest506RuntimeFactory.Create())
            {
                await foreignRuntime.InitializeModulesAsync(_ => { });
                TupleTerm alien = (TupleTerm)Get(Get(foreignRuntime, Domain), Value);
                await first.InvokeAsync(context =>
                {
                    RejectForeign(() => Call(
                        roots!,
                        SetLocal,
                        Call(roots!, Reserve)!,
                        alien
                    ));

                    return Task.FromResult<Term>(Nil.Value);
                });
            }
            // Execute the actual prepared literal entry; its result must still
            // escape as a portable Host value.
            Term result = await first.InvokeAsync(async context => await runtime.Modules.Call(context, image.Name, Run));
            Require(result is MapTerm { Entries.Count: 0 } && GetNullable(Get(result, FlatKeys), Storage) is null);

            read = (IDisposable)Call(domain, EnterRead, storage)!;
            object actualCarrier = ((IEnumerable)Get(roots!, FrameAuthorities)).Cast<object>().Single();
            held = (IDisposable)Call(actualCarrier, RetainAuthority)!;
            roots!.Dispose();
            roots = null;
            otherRoots!.Dispose();
            otherRoots = null;
            await first.DisposeAsync();
            first = null;
            await second.DisposeAsync();
            second = null;
            closing = runtime.DisposeAsync().AsTask();
            var deadline = Stopwatch.StartNew();
            while ((bool)Field(domain, OwnerOpen))
            {
                if (closing.IsCompleted) await closing;
                Require(deadline.Elapsed < TimeSpan.FromSeconds(DeadlineSeconds));
                await Task.Delay(1);
            }
            Require(!closing.IsCompleted && !(bool)Get(cells, Disposed));
            Require((int)Get(Field(domain, Holders), CollectionCount) == 1);
            held.Dispose();
            held = null;
            Require((int)Get(Field(domain, Holders), CollectionCount) == 0 && (int)Field(domain, ReaderCount) == 1);
            Require(!closing.IsCompleted && !(bool)Get(cells, Disposed));
            Require((nuint)Call(cells, ReadHeader, 0)! == 0);
            read.Dispose();
            read = null;
            await closing.WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
            Require((bool)Get(cells, Disposed));
            bool stale = false;
            try
            {
                _ = canonical.Items.Count;
            }
            catch (ObjectDisposedException)
            {
                stale = true;
            }
            Require(stale);
        }
        finally
        {
            held?.Dispose();
            read?.Dispose();
            roots?.Dispose();
            otherRoots?.Dispose();
            if (first is not null) await first.DisposeAsync();
            if (second is not null) await second.DisposeAsync();
            if (closing is not null) await closing;
            else await runtime.DisposeAsync();
        }
    }

    private static object Get(object target, string name) => GetNullable(target, name)!;

    private static object? GetNullable(object target, string name) => target.GetType().GetProperty(name, Members)!.GetValue(target);

    private static object Field(object target, string name) => target.GetType().GetField(name, Members)!.GetValue(target)!;

    private static object? Call(object target, string name, params object[] arguments)
    {
        try
        {
            return target.GetType().GetMethods(Members).Single(method => method.Name == name && method.GetParameters().Length == arguments.Length).Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static void RejectForeign(Action action)
    {
        bool rejected = false;
        try
        {
            action();
        }
        catch (InvalidOperationException error) when (error.Message == Foreign)
        {
            rejected = true;
        }
        Require(rejected);
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }
}
