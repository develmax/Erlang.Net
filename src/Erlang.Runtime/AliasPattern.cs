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
// Modified: CLR adaptation of OTP-29.1.1 erl_eval match1 aliased patterns.
// Preserve the original key/size scope and reuse the transactional match boundary.
namespace Erlang;

public sealed record AliasPattern(Pattern Left, Pattern Right) : Pattern
{
    protected override bool MatchCore(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context = null,
        Dictionary<string, Term>? keyScope = null
    ) => Left.Match(
        value,
        bindings,
        context,
        keyScope
    ) && Right.Match(
        value,
        bindings,
        context,
        keyScope
    );
}
