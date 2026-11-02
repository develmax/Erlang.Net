using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using Erlang.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Erlang.Tests.GeneratedSourcePreparation190Names;

namespace Erlang.Tests;

internal static class GeneratedSourcePreparation190Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

    internal static async Task Run()
    {
        string source = SourceStart + new string(BinaryByte, BinaryBytes) + SourceEnd;
        string generated = CodeGeneration.CompileModule(source, SourceFile);
        string[] platform = ((string?)AppContext.GetData(PlatformAssemblies)
            ?? throw new InvalidOperationException(Failure)).Split(Path.PathSeparator);
        MetadataReference[] references = platform.Concat([
            typeof(ModuleDefinition).Assembly.Location,
            typeof(ModuleRegistry).Assembly.Location,
            typeof(Term).Assembly.Location
        ]).Distinct(StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        var compilation = CSharpCompilation.Create(
            GeneratedAssembly,
            [CSharpSyntaxTree.ParseText(generated, new CSharpParseOptions(LanguageVersion.Preview))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release)
        );
        using var binary = new MemoryStream();
        var emission = compilation.Emit(binary);
        if (!emission.Success)
            throw new InvalidOperationException(CompilationFailure + string.Join(
                Environment.NewLine,
                emission.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error)
            ));
        binary.Position = 0;
        var load = new AssemblyLoadContext(GeneratedAssembly, isCollectible: true);
        try
        {
            Type generatedType = load.LoadFromStream(binary).GetType(GeneratedType, throwOnError: true)!;
            var definition = (ModuleDefinition)generatedType.GetProperty(Definition)!.GetValue(null)!;
            var prepare = generatedType.GetMethod(Prepare)!
                .CreateDelegate<Func<ModuleRegistry, PreparedSourceModule819>>();
            // The canonical serialization remains free of the operational API.
            // Root additionally compares pre-/post-change canonical bytes.
            string canonical = (string)typeof(CodeGeneration).GetMethod(
                CanonicalCode,
                BindingFlags.Static | BindingFlags.NonPublic
            )!.Invoke(null, [definition, string.Empty, false])!;
            Require(!canonical.Contains(Prepare, StringComparison.Ordinal));
            var standalone = new ModuleRegistry();
            bool rejected = false;
            try
            {
                await using PreparedSourceModule819 unexpected = prepare(standalone);
            }
            catch (NotSupportedException)
            {
                rejected = true;
            }
            Require(rejected && !standalone.IsModuleLoaded(SourceModule));
            foreach (bool owned in new[] { false, true })
                await One(owned, definition, prepare);
        }
        finally
        {
            load.Unload();
        }
    }

    private static async Task One(
        bool owned,
        ModuleDefinition definition,
        Func<ModuleRegistry, PreparedSourceModule819> prepare
    )
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
        await runtime.InitializeModulesAsync(_ => { });
        // This calls the method on the compiled generated TYPE. It does not
        // bypass the generated API by invoking Definition's method directly.
        PreparedSourceModule819 unused = prepare(runtime.Modules);
        Task unusedRetirement = (Task)Get(Field(Field(unused, Literals), Domain), Retirement)!;
        await unused.DisposeAsync();
        await unusedRetirement.WaitAsync(Deadline);
        Require(!runtime.Modules.IsModuleLoaded(SourceModule));
        await using (PreparedSourceModule819 prepared = prepare(runtime.Modules))
            await runtime.InitializeModulesAsync(_ => prepared.Register());
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(SourceModule)!;
        object domain = Get(image, ImageDomain)!;
        Task retired = (Task)Get(domain, Retirement)!;
        Require(ReferenceEquals(runtime.Modules.SourceOwner(SourceModule, Function, 0), definition));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var expected = Term.Tuple(
            new BitString(Enumerable.Repeat((byte)BinaryByte, BinaryBytes).ToArray()),
            Term.List(Term.I(ListFirst), Term.I(ListSecond))
        );
        Term? held = null;
        Term? detached = null;
        await actor.InvokeAsync(async context =>
        {
            using SourceCodeLease lease = await runtime.Modules.AcquireSourceCode(context, image);
            using IDisposable execution = lease.Attach(definition);
            detached = await runtime.Modules.Call(context, SourceModule, Function);
            Require(detached.Equals(expected) && Get(detached, Storage) is null);
            _ = await Execution.EvaluateAsync(
                new Expr.Call(DictionaryModule, Put, [
                    new Expr.Literal(Term.A(Key)),
                    new Expr.Call(SourceModule, Function, [])
                ]),
                context
            );
            object dictionary = context.Dictionary;
            lock (Field(dictionary, DictionaryGate))
                held = ((Dictionary<Term, Term>)Field(dictionary, DictionaryData))[Term.A(Key)];
            Require(held.Equals(expected)
                && ReferenceEquals(Get(Get(Get(held, Storage)!, StorageOwner)!, HeapDomain), domain));
            CollectNow(context);
            Require(held.Equals(expected));

            return Nil.Value;
        });
        _ = runtime.Modules.DeleteModule(SourceModule);
        await actor.InvokeAsync(async context =>
        {
            Require(await (ValueTask<bool>)Call(
                runtime,
                Purge,
                context,
                SourceModule
            )!);
            Require(!retired.IsCompleted && held!.Equals(expected));
            Require(context.Dictionary.Remove(Term.A(Key)));
            CollectNow(context);

            return Nil.Value;
        });
        await retired.WaitAsync(Deadline);
        bool stale = false;
        try
        {
            _ = ((TupleTerm)held!).Items[0];
        }
        catch (ObjectDisposedException)
        {
            stale = true;
        }
        Require(stale && detached!.Equals(expected));
    }

    private static void CollectNow(ProcessContext context)
    {
        Call(
            Get(context, Heap)!,
            Collect,
            true,
            FullSweep
        );
    }

    private static object? Get(object target, string name)
        => target.GetType().GetProperty(name, Members)!.GetValue(target);

    private static object Field(object target, string name)
        => target.GetType().GetField(name, Members)!.GetValue(target)!;

    private static object? Call(object target, string name, params object[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(name, Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } actual)
        {
            ExceptionDispatchInfo.Capture(actual).Throw();
            throw;
        }
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Failure);
    }
}

