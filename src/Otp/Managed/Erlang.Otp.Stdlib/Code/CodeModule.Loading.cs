// Ported from OTP-29.1.1 code.erl (Apache-2.0); original notices retained.
namespace Erlang;

public static partial class CodeModule
{
    private static async ValueTask<Term> ObjectCode(ProcessContext context, Term module)
    {
        Term result = await Server(context, Request(CodeServerProtocolNames.GetObjectCode, module));
        if (result is TupleTerm { Items.Count: 3 } tuple && tuple.Items[1].Equals(Term.A(CodeDocumentationNames.Missing)))
        {
            Term read = await Call(
                context,
                CodeLibraryDependencies.Loader,
                CodeLoaderFunctions.ReadFile,
                tuple.Items[2]
            );

            return read is TupleTerm { Items.Count: 2 } pair && pair.Items[0].Equals(Term.A(CodeResultAtoms.Ok))
                ? Term.Tuple(tuple.Items[0], pair.Items[1], tuple.Items[2]) : Term.A(CodeResultAtoms.Error);
        }

        return result;
    }

    private static async ValueTask<Term> LoadFile(ProcessContext context, Term module)
    {
        Term found = await ObjectCode(context, module);

        return found is TupleTerm { Items.Count: 3 } tuple
            ? await Load(
                context,
                module,
                tuple.Items[2],
                tuple.Items[1],
                false
            ) : Error(CodeLoadingNames.NoFile);
    }

    private static async ValueTask<Term> Load(
        ProcessContext context,
        Term module,
        Term file,
        Term binary,
        bool purge
    )
    {
        Term prepared = await Call(
            context,
            CodeLibraryDependencies.Erlang,
            CodeRuntimeFunctions.PrepareLoading,
            module,
            binary
        );

        return IsError(prepared) ? prepared
            : await Server(
                context,
                Request(
                    CodeServerProtocolNames.LoadModule,
                    prepared,
                    module,
                    file,
                    CoreModules.Boolean(purge),
                    CoreModules.Boolean(false)
                )
            );
    }

    private static async ValueTask<Term> EnsureLoaded(ProcessContext context, Term module)
    {
        if ((await Call(
            context,
            CodeLibraryDependencies.Erlang,
            CodeRuntimeFunctions.ModuleLoaded,
            module
        )).Equals(Term.A(ErlangBooleanAtoms.True)))
        {
            return Request(CodeLoadingNames.ModuleResult, module);
        }
        if ((await Call(context, CodeLibraryDependencies.Server, CodeServerProtocolNames.GetMode)).Equals(Term.A(CodeServerModes.Embedded)))
        {
            return Error(CodeLoadingNames.EmbeddedModeError);
        }
        Term result = await Server(context, Request(CodeServerProtocolNames.GetObjectCodeForLoading, module));
        if (result is not TupleTerm { Items.Count: 3 } tuple)
        {
            return result;
        }
        Term binary = tuple.Items[0];
        if (binary.Equals(Term.A(CodeDocumentationNames.Missing)))
        {
            Term read = await Call(
                context,
                CodeLibraryDependencies.Loader,
                CodeLoaderFunctions.ReadFile,
                tuple.Items[1]
            );
            binary = read is TupleTerm pair && pair.Items[0].Equals(Term.A(CodeResultAtoms.Ok))
                ? pair.Items[1] : Error(CodeLoadingNames.NoFile);
        }
        Term prepared = IsError(binary) ? binary : await Call(
            context,
            CodeLibraryDependencies.Erlang,
            CodeRuntimeFunctions.PrepareLoading,
            module,
            binary
        );
        if (IsError(prepared))
        {
            await Server(context, Request(CodeServerProtocolNames.LoadError, module, tuple.Items[2]));

            return prepared;
        }
        Term loaded = await Server(context, Request(
            CodeServerProtocolNames.LoadOk,
            prepared,
            module,
            tuple.Items[1],
            tuple.Items[2]
        ));
        await Call(context, CodeLibraryDependencies.Erlang, CodeRuntimeFunctions.GarbageCollect);

        return loaded;
    }

    private static async ValueTask<Term> LoadAbsolute(ProcessContext context, Term file, Term? module)
    {
        module ??= await Call(
            context,
            CodeLibraryDependencies.Erlang,
            CodeRuntimeFunctions.ListToAtom,
            await Call(
                context,
                CodeLibraryDependencies.Filename,
                CodeFilenameFunctions.Basename,
                file
            )
        );
        if (file is not Atom && !IntegerList(file))
        {
            return Error(ErlangErrorReasons.BadArgument);
        }
        Term name = await Call(
            context,
            CodeLibraryDependencies.Lists,
            CodeDependencyFunctions.ListsConcat,
            Term.List(file, Term.String(CodeObjectFileNames.Extension))
        );
        name = await Call(
            context,
            CodeLibraryDependencies.Server,
            CodeDependencyFunctions.CodeServerAbsname,
            name
        );
        Term read = await Call(
            context,
            CodeLibraryDependencies.Loader,
            CodeLoaderFunctions.ReadFile,
            name
        );

        return read is TupleTerm pair && pair.Items[0].Equals(Term.A(CodeResultAtoms.Ok))
            ? await Load(
                context,
                module,
                name,
                pair.Items[1],
                false
            ) : Error(CodeLoadingNames.NoFile);
    }

    private static ValueTask<Term> Finish(ProcessContext context, Term prepared, bool ensure)
        => Server(context, Request(CodeServerProtocolNames.FinishLoading, prepared, CoreModules.Boolean(ensure)));

    private static async ValueTask<Term> FinishPublic(ProcessContext context, Term argument)
    {
        if (argument is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(Term.A(CodeLoadingNames.PreparedEnvelope)) && IsList(tuple.Items[1]))
        {
            bool valid = true;
            Term rest = tuple.Items[1];
            while (rest is Cons cell)
            {
                if (cell.Head is not TupleTerm { Items.Count: 2 } row || row.Items[0] is not Atom
                    || row.Items[1] is not TupleTerm { Items.Count: 2 } payload || !IsList(payload.Items[1]))
                {
                    valid = false;
                    break;
                }
                try
                {
                    if (!(await Call(
                        context,
                        CodeLibraryDependencies.Erlang,
                        CodeRuntimeFunctions.HasPreparedCodeOnLoad,
                        payload.Items[0]
                    ))
                        .Equals(Term.A(ErlangBooleanAtoms.False)))
                    {
                        valid = false;
                        break;
                    }
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error)
                {
                    valid = false;
                    break;
                }
                rest = cell.Tail;
            }
            if (valid && rest is Nil)
            {
                return await Finish(context, tuple.Items[1], false);
            }
        }

        throw new ErlangException(ErlangErrorReasons.FunctionClause);
    }

    private static async ValueTask<Term> Prepare(ProcessContext context, Term input, string operation)
    {
        var binaries = new List<Term>();
        var modules = new List<Term>();
        Term rest = input;
        while (rest is Cons cell)
        {
            if (cell.Head is Atom)
            {
                modules.Insert(0, cell.Head);
            }
            else if (cell.Head is TupleTerm { Items.Count: 3 } tuple && tuple.Items[0] is Atom
                && IsList(tuple.Items[1]) && tuple.Items[2] is BitString { IsBinary: true })
            {
                binaries.Insert(0, cell.Head);
            }
            else
            {
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
            }
            rest = cell.Tail;
        }
        if (rest is not Nil)
        {
            throw CodeSource.Partition(
                context,
                operation,
                rest,
                Cons.From(binaries),
                Cons.From(modules)
            );
        }
        Term[] names = binaries.Cast<TupleTerm>().Select(row => row.Items[0]).Concat(modules).Order(CodeTermComparer.Instance).ToArray();
        var duplicates = new List<Term>();
        for (int i = 1; i < names.Length; i++)
        {
            if (names[i - 1].Equals(names[i]))
            {
                duplicates.Insert(0, Term.Tuple(names[i], Term.A(CodeLoadingNames.Duplicated)));
            }
        }
        if (duplicates.Count != 0)
        {
            return Term.Tuple(Term.A(CodeResultAtoms.Error), Cons.From(duplicates));
        }
        var prepared = new List<Term>();
        var errors = new List<Term>();
        await PrepareItems(
            context,
            binaries,
            true,
            prepared,
            errors
        );
        await PrepareItems(
            context,
            modules,
            false,
            prepared,
            errors
        );
        if (errors.Count != 0)
        {
            return Term.Tuple(Term.A(CodeResultAtoms.Error), Cons.From(errors));
        }
        foreach (TupleTerm row in prepared)
        {
            var payload = (TupleTerm)row.Items[1];
            if ((await Call(
                context,
                CodeLibraryDependencies.Erlang,
                CodeRuntimeFunctions.HasPreparedCodeOnLoad,
                payload.Items[0]
            ))
                .Equals(Term.A(ErlangBooleanAtoms.True)))
            {
                errors.Add(Term.Tuple(row.Items[0], Term.A(CodeLoadingNames.OnLoadNotAllowed)));
            }
        }

        return errors.Count == 0 ? Cons.From(prepared) : Term.Tuple(Term.A(CodeResultAtoms.Error), Cons.From(errors));
    }

    private static async ValueTask PrepareItems(
        ProcessContext context,
        IEnumerable<Term> items,
        bool binary,
        List<Term> prepared,
        List<Term> errors
    )
    {
        Term list = Cons.From(items);
        if (list is Nil)
        {
            return;
        }
        var recipe = new CodeParallelRecipe(context.Runtime.Functions, false);
        Term spawned = await Call(
            context,
            CodeLibraryDependencies.Erlang,
            CodeRuntimeFunctions.SpawnMonitor,
            recipe.Rebind([list, CoreModules.Boolean(binary)])
        );
        Term reference = ((TupleTerm)spawned).Items[1];
        TupleTerm message = (await context.ReceiveAsync(value => value is TupleTerm { Items.Count: CodeMonitorProtocol.MessageArity } down
            && down.Items[CodeMonitorProtocol.TagIndex].Equals(Term.A(CodeMonitorProtocol.Down)) && down.Items[CodeMonitorProtocol.ReferenceIndex].Equals(reference)
            && down.Items[CodeMonitorProtocol.EntityIndex].Equals(Term.A(CodeMonitorProtocol.ProcessEntity)) ? down : null))!;
        var results = (TupleTerm)message.Items[CodeMonitorProtocol.OutcomeIndex];
        prepared.AddRange(Cons.Items(results.Items[0]));
        errors.AddRange(Cons.Items(results.Items[1]));
    }

    private static async ValueTask<Term> EnsureModules(ProcessContext context, Term input)
    {
        var modules = new List<Term>();
        Term rest = input;
        while (rest is Cons cell && cell.Head is Atom)
        {
            if (!(await Call(
                context,
                CodeLibraryDependencies.Erlang,
                CodeRuntimeFunctions.ModuleLoaded,
                cell.Head
            )).Equals(Term.A(ErlangBooleanAtoms.True)))
            {
                modules.Add(cell.Head);
            }
            rest = cell.Tail;
        }
        if (rest is not Nil)
        {
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        }
        var prepared = new List<Term>();
        var errors = new List<Term>();
        await PrepareItems(
            context,
            modules.Distinct().Order(CodeTermComparer.Instance),
            false,
            prepared,
            errors
        );
        var normal = new List<Term>();
        var onLoad = new List<TupleTerm>();
        foreach (TupleTerm row in prepared)
        {
            Term has = await Call(
                context,
                CodeLibraryDependencies.Erlang,
                CodeRuntimeFunctions.HasPreparedCodeOnLoad,
                ((TupleTerm)row.Items[1]).Items[0]
            );
            if (has.Equals(Term.A(ErlangBooleanAtoms.True)))
            {
                onLoad.Add(row);
            }
            else
            {
                normal.Add(row);
            }
        }
        Term result = await Finish(context, Cons.From(normal), true);
        if (IsError(result))
        {
            errors.InsertRange(0, Cons.Items(((TupleTerm)result).Items[1]));
        }
        foreach (TupleTerm row in onLoad)
        {
            var payload = (TupleTerm)row.Items[1];
            Term loaded = await Server(
                context,
                Request(
                    CodeServerProtocolNames.LoadModule,
                    payload.Items[0],
                    row.Items[0],
                    payload.Items[1],
                    CoreModules.Boolean(false),
                    CoreModules.Boolean(true)
                )
            );
            if (IsError(loaded))
            {
                errors.Insert(0, Term.Tuple(row.Items[0], ((TupleTerm)loaded).Items[1]));
            }
        }

        return errors.Count == 0 ? Term.A(CodeResultAtoms.Ok) : Term.Tuple(Term.A(CodeResultAtoms.Error), Cons.From(errors));
    }

    internal static async ValueTask<Term> RunParallel(ProcessContext context, IReadOnlyList<Term> environment, bool itemWorker)
    {
        bool binary = CoreModules.Bool(environment[1]);
        if (itemWorker)
        {
            Term item = environment[0];
            Term module = binary ? ((TupleTerm)item).Items[0] : item;
            Term outcome;
            try
            {
                Term found = binary ? item : await ObjectCode(context, module);
                if (found is not TupleTerm { Items.Count: 3 } row)
                {
                    outcome = Request(CodePreparationProtocol.Bad, Term.Tuple(module, Term.A(CodeLoadingNames.NoFile)));
                }
                else
                {
                    Term prepared = await Call(
                        context,
                        CodeLibraryDependencies.Erlang,
                        CodeRuntimeFunctions.PrepareLoading,
                        module,
                        row.Items[binary ? 2 : 1]
                    );
                    outcome = IsError(prepared)
                        ? Request(CodePreparationProtocol.Bad, Term.Tuple(module, ((TupleTerm)prepared).Items[1]))
                        : Request(CodePreparationProtocol.Good, Term.Tuple(module, Term.Tuple(prepared, row.Items[binary ? 1 : 2])));
                }
            }
            catch (ErlangException error)
            {
                outcome = Request(CodePreparationProtocol.Bad, Term.Tuple(module, error.Reason));
            }

            return await Call(
                context,
                CodeLibraryDependencies.Erlang,
                CodeRuntimeFunctions.Exit,
                outcome
            );
        }
        int count = 0;
        var recipe = new CodeParallelRecipe(context.Runtime.Functions, true);
        foreach (Term item in Cons.Items(environment[0]))
        {
            await Call(
                context,
                CodeLibraryDependencies.Erlang,
                CodeRuntimeFunctions.SpawnMonitor,
                recipe.Rebind([item, environment[1]])
            );
            count++;
        }
        var good = new List<Term>();
        var bad = new List<Term>();
        while (count > 0)
        {
            TupleTerm message = (await context.ReceiveAsync(value => value is TupleTerm { Items.Count: CodeMonitorProtocol.MessageArity } down
                && down.Items[CodeMonitorProtocol.TagIndex].Equals(Term.A(CodeMonitorProtocol.Down)) && down.Items[CodeMonitorProtocol.EntityIndex].Equals(Term.A(CodeMonitorProtocol.ProcessEntity))
                && down.Items[CodeMonitorProtocol.OutcomeIndex] is TupleTerm { Items.Count: 2 } outcome
                && outcome.Items[0] is Atom { Name: CodePreparationProtocol.Good or CodePreparationProtocol.Bad } ? down : null))!;
            var result = (TupleTerm)message.Items[CodeMonitorProtocol.OutcomeIndex];
            (result.Items[0].Equals(Term.A(CodePreparationProtocol.Good)) ? good : bad).Insert(0, result.Items[1]);
            count--;
        }

        return await Call(
            context,
            CodeLibraryDependencies.Erlang,
            CodeRuntimeFunctions.Exit,
            Term.Tuple(Cons.From(good), Cons.From(bad))
        );
    }
}
