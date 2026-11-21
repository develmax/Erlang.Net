# Sequential siblings with nested list generators

This is a source-only follow-up to tuple-outer-list-generators-1971-intake. The production base uses that intake's after image for RecursiveListGenerators and the hash-verified frozen Part 1969 source archive for RecursiveListComprehensions. The test patch adds one registration after the standalone fixture. No SDK was run.

## Confirmed residual release shapes

Four of the sixteen remaining direct-dependent missing keys are in the bounded sequential cohort. Pinned SSA and physical release order are in release-ssa.stdout.log.

| Owner | Target exact keys | Dependency |
| --- | ---: | --- |
| dialyzer_behaviours:get_behaviours/1 | 2 | Later two-generator helper pair gets Core/SSA ordinals 2 and 3 after two prior single-generator expressions. Physical order is 2, 3, 1, 0. |
| qlc_pt:qualifier_dependencies/2 | 2 | Leading flat-tuple pair gets 0 and 1. The next ordinary pair has a nested outer pattern and still reserves 2 and 3. The following single generator gets 4 in OTP; its physical relative order is 4, 0, 1 when only the supported pairs are emitted. |
| dialyzer_callgraph:condensation/1 and dialyzer_cplt:to_file1/4 | 4 | Nested pair plus closure siblings. Requires integration with closure numbering and placement. |
| yecc:find_identical_shift_states/1 | 4 | Direct append fusion. Pinned left pair receives the computed right result as a further capture and returns it through the outer exhausted arm. Right pair itself has standard inner-to-outer return. |
| yecc:find_partial_shift_states/2 | 2 | A source zip and additional record generators consume ordinals. |
| erl_lint:check_nifs/2 | 2 | Four-field tagged outer tuple generator; partition shares sequence mechanics but requires a broader outer-pattern proof. |

The candidate limits the new scheduling rule to the source sequence of one unconditional unguarded clause whose list comprehension results are stored in named direct matches or passed to a single-argument static call in that match. It accepts ordinary generators with existing source shapes and requires an actually lowerable flat outer tuple pair to opt the owner in. The existing helper planner keeps capture order and checks source scopes. The two-gen entries reserve both ordinals even if a sibling falls back. The normal reverse-definition schedule emits a planned pair as adjacent outer, inner functions. Closure, zip, direct append, conditional, and captured multiline source patterns remain outside this scheduling qualification.

## Evidence and next validation

The permanent fixture checks seven exact function name/arity pairs in physical order, including a later singleton numbered 4 despite its earlier two-generator sibling using the existing fallback. The same source compiled with pinned native OTP gives physical 4, 2, 3, 0, 1 and 2, 3, 1, 0 for the two respective owners. Pinned native OTP passed four profiles across both owners, including inner generator failure class/reason, first helper and source line. The frozen P1970 compiler 559E7F5052109CB87941E14A065EB36A7BAEA3F225EF85877A7AF8B239549F75 compiled the fixture with no lc$^ keys. Actual candidate compile, managed test, native candidate differential and release effects remain unverified. Strict replay of both patches against this isolated snapshot passed.

The targeted validation should include both release modules, the standalone qlc_pt:mforms/1 and mforms/2 helpers, a zipped yecc module as a shape exclusion control, and currently exact comprehension modules compiled in the same gate. Report recovered keys, normalized instruction bodies, native behavior, and complete raw equality independently.
