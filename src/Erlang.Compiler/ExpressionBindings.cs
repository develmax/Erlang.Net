// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2026. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Modified: C# adaptation of OTP-29.1.1 erl_eval:expr_list/expr(cons)/merge_bindings
// (ordered-dictionary branch), and erl_lint:expr_list/vtupd_export_expr_list.
// Uses async callbacks, CLR collections and the current name-only lint model.
// See docs/OTP-PORTS.md for exact provenance and remaining deviations.

namespace Erlang.Compiler;

internal static class ExpressionBindings
{
    public static async ValueTask<Term[]> EvaluateList(
        IReadOnlyList<Expr> expressions,
        Dictionary<string, Term> bindings,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate
    )
    {
        var original = new Dictionary<string, Term>(bindings, StringComparer.Ordinal);
        var merged = new Dictionary<string, Term>(original, StringComparer.Ordinal);
        var values = new Term[expressions.Count];
        for (int i = 0; i < expressions.Count; i++)
        {
            var scope = new Dictionary<string, Term>(original, StringComparer.Ordinal);
            values[i] = await evaluate(expressions[i], scope);
            merged = Merge(scope, merged);
        }
        foreach (var binding in merged)
            bindings[binding.Key] = binding.Value;

        return values;
    }

    public static Dictionary<string, Term> Merge(
        IReadOnlyDictionary<string, Term> first,
        IReadOnlyDictionary<string, Term> second
    )
    {
        var merged = new Dictionary<string, Term>(second, StringComparer.Ordinal);
        foreach (var binding in first.OrderBy(pair => Term.A(pair.Key)))
        {
            if (merged.TryGetValue(binding.Key, out var previous) && !previous.Equals(binding.Value))
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), previous));
            merged[binding.Key] = binding.Value;
        }

        return merged;
    }

    public static async ValueTask<Term> EvaluateCons(
        Expr.List list,
        Dictionary<string, Term> bindings,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate
    )
    {
        var original = new Dictionary<string, Term>(bindings, StringComparer.Ordinal);
        var scopes = new Dictionary<string, Term>[list.Items.Count];
        var values = new Term[list.Items.Count];
        for (int i = 0; i < values.Length; i++)
        {
            scopes[i] = new Dictionary<string, Term>(original, StringComparer.Ordinal);
            values[i] = await evaluate(list.Items[i], scopes[i]);
        }
        var tailScope = new Dictionary<string, Term>(original, StringComparer.Ordinal);
        var tail = list.Tail is null ? Nil.Value : await evaluate(list.Tail, tailScope);
        for (int i = scopes.Length - 1; i >= 0; i--)
            tailScope = Merge(scopes[i], tailScope);
        foreach (var binding in tailScope)
            bindings[binding.Key] = binding.Value;

        return Cons.From(values, tail);
    }

    public static void ValidateList(
        IEnumerable<Expr> expressions,
        HashSet<string> bound,
        Action<Expr, HashSet<string>> validate
    )
    {
        var exported = new HashSet<string>(bound);
        foreach (var expression in expressions)
        {
            var scope = new HashSet<string>(bound);
            validate(expression, scope);
            exported.UnionWith(scope);
        }
        bound.UnionWith(exported);
    }
}
