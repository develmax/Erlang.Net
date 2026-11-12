// SPDX-License-Identifier: Apache-2.0
// Accessor structure and source lines follow OTP-29.1.1 diameter_exprecs.erl.

namespace Erlang.Compiler;

internal static class DiameterRecordSourceTransform
{
    internal static bool IsOption(Term option) =>
        option is TupleTerm { Items: [Atom { Name: "parse_transform" }, Atom { Name: "diameter_exprecs" }] };

    internal static (FunctionDefinition Function, int Line)[] Generate(
        IReadOnlyList<ModuleAttribute> attributes,
        IReadOnlyDictionary<string, RecordDefinition> definitions
    )
    {
        var records = attributes.Where(attribute => attribute.Name == "export_records")
            .SelectMany(attribute => Cons.Items(attribute.Value))
            .Select(term => term is Atom name && definitions.TryGetValue(name.Name, out var definition)
                ? definition : throw new NotSupportedException(BeamEmissionFormat.Unsupported))
            .ToArray();

        var functions = new List<(FunctionDefinition, int)>
        {
            Function("#info-", 1, 188, [Clause([Variable("RecName")], Local("#info-", Value("RecName"), AtomValue("fields")))]),
            Function("#info-", 2, 194, WithBadArg(records.Select(record =>
                Clause([Tag(record), Variable("Info")], Local("#info-" + record.Name, Value("Info")))), 2)),
            Function("#new-", 1, 203, WithBadArg(records.SelectMany(record => new[]
            {
                Clause([Tag(record)], NewRecord(record)),
                Clause([new Pattern.List([Tag(record)], Variable("Vals"))], Local("#new-" + record.Name, Value("Vals")))
            }), 1)),
            Function("#new-", 2, 215, WithBadArg(records.Select(record =>
                Clause([Tag(record), Variable("Vals")], Local("#new-" + record.Name, Value("Vals")))), 2)),
            Function("#get-", 1, 224, WithBadArg(records.Select(record =>
                Clause([RecordAlias(record)], new Expr.List([AtomValue(record.Name)], Local("#get-" + record.Name, Value("Rec"))))), 1)),
            Function("#get-", 2, 233, WithBadArg(records.Select(record =>
                Clause([Variable("Attrs"), RecordAlias(record)], Local("#get-" + record.Name, Value("Attrs"), Value("Rec")))), 2)),
            Function("#set-", 2, 243, WithBadArg(records.Select(record =>
                Clause([Variable("Vals"), RecordAlias(record)], Local("#set-" + record.Name, Value("Vals"), Value("Rec")))), 2))
        };
        foreach (RecordDefinition record in records)
        {
            string create = "#new-" + record.Name;
            string get = "#get-" + record.Name;
            string set = "#set-" + record.Name;
            string info = "#info-" + record.Name;
            var names = new Expr.Literal(Cons.From(record.Fields.Select(field => Term.A(field.Name))));
            int arity = record.Fields.Count + 1;

            functions.Add(Function(create, 0, 252, [Clause([], NewRecord(record))]));
            functions.Add(Function(create, 1, 258, [Clause([Variable("Vals")], Local(set, Value("Vals"), NewRecord(record)))]));
            functions.Add(Function(get, 1, 281, [Clause([Variable("Rec")],
                new Expr.Call("lists", "zip", [names, Local("tl", Local("tuple_to_list", Value("Rec")))]))]));

            var getClauses = new List<Clause>
            {
                new([Variable("Attrs"), Variable("Rec")], Local("is_list", Value("Attrs")),
                    new Expr.ListComprehension([Local(get, Value("A"), Value("Rec"))],
                        [new ComprehensionQualifier.Generator(Variable("A"), Value("Attrs"))]))
            };
            getClauses.AddRange(record.Fields.Select((field, index) =>
                Clause([Literal(field.Name), Variable("Rec")], new Expr.RecordAccess(Value("Rec"), record.Name, arity, index + 1))));
            functions.Add(Function(get, 2, 288, getClauses));

            var setClauses = new List<Clause>
            {
                new([Variable("Vals"), Variable("Rec")], Local("is_list", Value("Vals")),
                    new Expr.Call("lists", "foldl", [new Expr.FunctionReference(set, 2), Value("Rec"), Value("Vals")]))
            };
            setClauses.AddRange(record.Fields.Select((field, index) =>
                Clause([new Pattern.Tuple([Literal(field.Name), Variable("V")]), Variable("Rec")],
                    new Expr.RecordUpdate(Value("Rec"), record.Name, arity, [new(index + 1, Value("V"))]))));
            functions.Add(Function(set, 2, 264, setClauses));

            var infoClauses = new List<Clause>
            {
                Clause([Literal("fields")], names),
                Clause([Literal("size")], new Expr.Literal(Term.I(arity)))
            };
            infoClauses.AddRange(record.Fields.Select((field, index) =>
                Clause([new Pattern.Tuple([Literal("index"), Literal(field.Name)])], new Expr.Literal(Term.I(index + 2)))));
            functions.Add(Function(info, 1, 301, infoClauses));
        }

        return functions.ToArray();
    }

    private static (FunctionDefinition, int) Function(string name, int arity, int line, IReadOnlyList<Clause> clauses) =>
        (new(name, arity, clauses), line);

    private static Clause[] WithBadArg(IEnumerable<Clause> clauses, int arity) =>
        [.. clauses, Clause(Enumerable.Repeat<Pattern>(Variable(VariableScopeNames.Wildcard), arity).ToArray(),
            new Expr.Call(ErlangModuleNames.Module, "error", [AtomValue("badarg")]))];

    private static Clause Clause(IReadOnlyList<Pattern> patterns, Expr body) => new(patterns, null, body);
    private static Pattern.Variable Variable(string name) => new(name);
    private static Expr.Variable Value(string name) => new(name);
    private static Pattern.Literal Literal(string name) => new(Term.A(name));
    private static Expr.Literal AtomValue(string name) => new(Term.A(name));
    private static Expr.Call Local(string name, params Expr[] arguments) => new(null, name, arguments);
    private static Pattern Tag(RecordDefinition record) => Literal(record.Name);
    private static Expr NewRecord(RecordDefinition record) => new Expr.RecordConstruction(record, [], null);
    private static Pattern RecordAlias(RecordDefinition record) => new AliasPattern(
        new Pattern.Tuple([Tag(record), .. record.Fields.Select(_ => Variable(VariableScopeNames.Wildcard))]), Variable("Rec"));
}
