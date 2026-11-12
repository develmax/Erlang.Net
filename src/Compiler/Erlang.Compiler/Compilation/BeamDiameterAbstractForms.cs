// SPDX-License-Identifier: Apache-2.0
// Abstract forms and annotations follow OTP-29.1.1 diameter_exprecs.erl.

namespace Erlang.Compiler;

internal static class BeamDiameterAbstractForms
{
    internal static IEnumerable<Term> Write(IReadOnlyList<FunctionDefinition> accessors)
    {
        var records = accessors.Where(function => function.Arity == 0
                && function.Clauses is [{ Body: Expr.RecordConstruction }])
            .Select(function => ((Expr.RecordConstruction)function.Clauses[0].Body).Definition)
            .ToArray();
        yield return Form("attribute", 129, Term.A("export"),
            Cons.From(accessors.Select(function => Term.Tuple(Term.A(function.Name), Term.I(function.Arity)))));

        yield return Function("#info-", 1, 188, [Clause(189, [Var("RecName", 189)],
            Call("#info-", 191, Var("RecName", 191), Atom("fields", 191)))]);
        yield return Function("#info-", 2, 194, WithBadArg(records.Select(record =>
            Clause(198, [Atom(record.Name, 198), Var("Info", 198)], Call("#info-" + record.Name, 200, Var("Info", 200)))), 2, 195));
        yield return Function("#new-", 1, 203, WithBadArg(records.SelectMany(record => new[]
        {
            Clause(207, [Atom(record.Name, 207)], Record(record.Name, 209)),
            Clause(210, [Form("cons", 210, Atom(record.Name, 210), Var("Vals", 210))],
                Call("#new-" + record.Name, 212, Var("Vals", 212)))
        }), 1, 204));
        yield return Function("#new-", 2, 215, WithBadArg(records.Select(record =>
            Clause(219, [Atom(record.Name, 219), Var("Vals", 219)], Call("#new-" + record.Name, 221, Var("Vals", 221)))), 2, 216));
        yield return Function("#get-", 1, 224, WithBadArg(records.Select(record =>
            Clause(228, [Alias(record.Name, 228)], Form("cons", 230, Atom(record.Name, 230),
                Call("#get-" + record.Name, 230, Var("Rec", 230))))), 1, 225));
        yield return Function("#get-", 2, 233, WithBadArg(records.Select(record =>
            Clause(237, [Var("Attrs", 237), Alias(record.Name, 238)],
                Call("#get-" + record.Name, 240, Var("Attrs", 240), Var("Rec", 240)))), 2, 234));
        yield return Function("#set-", 2, 243, WithBadArg(records.Select(record =>
            Clause(247, [Var("Vals", 247), Alias(record.Name, 247)],
                Call("#set-" + record.Name, 249, Var("Vals", 249), Var("Rec", 249)))), 2, 244));

        foreach (var record in records)
        {
            string name = record.Name;
            string get = "#get-" + name;
            string set = "#set-" + name;
            yield return Function("#new-" + name, 0, 252, [Clause(253, [], Record(name, 255))]);
            yield return Function("#new-" + name, 1, 258,
                [Clause(259, [Var("Vals", 259)], Call(set, 261, Var("Vals", 261), Record(name, 261)))]);
            Term names = Form("nil", 284);
            foreach (var field in record.Fields.Reverse())
            {
                names = Form("cons", 284, Atom(field.Name, 284), names);
            }

            yield return Function(get, 1, 281, [Clause(282, [Var("Rec", 282)],
                Remote("lists", "zip", 284, names, Call("tl", 280, Call("tuple_to_list", 280, Var("Rec", 280)))))]);
            yield return Function(get, 2, 288,
                [Clause(289, [Var("Attrs", 289), Var("Rec", 289)],
                    Form("lc", 291, Call(get, 291, Var("A", 291), Var("Rec", 291)),
                        Term.List(Form("generate", 292, Var("A", 292), Var("Attrs", 292)))),
                    Call("is_list", 290, Var("Attrs", 290))),
                 .. record.Fields.Select(field => Clause(296, [Atom(field.Name, 296), Var("Rec", 296)],
                    Form("record_field", 298, Var("Rec", 298), Term.A(name), Atom(field.Name, 298))))]);
            yield return Function(set, 2, 264,
                [Clause(265, [Var("Vals", 265), Var("Rec", 265)],
                    Remote("lists", "foldl", 267, Form("fun", 267, Term.Tuple(Term.A("function"), Term.A(set), Term.I(2))),
                        Var("Rec", 268), Var("Vals", 269)), Call("is_list", 266, Var("Vals", 266))),
                 .. record.Fields.Select(field => Clause(273, [Tuple(273, Atom(field.Name, 273), Var("V", 273)), Var("Rec", 273)],
                    Form("record", 275, Var("Rec", 275), Term.A(name),
                        Term.List(Form("record_field", 276, Atom(field.Name, 276), Var("V", 276))))))]);
            yield return Function("#info-" + name, 1, 301,
                [Clause(302, [Atom("fields", 302)], Call("record_info", 304, Atom("fields", 304), Atom(name, 304))),
                 Clause(305, [Atom("size", 305)], Call("record_info", 307, Atom("size", 307), Atom(name, 307))),
                 .. record.Fields.Select(field => Clause(311, [Tuple(311, Atom("index", 311), Atom(field.Name, 311))],
                    Form("record_index", 313, Term.A(name), Atom(field.Name, 313))))]);
        }
    }

    private static IEnumerable<Term> WithBadArg(IEnumerable<Term> clauses, int arity, int line) =>
        clauses.Append(Clause(line, Enumerable.Repeat(Var("_", line), arity), Remote("erlang", "error", line, Atom("badarg", line))));

    private static Term Form(string tag, int line, params Term[] operands) => Term.Tuple([Term.A(tag), Term.I(line), .. operands]);

    private static Term Function(string name, int arity, int line, IEnumerable<Term> clauses) =>
        Form("function", line, Term.A(name), Term.I(arity), Cons.From(clauses));

    private static Term Clause(int line, IEnumerable<Term> patterns, Term body, Term? guard = null) =>
        Form("clause", line, Cons.From(patterns), guard is null ? Nil.Value : Term.List(Term.List(guard)), Term.List(body));

    private static Term Var(string name, int line) => Form("var", line, Term.A(name));

    private static Term Atom(string name, int line) => Form("atom", line, Term.A(name));

    private static Term Record(string name, int line) => Form("record", line, Term.A(name), Nil.Value);

    private static Term Alias(string name, int line) => Form("match", line, Record(name, line), Var("Rec", line));

    private static Term Tuple(int line, params Term[] elements) => Form("tuple", line, Term.List(elements));

    private static Term Call(string name, int line, params Term[] arguments) =>
        Form("call", line, Atom(name, line), Term.List(arguments));

    private static Term Remote(string module, string name, int line, params Term[] arguments) =>
        Form("call", line, Form("remote", line, Atom(module, line), Atom(name, line)), Term.List(arguments));
}
