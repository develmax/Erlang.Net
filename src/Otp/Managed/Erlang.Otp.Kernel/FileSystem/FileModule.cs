// Derived from lib/kernel/src/file.erl, Erlang/OTP 29.1.1, Apache-2.0.
// Copyright Ericsson AB 1996-2026. Original notices remain in Otp/Original.
namespace Erlang;

/// <summary>The managed file library; file servers and descriptor drivers retain their OTP protocols.</summary>
public static partial class FileModule
{
    public const string Name = "file";

    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (FileFunctionNames.FormatError, FileApiArity.Unary),
        (FileFunctionNames.GetCwd, 0),
        (FileFunctionNames.GetCwd, FileApiArity.Unary),
        (FileFunctionNames.SetCwd, FileApiArity.Unary),
        (FileFunctionNames.Delete, FileApiArity.Unary),
        (FileFunctionNames.Delete, FileApiArity.Binary),
        (FileFunctionNames.Rename, FileApiArity.Binary),
        (FileFunctionNames.MakeDir, FileApiArity.Unary),
        (FileFunctionNames.DelDir, FileApiArity.Unary),
        (FileFunctionNames.DelDirR, FileApiArity.Unary),
        (FileFunctionNames.ListDir, FileApiArity.Unary),
        (FileFunctionNames.ListDirAll, FileApiArity.Unary),
        (FileFunctionNames.ReadFileInfo, FileApiArity.Unary),
        (FileFunctionNames.ReadFileInfo, FileApiArity.Binary),
        (FileFunctionNames.WriteFileInfo, FileApiArity.Binary),
        (FileFunctionNames.WriteFileInfo, FileApiArity.Ternary),
        (FileFunctionNames.Altname, FileApiArity.Unary),
        (FileFunctionNames.ReadLinkInfo, FileApiArity.Unary),
        (FileFunctionNames.ReadLinkInfo, FileApiArity.Binary),
        (FileFunctionNames.ReadLink, FileApiArity.Unary),
        (FileFunctionNames.ReadLinkAll, FileApiArity.Unary),
        (FileFunctionNames.MakeLink, FileApiArity.Binary),
        (FileFunctionNames.MakeSymlink, FileApiArity.Binary),
        (FileFunctionNames.ReadFile, FileApiArity.Unary),
        (FileFunctionNames.ReadFile, FileApiArity.Binary),
        (FileFunctionNames.WriteFile, FileApiArity.Binary),
        (FileFunctionNames.WriteFile, FileApiArity.Ternary),
        (FileFunctionNames.IpreadS32buP32bu, FileApiArity.Ternary),
        (FileFunctionNames.Open, FileApiArity.Binary),
        (FileFunctionNames.Close, FileApiArity.Unary),
        (FileFunctionNames.Advise, FileApiArity.Advise),
        (FileFunctionNames.Allocate, FileApiArity.Ternary),
        (FileFunctionNames.Read, FileApiArity.Binary),
        (FileFunctionNames.Write, FileApiArity.Binary),
        (FileFunctionNames.Pread, FileApiArity.Binary),
        (FileFunctionNames.Pread, FileApiArity.Ternary),
        (FileFunctionNames.Pwrite, FileApiArity.Binary),
        (FileFunctionNames.Pwrite, FileApiArity.Ternary),
        (FileFunctionNames.ReadLine, FileApiArity.Unary),
        (FileFunctionNames.Position, FileApiArity.Binary),
        (FileFunctionNames.Truncate, FileApiArity.Unary),
        (FileFunctionNames.Datasync, FileApiArity.Unary),
        (FileFunctionNames.Sync, FileApiArity.Unary),
        (FileFunctionNames.Copy, FileApiArity.Binary),
        (FileFunctionNames.Copy, FileApiArity.Ternary),
        (FileFunctionNames.Consult, FileApiArity.Unary),
        (FileFunctionNames.PathConsult, FileApiArity.Binary),
        (FileFunctionNames.Eval, FileApiArity.Unary),
        (FileFunctionNames.Eval, FileApiArity.Binary),
        (FileFunctionNames.PathEval, FileApiArity.Binary),
        (FileFunctionNames.PathEval, FileApiArity.Ternary),
        (FileFunctionNames.PathOpen, FileApiArity.Ternary),
        (FileFunctionNames.Script, FileApiArity.Unary),
        (FileFunctionNames.Script, FileApiArity.Binary),
        (FileFunctionNames.PathScript, FileApiArity.Binary),
        (FileFunctionNames.PathScript, FileApiArity.Ternary),
        (FileFunctionNames.ChangeOwner, FileApiArity.Binary),
        (FileFunctionNames.ChangeOwner, FileApiArity.Ternary),
        (FileFunctionNames.ChangeGroup, FileApiArity.Binary),
        (FileFunctionNames.ChangeMode, FileApiArity.Binary),
        (FileFunctionNames.ChangeTime, FileApiArity.Binary),
        (FileFunctionNames.ChangeTime, FileApiArity.Ternary),
        (FileFunctionNames.Sendfile, FileApiArity.Binary),
        (FileFunctionNames.Sendfile, FileApiArity.Sendfile),
        (FileFunctionNames.RawReadFileInfo, FileApiArity.Unary),
        (FileFunctionNames.RawWriteFileInfo, FileApiArity.Binary),
        (FileFunctionNames.CopyOpened, FileApiArity.Ternary),
        (FileFunctionNames.IpreadS32buP32buInt, FileApiArity.Ternary),
        (FileFunctionNames.NativeNameEncoding, 0)
    });

    public static void Register(ModuleRegistry registry)
        => registry.InstallSourceWrappers(target =>
        {
            foreach (var export in Exports.Where(value => value.Name != FileProtocol.NativeNameEncoding))
            {
                string function = export.Name;
                target.RegisterReviewedManagedLibrary789(
                    Name,
                    function,
                    export.Arity,
                    (context, arguments) => Invoke(context, function, arguments)
                );
            }
        });

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        if (!Exports.Contains((function, arguments.Count)))
        {
            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
        using var tail = context.EnterSourceTailCall(Name, function);

        return function switch
        {
            FileFunctionNames.NativeNameEncoding => await Call(context, Name, function),
            FileFunctionNames.FormatError => await FormatError(context, arguments[0]),
            FileFunctionNames.Open => await Open(context, arguments[0], arguments[1]),
            FileFunctionNames.WriteFile when arguments.Count == FileApiArity.Ternary => await WriteFile(context, arguments),
            FileFunctionNames.DelDirR => await DeleteTree(context, arguments[0]),
            FileFunctionNames.Copy => await Copy(
                context,
                arguments[0],
                arguments[1],
                arguments.Count == FileApiArity.Ternary ? arguments[2] : A(FileProtocol.Infinity)
            ),
            FileFunctionNames.CopyOpened => ValidLength(arguments[2]) && IsHandle(arguments[0]) && IsHandle(arguments[1])
                ? await CopyOpened(
                    context,
                    arguments[0],
                    arguments[1],
                    arguments[2]
                ) : BadArgument,
            FileFunctionNames.IpreadS32buP32buInt => await IndirectRead(
                context,
                arguments[0],
                arguments[1],
                arguments[2]
            ),
            FileFunctionNames.PathOpen => await PathOpen(
                context,
                arguments[0],
                arguments[1],
                arguments[2]
            ),
            FileFunctionNames.Consult or FileFunctionNames.Eval or FileFunctionNames.Script or FileFunctionNames.PathConsult or FileFunctionNames.PathEval or FileFunctionNames.PathScript => await ReadTerms(context, function, arguments),
            FileFunctionNames.Sendfile => await SendFile(context, arguments),
            FileFunctionNames.ChangeOwner or FileFunctionNames.ChangeGroup or FileFunctionNames.ChangeMode or FileFunctionNames.ChangeTime => await ChangeInfo(context, function, arguments),
            FileFunctionNames.Close or FileFunctionNames.Read or FileFunctionNames.Write or FileFunctionNames.ReadLine or FileFunctionNames.Pread or FileFunctionNames.Pwrite or FileFunctionNames.Advise or FileFunctionNames.Allocate
                or FileFunctionNames.Position or FileFunctionNames.Truncate or FileFunctionNames.Datasync or FileFunctionNames.Sync or FileFunctionNames.IpreadS32buP32bu => await Device(context, function, arguments),
            _ => await PathOperation(context, function, arguments)
        };
    }

    private static Atom A(string value) => Term.A(value);

    private static Term Ok => A(FileProtocol.Ok);

    private static Term BadArgument => Error(A(ErlangErrorReasons.BadArgument));

    private static Term Error(Term reason) => Term.Tuple(A(FileProtocol.Error), reason);

    private static Term Success(Term value) => Term.Tuple(Ok, value);

    private static bool IsError(Term value) => value is TupleTerm { Items.Count: FileTupleLayout.ResultArity } tuple && tuple.Items[0].Equals(A(FileProtocol.Error));

    private static bool IsSuccess(Term value) => value is TupleTerm { Items.Count: FileTupleLayout.ResultArity } tuple && tuple.Items[0].Equals(Ok);

    private static bool IsList(Term value)
    {
        while (value is Cons cell)
        {
            value = cell.Tail;
        }

        return value is Nil;
    }

    private static bool Member(Term list, string value) => Cons.Items(list).Any(item => item.Equals(A(value)));

    private static bool Descriptor(Term value, out Term module)
    {
        if (value is TupleTerm { Items.Count: FileTupleLayout.DescriptorArity } tuple && tuple.Items[0].Equals(A(FileProtocol.Descriptor)))
        {
            module = tuple.Items[1];

            return true;
        }
        module = Nil.Value;

        return false;
    }

    private static bool IsHandle(Term value) => value is Pid || Descriptor(value, out _);

    private static bool Nonnegative(Term value) => value is Integer integer && integer.Value >= 0;

    private static bool ValidLength(Term value) => value is Atom || Nonnegative(value);

    private static ValueTask<Term> Call(
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

    private static ValueTask<Term> CallModule(
        ProcessContext context,
        Term module,
        string function,
        params Term[] arguments
    )
        => Call(
            context,
            CoreModules.AtomName(module),
            function,
            arguments
        );

    private static Term CheckArguments(IEnumerable<Term> arguments)
        => arguments.FirstOrDefault(IsError) ?? Ok;

    private static Term ModeList(Term mode)
    {
        if (mode is Atom atom)
        {
            return atom.Name switch
            {
                FileProtocol.Read => Term.List(mode),
                FileProtocol.Write => Term.List(mode),
                FileProtocol.ReadWrite => Term.List(A(FileProtocol.Read), A(FileProtocol.Write)),
                _ => Term.List(BadArgument)
            };
        }
        if (mode is TupleTerm { Items.Count: FileTupleLayout.ModePairArity } tuple && tuple.Items[1] is Atom)
        {
            if (tuple.Items[0].Equals(A(FileProtocol.Binary)))
            {
                return new Cons(A(FileProtocol.Binary), ModeList(tuple.Items[1]));
            }
            if (tuple.Items[0].Equals(A(FileProtocol.Character)))
            {
                return ModeList(tuple.Items[1]);
            }
        }

        return Term.List(BadArgument);
    }

    private static async ValueTask<Term> FileName(ProcessContext context, Term name)
    {
        if (name is BitString { IsBinary: true })
        {
            return name;
        }
        Term encoding = await Call(context, Name, FileProtocol.NativeNameEncoding);
        var result = new List<Term>();
        var pending = new Stack<Term>();
        pending.Push(name);
        while (pending.TryPop(out Term? item))
        {
            if (item is Cons cell)
            {
                pending.Push(cell.Tail);
                if (cell.Head is Integer integer && (encoding.Equals(A(FileProtocol.Utf8)) || integer.Value < FileNameRules.Latin1ExclusiveLimit))
                {
                    result.Add(integer);
                }
                else
                {
                    pending.Push(cell.Head);
                }
            }
            else if (item is Atom atom)
            {
                result.AddRange(Cons.Items(Term.String(atom.Name)));
            }
            else if (item is not Nil)
            {
                return BadArgument;
            }
        }

        return Cons.From(result);
    }

    private static async ValueTask<Term> MakeBinary(ProcessContext context, Term data)
    {
        if (data is BitString { IsBinary: true })
        {
            return data;
        }
        try
        {
            return await Call(
                context,
                FileProtocol.Erlang,
                FileProtocol.IolistToBinary,
                data
            );
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error)
        {
            return Error(error.Reason);
        }
    }

    private static async ValueTask<Term> Server(ProcessContext context, string command, params Term[] arguments)
    {
        Term tag = await Call(
            context,
            FileProtocol.Erlang,
            FileProtocol.SpreadTag,
            A(FileProtocol.True)
        );
        Term response = await Call(
            context,
            FileProtocol.GenServer,
            FileProtocol.Call,
            A(FileProtocol.FileServer),
            Term.Tuple(new[] { A(command) }.Concat(arguments).ToArray()),
            A(FileProtocol.Infinity)
        );
        _ = await Call(
            context,
            FileProtocol.Erlang,
            FileProtocol.RestoreTag,
            tag
        );

        return response;
    }

    private static async ValueTask<Term> CheckedServer(ProcessContext context, string command, params Term[] arguments)
    {
        Term check = CheckArguments(arguments);

        return IsError(check) ? check : await Server(context, command, arguments);
    }

    private static async ValueTask<Term> Request(ProcessContext context, Term device, Term request)
    {
        Term reference = await Call(
            context,
            FileProtocol.Erlang,
            FileProtocol.Monitor,
            A(FileProtocol.Process),
            device
        );
        _ = await Call(
            context,
            FileProtocol.Erlang,
            FileProtocol.Send,
            device,
            Term.Tuple(
                A(FileProtocol.FileRequest),
                context.Self,
                reference,
                request
            )
        );
        TupleTerm? response = await context.ReceiveErlangAsync(
            value =>
        {
            if (value is TupleTerm tuple)
            {
                if (tuple.Items.Count == FileMessageLayout.ReplyArity && tuple.Items[FileMessageLayout.TagIndex].Equals(A(FileProtocol.FileReply)) && tuple.Items[FileMessageLayout.ReferenceIndex].Equals(reference)
                    || tuple.Items.Count == FileMessageLayout.DownArity && tuple.Items[FileMessageLayout.TagIndex].Equals(A(FileProtocol.Down)) && tuple.Items[FileMessageLayout.ReferenceIndex].Equals(reference))
                {
                    return tuple;
                }
            }

            return null;
        },
            A(FileProtocol.Infinity)
        );
        if (response!.Items.Count == FileMessageLayout.DownArity)
        {
            return Error(A(FileProtocol.Terminated));
        }
        _ = await Call(
            context,
            FileProtocol.Erlang,
            FileProtocol.Demonitor,
            reference,
            Term.List(A(FileProtocol.Flush))
        );

        return response.Items[FileMessageLayout.ReplyValueIndex];
    }
}
