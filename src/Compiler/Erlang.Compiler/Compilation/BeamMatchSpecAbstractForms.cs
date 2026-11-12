// SPDX-License-Identifier: Apache-2.0
// Match specification annotations follow OTP-29.1.1 ms_transform.erl.

using System.Globalization;

namespace Erlang.Compiler;

internal static class BeamMatchSpecAbstractForms
{
    internal static Term Write(Term call)
    {
        if (call is not TupleTerm { Items: [Atom { Name: "call" }, _, _, var arguments] }
            || Cons.Items(arguments).SingleOrDefault() is not TupleTerm
                { Items: [Atom { Name: "fun" }, var location, TupleTerm { Items: [Atom { Name: "clauses" }, var clauses] }] })
        {
            throw Unsupported();
        }

        return List(location, Cons.Items(clauses).SelectMany(WriteClause));
    }

    private static IEnumerable<Term> WriteClause(Term form)
    {
        if (form is not TupleTerm { Items: [Atom { Name: "clause" }, var location, var parameters, var guards, var body] }
            || Cons.Items(parameters).SingleOrDefault() is not { } pattern)
        {
            throw Unsupported();
        }

        var bindings = new Dictionary<string, string>(StringComparer.Ordinal) { ["_"] = "_" };
        if (pattern is TupleTerm { Items: [Atom { Name: "match" }, _, var left, var right] })
        {
            if (left is TupleTerm { Items: [Atom { Name: "var" }, _, Atom alias] })
            {
                bindings[alias.Name] = "$_";
                pattern = right;
            }
            else if (right is TupleTerm { Items: [Atom { Name: "var" }, _, Atom rightAlias] })
            {
                bindings[rightAlias.Name] = "$_";
                pattern = left;
            }
            else
            {
                throw Unsupported();
            }
        }

        int next = 0;
        Term head = Head(pattern);
        Term actions = List(location, Cons.Items(body).Select(Value));
        Term[] alternatives = Cons.Items(guards).ToArray();
        foreach (var guard in alternatives.Length == 0 ? [Nil.Value] : alternatives)
        {
            yield return Form("tuple", location, Term.List(head, List(location, Cons.Items(guard).Select(Value)), actions));
        }

        Term Head(Term item)
        {
            if (item is TupleTerm { Items: [Atom { Name: "var" }, var at, Atom variable] })
            {
                if (!bindings.TryGetValue(variable.Name, out string? name))
                {
                    name = "$" + (++next).ToString(CultureInfo.InvariantCulture);
                    bindings.Add(variable.Name, name);
                }

                return AtomForm(at, name);
            }

            return item switch
            {
                TupleTerm { Items: [Atom { Name: "tuple" }, var tupleAt, var items] } =>
                    Form("tuple", tupleAt, Cons.From(Cons.Items(items).Select(Head))),
                TupleTerm { Items: [Atom { Name: "cons" }, var consAt, var first, var rest] } =>
                    Form("cons", consAt, Head(first), Head(rest)),
                _ => item
            };
        }

        Term Value(Term item) => item switch
        {
            TupleTerm { Items: [Atom { Name: "var" }, var at, Atom variable] } =>
                bindings.TryGetValue(variable.Name, out string? name)
                    ? AtomForm(at, name) : Form("tuple", at, Term.List(AtomForm(at, "const"), item)),
            TupleTerm { Items: [Atom { Name: "tuple" }, var at, var items] } =>
                Form("tuple", at, Term.List(Form("tuple", at, Cons.From(Cons.Items(items).Select(Value))))),
            TupleTerm { Items: [Atom { Name: "cons" }, var at, var first, var rest] } =>
                Form("cons", at, Value(first), Value(rest)),
            TupleTerm { Items: [Atom { Name: "op" }, var at, Atom operation, var operand] } =>
                Operation(at, AtomForm(at, operation.Name), [operand]),
            TupleTerm { Items: [Atom { Name: "op" }, var at, Atom operation, var first, var second] } =>
                Operation(at, AtomForm(at, operation.Name), [first, second]),
            TupleTerm { Items: [Atom { Name: "call" }, var at, TupleTerm { Items: [Atom { Name: "atom" }, _, _] } function, var args] } =>
                Operation(at, function, Cons.Items(args)),
            TupleTerm { Items: [Atom { Name: "call" }, var at,
                TupleTerm { Items: [Atom { Name: "remote" }, _, _, var function] }, var args] } =>
                Operation(at, function, Cons.Items(args)),
            _ => item
        };

        Term Operation(Term at, Term function, IEnumerable<Term> operands) =>
            Form("tuple", at, Cons.From(operands.Select(Value).Prepend(function)));
    }

    private static Term List(Term location, IEnumerable<Term> values)
    {
        Term list = Form("nil", location);
        foreach (var value in values.Reverse())
        {
            list = Form("cons", location, value, list);
        }

        return list;
    }

    private static Term AtomForm(Term location, string name) => Form("atom", location, Term.A(name));

    private static Term Form(string tag, Term location, params Term[] items) => Term.Tuple([Term.A(tag), location, .. items]);

    private static NotSupportedException Unsupported() => new(BeamEmissionFormat.Unsupported);
}
