using Erlang.Differential;

namespace Erlang.Tests;

internal static class OrdsetListsDispatchHostChecks
{
    internal static async Task Run()
    {
        await Success();
        await Failure();
    }

    private static async Task Success()
    {
        string source = await Read(OrdsetListsDispatchHostNames.SuccessFixture);
        using var artifact = GeneratedModuleCompiler.Compile(source, OrdsetListsDispatchHostNames.SuccessFixture);
        await using var runtime = new ProcessRuntime();
        runtime.Modules.Apply(registry => registry.DeleteModule(OrdsetListsDispatchHostNames.Lists));
        artifact.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term callback = Term.A(OrdsetListsDispatchHostNames.InvalidCallback);
        Term initial = Term.A(OrdsetListsDispatchHostNames.Initial);
        Term set = Term.A(OrdsetListsDispatchHostNames.Set);
        foreach (string operation in Operations())
        {
            Term[] arguments = operation == OrdsetListsDispatchHostNames.Fold ? [callback, initial, set] : [callback, set];
            Term actual = await actor.InvokeAsync(context => runtime.Modules.Call(
                context,
                OrdsetListsDispatchHostNames.Ordsets,
                operation,
                arguments
            ).AsTask());
            string replaced = operation switch
            {
                OrdsetListsDispatchHostNames.Fold => OrdsetListsDispatchHostNames.FoldReplaced,
                OrdsetListsDispatchHostNames.Filter => OrdsetListsDispatchHostNames.FilterReplaced,
                OrdsetListsDispatchHostNames.Map => OrdsetListsDispatchHostNames.MapReplaced,
                _ => OrdsetListsDispatchHostNames.FilterMapReplaced
            };
            Term expected = operation == OrdsetListsDispatchHostNames.Fold
                ? Term.Tuple(
                    Term.A(replaced),
                    callback,
                    initial,
                    set
                )
                : Term.Tuple(Term.A(replaced), callback, set);
            if (operation is OrdsetListsDispatchHostNames.Map or OrdsetListsDispatchHostNames.FilterMap)
                expected = Term.Tuple(Term.A(OrdsetListsDispatchHostNames.SortReplaced), expected);
            Require(actual.Equals(expected));
        }
    }

    private static async Task Failure()
    {
        string source = await Read(OrdsetListsDispatchHostNames.FailureFixture);
        using var artifact = GeneratedModuleCompiler.Compile(source, OrdsetListsDispatchHostNames.FailureFixture);
        await using var runtime = new ProcessRuntime();
        runtime.Modules.Apply(registry => registry.DeleteModule(OrdsetListsDispatchHostNames.Lists));
        artifact.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term callback = Term.A(OrdsetListsDispatchHostNames.InvalidCallback);
        Term initial = Term.A(OrdsetListsDispatchHostNames.Initial);
        Term set = Term.A(OrdsetListsDispatchHostNames.Set);
        foreach (string operation in Operations())
        {
            bool caught = false;
            Term[] arguments = operation == OrdsetListsDispatchHostNames.Fold ? [callback, initial, set] : [callback, set];
            try
            {
                await actor.InvokeAsync(context => runtime.Modules.Call(
                    context,
                    OrdsetListsDispatchHostNames.Ordsets,
                    operation,
                    arguments
                ).AsTask());
            }
            catch (ErlangException error)
            {
                caught = true;
                Require(error.Reason.Equals(Term.Tuple(Term.A(OrdsetListsDispatchHostNames.ReplacementFailure), Term.A(operation))));
                TupleTerm[] frames = Cons.Items(error.StackTraceTerm).OfType<TupleTerm>().ToArray();
                string target = operation == OrdsetListsDispatchHostNames.Fold ? OrdsetListsDispatchHostNames.FoldLeft : operation;
                TupleTerm? replacement = frames.FirstOrDefault(frame => Frame(frame, OrdsetListsDispatchHostNames.Lists, target));
                Require(replacement is not null);
                Require(Cons.Items(replacement!.Items[3]).Any(location => location.Equals(Term.Tuple(
                    Term.A(OrdsetListsDispatchHostNames.File),
                    Term.String(OrdsetListsDispatchHostNames.FailureFixture)
                ))));
                bool ordsetContinuation = frames.Any(frame => Frame(frame, OrdsetListsDispatchHostNames.Ordsets, operation));
                Require(ordsetContinuation == (operation is OrdsetListsDispatchHostNames.Map or OrdsetListsDispatchHostNames.FilterMap));
            }
            Require(caught);
        }
    }

    private static string[] Operations() =>
    [
        OrdsetListsDispatchHostNames.Fold,
        OrdsetListsDispatchHostNames.Filter,
        OrdsetListsDispatchHostNames.Map,
        OrdsetListsDispatchHostNames.FilterMap
    ];

    private static bool Frame(TupleTerm frame, string module, string function) => frame.Items.Count == 4
        && frame.Items[0].Equals(Term.A(module)) && frame.Items[1].Equals(Term.A(function));

    private static async Task<string> Read(string name)
    {
        var assembly = typeof(OrdsetListsDispatchHostChecks).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(value => value.EndsWith(name, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);

        return await reader.ReadToEndAsync();
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(OrdsetListsDispatchHostNames.Failure);
    }
}
