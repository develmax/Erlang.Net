// Derived from lib/kernel/src/code.erl, Erlang/OTP 29.1.1, Apache-2.0.
// Copyright Ericsson AB 1996-2026. See the retained upstream source and licenses.
namespace Erlang;

/// <summary>Managed code interface using the original code_server process protocol.</summary>
public static partial class CodeModule
{
    public const string Name = CodeLibraryDependencies.Code;
    public static IReadOnlyList<string> RequiredDependencies { get; } = Array.AsReadOnly(LibrarySelectionNames.CodeDependencies);

    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (CodeApiNames.ObjectFileExtension, 0), (CodeApiNames.SetPath, 1), (CodeApiNames.SetPath, 2), (CodeApiNames.GetPath, 0),
        (CodeApiNames.LoadFile, 1), (CodeApiNames.EnsureLoaded, 1), (CodeApiNames.EnsureModulesLoaded, 1),
        (CodeApiNames.LoadAbsolute, 1), (CodeApiNames.LoadAbsolute, 2), (CodeApiNames.LoadBinary, 3), (CodeApiNames.AtomicLoad, 1),
        (CodeApiNames.PrepareLoading, 1), (CodeApiNames.FinishLoading, 1), (CodeApiNames.Delete, 1), (CodeApiNames.Purge, 1),
        (CodeApiNames.SoftPurge, 1), (CodeApiNames.IsLoaded, 1), (CodeApiNames.AllLoaded, 0), (CodeApiNames.AllAvailable, 0),
        (CodeApiNames.Stop, 0), (CodeApiNames.RootDirectory, 0), (CodeApiNames.LibraryDirectory, 0), (CodeApiNames.LibraryDirectory, 1), (CodeApiNames.LibraryDirectory, 2),
        (CodeApiNames.CompilerDirectory, 0), (CodeApiNames.PrivateDirectory, 1), (CodeApiNames.StickDir, 1), (CodeApiNames.UnstickDir, 1),
        (CodeApiNames.StickModule, 1), (CodeApiNames.UnstickModule, 1), (CodeApiNames.IsSticky, 1), (CodeApiNames.GetObjectCode, 1),
        (CodeApiNames.AddPaths, 1), (CodeApiNames.AddPaths, 2), (CodeApiNames.AddPath, 1), (CodeApiNames.AddPath, 2),
        (CodeApiNames.AddPathsFirst, 1), (CodeApiNames.AddPathsFirst, 2), (CodeApiNames.AddPathsLast, 1), (CodeApiNames.AddPathsLast, 2),
        (CodeApiNames.AddPathFirst, 1), (CodeApiNames.AddPathFirst, 2), (CodeApiNames.AddPathLast, 1), (CodeApiNames.AddPathLast, 2),
        (CodeApiNames.DeletePath, 1), (CodeApiNames.DeletePaths, 1), (CodeApiNames.ClearCache, 0),
        (CodeApiNames.ReplacePath, 2), (CodeApiNames.ReplacePath, 3), (CodeApiNames.StartLink, 0), (CodeApiNames.Which, 1),
        (CodeApiNames.GetDocumentation, 1), (CodeApiNames.GetDocumentation, 2), (CodeApiNames.WhereIsFile, 1), (CodeApiNames.WhereIsFile, 2),
        (CodeApiNames.Clash, 0), (CodeApiNames.ModuleStatus, 0), (CodeApiNames.ModuleStatus, 1), (CodeApiNames.ModifiedModules, 0),
        (CodeApiNames.GetMode, 0), (CodeApiNames.GetDebugInfo, 1), (CodeApiNames.CoverageSupport, 0),
        (CodeApiNames.GetCoverage, 2), (CodeApiNames.GetCoverageMode, 0), (CodeApiNames.GetCoverageMode, 1),
        (CodeApiNames.ResetCoverage, 1), (CodeApiNames.SetCoverageMode, 1), (CodeApiNames.GetChunk, 2), (CodeApiNames.ModuleMd5, 1)
    });

    public static void Register(ModuleRegistry registry)
    {
        registry.InstallManagedCodePrimitives();
        registry.InstallSourceWrappers(target =>
        {
            foreach (var export in Exports)
            {
                if (IsNative(export.Name))
                {
                    continue;
                }
                string function = export.Name;
                target.RegisterReviewedManagedLibrary789(
                    Name,
                    function,
                    export.Arity,
                    (context, arguments) => Invoke(context, function, arguments)
                );
            }
        });
    }

    private static bool IsNative(string function)
        => function is CodeApiNames.CoverageSupport or CodeApiNames.GetCoverage or CodeApiNames.GetCoverageMode
            or CodeApiNames.ResetCoverage or CodeApiNames.SetCoverageMode or CodeApiNames.GetDebugInfo;

    internal static ValueTask<Term> Call(
        ProcessContext context,
        string module,
        string function,
        params Term[] arguments
    )
        => context.Runtime.Modules.Call(
            context,
            module,
            function,
            arguments
        );

    private static ValueTask<Term> Server(ProcessContext context, Term request)
        => Call(
            context,
            CodeLibraryDependencies.Server,
            CodeServerProtocolNames.Call,
            request
        );

    private static Term Request(string operation, params Term[] arguments)
        => Term.Tuple(new[] { Term.A(operation) }.Concat(arguments).ToArray());

    private static bool IsList(Term term) => term is Cons or Nil;

    private static bool IsName(Term term) => term is Atom || IsList(term);

    private static bool IsCache(Term term) => term is Atom { Name: CodePathNames.Cache or CodePathNames.NoCache };

    private static Term Error(string reason) => Term.Tuple(Term.A(CodeResultAtoms.Error), Term.A(reason));

    private static bool IsError(Term term) => term is TupleTerm { Items.Count: 2 } tuple
        && tuple.Items[0].Equals(Term.A(CodeResultAtoms.Error));

    private static bool IntegerList(Term term)
    {
        while (term is Cons cell)
        {
            if (cell.Head is not Integer)
            {
                return false;
            }
            term = cell.Tail;
        }

        return term is Nil;
    }

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        using var tail = context.EnterSourceTailCall(Name, function);
        try
        {
            return await Dispatch(context, function, arguments);
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
            && !error.AutomaticStackTraceCaptured && !error.ExplicitStackTrace
            && error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
        {
            throw CodeSource.Clause(context, function, arguments);
        }
    }

    private static async ValueTask<Term> Dispatch(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        if (!Exports.Contains((function, arguments.Count)))
        {
            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
        if (IsNative(function))
        {
            return await Call(
                context,
                Name,
                function,
                arguments.ToArray()
            );
        }
        Term a = arguments.Count > 0 ? arguments[0] : Nil.Value;
        Term b = arguments.Count > 1 ? arguments[1] : Nil.Value;
        switch (function)
        {
            case CodeApiNames.ObjectFileExtension:
                return await Call(context, CodeLibraryDependencies.Init, function);
            case CodeApiNames.GetMode:
                return await Call(context, CodeLibraryDependencies.Server, function);
            case CodeApiNames.GetPath:
            case CodeApiNames.AllLoaded:
            case CodeApiNames.Stop:
            case CodeApiNames.ClearCache:
                return await Server(context, Term.A(function));
            case CodeApiNames.RootDirectory:
            case CodeApiNames.CompilerDirectory:
                return await Server(context, Request(CodeServerProtocolNames.Dir, Term.A(function)));
            case CodeApiNames.LibraryDirectory when arguments.Count == 0:
                return await Server(context, Request(CodeServerProtocolNames.Dir, Term.A(function)));
            case CodeApiNames.LibraryDirectory when arguments.Count == 1 && IsName(a):
            case CodeApiNames.PrivateDirectory when IsName(a):
                return await Server(context, Request(CodeServerProtocolNames.Dir, Request(function, a)));
            case CodeApiNames.LibraryDirectory when arguments.Count == 2 && a is Atom && b is Atom:
                return await Server(context, Request(CodeServerProtocolNames.Dir, Request(function, a, b)));
            case CodeApiNames.Delete when a is Atom:
            case CodeApiNames.Purge when a is Atom:
            case CodeApiNames.SoftPurge when a is Atom:
            case CodeApiNames.StickModule when a is Atom:
            case CodeApiNames.UnstickModule when a is Atom:
            case CodeApiNames.StickDir when IsList(a):
            case CodeApiNames.UnstickDir when IsList(a):
            case CodeApiNames.DeletePath when IsName(a):
            case CodeApiNames.DeletePaths when IsList(a):
            case CodeApiNames.WhereIsFile when arguments.Count == 1 && IsList(a):
                return await Server(context, Request(function, a));
            case CodeApiNames.IsLoaded when a is Atom:
            case CodeApiNames.IsSticky when a is Atom:
                return await Call(
                    context,
                    CodeLibraryDependencies.Server,
                    function,
                    a
                );
            case CodeApiNames.GetObjectCode when a is Atom:
                return await ObjectCode(context, a);
            case CodeApiNames.LoadFile when a is Atom:
                return await LoadFile(context, a);
            case CodeApiNames.EnsureLoaded when a is Atom:
                return await EnsureLoaded(context, a);
            case CodeApiNames.LoadAbsolute when IsName(a) && (arguments.Count == 1 || b is Atom):
                return await LoadAbsolute(context, a, arguments.Count == 1 ? null : b);
            case CodeApiNames.LoadBinary when a is Atom && IsName(b) && arguments[2] is BitString { IsBinary: true }:
                return b is Atom || IntegerList(b)
                    ? await Load(
                        context,
                        a,
                        b,
                        arguments[2],
                        true
                    )
                    : Error(ErlangErrorReasons.BadArgument);
            case CodeApiNames.SetPath:
            case CodeApiNames.AddPath:
            case CodeApiNames.AddPathFirst:
            case CodeApiNames.AddPathLast:
            case CodeApiNames.AddPaths:
            case CodeApiNames.AddPathsFirst:
            case CodeApiNames.AddPathsLast:
                return await Paths(context, function, arguments);
            case CodeApiNames.ReplacePath:
                Term cache = arguments.Count == 2 ? Term.A(CodePathNames.NoCache) : arguments[2];
                if (IsName(a) && IsName(b) && IsCache(cache))
                {
                    return await Server(context, Request(
                        function,
                        a,
                        b,
                        cache
                    ));
                }
                break;
            case CodeApiNames.Which when a is Atom:
                Term loaded = await Call(
                    context,
                    CodeLibraryDependencies.Server,
                    CodeServerProtocolNames.IsLoaded,
                    a
                );

                return loaded is TupleTerm loadedFile ? loadedFile.Items[1]
                    : await Server(context, Request(CodeApiNames.WhereIsFile, Term.String(((Atom)a).Name + CodeObjectFileNames.Extension)));
            case CodeApiNames.WhereIsFile when arguments.Count == 2:
                return await Where(context, a, b);
            case CodeApiNames.AllAvailable:
                return await Available(context);
            case CodeApiNames.ModuleStatus:
                return await Status(context, arguments);
            case CodeApiNames.ModifiedModules:
                return Cons.From(Cons.Items(await Status(context, [])).Cast<TupleTerm>()
                    .Where(row => row.Items[1].Equals(Term.A(CodeModuleStatusNames.Modified))).Select(row => row.Items[0]));
            case CodeApiNames.GetChunk:
            case CodeApiNames.ModuleMd5:
                return await Fingerprint(context, function, arguments);
            case CodeApiNames.EnsureModulesLoaded:
                return await EnsureModules(context, a);
            case CodeApiNames.AtomicLoad:
            case CodeApiNames.PrepareLoading:
                Term prepared = await Prepare(context, a, function);
                if (IsError(prepared))
                {
                    return prepared;
                }

                return function == CodeApiNames.AtomicLoad ? await Finish(context, prepared, false)
                    : Term.Tuple(Term.A(CodeResultAtoms.Ok), Request(CodeLoadingNames.PreparedEnvelope, prepared));
            case CodeApiNames.FinishLoading:
                return await FinishPublic(context, a);
            case CodeApiNames.GetDocumentation:
                if (arguments.Count == 1 && a is not Atom)
                {
                    throw new ErlangException(ErlangErrorReasons.FunctionClause);
                }

                return await Documentation(
                    context,
                    a,
                    arguments.Count == 1
                    ? new MapTerm([new KeyValuePair<Term, Term>(
                        Term.A(CodeDocumentationNames.Sources),
                        Term.List(Term.A(CodeDocumentationNames.Eep48), Term.A(CodeDocumentationNames.DebugInfo))
                    )]) : b
                );
            case CodeApiNames.Clash:
                return await Clash(context);
            case CodeApiNames.StartLink:
                return await Start(context);
        }

        throw new ErlangException(ErlangErrorReasons.FunctionClause);
    }
}
