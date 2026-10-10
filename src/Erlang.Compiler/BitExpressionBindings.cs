// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1999-2025. All Rights Reserved.
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
        var segments = new (Term Value, Term? Size, BitSegment Segment)[bits.Segments.Count];
        var scope = new Dictionary<string, Term>(bindings, StringComparer.Ordinal);
        for (int i = 0; i < segments.Length; i++)
        {
            var segment = bits.Segments[i];
            var value = await evaluate(segment.Value, scope);
            var size = segment.Size is null ? null : await evaluate(segment.Size, scope);
            segments[i] = (value, size, segment);
        }
        var result = BitConstruction.Create(segments, encodeFloatInOrder: !compiled);
        foreach (var binding in scope)
            bindings[binding.Key] = binding.Value;

        return result;
    }

}
