# Part 1558 — Emit inferred input types on tagged tuple projections

The tagged tuple test emitter accepts the inferred type of its input. Single tagged tuple projections now emit the atom-or-tuple type of known lookup results. Existing tests that first establish tuple shape continue to use the tuple type. This follows the is_tagged_tuple annotation rule in OTP 29.1.1 beam_ssa_type.erl and adds no dependency.

The instructions in pubkey_cert:current_any_policy_qualifiers/1 match OTP after label normalization. A fixture compared six cases on the OTP VM, including missing keys, a wrong tag and a wrong arity. Three focused compiler tests passed. [Disassembly](current_any_policy_qualifiers.txt) · [Fixture](beam_tagged_lookup_projection.erl) · [Outcomes](fixture.txt) · [Tests](focused.json).

## Corpus results

All 1,366 sources were accepted and generated. The comparison found 390 Equal and 976 Different. ELDAPv3 matched from the C# ASN.1 generator. Only pubkey_cert changed. One function gained a complete instruction match after label normalization and none lost one. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader](loader-and-code.txt).

## Stack frame differences

A scan of the preceding 976 differing images identified 790 functions in 332 modules that start with an assignment from a call and differ at frame allocation. The underlying causes vary. In ct_config:release_allocated/0, OTP folds the match specification into a literal and forwards the result directly to the tail call. In test_server_ctrl:html_convert_modules/3, OTP writes each dictionary lookup directly into its final argument register. [Frame ranking](baseline-frame-rank.txt) · [ct_config](frame-release_allocated.txt) · [test_server_ctrl](frame-html_convert_modules.txt) · [Nearest images](baseline-near.txt).
