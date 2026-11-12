# Part 1386 — Keep list element case dispatch in registers

List entry dispatch can plan a checked element access followed by a two-arm case. The case compares the result with a literal or an existing binding. A successful element access proves that smaller positions exist, allowing later branches to use direct tuple projection. The planner reuses field values already read, retains checks for larger positions and releases a source register when no later branch needs it. Tuple returns and ordinary tail calls use the existing emitters.

The empty-list branch can return a folded constant tuple. Nested case labels are numbered in block emission order before the empty-list label. The intermediate fixture matched OTP after label normalization while its Code and Attr chunks differed. The final deterministic fixture matches the complete OTP 29.1.1 image.

The focused OTP fixture covers a recursive skip, a successful nested match, a different tuple tag, the empty-list fallback and a returned field. It also checks badarg for non-tuples and undersized tuples and function_clause for an improper tail. Both fixture functions match normalized instructions. The release function asn1ct_gen:get_fieldtype/2 now has the OTP register flow after label normalization.

The full Windows OTP 29.1.1 comparison generated all 1,366 images with 372 Equal, 994 Different and zero SourceRejected. ELDAPv3 remains Equal using the C# ASN.1 generator. OTP accepted all 994 differing images for loading.

Thirteen images changed. One function gains a normalized instruction match and none lose one. Twelve peak frames move closer to OTP and four reach its size. Eight Code size gaps narrow, two widen and three retain their size. The net gap reduction is 290 bytes. shell_docs widens by 17 bytes and qlc by 11 bytes.

The list-entry first-difference group falls from 3,213 to 3,206 functions across 561 modules. The largest source-backed subgroup has a variable list head and sequence body in 274 functions across 136 modules. The next map cases show a frame-free cache hit and an allocated miss path.

The selected compiler cases, direct whole-module check, three earlier oracle probes and scoped layout check passed. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized instruction changes](normalized-functions.txt)
- [Peak frame changes](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [List case selector audit](list-case-selectors.txt)
- [List map case instructions](next-list-map-case.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
- [Complete deterministic fixture hashes](fixture-hashes.txt)
- [Oracle probe source](list_element_case_probe.erl)
- [Focused oracle](check-list-element-case.ps1)
- [Focused oracle result](probe.txt)
- [ASN.1 release instructions](asn1-list-element.txt)
