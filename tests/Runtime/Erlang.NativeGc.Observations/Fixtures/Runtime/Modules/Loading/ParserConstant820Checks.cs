using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Compiler;
using static Erlang.Tests.ParserConstant820Names;

namespace Erlang.Tests;

internal static class ParserConstant820Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

    private static object? Get(object target, string name) => target.GetType().GetProperty(name, Members)!.GetValue(target);

    private static object Field(object target, string name) => target.GetType().GetField(name, Members)!.GetValue(target)!;

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }

    private static object? Call(object target, string name, params object[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(name, Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } actual)
        {
            ExceptionDispatchInfo.Capture(actual).Throw();
            throw;
        }
    }

    private static string Fixtures() => Environment.GetEnvironmentVariable(FixtureEnvironment) ?? throw new InvalidOperationException(ParserConstant820Names.Missing);

    private static ModuleDefinition Read(string directory, string name) => new Parser(File.ReadAllText(Path.Combine(directory, name))).ParseModule();

    private static Term GuestValue(ProcessContext context)
    {
        object dictionary = context.Dictionary;
        lock (Field(dictionary, SourceLiteral819Names.DictionaryGate))
            return ((Dictionary<Term, Term>)Field(dictionary, SourceLiteral819Names.DictionaryData))[Term.A(GuestKey)];
    }

    private static async ValueTask<Term> Retain(ProcessContext context, string function)
    {
        // Actual Parser expression and registered guest function: the public Host
        // Call result is deliberately NOT used as native storage evidence.
        Expr expression = new Parser(Put + function + InvokeEnd).ParseExpression();
        _ = await Execution.EvaluateAsync(expression, context);

        return GuestValue(context);
    }

    internal static async Task Run()
    {
        ModuleDefinition template = Read(Fixtures(), SubjectFile);
        Expr root = template.Functions.Single(function => function.Name == TupleFunction).Clauses[0].Body;
        Require(root is Expr.Tuple);
        Expr nested = ((Expr.Tuple)root).Items[1];
        Require(nested is Expr.List);
        Expr bits = template.Functions.Single(function => function.Name == BitsFunction).Clauses[0].Body;
        Require(bits is Expr.Bits);
        ModuleDefinition negative = new Parser(NegativeSource).ParseModule();
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        await first.InitializeModulesAsync(_ => { });
        await second.InitializeModulesAsync(_ => { });
        await using PreparedSourceModule819 one = template.PrepareSourceModule819(first.Modules);
        await using PreparedSourceModule819 two = template.PrepareSourceModule819(second.Modules);
        object view = Field(one, SourceLiteral819Names.LiteralView);
        var constants = (Dictionary<object, Term>)Field(view, Constants);
        Require(constants.ContainsKey(root) && constants.ContainsKey(bits) && !constants.ContainsKey(nested));
        Require(!constants.ContainsKey(root with { }));
        Require(!constants.ContainsKey(template.Functions.Single(function => function.Name == NativeFunction).Clauses[0].Body));
        Require(!constants.ContainsKey(template.Functions.Single(function => function.Name == SparseFunction).Clauses[0].Body));
        await using PreparedSourceModule819 bad = negative.PrepareSourceModule819(first.Modules);
        await first.InitializeModulesAsync(_ =>
        {
            one.Register();
            bad.Register();
        });
        await second.InitializeModulesAsync(_ => two.Register());
        ModuleCodeImage firstImage = first.Modules.CurrentCodeImage(ParserConstant820Names.Module)!;
        object firstDomain = Get(firstImage, SourceLiteral819Names.ImageDomain)!;
        object secondDomain = Get(second.Modules.CurrentCodeImage(ParserConstant820Names.Module)!, SourceLiteral819Names.ImageDomain)!;
        Require(!ReferenceEquals(firstDomain, secondDomain));
        Task retired = (Task)Get(firstDomain, SourceLiteral819Names.Retirement)!;
        await using var actorOne = new RuntimeNodeFoundationHostActor(first);
        await using var actorTwo = new RuntimeNodeFoundationHostActor(second);
        Term? actual = null, other = null, host = null;
        await actorOne.InvokeAsync(async context =>
        {
            foreach (string function in new[] { TupleFunction, ListFunction, ImproperFunction, BitsFunction, BinaryFunction })
            {
                Term value = await Retain(context, function);
                object heapTypeOwner = Get(context, SourceLiteral819Names.Heap)!;
                object? observedStorage = heapTypeOwner.GetType().GetMethod(HeapStorageMethod, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [value]);
                object storage = observedStorage ?? throw new InvalidOperationException(Failure);
                Require(ReferenceEquals(Get(Get(storage, SourceLiteral819Names.StorageOwner)!, SourceLiteral819Names.HeapDomain), firstDomain));
                Term again = await Retain(context, function);
                Require(ReferenceEquals(value, again));
            }
            foreach (string failureFunction in new[] { Bad, BadFloat })
            {
                bool failedAtRuntime = false;
                try
                {
                    _ = await first.Modules.Call(context, NegativeModule, failureFunction);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                {
                    failedAtRuntime = true;
                }
                Require(failedAtRuntime && context.Dictionary[Term.A(Marker)].Equals(Term.I(17)));
                Require(context.Dictionary.Remove(Term.A(Marker)));
            }
            actual = await Retain(context, TupleFunction);
            host = await first.Modules.Call(context, ParserConstant820Names.Module, TupleFunction);
            Require(host.Equals(actual) && Get(host, SourceLiteral819Names.Storage) is null);
            object heap = Get(context, SourceLiteral819Names.Heap)!;
            Call(
                heap,
                SourceLiteral819Names.Collect,
                true,
                (ushort)0
            );
            Require(((TupleTerm)actual).Items.Count == ((Expr.Tuple)root).Items.Count);

            return true;
        });
        await actorTwo.InvokeAsync(async context =>
        {
            other = await Retain(context, TupleFunction);
            Require(other.Equals(actual) && !ReferenceEquals(other, actual));
            Require(ReferenceEquals(
                Get(Get(Get(other, SourceLiteral819Names.Storage)!, SourceLiteral819Names.StorageOwner)!, SourceLiteral819Names.HeapDomain),
                secondDomain
            ));
            Require(ReferenceEquals(template.Functions.Single(function => function.Name == TupleFunction).Clauses[0].Body, root));

            return true;
        });
        first.Modules.DeleteModule(ParserConstant820Names.Module);
        await actorOne.InvokeAsync(async context =>
        {
            Require(await (ValueTask<bool>)Call(
                first,
                SourceLiteral819Names.Purge,
                context,
                ParserConstant820Names.Module
            )!);
            Require(!retired.IsCompleted && actual!.Equals(host));
            Require(context.Dictionary.Remove(Term.A(GuestKey)));
            Call(
                Get(context, SourceLiteral819Names.Heap)!,
                SourceLiteral819Names.Collect,
                true,
                (ushort)0
            );

            return true;
        });
        await retired.WaitAsync(Deadline);
        bool stale = false;
        try
        {
            _ = ((TupleTerm)actual!).Items[0];
        }
        catch (ObjectDisposedException)
        {
            stale = true;
        }
        Require(stale && other!.Equals(host));
        await actorTwo.InvokeAsync(context =>
        {
            Require(context.Dictionary.Remove(Term.A(GuestKey)));
            Call(
                Get(context, SourceLiteral819Names.Heap)!,
                SourceLiteral819Names.Collect,
                true,
                (ushort)0
            );

            return Task.FromResult(true);
        });
        GC.KeepAlive(actual);
        GC.KeepAlive(other);
        GC.KeepAlive(template);
    }

    internal static async Task DeepPreparation()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(_ => { });
        Expr body = new Expr.Literal(Term.I(23));
        for (int index = 0; index < DeepLayers; index++)
            body = index % 2 == 0 ? new Expr.Tuple([body]) : new Expr.List([body]);
        var manual = new ModuleDefinition(DeepModule, [(DeepFunction, 0)], [new FunctionDefinition(DeepFunction, 0, [new Clause([], null, body)])]);
        await CheckDeepPrepared(
            runtime,
            manual,
            body,
            DeepLayers
        );
        string nested = new string('{', ParsedLayers) + "23" + new string('}', ParsedLayers);
        ModuleDefinition parsed = new Parser("-module(" + DeepModule + "). -export([" + DeepFunction + "/0]). " + DeepFunction + "() -> " + nested + ".").ParseModule();
        Expr parsedBody = parsed.Functions.Single(function => function.Name == DeepFunction).Clauses[0].Body;
        Require(parsedBody is Expr.Tuple);
        await CheckDeepPrepared(
            runtime,
            parsed,
            parsedBody,
            ParsedLayers
        );
    }

    private static async Task CheckDeepPrepared(
        ProcessRuntime runtime,
        ModuleDefinition definition,
        Expr body,
        int depth
    )
    {
        await using PreparedSourceModule819 prepared = definition.PrepareSourceModule819(runtime.Modules);
        object view = Field(prepared, SourceLiteral819Names.LiteralView);
        object domain = Field(view, SourceLiteral819Names.DomainField);
        var slots = (Dictionary<object, Term>)Field(view, Constants);
        Require(slots.ContainsKey(body));
        var actualRoots = (IReadOnlyDictionary<Term, Term>)Field(view, SourceLiteral819Names.ValuesField);
        Term actual = actualRoots[slots[body]], current = actual;
        for (int index = 0; index < depth; index++)
        {
            object storage = Get(current, SourceLiteral819Names.Storage) ?? throw new InvalidOperationException(Failure);
            Require(ReferenceEquals(Get(Get(storage, SourceLiteral819Names.StorageOwner)!, SourceLiteral819Names.HeapDomain), domain));
            current = current switch
            {
                TupleTerm tuple => tuple.Items[0],
                Cons cell => cell.Head,
                _ => throw new InvalidOperationException(Failure)
            };
        }
        Require(current.Equals(Term.I(23)));
        await prepared.DisposeAsync();
        await ((Task)Get(domain, SourceLiteral819Names.Retirement)!).WaitAsync(Deadline);
        bool stale = false;
        try
        {
            _ = actual is TupleTerm tuple ? tuple.Items[0] : ((Cons)actual).Head;
        }
        catch (ObjectDisposedException)
        {
            stale = true;
        }
        Require(stale);
        GC.KeepAlive(actual);
        GC.KeepAlive(definition);
    }

    internal static async Task OriginalObserver()
    {
        string directory = Fixtures();
        ModuleDefinition subject = Read(directory, SubjectFile), probe = Read(directory, ProbeFile);
        await using var runtime = new ProcessRuntime();
        await ParserConstant820LibrarySetup.Load(runtime);
        await using PreparedSourceModule819 preparedSubject = subject.PrepareSourceModule819(runtime.Modules);
        await using PreparedSourceModule819 preparedProbe = probe.PrepareSourceModule819(runtime.Modules);
        await runtime.InitializeModulesAsync(_ =>
        {
            preparedSubject.Register();
            preparedProbe.Register();
            ParserConstant820LibrarySetup.RequireDebugOriginal(runtime.Modules);
        });
        await using var diagnostics = ParserConstant820Diagnostic.Begin(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term? result = null;
        await actor.InvokeAsync(async context =>
        {
            result = await runtime.Modules.Call(context, ProbeModule, Observe);

            return true;
        });
        string? output = Environment.GetEnvironmentVariable(OutcomeEnvironment);
        if (!string.IsNullOrEmpty(output)) await File.WriteAllBytesAsync(output, ExternalTermFormat.Encode(result!));
        GC.KeepAlive(subject);
        GC.KeepAlive(probe);
    }
}
