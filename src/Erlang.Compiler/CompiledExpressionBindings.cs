// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2026. All Rights Reserved.
// Copyright Ericsson AB 1999-2026. All Rights Reserved.
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
// Modified: CLR subset adapter for OTP-29.1.1 v3_core safe_list,
// ulinearize_exprs and uexprs match failures, using the erl_eval binding helper.
// Retains independent child environments (including closure capture), checks
// each returned child's bindings in source order, and reports its new value.
// This is not a port of full Core Erlang lowering or optimization.

namespace Erlang.Compiler;

internal static class CompiledExpressionBindings
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
            merged = ExpressionBindings.Merge(merged, scope);
        }
        foreach (var binding in merged)
            bindings[binding.Key] = binding.Value;

        return values;
    }

    public static async ValueTask<Term> EvaluateCons(
        Expr.List list,
        Dictionary<string, Term> bindings,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate
    )
    {
        var expressions = list.Tail is null ? list.Items : list.Items.Append(list.Tail).ToArray();
        var values = await EvaluateList(expressions, bindings, evaluate);

        return Cons.From(values.Take(list.Items.Count), list.Tail is null ? Nil.Value : values[^1]);
    }
}
