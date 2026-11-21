# Missing pinned helper keys after Part1982

Compared the exact Part1982 final-label manifest and raw pinned OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. The analysis verifies each reported missing symbol exists in the reference and is absent from the frozen candidate. It only compares images. It does not use release bytes to generate code.

| Pinned key family | Keys | Modules | One-to-one body pairs with lines | Additional pairs after removing lines | No candidate peer |
| --- | ---: | ---: | ---: | ---: | ---: |
| Ordinary comprehension lc | 1,866 | 309 | 0 | 0 | 1,841 |
| SSA anonymous closure | 621 | 50 | 157 | 4 | 70 |
| Core fun closure | 222 | 69 | 84 | 0 | 30 |
| Source-named | 158 | 31 | 0 | 0 | 158 |
| Inlined function-clause helper | 102 | 65 | 87 | 9 | 6 |
| Other named local fun | 52 | 8 | 0 | 0 | 0 |
| Zip list helper | 48 | 22 | 0 | 0 | 48 |
| Binary list-generator helper | 30 | 14 | 0 | 0 | 30 |
| Overall | 3,099 | 358 | 328 | 13 | 2,153 |

Pairing permits only candidate-only keys with the same lexical owner, emitted arity and compatible generated family. It compares literal values, types, operations and call targets while canonicalizing each body's labels and self-reference, and excluding the fun-table index/unique metadata displayed in make_fun3 disassembly. The further categories are unique owner/arity/line 388, owner/arity alone 16, competing peers 148 and same-family peers at other arity 53. These classes are diagnostic. Absence of a peer does not itself prove that an owner contains no equivalent inlined instructions.

The source linkage for the 1,841 no-peer ordinary LC keys finds 545 singleton comprehension owners, 1,115 unique generator-line matches, 97 ambiguous owners and 84 whose declared owner has no source lc node. The broadest source shape is one ordinary variable generator on a variable and a local call element (146 keys in 51 modules). That shape is already admitted when all operand and owner conditions hold. Release examples include nested calls and context exclusions. A blanket enablement would conflate several causes.

Inlined failures are stronger evidence of naming drift than missing code. The pinned beam_core_to_ssa.erl:576–594 reserves a failure name only when the original function_clause operands do not agree with the caller's current arguments and there is no named inlining annotation. That counter also includes letrec and surviving automatic anonymous lifts. Same instruction pairs include yecc:compute_state/2, diameter_codegen:gen_hrl/2 and sofs:join/4 with distinct source contexts. Current C# failure reservation handles only two exceptionally simple bare tuple callbacks in a top-level sequence. Other cases include independently removed closed anonymous failures, branch traversal and prior LC letrec. A shared Core-to-SSA event model is needed to advance all 96 name/line pairs safely.

## Isolated strict comparison guard proposal

Pinned v3_core.erl:1822–1868 assigns an lc letrec before lowering the remaining qualifiers. For a strict generator with accumulated guard tests, it emits accepted pattern plus guard, a second clause with the same pattern and no guard that recurses on the tail, then the strict malformed-head clause. Nil terminates and a non-list tail raises bad_generator. The saved pinned beam_ssa_type:simplify_terminator/4 helper shows all four outcomes. Some other witnessed release owners prove the tuple shape and proper generator at the source/type stages and omit one or both error paths; reaching the helper does not establish their complete body parity.

The current extractor requires only one qualifier for a strict generator. Its general mapper also replaces all prior skip clauses with the malformed-head failure in its strict path. The proposal admits one flat term comparison on a variable-input flat tuple generator, then preserves the matched unguarded skip before the strict failure. Existing strict extraction scopes, opaque ordinary filters, aliases, other generator kinds and surrounding exception policy are unchanged. It does not extend frame passes or rename any existing helper.

Eight observed missing keys in six modules satisfy the exact flat source comparison gate, before the existing independent-owner and mapped-value checks. They are in beam_ssa_codegen, beam_ssa_opt, beam_ssa_type, cerl_inline twice, compile and erl_lint twice. beam_types:subtract/2 is a broader comparison witness with a non-flat operand and is outside this proposal. Use first single-LC controls beam_ssa_type:simplify_terminator/4 and compile:beam_strip_types/2, plus partially independent cerl_inline:add_match_bindings/2. The existing unmodified extractor may still reject the other owner contexts. Equal controls include inets_sup:children/0 (ordinary filters), cdv_detail_wx:create_pages/3 (flat tuple mapping), and ASN.1 asn1ct_table/ELDAPv3 ordinary generator cases. No Equal image contained the exact narrow strict-guard shape. The equivalent positive, no-filter and relaxed malformed-row behavior is exercised in the expanded permanent test.

Patch strict-list-comparison-helper.patch SHA-256 78CC0232131247E131E48E8D72177824D3B98C81CA0D5D884B8D73CEF39E8210. Strict forward/reverse replay and git diff --check passed. The existing compiler/beam/lifts-single-strict-list-generators test now requires the new generated helper and checks success order, matched skip, badmatch payload and bad_generator tail, with helper stack locations. No compiler build, test or native execution of generated images was run.
