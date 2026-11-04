namespace Erlang;

internal static class CodeSource
{
    internal static ErlangException InvalidModuleStatus(ProcessContext context, Term module, int comprehensionDepth)
    {
        ErlangException error = Capture(
            context,
            CodeApiNames.IsLoaded,
            [module],
            Term.A(ErlangErrorReasons.FunctionClause),
            CodeSourceLocations.IsLoadedClause
        );
        var frames = Cons.Items(error.StackTraceTerm).ToList();

        Term Continuation(string function, int line)
            => Term.Tuple(
                Term.A(CodeModule.Name),
                Term.A(function),
                Term.I(2),
                Term.List(
                    Term.Tuple(Term.A(CodeSourceFields.File), Term.String(CodeSourceFields.FileName)),
                    Term.Tuple(Term.A(CodeSourceFields.Line), Term.I(line))
                )
            );
        frames.Insert(1, Continuation(CodeApiNames.ModuleStatus, CodeSourceLocations.ModuleStatusLoadedCall));
        for (int i = 0; i < Math.Min(comprehensionDepth, context.Runtime.BacktraceDepth); i++)
        {
            frames.Insert(2, Continuation(CodeSourceFunctions.ModuleStatusListComprehension, CodeSourceLocations.ModuleStatusListCall));
        }

        return new ErlangException(ErlangErrorReasons.FunctionClause)
            .WithCapturedStack(Cons.From(frames.Take(Math.Max(CodeSourceLocations.MinimumBacktraceFrames, context.Runtime.BacktraceDepth))));
    }

    internal static ErlangException Partition(
        ProcessContext context,
        string operation,
        Term rest,
        Term binaries,
        Term modules
    )
    {
        int line = operation == CodeApiNames.AtomicLoad ? CodeSourceLocations.AtomicLoadPreparationCall : CodeSourceLocations.PrepareLoadingPreparationCall;
        ErlangException error = Capture(
            context,
            operation,
            [],
            Term.A(ErlangErrorReasons.FunctionClause),
            line
        );
        var frames = Cons.Items(error.StackTraceTerm).ToList();

        Term Frame(string function, Term arguments, int location)
            => Term.Tuple(
                Term.A(CodeModule.Name),
                Term.A(function),
                arguments,
                Term.List(
                    Term.Tuple(Term.A(CodeSourceFields.File), Term.String(CodeSourceFields.FileName)),
                    Term.Tuple(Term.A(CodeSourceFields.Line), Term.I(location))
                )
            );
        frames[0] = Frame(operation, Term.I(1), line);
        frames.Insert(0, Frame(CodeSourceFunctions.PrepareLoading, Term.I(1), CodeSourceLocations.PartitionLoadCall));
        frames.Insert(0, Frame(CodeSourceFunctions.PartitionLoad, Term.List(rest, binaries, modules), CodeSourceLocations.PartitionLoadClause));

        return new ErlangException(ErlangErrorReasons.FunctionClause)
            .WithCapturedStack(Cons.From(frames.Take(Math.Max(CodeSourceLocations.MinimumBacktraceFrames, context.Runtime.BacktraceDepth))));
    }

    internal static ErlangException Clause(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        int arity = arguments.Count;
        if (arity == 1 && function is CodeApiNames.SetPath or CodeApiNames.AddPath or CodeApiNames.AddPathFirst or CodeApiNames.AddPathLast
            or CodeApiNames.AddPaths or CodeApiNames.AddPathsFirst or CodeApiNames.AddPathsLast)
        {
            arguments = [arguments[0], Term.A(CodePathNames.NoCache)];
        }
        if (function == CodeApiNames.ReplacePath && arity == 2)
        {
            arguments = [arguments[0], arguments[1], Term.A(CodePathNames.NoCache)];
        }
        int line = function switch
        {
            CodeApiNames.LoadFile => CodeSourceLocations.LoadFileClause,
            CodeApiNames.EnsureLoaded => CodeSourceLocations.EnsureLoadedClause,
            CodeApiNames.LoadAbsolute => arity == 1 ? CodeSourceLocations.LoadAbsoluteClause : CodeSourceLocations.LoadAbsoluteNamedClause,
            CodeApiNames.LoadBinary => CodeSourceLocations.LoadBinaryClause,
            CodeApiNames.Delete => CodeSourceLocations.DeleteClause,
            CodeApiNames.Purge => CodeSourceLocations.PurgeClause,
            CodeApiNames.SoftPurge => CodeSourceLocations.SoftPurgeClause,
            CodeApiNames.IsLoaded => CodeSourceLocations.IsLoadedClause,
            CodeApiNames.GetObjectCode => CodeSourceLocations.GetObjectCodeClause,
            CodeApiNames.LibraryDirectory => arity == 1 ? CodeSourceLocations.LibraryDirectoryClause : CodeSourceLocations.LibrarySubdirectoryClause,
            CodeApiNames.PrivateDirectory => CodeSourceLocations.PrivateDirectoryClause,
            CodeApiNames.StickDir => CodeSourceLocations.StickDirectoryClause,
            CodeApiNames.UnstickDir => CodeSourceLocations.UnstickDirectoryClause,
            CodeApiNames.StickModule => CodeSourceLocations.StickModuleClause,
            CodeApiNames.UnstickModule => CodeSourceLocations.UnstickModuleClause,
            CodeApiNames.IsSticky => CodeSourceLocations.IsStickyClause,
            CodeApiNames.SetPath => CodeSourceLocations.SetPathClause,
            CodeApiNames.AddPath => CodeSourceLocations.AddPathClause,
            CodeApiNames.AddPathLast => CodeSourceLocations.AddPathLastClause,
            CodeApiNames.AddPathFirst => CodeSourceLocations.AddPathFirstClause,
            CodeApiNames.AddPaths => CodeSourceLocations.AddPathsClause,
            CodeApiNames.AddPathsLast => CodeSourceLocations.AddPathsLastClause,
            CodeApiNames.AddPathsFirst => CodeSourceLocations.AddPathsFirstClause,
            CodeApiNames.DeletePath => CodeSourceLocations.DeletePathClause,
            CodeApiNames.DeletePaths => CodeSourceLocations.DeletePathsClause,
            CodeApiNames.ReplacePath => CodeSourceLocations.ReplacePathClause,
            CodeApiNames.EnsureModulesLoaded => CodeSourceLocations.EnsureModulesLoadedClause,
            CodeApiNames.AtomicLoad => CodeSourceLocations.AtomicLoadInvalidItems,
            CodeApiNames.PrepareLoading => CodeSourceLocations.PrepareLoadingInvalidItems,
            CodeApiNames.FinishLoading => CodeSourceLocations.FinishLoadingClause,
            CodeApiNames.Which => CodeSourceLocations.WhichClause,
            CodeApiNames.WhereIsFile => arity == 1 ? CodeSourceLocations.WhereIsFileClause : CodeSourceLocations.WhereIsFileSearchClause,
            CodeApiNames.GetDocumentation => arity == 1 ? CodeSourceLocations.GetDocumentationClause : CodeSourceLocations.GetDocumentationOptionsClause,
            _ => CodeSourceLocations.UnknownLine
        };

        return Capture(
            context,
            function,
            arguments,
            Term.A(ErlangErrorReasons.FunctionClause),
            line
        );
    }

    internal static ErlangException Capture(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        Term reason,
        int line
    )
    {
        var frames = new List<Term>
        {
            Term.Tuple(
                Term.A(CodeModule.Name),
                Term.A(function),
                Cons.From(arguments),
                Term.List(
                    Term.Tuple(Term.A(CodeSourceFields.File), Term.String(CodeSourceFields.FileName)),
                    Term.Tuple(Term.A(CodeSourceFields.Line), Term.I(line))
                )
            )
        };
        ErlangExecutionFrame? caller = context.CaptureExecutionChain();
        object? previous = null;
        while (caller is not null && frames.Count < Math.Max(CodeSourceLocations.MinimumBacktraceFrames, context.Runtime.BacktraceDepth))
        {
            if (caller.ContinuationIdentity is { } identity && ReferenceEquals(previous, identity))
            {
                caller = caller.Parent;
                continue;
            }
            frames.Add(Term.Tuple(
                Term.A(caller.Module),
                Term.A(caller.Function),
                caller.HostHandlerArguments is { } args ? Cons.From(args) : Term.I(caller.Arity),
                caller.HostHandlerArguments is not null ? Nil.Value : caller.SourceLocation ?? Nil.Value
            ));
            previous = caller.ContinuationIdentity;
            caller = caller.Parent;
        }

        return new ErlangException(reason).WithCapturedStack(Cons.From(frames));
    }
}
