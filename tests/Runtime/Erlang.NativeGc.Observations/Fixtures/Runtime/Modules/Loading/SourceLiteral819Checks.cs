using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Compiler;
using static Erlang.Tests.SourceLiteral819Names;

namespace Erlang.Tests;

internal static class SourceLiteral819Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

    private static object Get(object target, string name) => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

    private static object Field(object target, string name) => target.GetType().GetField(name, Members)!.GetValue(target)!;

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }

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

    private static async ValueTask RetainActualGuestLiteral(ProcessContext context)
    {
        // This public interpreter service establishes its genuine compiler
        // entry. The registered Source return feeds the real dictionary BIF;
        // no test-created capability or synthetic ownership flag is installed.
        _ = await Execution.EvaluateAsync(
            new Expr.Call(DictionaryModule, Put, [
            new Expr.Literal(Term.A(GuestKey)),
            new Expr.Call(SourceLiteral819Names.Module, Function, [])
        ]),
            context
        );
    }

    private static Term ObserveGuestDictionaryValue(ProcessContext context, Term key)
    {
        object dictionary = context.Dictionary;
        object gate = Field(dictionary, DictionaryGate);
        lock (gate)
            return ((Dictionary<Term, Term>)Field(dictionary, DictionaryData))[key];
    }

    internal static async Task Run()
    {
        await PublicReceiverFunction414Checks.Run();
        await NativeRecordDefaults313Checks.Run();
        await GeneratedSourcePreparation190Checks.Run();
        byte[] expected = Enumerable.Range(0, BinaryBytes).Select(value => (byte)value).ToArray();
        var portable = Term.Tuple(new BitString(expected), Term.List(Term.I(17), Term.I(23)));
        var expression = new Expr.Literal(portable);
        Term patternOnly = Term.Tuple(Term.A(PatternAtom), new BitString(expected.Reverse().ToArray()));
        var template = new ModuleDefinition(
            SourceLiteral819Names.Module,
            [(Function, 0), (MatchFunction, 1)],
            [
            new FunctionDefinition(Function, 0, [new Clause([], null, expression)]),
            new FunctionDefinition(MatchFunction, 1, [new Clause([new Pattern.Literal(patternOnly)], null, new Expr.Literal(Term.A(Matched)))])
        ]
        );
        var standalone = new ModuleRegistry();
        bool rejected = false;
        try
        {
            _ = template.PrepareSourceModule819(standalone);
        }
        catch (NotSupportedException)
        {
            rejected = true;
        }
        Require(rejected && !standalone.IsModuleLoaded(SourceLiteral819Names.Module));
        // Existing standalone metadata validation remains an explicit portable
        // boundary; it is not counted as native source/image execution proof.
        await using PreparedBeamImage metadata = standalone.PrepareBeam(BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(portable)));
        Require(Get(metadata, ModuleLiteral801Names.Domain) is null);

        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        await first.InitializeModulesAsync(_ => { });
        await second.InitializeModulesAsync(_ => { });
        await using PreparedSourceModule819 preparedOne = template.PrepareSourceModule819(first.Modules);
        await using PreparedSourceModule819 preparedTwo = template.PrepareSourceModule819(second.Modules);
        await first.InitializeModulesAsync(_ => preparedOne.Register());
        await second.InitializeModulesAsync(_ => preparedTwo.Register());
        ModuleCodeImage imageOne = first.Modules.CurrentCodeImage(SourceLiteral819Names.Module)!;
        ModuleCodeImage imageTwo = second.Modules.CurrentCodeImage(SourceLiteral819Names.Module)!;
        object domainOne = Get(imageOne, ImageDomain), domainTwo = Get(imageTwo, ImageDomain);
        Require(!ReferenceEquals(domainOne, domainTwo));
        Require(ReferenceEquals(first.Modules.SourceOwner(SourceLiteral819Names.Module, Function, 0), template));
        Require(ReferenceEquals(second.Modules.SourceOwner(SourceLiteral819Names.Module, Function, 0), template));
        Require(ReferenceEquals(expression.Value, portable) && Get(portable, Storage) is null);
        Task retired = (Task)Get(domainOne, Retirement);
        await using var actorOne = new RuntimeNodeFoundationHostActor(first);
        await using var actorTwo = new RuntimeNodeFoundationHostActor(second);
        IDisposable? rootOne = null, rootTwo = null, flat222 = null;
        object? heapOne = null, heapTwo = null;
        Term? actualOne = null, actualTwo = null, exported = null;
        Term? hostOne = null, hostTwo = null, matchOne = null, matchTwo = null;
        int slotOne = -1, slotTwo = -1;
        try
        {
            await actorOne.InvokeAsync(async context =>
            {
                using SourceCodeLease admitted = await first.Modules.AcquireSourceCode(context, imageOne);
                using IDisposable executing = admitted.Attach(template);
                hostOne = await first.Modules.Call(context, SourceLiteral819Names.Module, Function);
                Require(hostOne.Equals(portable) && Get(hostOne, Storage) is null);
                await RetainActualGuestLiteral(context);
                actualOne = ObserveGuestDictionaryValue(context, Term.A(GuestKey));
                NoBackingClosure192Checks.AssertLiteralChild(context, actualOne);
                PublishedFunctionClosure293Checks.AssertLiteralChild(context, actualOne);
                OwnedMapClosure302Checks.AssertLiteralChild(context, actualOne);
                flat222 = FlatLiteralCapture222Checks.Run(context, actualOne);
                matchOne = await first.Modules.Call(
                    context,
                    SourceLiteral819Names.Module,
                    MatchFunction,
                    patternOnly
                );
                Require(matchOne.Equals(Term.A(Matched)));
                Term dynamicPattern = Term.Tuple(Term.A(PatternAtom), first.MakeReference());
                Require(new Pattern.Literal(dynamicPattern).Match(dynamicPattern, new Dictionary<string, Term>(), context));
                Require(actualOne.Equals(portable) && ReferenceEquals(Get(Get(Get(actualOne, Storage), StorageOwner), HeapDomain), domainOne));
                heapOne = Get(context, Heap);
                rootOne = (IDisposable)Call(heapOne, Enter)!;
                slotOne = (int)Call(rootOne, Reserve)!;
                Require(ReferenceEquals(Call(
                    rootOne,
                    Adopt,
                    slotOne,
                    actualOne
                ), actualOne));
                exported = (Term)Call(
                    heapOne,
                    Export,
                    rootOne,
                    actualOne
                )!;
                Require(exported.Equals(portable) && Get(exported, Storage) is null);

                return true;
            });
            await actorTwo.InvokeAsync(async context =>
            {
                using SourceCodeLease admitted = await second.Modules.AcquireSourceCode(context, imageTwo);
                using IDisposable executing = admitted.Attach(template);
                hostTwo = await second.Modules.Call(context, SourceLiteral819Names.Module, Function);
                Require(hostTwo.Equals(portable) && Get(hostTwo, Storage) is null);
                matchTwo = await second.Modules.Call(
                    context,
                    SourceLiteral819Names.Module,
                    MatchFunction,
                    patternOnly
                );
                Require(matchTwo.Equals(Term.A(Matched)));
                await RetainActualGuestLiteral(context);
                actualTwo = ObserveGuestDictionaryValue(context, Term.A(GuestKey));
                NoBackingClosure192Checks.AssertLiteralChild(context, actualTwo);
                using (FlatLiteralCapture222Checks.Run(context, actualTwo))
                    FlatLiteralCapture222Checks.RejectForeignHolder(Get(context, Heap), flat222!);
                flat222!.Dispose();
                flat222 = null;
                Require(actualTwo.Equals(portable) && !ReferenceEquals(actualOne, actualTwo));
                Require(ReferenceEquals(Get(Get(Get(actualTwo, Storage), StorageOwner), HeapDomain), domainTwo));
                heapTwo = Get(context, Heap);
                rootTwo = (IDisposable)Call(heapTwo, Enter)!;
                slotTwo = (int)Call(rootTwo, Reserve)!;
                Require(ReferenceEquals(Call(
                    rootTwo,
                    Adopt,
                    slotTwo,
                    actualTwo
                ), actualTwo));
                using IDisposable foreign = (IDisposable)Call(heapTwo, Enter)!;
                int slot = (int)Call(foreign, Reserve)!;
                bool foreignRejected = false;
                try
                {
                    Call(
                        foreign,
                        Adopt,
                        slot,
                        actualOne!
                    );
                }
                catch (InvalidOperationException error) when (error.Message == ModuleLiteral801Names.Foreign)
                {
                    foreignRejected = true;
                }
                Require(foreignRejected);

                return true;
            });
            string? output = Environment.GetEnvironmentVariable(OutcomeEnvironment);
            if (!string.IsNullOrEmpty(output))
            {
                await File.WriteAllBytesAsync(output, ExternalTermFormat.Encode(Term.Tuple(hostOne!, matchOne!)));
                await File.WriteAllBytesAsync(output + SecondVmSuffix, ExternalTermFormat.Encode(Term.Tuple(hostTwo!, matchTwo!)));
            }
            first.Modules.DeleteModule(SourceLiteral819Names.Module);
            await actorOne.InvokeAsync(async context =>
            {
                Require(await (ValueTask<bool>)Call(
                    first,
                    Purge,
                    context,
                    SourceLiteral819Names.Module
                )!);
                Require(!retired.IsCompleted && actualOne!.Equals(portable));
                Require(context.Dictionary.Remove(Term.A(GuestKey)));
                Call(rootOne!, Clear, slotOne);
                rootOne!.Dispose();
                rootOne = null;
                Call(
                    heapOne!,
                    Collect,
                    true,
                    (ushort)0
                );

                return true;
            });
            await retired.WaitAsync(Deadline);
            bool stale = false;
            try
            {
                _ = ((TupleTerm)actualOne!).Items[0];
            }
            catch (ObjectDisposedException)
            {
                stale = true;
            }
            Require(stale && actualTwo!.Equals(portable) && exported!.Equals(portable));
            Require(((BitString)((TupleTerm)expression.Value).Items[0]).ToArray().SequenceEqual(expected));
            await actorTwo.InvokeAsync(context =>
            {
                Require(context.Dictionary.Remove(Term.A(GuestKey)));
                Call(rootTwo!, Clear, slotTwo);
                rootTwo!.Dispose();
                rootTwo = null;
                Call(
                    heapTwo!,
                    Collect,
                    true,
                    (ushort)0
                );

                return Task.FromResult(true);
            });
        }
        finally
        {
            flat222?.Dispose();
            rootOne?.Dispose();
            rootTwo?.Dispose();
        }

        // An unused prepared binding has a real code-owner lifetime, not a
        // process/registry strong entry or a weak CLR finalizer as its proof.
        PreparedSourceModule819 unused = template.PrepareSourceModule819(first.Modules);
        object view = Field(unused, LiteralView), unusedDomain = Field(view, DomainField);
        Term unusedActual = ((IReadOnlyDictionary<Term, Term>)Field(view, ValuesField))[portable];
        Require(ReferenceEquals(Get(Get(Get(unusedActual, Storage), StorageOwner), HeapDomain), unusedDomain));
        await unused.DisposeAsync();
        await ((Task)Get(unusedDomain, Retirement)).WaitAsync(Deadline);
        bool closed = false;
        try
        {
            _ = ((TupleTerm)unusedActual).Items[0];
        }
        catch (ObjectDisposedException)
        {
            closed = true;
        }
        Require(closed);
        await using var rollback = new ProcessRuntime();
        await rollback.InitializeModulesAsync(_ => { });
        rollback.Modules.PublishModule(SourceLiteral819Names.Module);
        ModuleCodeImage placeholder = rollback.Modules.CurrentCodeImage(SourceLiteral819Names.Module)!;
        await using PreparedSourceModule819 pending = template.PrepareSourceModule819(rollback.Modules);
        object pendingDomain = Field(Field(pending, LiteralView), DomainField);
        var injected = new InvalidOperationException(Rollback);
        bool primaryPreserved = false;
        try
        {
            await rollback.InitializeModulesAsync(_ =>
            {
                pending.Register();
                throw injected;
            });
        }
        catch (InvalidOperationException error) when (ReferenceEquals(error, injected))
        {
            primaryPreserved = true;
        }
        Require(primaryPreserved && ReferenceEquals(placeholder, rollback.Modules.CurrentCodeImage(SourceLiteral819Names.Module)));
        Require(Get(placeholder, ImageDomain) is null && !rollback.Modules.HasExport(SourceLiteral819Names.Module, Function, 0));
        await ((Task)Get(pendingDomain, Retirement)).WaitAsync(Deadline);
        GC.KeepAlive(template);
        GC.KeepAlive(actualOne);
        GC.KeepAlive(actualTwo);
        GC.KeepAlive(unusedActual);
    }
}
