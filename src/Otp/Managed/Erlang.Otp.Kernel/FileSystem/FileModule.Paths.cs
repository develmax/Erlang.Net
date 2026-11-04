// Ported from OTP-29.1.1 file.erl (Apache-2.0), Ericsson AB 1996-2026.
namespace Erlang;

public static partial class FileModule
{
    private static async ValueTask<Term> Open(ProcessContext context, Term item, Term modes)
    {
        if (!IsList(modes))
        {
            return await Open(context, item, ModeList(modes));
        }
        bool raw = Member(modes, FileProtocol.Raw);
        bool ram = Member(modes, FileProtocol.Ram);
        if (raw && ram)
        {
            throw FileSource.Error(
                context,
                FileFunctionNames.Open,
                [item, modes],
                A(ErlangErrorReasons.BadArgument),
                FileSourceLocations.OpenConflictingModes
            );
        }
        if (ram)
        {
            return await Call(
                context,
                FileProtocol.RamFile,
                FileFunctionNames.Open,
                item,
                modes
            );
        }
        Term name = await FileName(context, item);
        if (raw)
        {
            return await Call(
                context,
                FileProtocol.RawFile,
                FileFunctionNames.Open,
                name,
                modes
            );
        }
        Term check = CheckArguments(new[] { name }.Concat(Cons.Items(modes)));

        return IsError(check) ? check : await Server(
            context,
            FileFunctionNames.Open,
            name,
            modes
        );
    }

    private static async ValueTask<Term> PathOperation(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        if (function == FileFunctionNames.GetCwd && arguments.Count == 0)
        {
            return await Server(context, function);
        }
        Term name = arguments[0];
        if (function == FileFunctionNames.RawReadFileInfo)
        {
            return await PathOperation(context, FileFunctionNames.ReadFileInfo, [name, Term.List(A(FileProtocol.Raw))]);
        }
        if (function == FileFunctionNames.RawWriteFileInfo)
        {
            if (!IsFileInfo(arguments[1]))
            {
                throw FileSource.Clause(
                    context,
                    function,
                    arguments,
                    FileSourceLocations.RawWriteInfoClause
                );
            }

            return await PathOperation(context, FileFunctionNames.WriteFileInfo, [name, arguments[1], Term.List(A(FileProtocol.Raw))]);
        }
        if (function == FileFunctionNames.ReadFileInfo && IsHandle(name))
        {
            Term options = arguments.Count == FileApiArity.Unary ? Nil.Value : arguments[1];
            if (!IsList(options))
            {
                throw FileSource.Clause(
                    context,
                    function,
                    arguments,
                    FileSourceLocations.ReadInfoOptionsClause
                );
            }

            return Descriptor(name, out Term module)
                ? await CallModule(
                    context,
                    module,
                    FileProtocol.ReadHandleInfo,
                    name,
                    options
                )
                : await Request(context, name, Term.Tuple(A(FileProtocol.ReadHandleInfo), options));
        }
        int optionIndex = function switch
        {
            FileFunctionNames.Delete or FileFunctionNames.ReadFile or FileFunctionNames.ReadFileInfo or FileFunctionNames.ReadLinkInfo when arguments.Count == FileApiArity.Binary => 1,
            FileFunctionNames.WriteFileInfo when arguments.Count == FileApiArity.Ternary => 2,
            _ => -1
        };
        if (function == FileFunctionNames.WriteFileInfo && !IsFileInfo(arguments[1]))
        {
            throw FileSource.Clause(
                context,
                function,
                arguments,
                arguments.Count == FileApiArity.Binary ? FileSourceLocations.WriteInfoClause : FileSourceLocations.WriteInfoOptionsClause
            );
        }
        if (optionIndex >= 0 && !IsList(arguments[optionIndex]))
        {
            int line = function switch
            {
                FileFunctionNames.Delete => FileSourceLocations.DeleteOptionsClause,
                FileFunctionNames.ReadFile => FileSourceLocations.ReadFileOptionsClause,
                FileFunctionNames.ReadFileInfo => FileSourceLocations.ReadInfoOptionsClause,
                FileFunctionNames.ReadLinkInfo => FileSourceLocations.ReadLinkInfoOptionsClause,
                _ => FileSourceLocations.WriteInfoOptionsClause
            };
            throw FileSource.Clause(
                context,
                function,
                arguments,
                line
            );
        }
        var values = arguments.ToArray();
        values[0] = await FileName(context, name);
        if (function is FileFunctionNames.Rename or FileFunctionNames.MakeLink or FileFunctionNames.MakeSymlink)
        {
            values[1] = await FileName(context, values[1]);
        }
        else if (function == FileFunctionNames.WriteFile)
        {
            values[1] = await MakeBinary(context, values[1]);
        }
        if (optionIndex < 0)
        {
            return await CheckedServer(context, function, values);
        }
        Term opts = values[optionIndex];
        Term check = CheckArguments(function is FileFunctionNames.Delete or FileFunctionNames.ReadFile ? Cons.Items(opts) : values);
        if (IsError(check))
        {
            return check;
        }
        if (Member(opts, FileProtocol.Raw))
        {
            return await Call(
                context,
                FileProtocol.PrimFile,
                function,
                function is FileFunctionNames.Delete or FileFunctionNames.ReadFile ? [values[0]] : values
            );
        }

        return await Server(context, function, function is FileFunctionNames.Delete or FileFunctionNames.ReadFile ? [values[0]] : values);
    }

    private static bool IsFileInfo(Term value)
        => value is TupleTerm { Items.Count: FileInformationLayout.Arity } tuple && tuple.Items[0].Equals(A(FileProtocol.FileInfo));

    private static async ValueTask<Term> WriteFile(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term name = arguments[0];
        Term data = arguments[1];
        Term modes = arguments[2];
        if (!IsList(modes))
        {
            throw FileSource.Clause(
                context,
                FileFunctionNames.WriteFile,
                arguments,
                FileSourceLocations.WriteFileModesClause
            );
        }
        if (Member(modes, FileProtocol.Raw))
        {
            try
            {
                _ = await Call(
                    context,
                    FileProtocol.Erlang,
                    FileProtocol.IolistSize,
                    data
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error)
            {
                return Error(error.Reason);
            }
        }
        else
        {
            data = await MakeBinary(context, data);
            if (IsError(data))
            {
                return data;
            }
        }
        Term opened = await Open(context, name, new Cons(A(FileProtocol.Binary), new Cons(A(FileProtocol.Write), modes)));
        if (!IsSuccess(opened))
        {
            return opened;
        }
        Term handle = ((TupleTerm)opened).Items[1];
        Term written = await Device(context, FileFunctionNames.Write, [handle, data]);
        Term closed = await Device(context, FileFunctionNames.Close, [handle]);

        return written.Equals(Ok) ? closed : written;
    }

    private static async ValueTask<Term> DeleteTree(ProcessContext context, Term file)
    {
        Term information = await PathOperation(context, FileFunctionNames.ReadLinkInfo, [file]);
        if (!IsSuccess(information))
        {
            return information;
        }
        Term info = ((TupleTerm)information).Items[1];
        if (!IsFileInfo(info) || !((TupleTerm)info).Items[FileInformationLayout.TypeIndex].Equals(A(FileProtocol.Directory)))
        {
            return await PathOperation(context, FileFunctionNames.Delete, [file]);
        }
        Term listed = await PathOperation(context, FileFunctionNames.ListDirAll, [file]);
        if (IsSuccess(listed))
        {
            foreach (Term child in Cons.Items(((TupleTerm)listed).Items[1]))
            {
                Term joined = await Call(
                    context,
                    FileProtocol.Filename,
                    FileDependencyNames.FilenameJoinFunction,
                    file,
                    child
                );
                _ = await DeleteTree(context, joined);
            }
        }

        return await PathOperation(context, FileFunctionNames.DelDir, [file]);
    }

    private static async ValueTask<Term> ChangeInfo(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        bool time = function == FileFunctionNames.ChangeTime;
        if (!arguments.Skip(1).All(value => time ? IsDateTime(value) : value is Integer))
        {
            int line = function switch
            {
                FileFunctionNames.ChangeMode => FileSourceLocations.ChangeModeClause,
                FileFunctionNames.ChangeOwner => arguments.Count == FileApiArity.Binary ? FileSourceLocations.ChangeOwnerClause : FileSourceLocations.ChangeOwnerAndGroupClause,
                FileFunctionNames.ChangeGroup => FileSourceLocations.ChangeGroupClause,
                _ => arguments.Count == FileApiArity.Binary ? FileSourceLocations.ChangeModificationTimeClause : FileSourceLocations.ChangeAccessAndModificationTimeClause
            };
            throw FileSource.Clause(
                context,
                function,
                arguments,
                line
            );
        }
        Term[] fields = Enumerable.Repeat<Term>(A(FileProtocol.Undefined), FileInformationLayout.Arity).ToArray();
        fields[0] = A(FileProtocol.FileInfo);
        int index = function switch
        {
            FileFunctionNames.ChangeMode => FileInformationLayout.ModeIndex,
            FileFunctionNames.ChangeOwner => FileInformationLayout.OwnerIndex,
            FileFunctionNames.ChangeGroup => FileInformationLayout.GroupIndex,
            _ => arguments.Count == FileApiArity.Binary ? FileInformationLayout.ModificationTimeIndex : FileInformationLayout.AccessTimeIndex
        };
        fields[index] = arguments[1];
        if (arguments.Count == FileApiArity.Ternary)
        {
            fields[time ? FileInformationLayout.ModificationTimeIndex : FileInformationLayout.GroupIndex] = arguments[2];
        }

        return await PathOperation(context, FileFunctionNames.WriteFileInfo, [arguments[0], Term.Tuple(fields)]);
    }

    private static bool IsDateTime(Term value)
        => value is TupleTerm { Items.Count: FileTupleLayout.DateTimeArity } pair
            && pair.Items.All(part => part is TupleTerm { Items.Count: FileTupleLayout.DateTimePartArity } triple && triple.Items.All(item => item is Integer));

    private static async ValueTask<Term> PathOpen(
        ProcessContext context,
        Term paths,
        Term name,
        Term modes
    )
    {
        Term filename = await FileName(context, name);
        if (IsError(filename))
        {
            return filename;
        }
        Term kind = await Call(
            context,
            FileProtocol.Filename,
            FileDependencyNames.FilenamePathTypeFunction,
            filename
        );
        if (!kind.Equals(A(FileProtocol.Relative)))
        {
            Term opened = await Open(context, name, modes);

            return IsSuccess(opened) ? Term.Tuple(Ok, ((TupleTerm)opened).Items[1], name) : opened;
        }
        while (paths is Cons cell)
        {
            Term directory = await FileName(context, cell.Head);
            if (IsError(directory))
            {
                return directory;
            }
            Term full = directory.Equals(Term.String(FileNameRules.CurrentDirectory)) ? filename : await Call(
                context,
                FileProtocol.Filename,
                FileDependencyNames.FilenameJoinFunction,
                directory,
                filename
            );
            Term opened = await Open(context, full, modes);
            if (IsSuccess(opened))
            {
                return Term.Tuple(Ok, ((TupleTerm)opened).Items[1], full);
            }
            if (!opened.Equals(Error(A(FileProtocol.NotFound))) && !opened.Equals(Error(A(FileProtocol.NotDirectory))))
            {
                return opened;
            }
            paths = cell.Tail;
        }
        if (paths is not Nil)
        {
            throw FileSource.Clause(
                context,
                FileFunctionNames.PathOpenFirst,
                [paths, filename, modes, A(FileProtocol.NotFound)],
                FileSourceLocations.PathOpenFirstClause
            );
        }

        return Error(A(FileProtocol.NotFound));
    }
}
