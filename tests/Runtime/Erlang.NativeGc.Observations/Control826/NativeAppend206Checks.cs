using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Compiler;
using Erlang.Otp;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;
using static Erlang.Tests.NativeAppend206Names;

namespace Erlang.Tests;

internal static class NativeAppend206Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    internal static async Task Run()
    {
        foreach (bool owned in new[] { false, true })
        {
            foreach (bool original in new[] { false, true }) await One(owned, original);
            await OpaqueOverride(owned);
        }
    }

    private static async Task One(bool owned, bool original)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
        if (original)
        {
            await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(runtime, registry));
            await PreparedBeamFixtureScope171.RunAsync(
                runtime,
                (registry, scope) =>
            {
                byte[] catalog = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, OriginalFolder, DependencyCatalogFile));
                Require(Convert.ToHexString(SHA256.HashData(catalog)) == DependencyCatalogSha256);
                using JsonDocument declared = JsonDocument.Parse(catalog);
                var dependencies = new BeamDependencySelection(Dependencies.Concat(
                    declared.RootElement.GetProperty(DeclaredModules).EnumerateArray().Select(module => module.GetString()!)
                ).Distinct().ToArray());
                PreparedBeamImage erlang = scope.Prepare(() => registry.PrepareBeam(OtpErts.ReadImage(ErtsModule), dependencies));
                PreparedBeamImage lists = scope.Prepare(() => registry.PrepareBeam(
                    File.ReadAllBytes(Path.Combine(
                        AppContext.BaseDirectory,
                        OriginalFolder,
                        LibraryFolder,
                        ListsImage
                    )), dependencies));
                PreparedBeamImage debug = scope.Prepare(() => registry.PrepareBeam(
                    File.ReadAllBytes(Path.Combine(
                        AppContext.BaseDirectory,
                        OriginalFolder,
                        LibraryFolder,
                        DebugImage
                    )), dependencies));
                PreparedBeamImage observer = scope.Prepare(() => registry.PrepareBeam(Resource(ObserverImage), dependencies));
                Require(erlang.NativePrimitiveExports.Any(entry => entry.Name == Append && entry.Arity == Arity)
                    && erlang.NativePrimitiveExports.Any(entry => entry.Name == Operator && entry.Arity == Arity));

                return ValueTask.FromResult<Action<ModuleRegistry>>(target =>
                {
                    Require(target.PublishPreparedBeam([erlang, lists, debug, observer]).Status == BeamPublicationStatus.Published);
                    Require(ReferenceEquals(target.SourceOwner(ErtsModule, Append, Arity), erlang)
                        && ReferenceEquals(target.SourceOwner(ErtsModule, Operator, Arity), erlang)
                        && ReferenceEquals(target.SourceOwner(DebugModule, Same, Arity), debug));
                });
            }
            );
        }
        if (original) await CompareOriginal(runtime, owned);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var foreignRuntime = OwnedGuest506RuntimeFactory.Create(owned);
        await using var foreignActor = new RuntimeNodeFoundationHostActor(foreignRuntime);
        Term? foreign = null;
        IDisposable? foreignFrame = null;
        await foreignActor.InvokeAsync(context =>
        {
            foreignFrame = (IDisposable)Call(Property(context, Heap), Enter)!;
            foreign = Own(foreignFrame, Term.List(new FloatTerm(1.5)));

            return Task.FromResult(true);
        });
        var provider = Provider(runtime);
        int destroyed = 0;
        Destructor destructor = (_, _) => Interlocked.Increment(ref destroyed);
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
        Marshal.WriteByte((nint)Property(payload, SuspensionRootLease821Names.Pointer), 0, Sentinel);
        BitString resource = Binary(runtime, payload);
        Task? imageRetirement = null;
        bool released = false;
        try
        {
            await actor.InvokeAsync(async context =>
            {
                object heap = Property(context, Heap);
                Term stale;
                using (var frame = (IDisposable)Call(heap, Enter)!)
                {
                    Term binary = Own(frame, new BitString(new byte[OrdinaryBytes]));
                    Term native = Own(frame, resource);
                    Term number = Own(frame, new FloatTerm(1.5));
                    Term integer = Own(frame, new Integer(BigInteger.One << IntegerShift));
                    Term left = Own(frame, Term.List(
                        binary,
                        native,
                        number,
                        integer
                    ));
                    Term right = Own(frame, Term.List(Term.A(Tail), binary));
                    Term[] expectedHeads = Cons.Items(left).ToArray();
                    var paths = new List<(string Module, string Function)> { (ErtsModule, Operator), (ErtsModule, Append) };
                    if (original) paths.Add((ListsModule, Append));
                    foreach (var path in paths)
                    {
                        Require(ReferenceEquals(await Store(
                            context,
                            path.Module,
                            path.Function,
                            Nil.Value,
                            right
                        ), right));
                        Require(ReferenceEquals(await Store(
                            context,
                            path.Module,
                            path.Function,
                            left,
                            Nil.Value
                        ), left));
                        Term result = await Store(
                            context,
                            path.Module,
                            path.Function,
                            left,
                            right
                        );
                        Require(!ReferenceEquals(result, left));
                        Term cursor = result;
                        foreach (Term head in expectedHeads)
                        {
                            Require(cursor is Cons && StorageOf(heap, cursor) is not null);
                            var cell = (Cons)cursor;
                            Require(ReferenceEquals(cell.Head, head));
                            cursor = cell.Tail;
                        }
                        Require(ReferenceEquals(cursor, right));
                        Term atomTail = Term.A(Tail);
                        cursor = await Store(
                            context,
                            path.Module,
                            path.Function,
                            left,
                            atomTail
                        );
                        foreach (Term head in expectedHeads)
                        {
                            Require(cursor is Cons cell && ReferenceEquals(cell.Head, head));
                            cursor = ((Cons)cursor).Tail;
                        }
                        Require(ReferenceEquals(cursor, atomTail));
                        if (path.Module == ErtsModule) await BadArguments(context, path.Function);
                    }
                    await SynchronousRoutes(
                        context,
                        heap,
                        frame,
                        left,
                        right,
                        expectedHeads
                    );
                    bool rejectedForeign = false;
                    try
                    {
                        await Store(
                            context,
                            ErtsModule,
                            Operator,
                            foreign!,
                            Nil.Value
                        );
                    }
                    catch (InvalidOperationException error) when (error.Message == Foreign)
                    {
                        rejectedForeign = true;
                    }
                    Require(rejectedForeign);
                    Call(payload, SuspensionRootLease821Names.ReleaseNative);
                    released = true;
                    imageRetirement = (Task)Call(provider.Bindings, SuspensionRootLease821Names.RetireImage)!;
                    Call(
                        heap,
                        Collect,
                        true,
                        Major
                    );
                    Require(destroyed == 0 && !imageRetirement.IsCompleted && ((BitString)expectedHeads[1]).ToArray()[0] == Sentinel);
                    stale = Own(frame, Term.List(Term.I(1)));
                    await EraseResult(context);
                }
                Call(
                    heap,
                    Collect,
                    true,
                    Major
                );
                bool rejected = false;
                try
                {
                    await Store(
                        context,
                        ErtsModule,
                        Operator,
                        stale,
                        Nil.Value
                    );
                }
                catch (ObjectDisposedException)
                {
                    rejected = true;
                }
                Require(rejected);

                return true;
            });
            await ((Task)Property(payload, SuspensionRootLease821Names.Retired)).WaitAsync(Deadline);
            await imageRetirement!.WaitAsync(Deadline);
            Require(destroyed == 1 && provider.Owner.Disposed);
        }
        finally
        {
            if (!released) Call(payload, SuspensionRootLease821Names.ReleaseNative);
            await runtime.DisposeAsync();
            await ((Task)Property(payload, SuspensionRootLease821Names.Retired)).WaitAsync(Deadline);
            imageRetirement ??= (Task)Call(provider.Bindings, SuspensionRootLease821Names.RetireImage)!;
            await imageRetirement.WaitAsync(Deadline);
            foreignFrame?.Dispose();
            GC.KeepAlive(resource);
            GC.KeepAlive(destructor);
        }
    }

    private static async Task CompareOriginal(ProcessRuntime runtime, bool owned)
    {
        Require(Convert.ToHexString(SHA256.HashData(Resource(ObserverImage))) == ObserverHash
            && Convert.ToHexString(SHA256.HashData(Resource(OriginalValues))) == OriginalHash);
        await using var parent = new RuntimeNodeFoundationHostActor(runtime);
        await parent.InvokeAsync(async context =>
        {
            var monitor = (TupleTerm)await Execution.EvaluateAsync(
                new Expr.Call(
                    ErtsModule,
                    SpawnMonitor,
                    [
                new Expr.Literal(Term.A(Observer)), new Expr.Literal(Term.A(Worker)),
                new Expr.Literal(Term.List(context.Self))
            ]
                ),
                context
            );
            TupleTerm? message = await context.ReceiveAsync<TupleTerm>(
                candidate => candidate is TupleTerm tuple
                && (tuple.Items.Count == 2 && tuple.Items[0].Equals(Term.A(Finished))
                    || tuple.Items.Count == 5 && tuple.Items[0].Equals(Term.A(Down))
                    && tuple.Items[1].Equals(monitor.Items[1]) && tuple.Items[3].Equals(monitor.Items[0])) ? tuple : null,
                Deadline
            );
            Require(message is not null);
            if (message!.Items.Count != 2)
                throw new InvalidOperationException(WorkerFailure + message.Items[4]);
            Term outcome = message.Items[1];
            TupleTerm? down = await context.ReceiveAsync<TupleTerm>(
                candidate => candidate is TupleTerm { Items.Count: 5 } tuple
                && tuple.Items[0].Equals(Term.A(Down)) && tuple.Items[1].Equals(monitor.Items[1])
                && tuple.Items[3].Equals(monitor.Items[0]) ? tuple : null,
                Deadline
            );
            Require(down is not null && down.Items[4].Equals(Term.A(Normal)) && Cons.Items(outcome).Count() == ExpectedRows);
            byte[] raw = ExternalTermFormat.Encode(outcome);
            byte[] deterministic = ExternalTermFormat.Encode(outcome, new EtfEncodingOptions { Deterministic = true });
            string? prefix = Environment.GetEnvironmentVariable(OutcomeEnvironment);
            if (prefix is not null)
            {
                Require(Path.IsPathFullyQualified(prefix));
                string profile = owned ? OwnedSuffix : OrdinarySuffix;
                using (var output = new FileStream(prefix + profile + RawSuffix, FileMode.CreateNew, FileAccess.Write)) output.Write(raw);
                using (var output = new FileStream(prefix + profile + DeterministicSuffix, FileMode.CreateNew, FileAccess.Write)) output.Write(deterministic);
            }
            Require(outcome.Equals(ExternalTermFormat.Decode(Resource(OriginalValues)))
                && raw.AsSpan().SequenceEqual(Resource(OriginalValues))
                && deterministic.AsSpan().SequenceEqual(Resource(OriginalDeterministicValues)));

            return true;
        });
    }

    private static byte[] Resource(string name)
    {
        using Stream input = typeof(NativeAppend206Checks).Assembly.GetManifestResourceStream(ResourcePrefix + name)!;
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }

    private static async Task BadArguments(ProcessContext context, string function)
    {
        Term right = Term.Tuple(Term.A(Tail));
        foreach (Term left in new Term[] { Term.A(Bad), new Cons(Term.I(1), Term.A(Bad)) })
        {
            bool rejected = false;
            try
            {
                await Store(
                    context,
                    ErtsModule,
                    function,
                    left,
                    right
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(BadArgument)))
            {
                rejected = Cons.Items(error.StackTraceTerm).OfType<TupleTerm>().Any(row =>
                    row.Items.Count >= 3 && row.Items[0].Equals(Term.A(ErtsModule)) && row.Items[1].Equals(Term.A(function))
                    && row.Items[2] is Cons first && first.Head.Equals(left)
                    && first.Tail is Cons second && second.Head.Equals(right) && second.Tail is Nil);
            }
            Require(rejected);
        }
    }

    private static async Task OpaqueOverride(bool owned)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        bool portable = false;
        runtime.Modules.DeleteModule(ErtsModule);
        runtime.Modules.Register(
            ErtsModule,
            Operator,
            Arity,
            (context, arguments) =>
        {
            object heap = Property(context, Heap);
            portable = StorageOf(heap, arguments[0]) is null && StorageOf(heap, arguments[1]) is null;

            return ValueTask.FromResult(Cons.From(Cons.Items(arguments[0]), arguments[1]));
        }
        );
        await actor.InvokeAsync(async context =>
        {
            object heap = Property(context, Heap);
            using var frame = (IDisposable)Call(heap, Enter)!;
            Term left = Own(frame, Term.List(new FloatTerm(1.5)));
            Term right = Own(frame, Term.List(new Integer(BigInteger.One << IntegerShift)));
            Term value = await Execution.EvaluateAsync(new Expr.Call(ErtsModule, Operator, [new Expr.Literal(left), new Expr.Literal(right)]), context);
            Require(portable && value.Equals(Term.List(new FloatTerm(1.5), new Integer(BigInteger.One << IntegerShift))));

            return true;
        });
    }

    private static async Task SynchronousRoutes(
        ProcessContext context,
        object heap,
        object frame,
        Term left,
        Term right,
        Term[] expectedHeads
    )
    {
        foreach (bool expression in new[] { false, true })
        {
            Task<Term> Evaluate(Term first, Term second) => expression
                ? StoreBinary(context, first, second)
                : Task.FromResult(ErlangOperatorValues.Binary(
                    Operator,
                    first,
                    second,
                    context
                ));

            Term[] emptyLeftTails = [Nil.Value, right, left, Term.A(Tail), .. expectedHeads];
            foreach (Term tail in emptyLeftTails)
                Require(ReferenceEquals(await Evaluate(Nil.Value, tail), tail));
            Require(ReferenceEquals(await Evaluate(left, Nil.Value), left));
            foreach (Term tail in new[] { right, left, Term.A(Tail) })
            {
                Term result = await Evaluate(left, tail);
                Require(!ReferenceEquals(result, left));
                Term cursor = result;
                foreach (Term head in expectedHeads)
                {
                    Require(cursor is Cons && (StorageOf(heap, cursor) is not null) == expression);
                    var cell = (Cons)cursor;
                    Require(ReferenceEquals(cell.Head, head));
                    cursor = cell.Tail;
                }
                Require(ReferenceEquals(cursor, tail));
            }
            Term improper = Own(frame, new Cons(expectedHeads[0], Term.A(Bad)));
            foreach (Term malformed in new[] { Term.A(Bad), improper })
                foreach (Term tail in new[] { Nil.Value, right })
                {
                    bool rejected = false;
                    try
                    {
                        _ = await Evaluate(malformed, tail);
                    }
                    catch (ErlangException error) when (error.Reason.Equals(Term.A(BadArgument)))
                    {
                        rejected = true;
                    }
                    Require(rejected);
                }
        }
    }

    private static async Task<Term> StoreBinary(ProcessContext context, Term left, Term right)
    {
        // This is an actual Expr.Binary, not another erlang MFA call. Retain
        // its result through the real dictionary BIF before inspecting cells.
        _ = await Execution.EvaluateAsync(
            new Expr.Sequence([
            new Expr.Call(ErtsModule, Put, [
                new Expr.Literal(Term.A(Key)),
                new Expr.Binary(Operator, new Expr.Literal(left), new Expr.Literal(right))
            ]),
            new Expr.Literal(Nil.Value)
        ]),
            context
        );
        object dictionary = context.Dictionary;
        lock (Field(dictionary, DictionaryGate))
            return ((Dictionary<Term, Term>)Field(dictionary, DictionaryData))[Term.A(Key)];
    }

    private static async Task<Term> Store(
        ProcessContext context,
        string module,
        string function,
        Term left,
        Term right
    )
    {
        _ = await Execution.EvaluateAsync(
            new Expr.Sequence([
            new Expr.Call(
                ErtsModule,
                Put,
                [new Expr.Literal(Term.A(Key)), new Expr.Call(module, function, [new Expr.Literal(left), new Expr.Literal(right)])]
            ),
            new Expr.Literal(Nil.Value)
        ]),
            context
        );
        object dictionary = context.Dictionary;
        lock (Field(dictionary, DictionaryGate)) return ((Dictionary<Term, Term>)Field(dictionary, DictionaryData))[Term.A(Key)];
    }

    private static async Task EraseResult(ProcessContext context)
    {
        _ = await Execution.EvaluateAsync(new Expr.Sequence([
            new Expr.Call(ErtsModule, Erase, [new Expr.Literal(Term.A(Key))]), new Expr.Literal(Nil.Value)
        ]), context);
    }

    private static Term Own(object frame, Term value)
        => (Term)Call(
            frame,
            Adopt,
            (int)Call(frame, Reserve)!,
            value
        )!;

    private static object? StorageOf(object heap, Term value)
        => heap.GetType().GetMethod(Storage, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, [value]);

    private static object Field(object target, string name)
        => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(target)!;

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }
}
