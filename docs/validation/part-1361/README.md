# Part 1361 — Keep private constant arguments out of case frames

The register case planner reads the literal arguments agreed by all direct call sites of a private function. It substitutes those values in branch bodies and recomputes the arguments live after the selector call. The original argument registers still feed the selector. Exported functions, local function references, native functions and modules compiled with no_module_opt keep the ordinary planner.

The call scheduler keeps the relative order of argument assignments and emits a literal assignment to X0 after trimming the branch frame. The focused fixture matches take_entry/4 with OTP after label normalization and exercises both outcomes, partially agreed arguments, an escaped local function and no_module_opt. Seven earlier probes passed.

The complete Windows OTP 29.1.1 run generated all 1,366 images. The result is 365 Equal, 1,001 Different and zero SourceRejected. ELDAPv3 remains Equal. OTP accepted all 1,001 differing images for loading. One generated image changed. In xmerl_xsd, keysearch_delete/4 now matches normalized release instructions and uses three frame slots instead of five. The Code size gap narrowed by six bytes.

The chunk audit compares actual payloads. Code differs in all 1,001 differing images. All 999 changed export tables have identical exported symbols after decoding. All 974 changed Attr payloads have equal attributes after removing vsn. The default vsn is derived from a digest of essential chunks including Code. There are nine images with five differing chunks, and none with fewer. Full runtime and readiness checks were not run.

The normalized instruction audit finds 15 modules with one differing function and 13 with two. Three wx comparisons isolate entry guard placement, call result liveness and terminal calls. Each compared wrapper is the only normalized function difference in its module. OTP uses two frame slots in wxProgressDialog:new/3 and the generated function uses seven. The guards run before frame allocation in OTP. Three other images, ex_textCtrl, ex_htmlWindow and ex_notebook, share the same five differing function names and five differing chunk kinds.

- [Full corpus](otp-corpus.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Code size changes](code-delta.json)
- [OTP loader check](loader.txt)
- [keysearch_delete/4 instructions](keysearch-delete.txt)
- [Focused check](check-private-case-constants.ps1)
- [Focused output](private-case-constants.txt)
- [Entry case field regression](entry-case-field.txt)
- [Deferred case field regression](deferred-case-field.txt)
- [Computed case tuple regression](computed-case-tuple.txt)
- [Literal case regression](literal-case.txt)
- [Two-call case regression](two-call-case.txt)
- [Call tuple sequence regression](call-tuple-sequence.txt)
- [Entry tuple binding regression](entry-tuple-binding.txt)
- [Chunk payload audit](chunk-ranking.txt)
- [Chunk audit script](rank-release-chunks.eval)
- [Function gap audit](function-gap-audit.txt)
- [wx instruction comparison](wx-call-order.txt)
- [wx function gaps](wx-function-gaps.txt)
