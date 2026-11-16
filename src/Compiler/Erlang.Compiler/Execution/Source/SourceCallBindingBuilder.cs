namespace Erlang.Compiler;

internal sealed class SourceCallBindingBuilder
{
    private bool suppressAll;
    private readonly HashSet<(string Name, int Arity)> suppressed = [];
    private readonly List<(string Module, string Name, int Arity)> imports = [];

    internal int EunitAutoExportCount { get; private set; }
    internal bool HasMatchSpecTransform { get; private set; }
    internal bool HasDiameterRecordTransform { get; private set; }
    internal bool HasOtherCompileOptions { get; private set; }
    internal bool DisableModuleOptimization { get; private set; }
    internal bool LegacyBeamTables { get; private set; }
    private bool enableAutomaticInlining;
    private bool disableAutomaticInlining;
    internal bool AutomaticInlining => enableAutomaticInlining && !disableAutomaticInlining;
    internal int? AutomaticInlineSize { get; private set; }
    internal HashSet<(string Name, int Arity)> InlineTargets { get; } = [];

    public void Import(string module, Term signatures, int offset)
    {
        ModuleNameValidation.Validate(module);
        foreach (var signature in ReadSignatures(signatures, offset).Distinct())
        {
            if (imports.Any(value => value.Name == signature.Name && value.Arity == signature.Arity))
                throw new CompileException(
                    CompilerDiagnosticCodes.FunctionDefinition,
                    SourceCallBindingDiagnostics.DuplicateImport(signature.Name, signature.Arity),
                    offset
                );
            imports.Add((module, signature.Name, signature.Arity));
        }
    }

    public void Compile(Term options, int offset)
    {
        if (options is Cons or Nil)
        {
            foreach (var option in Cons.Items(options))
                Compile(option, offset);

            return;
        }
        if (DiameterRecordSourceTransform.IsOption(options))
        {
            HasDiameterRecordTransform = true;
            return;
        }
        if (MatchSpecSourceTransform.IsOption(options))
        {
            HasMatchSpecTransform = true;
            return;
        }
        if (EunitAutoExport.IsTransform(options))
        {
            EunitAutoExportCount++;
            return;
        }
        if (options is Atom { Name: "no_module_opt" })
        {
            DisableModuleOptimization = true;
            return;
        }
        if (CompilerWarningOptions.IsSupported(options)
            || options is Atom { Name: "no_native" or "verbose" or CompilerOptionNames.NoAutoImportTypes })
            return;
        if (options is Atom { Name: CompilerOptionNames.Inline or CompilerOptionNames.NoInline } inline)
        {
            enableAutomaticInlining |= inline.Name == CompilerOptionNames.Inline;
            disableAutomaticInlining |= inline.Name == CompilerOptionNames.NoInline;
            return;
        }
        if (options is TupleTerm { Items: [Atom { Name: CompilerOptionNames.InlineSize }, Integer size] }
            && size.Value >= int.MinValue && size.Value <= int.MaxValue)
        {
            AutomaticInlineSize ??= Math.Max(1, (int)size.Value);
            return;
        }
        if (options is TupleTerm { Items: [Atom { Name: CompilerOptionNames.Inline }, var targets] })
        {
            InlineTargets.UnionWith(ReadSignatures(targets is TupleTerm ? Term.List(targets) : targets, offset));

            return;
        }
        if (options is Atom { Name: "r26" or "r27" })
        {
            LegacyBeamTables = true;
            return;
        }
        if (options is Atom { Name: CompilerOptionNames.DisableSsaOptimization })
        {
            HasOtherCompileOptions = true;
            return;
        }
        if (options is Atom { Name: SourceCallBindingNames.NoAutoImport })
        {
            suppressAll = true;

            return;
        }
        if (options is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0] is Atom { Name: SourceCallBindingNames.NoAutoImport })
        {
            var pending = new Stack<Term>();
            pending.Push(tuple.Items[1]);
            while (pending.TryPop(out var entry))
            {
                if (entry is Cons cell)
                {
                    pending.Push(cell.Tail);
                    pending.Push(cell.Head);
                }
                else if (entry is TupleTerm { Items.Count: 2 } signature && signature.Items[0] is Atom name && signature.Items[1] is Integer arity && arity.Value >= int.MinValue && arity.Value <= int.MaxValue)
                    suppressed.Add((name.Name, (int)arity.Value));
                else if (entry is not Nil && entry is not TupleTerm)
                {
                    // erl_lint filters non-signature entries after flattening the list.
                }
            }

            return;
        }

        throw new CompileException(CompilerDiagnosticCodes.UnsupportedSyntax, $"Unsupported -compile option: {options}", offset);
    }

    internal static IEnumerable<(string Name, int Arity)> ReadSignatures(Term list, int offset)
    {
        while (list is Cons cell)
        {
            if (cell.Head is not TupleTerm { Items.Count: 2 } tuple || tuple.Items[0] is not Atom name || tuple.Items[1] is not Integer arity || arity.Value < 0 || arity.Value > byte.MaxValue)
                throw new CompileException(CompilerDiagnosticCodes.Syntax, SourceCallBindingDiagnostics.InvalidDirective, offset);
            yield return (name.Name, (int)arity.Value);
            list = cell.Tail;
        }
        if (list is not Nil)
            throw new CompileException(CompilerDiagnosticCodes.Syntax, SourceCallBindingDiagnostics.InvalidDirective, offset);
    }

    public SourceCallBindings Build() => new(suppressAll, suppressed.OrderBy(value => value.Name, StringComparer.Ordinal).ThenBy(value => value.Arity), imports);
}
