// Ported from OTP-29.1.1 code.erl / eep48.hrl (Apache-2.0).
namespace Erlang;

public static partial class CodeModule
{
    private static async ValueTask<Term> Documentation(ProcessContext context, Term module, Term options)
    {
        if (options is not MapTerm map || !map.TryGet(Term.A(CodeDocumentationNames.Sources), out Term? sources))
        {
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        }
        if (sources is Nil)
        {
            return Error(CodeDocumentationNames.Missing);
        }
        if (sources is not Cons source)
        {
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        }
        Term filename = await Invoke(context, CodeApiNames.Which, [module]);
        if (filename.Equals(Term.A(CodeLoadedModuleNames.Preloaded)))
        {
            Term directory = await Invoke(context, CodeApiNames.LibraryDirectory, [Term.A(CodeStartupNames.RuntimeApplication)]);
            if (IsError(directory))
            {
                return Error(CodeDocumentationNames.Missing);
            }
            filename = await Call(
                context,
                CodeLibraryDependencies.Filename,
                CodeFilenameFunctions.Join,
                Term.List(directory, Term.String(CodeStartupNames.EbinDirectory),
                Term.String(((Atom)module).Name + CodeObjectFileNames.Extension))
            );
        }
        else if (filename.Equals(Term.A(CodeLoadedModuleNames.CoverCompiled)))
        {
            filename = await Where(
                context,
                await Server(context, Term.A(CodeServerProtocolNames.GetPath)),
                Term.String(((Atom)module).Name + CodeObjectFileNames.Extension)
            );
            if (filename.Equals(Term.A(CodePathNames.NonExisting)))
            {
                return Error(CodeDocumentationNames.Missing);
            }
        }
        else if (filename is Atom)
        {
            return Term.Tuple(Term.A(CodeResultAtoms.Error), filename);
        }
        Term result;
        if (source.Head.Equals(Term.A(CodeDocumentationNames.Eep48)))
        {
            result = await DocChunk(context, filename, module);
        }
        else if (source.Head.Equals(Term.A(CodeDocumentationNames.DebugInfo)))
        {
            result = await DocAst(context, filename);
        }
        else
        {
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), source.Head));
        }
        if (!result.Equals(Error(CodeDocumentationNames.Missing)))
        {
            return result;
        }
        var next = new MapTerm(map.Entries.Where(entry => !entry.Key.Equals(Term.A(CodeDocumentationNames.Sources)))
            .Append(new KeyValuePair<Term, Term>(Term.A(CodeDocumentationNames.Sources), source.Tail)));

        return await Documentation(context, module, next);
    }

    private static bool MissingBeamChunk(Term result)
        => result is TupleTerm { Items.Count: CodeAbstractSyntaxLayout.TypePayloadArity } tuple && tuple.Items[0].Equals(Term.A(CodeResultAtoms.Error))
            && tuple.Items[2] is TupleTerm error && error.Items[0] is Atom { Name: CodeDocumentationNames.MissingChunk or CodeDocumentationNames.FileError };

    private static async ValueTask<Term> DecodeDoc(ProcessContext context, Term bytes)
        => Term.Tuple(Term.A(CodeResultAtoms.Ok), await Call(
            context,
            CodeLibraryDependencies.Erlang,
            CodeRuntimeFunctions.BinaryToTerm,
            bytes
        ));

    private static async ValueTask<Term> DocChunk(ProcessContext context, Term filename, Term module)
    {
        Term result = await Call(
            context,
            CodeLibraryDependencies.BeamLibrary,
            CodeDependencyFunctions.BeamChunks,
            filename,
            Term.List(Term.String(CodeDocumentationNames.DocsChunk))
        );
        if (!MissingBeamChunk(result))
        {
            if (result is TupleTerm { Items.Count: 2 } pair && pair.Items[0].Equals(Term.A(CodeResultAtoms.Ok))
                && pair.Items[1] is TupleTerm payload && payload.Items[0].Equals(module)
                && payload.Items[1] is Cons first && first.Head is TupleTerm chunk)
            {
                return await DecodeDoc(context, chunk.Items[1]);
            }

            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), result));
        }
        Term root = await Invoke(context, CodeApiNames.RootDirectory, []);
        while (true)
        {
            Term directory = await Call(
                context,
                CodeLibraryDependencies.Filename,
                CodeFilenameFunctions.Dirname,
                filename
            );
            if (directory.Equals(filename) || directory.Equals(root))
            {
                return Error(CodeDocumentationNames.Missing);
            }
            Term chunkName = await Call(
                context,
                CodeLibraryDependencies.Filename,
                CodeFilenameFunctions.Join,
                Term.List(
                    directory,
                    Term.String(CodeDocumentationNames.DocumentationDirectory),
                    Term.String(CodeDocumentationNames.ChunkDirectory),
                    Term.String(((Atom)module).Name + CodeDocumentationNames.ChunkExtension)
                )
            );
            Term read = await Call(
                context,
                CodeLibraryDependencies.File,
                CodeDependencyFunctions.FileReadFile,
                chunkName
            );
            if (read is TupleTerm pair && pair.Items[0].Equals(Term.A(CodeResultAtoms.Ok)))
            {
                return await DecodeDoc(context, pair.Items[1]);
            }
            if (!read.Equals(Error(CodeFileErrorAtoms.NoEntry)))
            {
                return read;
            }
            filename = directory;
        }
    }

    private static async ValueTask<Term> DocAst(ProcessContext context, Term filename)
    {
        Term result = await Call(
            context,
            CodeLibraryDependencies.BeamLibrary,
            CodeDependencyFunctions.BeamChunks,
            filename,
            Term.List(Term.A(CodeDocumentationNames.AbstractCode))
        );
        if (MissingBeamChunk(result))
        {
            return Error(CodeDocumentationNames.Missing);
        }
        if (result is not TupleTerm { Items.Count: 2 } pair || !pair.Items[0].Equals(Term.A(CodeResultAtoms.Ok))
            || pair.Items[1] is not TupleTerm payload || payload.Items[1] is not Cons first
            || first.Head is not TupleTerm chunk)
        {
            return result;
        }
        if (chunk.Items[1].Equals(Term.A(CodeDocumentationNames.NoAbstractCode)))
        {
            return Error(CodeDocumentationNames.Missing);
        }
        if (chunk.Items[1] is not TupleTerm raw || !raw.Items[0].Equals(Term.A(CodeDocumentationNames.RawAbstract)))
        {
            return result;
        }
        Term[] ast = Cons.Items(raw.Items[1]).ToArray();
        var functions = new List<Term>();
        var types = new List<Term>();
        foreach (Term node in ast)
        {
            if (node is not TupleTerm tuple)
            {
                continue;
            }
            bool function = tuple.Items.Count == CodeAbstractSyntaxLayout.FunctionArity && tuple.Items[CodeAbstractSyntaxLayout.TagIndex].Equals(Term.A(CodeDocumentationNames.Function));
            bool type = tuple.Items.Count == CodeAbstractSyntaxLayout.AttributeArity && tuple.Items[CodeAbstractSyntaxLayout.TagIndex].Equals(Term.A(CodeDocumentationNames.Attribute))
                && tuple.Items[CodeAbstractSyntaxLayout.AttributeKindIndex].Equals(Term.A(CodeDocumentationNames.Type)) && tuple.Items[CodeAbstractSyntaxLayout.AttributePayloadIndex] is TupleTerm { Items.Count: CodeAbstractSyntaxLayout.TypePayloadArity };
            if (!function && !type)
            {
                continue;
            }
            Term name = function ? tuple.Items[CodeAbstractSyntaxLayout.FunctionNameIndex] : ((TupleTerm)tuple.Items[CodeAbstractSyntaxLayout.AttributePayloadIndex]).Items[CodeAbstractSyntaxLayout.TypeNameIndex];
            Term arity = function ? tuple.Items[CodeAbstractSyntaxLayout.FunctionArityIndex] : Term.I(Cons.Items(((TupleTerm)tuple.Items[CodeAbstractSyntaxLayout.AttributePayloadIndex]).Items[CodeAbstractSyntaxLayout.TypeParametersIndex]).Count());
            var metadata = new List<KeyValuePair<Term, Term>>();
            if (type)
            {
                metadata.Add(new(Term.A(CodeDocumentationNames.Signature), Term.List(node)));
            }
            else
            {
                Term[] specs = ast.Where(candidate => IsSpec(candidate, name, arity)).ToArray();
                if (specs.Length == CodeAbstractSyntaxLayout.UniqueSpecificationCount)
                {
                    metadata.Add(new(Term.A(CodeDocumentationNames.Signature), Term.List(specs[0])));
                }
                else if (specs.Length != 0)
                {
                    throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), Cons.From(specs)));
                }
            }
            Term signature = await Call(
                context,
                CodeLibraryDependencies.IoLibrary,
                CodeDependencyFunctions.IoLibraryFormat,
                Term.String(CodeDocumentationNames.SignatureFormat),
                Term.List(name, arity)
            );
            Term encoded = await Call(
                context,
                CodeLibraryDependencies.Unicode,
                CodeDependencyFunctions.UnicodeCharactersToBinary,
                signature
            );
            Term entry = Term.Tuple(
                Term.Tuple(Term.A(function ? CodeDocumentationNames.Function : CodeDocumentationNames.Type), name, arity),
                tuple.Items[CodeAbstractSyntaxLayout.AnnotationIndex],
                Term.List(encoded),
                Term.A(CodeDocumentationNames.None),
                new MapTerm(metadata)
            );
            (function ? functions : types).Add(entry);
        }

        return Term.Tuple(
            Term.A(CodeResultAtoms.Ok),
            Term.Tuple(
                Term.A(CodeDocumentationNames.RecordTag),
                Term.I(CodeDocumentationNames.GeneratedAnnotation),
                Term.A(CodeDocumentationNames.Language),
                new BitString(CodeDocumentationNames.MimeType.ToArray()),
                Term.A(CodeDocumentationNames.None),
                new MapTerm([
                new KeyValuePair<Term, Term>(Term.A(CodeDocumentationNames.Generated), CoreModules.Boolean(true)),
                new KeyValuePair<Term, Term>(
                    Term.A(CodeDocumentationNames.OtpDocVersion),
                    Term.Tuple(
                        Term.I(CodeDocumentationNames.VersionMajor),
                        Term.I(CodeDocumentationNames.VersionMinor),
                        Term.I(CodeDocumentationNames.VersionPatch)
                    )
                )
            ]),
                Cons.From(functions.Concat(types))
            )
        );
    }

    private static bool IsSpec(Term candidate, Term name, Term arity)
    {
        if (candidate is not TupleTerm { Items.Count: CodeAbstractSyntaxLayout.AttributeArity } tuple || !tuple.Items[CodeAbstractSyntaxLayout.TagIndex].Equals(Term.A(CodeDocumentationNames.Attribute))
            || !tuple.Items[CodeAbstractSyntaxLayout.AttributeKindIndex].Equals(Term.A(CodeDocumentationNames.Spec)) || tuple.Items[CodeAbstractSyntaxLayout.AttributePayloadIndex] is not TupleTerm { Items.Count: CodeAbstractSyntaxLayout.SpecPayloadArity } spec
            || spec.Items[CodeAbstractSyntaxLayout.SpecSignatureIndex] is not TupleTerm signature)
        {
            return false;
        }

        return signature.Items.Count is CodeAbstractSyntaxLayout.LocalSignatureArity or CodeAbstractSyntaxLayout.RemoteSignatureArity && signature.Items[^CodeAbstractSyntaxLayout.NameFromEnd].Equals(name) && signature.Items[^CodeAbstractSyntaxLayout.ArityFromEnd].Equals(arity);
    }
}
