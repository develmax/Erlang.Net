// SPDX-License-Identifier: Apache-2.0
// Object macro behavior based on OTP-29.1.1 epp.erl, copyright Ericsson AB.

using System.Globalization;
using System.Text;

namespace Erlang.Compiler;

internal sealed class BeamMacroPreprocessor(
    string source,
    string? sourcePath = null,
    BeamIncludeResolver? includes = null,
    bool deterministicFileNames = true,
    IReadOnlyDictionary<string, Term>? predefined = null
)
{
    private const string FileDirective = "file";
    private const string FeatureDirective = "feature";
    internal const string MaybeFeature = "maybe_expr";
    private const string Define = "define";
    private const string Undef = "undef";
    private const string IfDefined = "ifdef";
    private const string IfNotDefined = "ifndef";
    private const string ElseIf = "elif";
    private const string EndIf = "endif";
    private const string ModuleMacro = "MODULE";
    private const string ModuleStringMacro = "MODULE_STRING";
    private const string FileMacro = "FILE";
    private const string FunctionNameMacro = "FUNCTION_NAME";
    private const string FunctionArityMacro = "FUNCTION_ARITY";
    private const string LineMacro = "LINE";
    private const string ReleaseMacro = "OTP_RELEASE";
    private const int OtpRelease = 29;
    private const string MacroMarker = "?";
    private const int ExpansionDepthLimit = 64;
    private const int ExpandedTokenLimit = 1_000_000;
    private const int IncludeDepthLimit = 32;
    private const int CombinedSourceLimit = 16_000_000;
    private readonly Dictionary<string, Dictionary<int, MacroDefinition>> definitions = new(StringComparer.Ordinal);
    private readonly List<Token> output = [];
    private readonly Stack<Conditional> conditions = new();
    private bool active = true;
    private bool inFeaturePrefix = true;
    internal bool MaybeEnabled { get; private set; } = true;
    private int remainingSubstitutionTokens = ExpandedTokenLimit;
    private readonly StringBuilder combinedSource = new(source);
    private readonly Stack<string> includeStack = new();
    private readonly List<BeamFileTransition> fileTransitions = [];
    internal string CombinedSource => combinedSource.ToString();
    internal IReadOnlyList<BeamFileTransition> FileTransitions => fileTransitions;
    private Token? module;
    private int? currentFormStart;
    private (string Name, int Arity)? currentFunction;

    private sealed record Conditional(Token Start, bool ParentActive, bool Taken, bool HasElse = false);
    private sealed record MacroDefinition(string[] Parameters, Token[] Replacement);
    private const int ObjectMacroArity = -1;

    internal List<Token> Run()
    {
        if (predefined is not null)
        {
            foreach (var (name, value) in predefined)
            {
                string spelling = value switch
                {
                    Atom atom => BeamMacroStringification.Quote(atom.Name, '\''),
                    Integer integer => integer.Value.ToString(CultureInfo.InvariantCulture),
                    Cons text => BeamMacroStringification.Quote(BeamDocumentationText.ReadText(text), '"'),
                    Nil => "[]",
                    _ => throw new NotSupportedException(BeamMacroDiagnostics.InvalidDefinition)
                };
                definitions.Add(name, new() { [ObjectMacroArity] = new([], Lexer.Scan(spelling).SkipLast(1).ToArray()) });
            }
        }

        if (sourcePath is not null)
        {
            includeStack.Push(sourcePath);
        }
        var tokens = ScanDocument(source, 0);
        ProcessDocument(tokens, sourcePath);
        output.Add(tokens[^1]);

        return output;
    }

    private void ProcessDocument(List<Token> tokens, string? currentPath, string? includeRequest = null)
    {
        int conditionDepth = conditions.Count;
        string? mappedFile = null;
        int lineOffset = 0;
        int columnResetLine = 0;
        int columnStart = 0;
        bool mapped = false;
        for (int start = 0; start < tokens.Count - 1;)
        {
            int end = start;
            while (end < tokens.Count - 1 && tokens[end].Kind != LexerTokenKinds.FormTerminator)
            {
                end++;
            }

            int formLength = end - start;
            var form = tokens.GetRange(start, formLength);
            Token terminator = tokens[end];
            if (mapped)
            {
                for (int index = 0; index < form.Count; index++)
                {
                    form[index] = Map(form[index]);
                }
                terminator = Map(terminator);
            }
            if (HandleConditional(form, terminator, conditionDepth))
            {
                start = end + 1;
                continue;
            }
            if (!active)
            {
                start = end + 1;
                continue;
            }
            string? directive = AttributeName(form);
            if (directive == FeatureDirective)
            {
                ReadFeature(form, terminator);
                start = end + 1;
                continue;
            }
            if (directive is not (ModuleAttributeNames.Module or ModuleAttributeNames.Doc or ModuleAttributeNames.ModuleDoc
                or Define or Undef or "include" or "include_lib"))
            {
                inFeaturePrefix = false;
            }
            if (!MaybeEnabled)
            {
                for (int index = 0; index < form.Count; index++)
                {
                    if (form[index] is { Kind: LexerTokenKinds.Keyword, Text: ErlangKeywords.Maybe or ErlangKeywords.Else })
                    {
                        form[index] = form[index] with { Kind = LexerTokenKinds.Atom };
                    }
                }
            }
            if (directive is "include" or "include_lib")
            {
                Include(form, terminator, currentPath, tokens[^1].Start, mapped ? null : includeRequest);
            }
            else if (TryReadExternalDocumentation(form, terminator, currentPath, mapped ? null : includeRequest))
            {
                // The directive expands to a documentation string and two file transitions.
            }
            else if (IsAttribute(form, Undef))
            {
                Token name = ReadMacroArgument(form, terminator);
                if (IsPredefined(name.Text))
                {
                    throw Error(BeamMacroDiagnostics.UnsupportedDirective(Undef), name);
                }
                definitions.Remove(name.Text);
            }
            else if (IsAttribute(form, Define))
            {
                if (terminator.Kind != LexerTokenKinds.FormTerminator)
                {
                    throw Error(BeamMacroDiagnostics.InvalidDefinition, form[0]);
                }
                DefineMacro(form);
            }
            else
            {
                int outputStart = output.Count;
                currentFormStart = outputStart;
                currentFunction = null;
                Expand(form, 0);
                currentFormStart = null;
                if (IsAttribute(form, FileDirective))
                {
                    var (file, line) = ReadFileDirective(outputStart, form[1], terminator);
                    output.RemoveRange(outputStart, output.Count - outputStart);
                    fileTransitions.Add(new(
                        outputStart,
                        file,
                        line,
                        form[1]
                    ));
                    string? mainFile = sourcePath is null ? null
                        : deterministicFileNames ? Path.GetFileName(sourcePath) : sourcePath;
                    mappedFile = file == mainFile ? null : file;
                    lineOffset = line - tokens[start + 1].PreprocessorLine;
                    columnResetLine = tokens[end].PreprocessorLine;
                    // The scanner consumes the separator after the dot before resetting the column.
                    columnStart = terminator.End + 1;
                    mapped = true;
                    start = end + 1;
                    continue;
                }
                if (output.Count - outputStart == 5 && IsAttribute(form, ModuleAttributeNames.Module)
                    && IsSymbol(output[outputStart + 2], ErlangSyntaxTokens.OpenParenthesis)
                    && IsName(output[outputStart + 3]) && IsSymbol(output[outputStart + 4], ErlangSyntaxTokens.CloseParenthesis))
                {
                    module = output[outputStart + 3];
                }
                if (terminator.Kind == LexerTokenKinds.FormTerminator)
                {
                    output.Add(terminator);
                }
            }
            start = end + 1;
        }

        if (conditions.Count != conditionDepth)
        {
            throw Error(BeamMacroDiagnostics.UnclosedConditional, conditions.Peek().Start);
        }
        if (mapped)
        {
            tokens[^1] = Map(tokens[^1]);
        }

        Token Map(Token token) => token with
        {
            PreprocessorFile = mappedFile,
            PreprocessorLine = checked(token.PreprocessorLine + lineOffset),
            HasExplicitFileLocation = true,
            PreprocessorColumnStart = token.Start >= columnStart
                && token.PreprocessorLine == columnResetLine ? columnStart : null
        };
    }

    private (string File, int Line) ReadFileDirective(int start, Token directive, Token terminator)
    {
        int cursor = start + 3;
        if (terminator.Kind != LexerTokenKinds.FormTerminator || cursor >= output.Count
            || !IsSymbol(output[start + 2], ErlangSyntaxTokens.OpenParenthesis)
            || output[cursor].Kind != LexerTokenKinds.String)
        {
            throw Error(BeamMacroDiagnostics.InvalidDirective(FileDirective), directive);
        }

        string file = StringLiteralTokens.Read(output, ref cursor);
        if (output.Count - cursor != 3 || !IsSymbol(output[cursor], ErlangSyntaxTokens.Comma)
            || output[cursor + 1].Kind != LexerTokenKinds.Integer
            || !int.TryParse(
                output[cursor + 1].Text,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int line
            )
            || !IsSymbol(output[cursor + 2], ErlangSyntaxTokens.CloseParenthesis))
        {
            throw Error(BeamMacroDiagnostics.InvalidDirective(FileDirective), directive);
        }

        int separator = Math.Max(file.LastIndexOf('/'), file.LastIndexOf('\\'));

        return (deterministicFileNames ? file[(separator + 1)..] : file, line);
    }

    private bool HandleConditional(List<Token> form, Token terminator, int conditionDepth)
    {
        string? directive = AttributeName(form);
        switch (directive)
        {
            case IfDefined:
            case IfNotDefined:
            case ErlangKeywords.If:
                bool parent = active;
                active = parent && (directive == ErlangKeywords.If
                    ? EvaluateCondition(form, terminator)
                    : IsDefined(ReadMacroArgument(form, terminator).Text) == (directive == IfDefined));
                conditions.Push(new(form[0], parent, active));

                return true;
            case ElseIf:
            case ErlangKeywords.Else:
            case EndIf:
                if (conditions.Count == conditionDepth || !conditions.TryPop(out var condition) || (directive != EndIf && condition.HasElse))
                {
                    throw Error(BeamMacroDiagnostics.UnexpectedConditional(directive), form[0]);
                }
                if (directive != ElseIf && (form.Count != 2 || terminator.Kind != LexerTokenKinds.FormTerminator))
                {
                    throw Error(BeamMacroDiagnostics.InvalidDirective(directive), form[0]);
                }
                if (directive == EndIf)
                {
                    active = condition.ParentActive;
                }
                else
                {
                    active = condition.ParentActive && !condition.Taken
                        && (directive == ErlangKeywords.Else || EvaluateCondition(form, terminator));
                    conditions.Push(condition with { Taken = condition.Taken || active, HasElse = directive == ErlangKeywords.Else });
                }

                return true;
            default:
                return false;
        }
    }

    private void ReadFeature(List<Token> form, Token terminator)
    {
        if (!inFeaturePrefix)
        {
            throw Error(BeamMacroDiagnostics.FeatureAfterPrefix, form[1]);
        }
        if (form.Count != 7 || !IsSymbol(form[2], ErlangSyntaxTokens.OpenParenthesis)
            || !IsName(form[3]) || !IsSymbol(form[4], ErlangSyntaxTokens.Comma)
            || !IsName(form[5]) || !IsSymbol(form[6], ErlangSyntaxTokens.CloseParenthesis)
            || terminator.Kind != LexerTokenKinds.FormTerminator || form[5].Text is not ("enable" or "disable"))
        {
            throw Error(BeamMacroDiagnostics.InvalidDirective(FeatureDirective), form[1]);
        }
        if (form[3].Text != MaybeFeature)
        {
            throw Error(BeamMacroDiagnostics.UnsupportedFeature(form[3].Text), form[3]);
        }

        MaybeEnabled = form[5].Text == "enable";
    }

    private bool TryReadExternalDocumentation(List<Token> form, Token terminator, string? currentPath, string? includeRequest)
    {
        if (AttributeName(form) is not (ModuleAttributeNames.Doc or ModuleAttributeNames.ModuleDoc)
            || terminator.Kind != LexerTokenKinds.FormTerminator || form.Count < 7)
        {
            return false;
        }

        bool wrapped = IsSymbol(form[2], ErlangSyntaxTokens.OpenParenthesis);
        int start = wrapped ? 3 : 2;
        if (form.Count <= start + 4 || !IsSymbol(form[start], ErlangSyntaxTokens.OpenTuple)
            || !IsName(form[start + 1]) || form[start + 1].Text != FileDirective
            || !IsSymbol(form[start + 2], ErlangSyntaxTokens.Comma)
            || form[start + 3].Kind != LexerTokenKinds.String)
        {
            return false;
        }

        int cursor = start + 3;
        string requested = StringLiteralTokens.Read(form, ref cursor);
        if (cursor != form.Count - (wrapped ? 2 : 1) || !IsSymbol(form[cursor], ErlangSyntaxTokens.CloseTuple)
            || wrapped && !IsSymbol(form[^1], ErlangSyntaxTokens.CloseParenthesis))
        {
            return false;
        }

        string? resolved = currentPath is null ? null : includes?.Resolve(currentPath, requested, library: false);
        if (resolved is null)
        {
            throw Error(BeamMacroDiagnostics.DocumentationMissing(requested), form[1]);
        }
        if (new FileInfo(resolved).Length > CombinedSourceLimit)
        {
            throw Error(BeamMacroDiagnostics.IncludeLimit, form[1]);
        }

        string content = File.ReadAllText(resolved);
        string file = deterministicFileNames ? Path.GetFileName(resolved) : requested;
        var location = form[1] with
        {
            PreprocessorLine = 1,
            PreprocessorColumnStart = form[1].Start,
            PreprocessorFile = file,
            HasExplicitFileLocation = true
        };
        fileTransitions.Add(new(output.Count, file, 1));
        output.Add(form[0]);
        output.Add(location);
        output.Add(location with { Kind = LexerTokenKinds.String, Text = content });
        output.Add(terminator);
        var annotation = (TupleTerm)BeamDocumentationWriter.Annotation(terminator, CombinedSource);
        int column = (int)((Integer)annotation.Items[1]).Value;
        fileTransitions.Add(new(output.Count, terminator.PreprocessorFile, terminator.PreprocessorLine, Column: column, IncludeRequest: includeRequest));
        return true;
    }

    private void Include(List<Token> form, Token terminator, string? currentPath, int documentEnd, string? parentIncludeRequest)
    {
        string directive = form[1].Text;
        if (form.Count < 5 || !IsSymbol(form[2], ErlangSyntaxTokens.OpenParenthesis)
            || !IsSymbol(form[^1], ErlangSyntaxTokens.CloseParenthesis) || terminator.Kind != LexerTokenKinds.FormTerminator)
        {
            throw Error(BeamMacroDiagnostics.InvalidDirective(directive), form[0]);
        }

        var path = new StringBuilder();
        for (int index = 3; index < form.Count - 1; index++)
        {
            if (form[index].Kind != LexerTokenKinds.String)
            {
                throw Error(BeamMacroDiagnostics.InvalidDirective(directive), form[0]);
            }
            path.Append(form[index].Text);
        }
        string requested = path.ToString();
        bool library = directive == "include_lib";
        string? resolved = currentPath is null ? null : includes?.Resolve(currentPath, requested, library);
        if (resolved is null)
        {
            throw Error(BeamMacroDiagnostics.IncludeMissing(requested), form[0]);
        }
        if (includeStack.Count >= IncludeDepthLimit || new FileInfo(resolved).Length > CombinedSourceLimit - combinedSource.Length)
        {
            throw Error(BeamMacroDiagnostics.IncludeLimit, form[0]);
        }

        string content = File.ReadAllText(resolved);
        if (content.Length >= CombinedSourceLimit - combinedSource.Length)
        {
            throw Error(BeamMacroDiagnostics.IncludeLimit, form[0]);
        }
        combinedSource.Append('\n');
        int offset = combinedSource.Length;
        combinedSource.Append(content);
        includeStack.Push(resolved);
        try
        {
            string file = Path.GetFileName(resolved);
            string? includeRequest = library ? requested : null;
            fileTransitions.Add(new(output.Count, file, 1, IncludeRequest: includeRequest));
            ProcessDocument(ScanDocument(content, offset, file), resolved, includeRequest);
            fileTransitions.Add(IncludeReturnTransition(form[0].PreprocessorFile, terminator, documentEnd, parentIncludeRequest));
        }
        catch (CompileException error)
        {
            throw new CompileException(error.Code, BeamMacroDiagnostics.IncludedError(requested, error.Message), form[0].Start);
        }
        finally
        {
            includeStack.Pop();
        }
    }

    private BeamFileTransition IncludeReturnTransition(string? file, Token terminator, int documentEnd, string? includeRequest)
    {
        int next = terminator.End;
        char following = next < documentEnd ? combinedSource[next] : '\0';
        // OTP consumes only the first whitespace character after the directive's full stop.
        if (following == '\n')
        {
            return new(output.Count, file, terminator.PreprocessorLine + 1, IncludeRequest: includeRequest);
        }

        int start = terminator.PreprocessorColumnStart ?? terminator.Start;
        if (terminator.PreprocessorColumnStart is null)
        {
            while (start > 0 && combinedSource[start - 1] != '\n')
            {
                start--;
            }
        }
        int column = 1;
        for (int index = start; index < next; index++)
        {
            if (!char.IsLowSurrogate(combinedSource[index]))
            {
                column++;
            }
        }
        if (char.IsWhiteSpace(following))
        {
            column++;
        }

        return new(output.Count, file, terminator.PreprocessorLine, Column: column, IncludeRequest: includeRequest);
    }

    private static List<Token> ScanDocument(string content, int offset, string? file = null)
    {
        var tokens = Lexer.Scan(content);
        int cursor = 0;
        int line = 1;
        for (int index = 0; index < tokens.Count; index++)
        {
            Token token = tokens[index];
            while (cursor < token.Start)
            {
                if (content[cursor++] == '\n')
                {
                    line++;
                }
            }
            tokens[index] = token with { Start = token.Start + offset, End = token.End + offset, PreprocessorLine = line, PreprocessorFile = file };
        }

        return tokens;
    }

    private bool EvaluateCondition(List<Token> form, Token terminator)
    {
        if (form.Count < 5 || !IsSymbol(form[2], ErlangSyntaxTokens.OpenParenthesis)
            || !IsSymbol(form[^1], ErlangSyntaxTokens.CloseParenthesis)
            || terminator.Kind != LexerTokenKinds.FormTerminator)
        {
            throw Error(BeamMacroDiagnostics.InvalidDirective(form[1].Text), form[0]);
        }

        int start = output.Count;
        Expand(form.GetRange(2, form.Count - 2), 0);
        var expression = output.GetRange(start, output.Count - start);
        output.RemoveRange(start, output.Count - start);
        expression.Add(terminator with { Kind = LexerTokenKinds.EndOfInput, Text = string.Empty });
        var parsed = new Parser(CombinedSource, expression).ParseExpression();

        return new BeamConditionalExpression(name => IsPredefined(name) || definitions.ContainsKey(name), form[0].Start).IsTrue(parsed);
    }

    private bool IsDefined(string name) =>
        definitions.ContainsKey(name) || name is LineMacro or FileMacro or "MACHINE" or "BEAM" or ReleaseMacro or "FEATURE_AVAILABLE" or "FEATURE_ENABLED"
        || (module is not null && name is ModuleMacro or ModuleStringMacro);

    private static bool IsPredefined(string name) =>
        name is ModuleMacro or LineMacro or FileMacro or FunctionNameMacro or FunctionArityMacro or ModuleStringMacro
            or "BASE_MODULE" or "BASE_MODULE_STRING" or "MACHINE" or "BEAM" or ReleaseMacro or "FEATURE_AVAILABLE" or "FEATURE_ENABLED";

    private static Token ReadMacroArgument(List<Token> form, Token terminator)
    {
        if (form.Count != 5 || !IsSymbol(form[2], ErlangSyntaxTokens.OpenParenthesis)
            || !IsName(form[3]) || !IsSymbol(form[4], ErlangSyntaxTokens.CloseParenthesis)
            || terminator.Kind != LexerTokenKinds.FormTerminator)
        {
            throw Error(BeamMacroDiagnostics.InvalidDirective(form[1].Text), form[0]);
        }

        return form[3];
    }

    private void DefineMacro(List<Token> form)
    {
        if (form.Count < 6 || !IsSymbol(form[2], ErlangSyntaxTokens.OpenParenthesis)
            || !IsName(form[3])
            || !IsSymbol(form[^1], ErlangSyntaxTokens.CloseParenthesis))
        {
            throw Error(BeamMacroDiagnostics.InvalidDefinition, form[0]);
        }
        Token name = form[3];
        int body = 4;
        int arity = ObjectMacroArity;
        string[] parameters = [];
        if (IsSymbol(form[body], ErlangSyntaxTokens.OpenParenthesis))
        {
            var arguments = BeamMacroArguments.Read(form, ref body);
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var argument in arguments)
            {
                if (argument is not [{ Kind: LexerTokenKinds.Variable } parameter] || !names.Add(parameter.Text))
                {
                    throw Error(BeamMacroDiagnostics.InvalidDefinition, name);
                }
            }
            parameters = arguments.Select(argument => argument[0].Text).ToArray();
            arity = parameters.Length;
            body++;
        }
        if (body >= form.Count - 1 || !IsSymbol(form[body], ErlangSyntaxTokens.Comma))
        {
            throw Error(BeamMacroDiagnostics.InvalidDefinition, name);
        }
        if (IsPredefined(name.Text))
        {
            throw Error(BeamMacroDiagnostics.Redefined(name.Text), name);
        }
        if (!definitions.TryGetValue(name.Text, out var overloads))
        {
            overloads = [];
            definitions.Add(name.Text, overloads);
        }
        if (!overloads.TryAdd(arity, new(parameters, form.GetRange(body + 1, form.Count - body - 2).ToArray())))
        {
            throw Error(BeamMacroDiagnostics.Redefined(name.Text), name);
        }
    }

    private void Expand(IReadOnlyList<Token> tokens, int depth)
    {
        for (int index = 0; index < tokens.Count; index++)
        {
            Token token = tokens[index];
            if (depth >= ExpansionDepthLimit || output.Count >= ExpandedTokenLimit)
            {
                throw Error(BeamMacroDiagnostics.ExpansionLimit, token);
            }
            if (!IsSymbol(token, MacroMarker))
            {
                output.Add(token);
                continue;
            }
            if (++index >= tokens.Count || !IsName(tokens[index]))
            {
                throw Error(BeamMacroDiagnostics.ExpectedName, token);
            }

            Token name = tokens[index];
            Token location = name;
            if (name.Text == ModuleMacro && module is not null)
            {
                output.Add(AtLocation(module, location));
            }
            else if (name.Text == ModuleStringMacro && module is not null)
            {
                output.Add(location with { Kind = LexerTokenKinds.String, Text = module.Text });
            }
            else if (name.Text == FileMacro && sourcePath is not null)
            {
                string file = location.PreprocessorFile
                    ?? (location.HasExplicitFileLocation && !deterministicFileNames ? sourcePath : Path.GetFileName(sourcePath));
                output.Add(location with { Kind = LexerTokenKinds.String, Text = file });
            }
            else if (name.Text is FunctionNameMacro or FunctionArityMacro)
            {
                var function = currentFunction ??= ReadCurrentFunction(location);
                output.Add(name.Text == FunctionNameMacro
                    ? location with { Kind = LexerTokenKinds.QuotedAtom, Text = function.Name }
                    : location with { Kind = LexerTokenKinds.Integer, Text = function.Arity.ToString(CultureInfo.InvariantCulture) });
            }
            else if (name.Text is LineMacro or ReleaseMacro)
            {
                string number = (name.Text == LineMacro ? location.PreprocessorLine : OtpRelease).ToString(CultureInfo.InvariantCulture);
                output.Add(location with { Kind = LexerTokenKinds.Integer, Text = number });
            }
            else if (definitions.TryGetValue(name.Text, out var overloads))
            {
                ExpandDefinition(
                    tokens,
                    ref index,
                    depth,
                    location,
                    name,
                    overloads
                );
            }
            else
            {
                throw Error(BeamMacroDiagnostics.Undefined(name.Text), location);
            }
        }
    }

    private (string Name, int Arity) ReadCurrentFunction(Token location)
    {
        if (currentFormStart is not int start || output.Count - start < 3
            || output[start].Kind is not (LexerTokenKinds.Atom or LexerTokenKinds.QuotedAtom)
            || !IsSymbol(output[start + 1], ErlangSyntaxTokens.OpenParenthesis))
        {
            throw Error(BeamMacroDiagnostics.UnsupportedFunctionContext, location);
        }
        int closing = start + 1;
        try
        {
            var parameters = BeamMacroArguments.Read(output, ref closing);
            return (output[start].Text, parameters.Count);
        }
        catch (CompileException)
        {
            throw Error(BeamMacroDiagnostics.UnsupportedFunctionContext, location);
        }
    }

    private void ExpandDefinition(
        IReadOnlyList<Token> tokens,
        ref int index,
        int depth,
        Token location,
        Token name,
        Dictionary<int, MacroDefinition> overloads
    )
    {
        bool called = index + 1 < tokens.Count && IsSymbol(tokens[index + 1], ErlangSyntaxTokens.OpenParenthesis);
        if (called && (overloads.Count != 1 || !overloads.ContainsKey(ObjectMacroArity)))
        {
            int closing = index + 1;
            var arguments = BeamMacroArguments.Read(tokens, ref closing);
            if (overloads.TryGetValue(arguments.Count, out var function))
            {
                index = closing;
                Expand(Substitute(function, arguments, location), depth + 1);

                return;
            }

            throw Error(BeamMacroDiagnostics.ArgumentMismatch(name.Text), location);
        }
        if (!overloads.TryGetValue(ObjectMacroArity, out var definition))
        {
            throw Error(BeamMacroDiagnostics.ArgumentMismatch(name.Text), location);
        }

        Expand(Substitute(definition, [], location), depth + 1);
    }

    private List<Token> Substitute(MacroDefinition definition, List<Token[]> arguments, Token location)
    {
        var bindings = new Dictionary<string, Token[]>(StringComparer.Ordinal);
        for (int index = 0; index < definition.Parameters.Length; index++)
        {
            bindings.Add(definition.Parameters[index], arguments[index]);
        }

        var result = new List<Token>();
        for (int index = 0; index < definition.Replacement.Length; index++)
        {
            Token token = definition.Replacement[index];
            if (index + 2 < definition.Replacement.Length && IsSymbol(token, MacroMarker)
                && IsSymbol(definition.Replacement[index + 1], MacroMarker)
                && definition.Replacement[index + 2] is { Kind: LexerTokenKinds.Variable } parameter
                && bindings.TryGetValue(parameter.Text, out var argument))
            {
                ReserveSubstitutionTokens(1, location);
                result.Add(location with { Kind = LexerTokenKinds.String, Text = BeamMacroStringification.Render(argument, location.Start) });
                index += 2;
                continue;
            }
            if (token.Kind == LexerTokenKinds.Variable && bindings.TryGetValue(token.Text, out var value))
            {
                ReserveSubstitutionTokens(value.Length, location);
                result.AddRange(value);
                if (value.Length != 0)
                {
                    location = value[^1];
                }
            }
            else
            {
                ReserveSubstitutionTokens(1, location);
                result.Add(AtLocation(token, location));
            }
        }

        return result;
    }

    private static Token AtLocation(Token token, Token location) => token with
    {
        Start = location.Start,
        End = location.End,
        PreprocessorLine = location.PreprocessorLine,
        PreprocessorFile = location.PreprocessorFile,
        HasExplicitFileLocation = location.HasExplicitFileLocation,
        SigilDelimiterOffset = 0,
        PreprocessorColumnStart = location.PreprocessorColumnStart
    };

    private void ReserveSubstitutionTokens(int count, Token location)
    {
        if (count > remainingSubstitutionTokens)
        {
            throw Error(BeamMacroDiagnostics.ExpansionLimit, location);
        }
        remainingSubstitutionTokens -= count;
    }

    private static string? AttributeName(IReadOnlyList<Token> form) =>
        form.Count >= 2 && IsSymbol(form[0], ErlangSyntaxTokens.AttributeIntroducer)
        && form[1].Kind is LexerTokenKinds.Atom or LexerTokenKinds.Keyword ? form[1].Text : null;

    private static bool IsAttribute(IReadOnlyList<Token> form, string name) => AttributeName(form) == name;

    private static bool IsName(Token token) =>
        token.Kind is LexerTokenKinds.Atom or LexerTokenKinds.QuotedAtom or LexerTokenKinds.Variable;

    private static bool IsSymbol(Token token, string text) =>
        token.Kind == LexerTokenKinds.Symbol && token.Text == text;

    private static CompileException Error(string message, Token token) =>
        new(CompilerDiagnosticCodes.UnsupportedSyntax, message, token.Start);
}
