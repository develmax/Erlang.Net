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
// Modified: CLR adaptation of OTP-29.1.1 erl_eval maybe_match_exprs/4 and maybe clauses.
// Private scopes preserve the no-export contract; compiled AST execution reuses CLR constraints.
namespace Erlang.Compiler;

internal static class MaybeExpressionExecution
{
    public static async ValueTask<Term> Evaluate(
        Expr.Maybe expression,
        Dictionary<string, Term> bindings,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate,
        Func<Pattern, Term, Dictionary<string, Term>, bool> match,
        Func<Clause, Term, Dictionary<string, Term>, bool> matchClause
    )
    {
        var body = CompiledBindingScope.Copy(bindings);
        Term value = Nil.Value;
        foreach (var item in expression.Items)
        {
            if (item is not Expr.MaybeMatch conditional)
            {
                value = await evaluate(item, body);
                continue;
            }
            value = await evaluate(conditional.Value, body);
            var candidate = CompiledBindingScope.Copy(body);
            if (match(conditional.Pattern, value, candidate))
            {
                body = candidate;
                continue;
            }
            if (expression.Clauses.Count == 0)
                return value;
            foreach (var clause in expression.Clauses)
            {
                var scope = CompiledBindingScope.Copy(bindings);
                if (matchClause(clause, value, scope))
                    return await evaluate(clause.Body, scope);
            }

            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.ElseClause), value));
        }

        return value;
    }
}
