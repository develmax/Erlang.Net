// Ported from OTP-29.1.1 code.erl (Apache-2.0); original notices retained.
namespace Erlang;

public static partial class CodeModule
{
    private static async ValueTask<Term> Paths(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        Term input = arguments[0];
        Term cache = arguments.Count == 1 ? Term.A(CodePathNames.NoCache) : arguments[1];
        if (!IsList(input) || !IsCache(cache))
        {
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        }
        bool single = function is CodeApiNames.AddPath or CodeApiNames.AddPathFirst or CodeApiNames.AddPathLast;
        Term rest = single ? Term.List(input) : input;
        var normalized = new List<Term>();
        bool valid = true;
        while (rest is Cons cell)
        {
            if (cell.Head is Atom atom)
            {
                normalized.Add(Term.String(atom.Name));
            }
            else if (IsList(cell.Head) && IntegerList(cell.Head))
            {
                normalized.Add(await Call(
                    context,
                    CodeLibraryDependencies.Filename,
                    CodeFilenameFunctions.Join,
                    Term.List(cell.Head)
                ));
            }
            else
            {
                valid = false;
            }
            rest = cell.Tail;
        }
        if (rest is not Nil)
        {
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        }
        if (function == CodeApiNames.SetPath)
        {
            return valid ? await Server(context, Request(function, Cons.From(normalized), cache)) : Error(CodePathNames.BadDirectory);
        }
        if (single && normalized.Count != 1)
        {
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), Term.Tuple(Term.A(CodeResultAtoms.Error), Nil.Value)));
        }
        Term position = Term.A(function is CodeApiNames.AddPathFirst or CodeApiNames.AddPathsFirst ? CodePathNames.First : CodePathNames.Last);

        return await Server(
            context,
            Request(
                single ? CodeApiNames.AddPath : CodeApiNames.AddPaths,
                position,
                single ? normalized[0] : Cons.From(normalized),
                cache
            )
        );
    }

    private static async ValueTask<Term> Where(ProcessContext context, Term paths, Term file)
    {
        while (paths is Cons cell)
        {
            if (cell.Head is TupleTerm { Items.Count: 2 } cached)
            {
                if (Cons.Items(cached.Items[1]).Any(item => item.Equals(file)))
                {
                    return await Call(
                        context,
                        CodeLibraryDependencies.Filename,
                        CodeFilenameFunctions.Append,
                        cached.Items[0],
                        file
                    );
                }
            }
            else
            {
                Term full = await Call(
                    context,
                    CodeLibraryDependencies.Filename,
                    CodeFilenameFunctions.Append,
                    cell.Head,
                    file
                );
                if (await Call(
                    context,
                    CodeLibraryDependencies.Loader,
                    CodeLoaderFunctions.ReadFileInfo,
                    full
                ) is TupleTerm tuple
                    && tuple.Items[0].Equals(Term.A(CodeResultAtoms.Ok)))
                {
                    return full;
                }
            }
            paths = cell.Tail;
        }
        if (paths is not Nil)
        {
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        }

        return Term.A(CodePathNames.NonExisting);
    }

    private static async ValueTask<Term> Available(ProcessContext context)
    {
        var available = new Dictionary<Term, Term>();
        Term mode = await Call(context, CodeLibraryDependencies.Server, CodeServerProtocolNames.GetMode);
        if (mode.Equals(Term.A(CodeServerModes.Interactive)))
        {
            foreach (Term path in Cons.Items(await Server(context, Term.A(CodeServerProtocolNames.GetPath))))
            {
                Term files = await Call(
                    context,
                    CodeLibraryDependencies.Loader,
                    CodeLoaderFunctions.ListDir,
                    path
                );
                if (files is not TupleTerm pair || !pair.Items[0].Equals(Term.A(CodeResultAtoms.Ok)))
                {
                    continue;
                }
                foreach (Term file in Cons.Items(pair.Items[1]))
                {
                    if ((await Call(
                        context,
                        CodeLibraryDependencies.Filename,
                        CodeFilenameFunctions.Extension,
                        file
                    )).Equals(Term.String(CodeObjectFileNames.Extension))
                        && !available.ContainsKey(file))
                    {
                        available.Add(file, path);
                    }
                }
            }
        }
        var rows = new Dictionary<Term, Term>();
        foreach (var entry in available)
        {
            Term module = await Call(
                context,
                CodeLibraryDependencies.Filename,
                CodeFilenameFunctions.Rootname,
                entry.Key
            );
            rows[module] = Term.Tuple(
                module,
                await Call(
                    context,
                    CodeLibraryDependencies.Filename,
                    CodeFilenameFunctions.Append,
                    entry.Value,
                    entry.Key
                ),
                CoreModules.Boolean(false)
            );
        }
        foreach (TupleTerm loaded in Cons.Items(await Server(context, Term.A(CodeServerProtocolNames.AllLoaded))))
        {
            Term name = Term.String(((Atom)loaded.Items[0]).Name);
            rows[name] = Term.Tuple(name, loaded.Items[1], CoreModules.Boolean(true));
        }

        return Cons.From(rows.OrderBy(entry => entry.Key, CodeTermComparer.Instance).Select(entry => entry.Value));
    }

    private static async ValueTask<Term> Status(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term input = arguments.Count == 0
            ? Cons.From(Cons.Items(await Server(context, Term.A(CodeServerProtocolNames.AllLoaded))).Cast<TupleTerm>().Select(row => row.Items[0]))
            : arguments[0];
        Term paths = await Server(context, Term.A(CodeServerProtocolNames.GetPath));
        if (!IsList(input))
        {
            return await ModuleStatus(context, input, paths);
        }
        var cache = new List<Term>();
        foreach (Term path in Cons.Items(paths))
        {
            Term result = await Call(
                context,
                CodeLibraryDependencies.Loader,
                CodeLoaderFunctions.ListDir,
                path
            );
            if (result is TupleTerm pair && pair.Items[0].Equals(Term.A(CodeResultAtoms.Ok)))
            {
                cache.Add(Term.Tuple(path, pair.Items[1]));
            }
        }
        var rows = new List<Term>();
        foreach (Term module in Cons.Items(input))
        {
            rows.Add(Term.Tuple(module, await ModuleStatus(
                context,
                module,
                Cons.From(cache),
                rows.Count + 1
            )));
        }

        return Cons.From(rows);
    }

    private static async ValueTask<Term> ModuleStatus(
        ProcessContext context,
        Term module,
        Term paths,
        int comprehensionDepth = 0
    )
    {
        if (module is not Atom atom)
        {
            throw CodeSource.InvalidModuleStatus(context, module, comprehensionDepth);
        }
        Term loaded = await Call(
            context,
            CodeLibraryDependencies.Server,
            CodeServerProtocolNames.IsLoaded,
            module
        );
        if (loaded.Equals(CoreModules.Boolean(false)))
        {
            return Term.A(CodeModuleStatusNames.NotLoaded);
        }
        Term origin = ((TupleTerm)loaded).Items[1];
        if (origin.Equals(Term.A(CodeLoadedModuleNames.Preloaded)) || origin is Nil)
        {
            return Term.A(CodeModuleStatusNames.Loaded);
        }
        Term path = await Where(context, paths, Term.String(atom.Name + CodeObjectFileNames.Extension));
        if (path.Equals(Term.A(CodePathNames.NonExisting)))
        {
            return Term.A(CodeModuleStatusNames.Removed);
        }
        if (origin.Equals(Term.A(CodeLoadedModuleNames.CoverCompiled)))
        {
            return Term.A(CodeModuleStatusNames.Modified);
        }
        Term md5 = await BeamMd5(context, path);
        if (md5.Equals(Term.A(CodeObjectFileNames.UndefinedDigest)))
        {
            Term found = await ObjectCode(context, module);
            if (found is TupleTerm row)
            {
                md5 = await BeamMd5(context, row.Items[1]);
            }
        }
        Term current = await Call(
            context,
            CodeLibraryDependencies.Erlang,
            CodeRuntimeFunctions.GetModuleInfo,
            module,
            Term.A(CodeObjectFileNames.ModuleMd5Key)
        );

        return Term.A(current.Equals(md5) ? CodeModuleStatusNames.Loaded : CodeModuleStatusNames.Modified);
    }

    private static async ValueTask<Term> BeamMd5(ProcessContext context, Term file)
    {
        Term result = await Call(
            context,
            CodeLibraryDependencies.BeamLibrary,
            CodeDependencyFunctions.BeamMd5,
            file
        );

        return result is TupleTerm pair && pair.Items[0].Equals(Term.A(CodeResultAtoms.Ok))
            && pair.Items[1] is TupleTerm payload ? payload.Items[1] : Term.A(CodeObjectFileNames.UndefinedDigest);
    }

    private static async ValueTask<Term> Fingerprint(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        Term bytes = arguments[0];
        bool form = bytes is BitString { IsBinary: true } binary && binary.ToArray().AsSpan().StartsWith(CodeObjectFileNames.FormId);
        if (!form)
        {
            try
            {
                bytes = await Call(
                    context,
                    CodeLibraryDependencies.Zlib,
                    CodeDependencyFunctions.ZlibGunzip,
                    bytes
                );
            }
            catch (ErlangException)
            {
                // The original deliberately passes the unchanged argument to the native primitive.
            }
        }

        try
        {
            return function == CodeApiNames.GetChunk
                ? await Call(
                    context,
                    CodeLibraryDependencies.Internal,
                    CodeDependencyFunctions.InternalBeamFileChunk,
                    bytes,
                    arguments[1]
                )
                : await Call(
                    context,
                    CodeLibraryDependencies.Internal,
                    CodeDependencyFunctions.InternalBeamFileMd5,
                    bytes
                );
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error)
        {
            throw CodeSource.Capture(
                context,
                function,
                function == CodeApiNames.GetChunk ? [bytes, arguments[1]] : [bytes],
                error.Reason,
                function == CodeApiNames.GetChunk ? CodeSourceLocations.GetChunkError : CodeSourceLocations.ModuleMd5Error
            );
        }
    }

    private static async ValueTask<Term> Clash(ProcessContext context)
    {
        var files = new List<(Term Path, Term File)>();
        foreach (Term path in Cons.Items(await Server(context, Term.A(CodeServerProtocolNames.GetPath))))
        {
            Term result = await Call(
                context,
                CodeLibraryDependencies.Loader,
                CodeLoaderFunctions.ListDir,
                path
            );
            if (result is not TupleTerm pair || !pair.Items[0].Equals(Term.A(CodeResultAtoms.Ok)))
            {
                await Call(
                    context,
                    CodeLibraryDependencies.Io,
                    CodeDependencyFunctions.IoFormat,
                    Term.String(CodeClashMessages.UnreadablePath),
                    Term.List(path)
                );
                continue;
            }
            foreach (Term file in Cons.Items(pair.Items[1]))
            {
                Term suffix = await Call(
                    context,
                    CodeLibraryDependencies.Filename,
                    CodeFilenameFunctions.Extension,
                    file
                );
                if (suffix.Equals(Term.String(CodeObjectFileNames.Extension)))
                {
                    files.Add((path, file));
                }
            }
        }
        int clashes = 0;
        for (int i = 0; i < files.Count; i++)
        {
            for (int j = i + 1; j < files.Count; j++)
            {
                if (!files[i].File.Equals(files[j].File))
                {
                    continue;
                }
                await Call(
                    context,
                    CodeLibraryDependencies.Io,
                    CodeDependencyFunctions.IoFormat,
                    Term.String(CodeClashMessages.HiddenModule),
                    Term.List(
                        await Call(
                            context,
                            CodeLibraryDependencies.Filename,
                            CodeFilenameFunctions.Join,
                            files[i].Path,
                            files[i].File
                        ),
                        await Call(
                            context,
                            CodeLibraryDependencies.Filename,
                            CodeFilenameFunctions.Join,
                            files[j].Path,
                            files[j].File
                        )
                    )
                );
                clashes++;
                break;
            }
        }

        return await Call(
            context,
            CodeLibraryDependencies.Io,
            CodeDependencyFunctions.IoFormat,
            Term.String(CodeClashMessages.Count),
            Term.List(Term.I(clashes))
        );
    }

    private static async ValueTask<Term> Start(ProcessContext context)
    {
        Term cache = await Call(
            context,
            CodeLibraryDependencies.Init,
            CodeDependencyFunctions.InitGetArgument,
            Term.A(CodeStartupNames.PathCacheArgument)
        );
        if (cache is TupleTerm)
        {
            await Call(
                context,
                CodeLibraryDependencies.ErrorLogger,
                CodeDependencyFunctions.ErrorLoggerWarningReport,
                Term.String(CodeStartupNames.RemovedCacheWarning)
            );
        }
        foreach (string dependency in new[] { CodeLibraryDependencies.BeamLibrary, CodeLibraryDependencies.Binary, CodeLibraryDependencies.Ets, CodeLibraryDependencies.Filename, CodeLibraryDependencies.OrderedSets, CodeLibraryDependencies.OrderedTrees, CodeLibraryDependencies.Lists, CodeLibraryDependencies.Os, CodeLibraryDependencies.Unicode })
        {
            await Call(
                context,
                dependency,
                CodeDependencyFunctions.ModuleInformation,
                Term.A(CodeStartupNames.ModuleInformationKey)
            );
        }
        await Call(context, CodeLibraryDependencies.Features, CodeDependencyFunctions.EnabledFeatures);
        var rootArgument = (TupleTerm)await Call(
            context,
            CodeLibraryDependencies.Init,
            CodeDependencyFunctions.InitGetArgument,
            Term.A(CodeStartupNames.RootArgument)
        );
        Term root = ((Cons)((Cons)rootArgument.Items[1]).Head).Head;
        root = await Call(
            context,
            CodeLibraryDependencies.Filename,
            CodeFilenameFunctions.Join,
            Term.List(root)
        );
        Term mode = Term.A(CodeServerModes.Interactive);
        Term modeArgument = await Call(
            context,
            CodeLibraryDependencies.Init,
            CodeDependencyFunctions.InitGetArgument,
            Term.A(CodeStartupNames.ModeArgument)
        );
        if (modeArgument is TupleTerm modes && modes.Items[1] is Cons first)
        {
            if (first.Tail is not Nil
                && (await Call(
                    context,
                    CodeLibraryDependencies.Logger,
                    CodeDependencyFunctions.LoggerAllow,
                    Term.A(CodeStartupNames.WarningLevel),
                    Term.A(Name)
                )).Equals(CoreModules.Boolean(true)))
            {
                await Call(
                    context,
                    CodeLibraryDependencies.Logger,
                    CodeDependencyFunctions.LoggerMacroLog,
                    new MapTerm([
                    new KeyValuePair<Term, Term>(Term.A(CodeStartupNames.MfaMetadata), Term.Tuple(Term.A(Name), Term.A(CodeStartupNames.StartGetModeFunction), Term.I(0))),
                    new KeyValuePair<Term, Term>(Term.A(CodeSourceFields.Line), Term.I(CodeSourceLocations.MultipleModesWarning)),
                    new KeyValuePair<Term, Term>(Term.A(CodeSourceFields.File), Term.String(CodeSourceFields.FileName))
                ]),
                    Term.A(CodeStartupNames.WarningLevel),
                    Term.String(CodeStartupNames.MultipleModesWarning),
                    Term.List(first.Head)
                );
            }
            if (first.Head.Equals(Term.List(Term.String(CodeServerModes.Embedded))))
            {
                mode = Term.A(CodeServerModes.Embedded);
            }
        }
        Term result = await Call(
            context,
            CodeLibraryDependencies.Server,
            CodeServerProtocolNames.StartLink,
            Term.List(root, mode)
        );
        Term nostick = await Call(
            context,
            CodeLibraryDependencies.Init,
            CodeDependencyFunctions.InitGetArgument,
            Term.A(CodeStartupNames.NoStickArgument)
        );
        if (mode.Equals(Term.A(CodeServerModes.Interactive)) && !nostick.Equals(Term.Tuple(Term.A(CodeResultAtoms.Ok), Term.List(Nil.Value))))
        {
            foreach (string library in new[] { CodeStartupNames.CompilerApplication, CodeStartupNames.StdlibApplication, CodeStartupNames.KernelApplication })
            {
                Term directory = await Server(context, Request(CodeServerProtocolNames.Dir, Request(CodeServerProtocolNames.LibDir, Term.A(library))));
                if (!IsError(directory))
                {
                    await Server(
                        context,
                        Request(
                            CodeServerProtocolNames.StickDir,
                            await Call(
                                context,
                                CodeLibraryDependencies.Filename,
                                CodeFilenameFunctions.Append,
                                directory,
                                Term.String(CodeStartupNames.EbinDirectory)
                            )
                        )
                    );
                }
            }
        }

        return result;
    }
}
