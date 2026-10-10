// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1999-2025. All Rights Reserved.
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
// Modified: C# adaptation of OTP-29.1.1 eval_bits:expr_grp/expr_grp1/eval_field.
// Evaluates values/sizes before construction, with sequential interpreter scopes.
// Compiled binary pre-expressions are also sequential, following v3_core:expr_bin_1.
// Modified: adapt v3_core:bitstr empty-string size prechecks and segment elimination.
// Binary storage, error metadata and resource limits remain CLR adaptations.
namespace Erlang.Compiler;

internal static class BitExpressionBindings
{
    public static async ValueTask<BitString> Evaluate(
        Expr.Bits bits,
        Dictionary<string, Term> bindings,
        Func<Expr, Dictionary<string, Term>, ValueTask<Term>> evaluate,
        bool compiled
    )
    {
        var segments = new List<(Term Value, Term? Size, BitSegment Segment)>(bits.Segments.Count);
        var scope = CompiledBindingScope.Copy(bindings);
        foreach (var segment in bits.Segments)
        {
            var value = await evaluate(segment.Value, scope);
            var size = segment.Size is null ? null : await evaluate(segment.Size, scope);
            if (compiled && segment.IsStringLiteral && value is Nil && segment.Type is BitSegmentTypes.Integer or BitSegmentTypes.Float)
            {
                if (size is null || size is Integer { Value.Sign: >= 0 })
                    continue;
                // Literal failures are lowered to a failed binary after its pre-expressions.
                // Dynamic sizes carry an immediate integer/nonnegative precheck.
                if (segment.Size is not Expr.Literal)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            segments.Add((value, size, segment));
        }
        var result = BitConstruction.Create(segments, encodeFloatInOrder: !compiled);
        foreach (var binding in scope)
            bindings[binding.Key] = binding.Value;

        return result;
    }

}
