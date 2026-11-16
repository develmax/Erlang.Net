# Part 1359 — Preserve tuple selectors across calls

A register case can test a tagged tuple and a literal without an explicit catch-all. The tuple test refines the type before the tag check. Both failure paths keep the original selector in X0 for case_clause.

When a projected field is needed only in the returned tuple, the case reserves a slot for the selector or the validated inner tuple. The branch trims expired arguments, calls the result producer, then extracts the saved field while the call result stays in X0. A field used by the call is still projected before argument preparation.

The focused fixture compares four decoded functions with OTP after label normalization. It checks flat and nested deferral, projection before a call, slot relocation, literal and wildcard fallback, and the original value in case_clause. Six previous probes passed.

The next audit finds 206 modules where an entry tuple field feeds a case selector call and 105 where an entry record field does. It finds one named field in 112 and 74 modules respectively. These source groups overlap.

The final Windows OTP 29.1.1 run generated all 1,366 images. It found 365 Equal, 1,001 Different and zero rejected sources. ELDAPv3 is Equal. OTP accepted all 1,001 differing images for loading.

Forty-six images changed. Seven functions gained normalized instruction matches with none lost. Peak stack size fell in 55 functions and matched OTP in 54. Code size gaps narrowed in 45 modules. The xmerl_xsd gap widened by one byte. Its keysearch_delete/4 now defers the field projection, and the generated frame retains two arguments that OTP specializes to constants. The net size gap shrank by 2,199 bytes. The generated xmerl_xpath image is unchanged, including expanded_name/3. Full runtime tests and readiness regeneration were not run.

- [Full corpus](otp-corpus.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Code size changes](code-delta.json)
- [OTP loader check](loader.txt)
- [keysearch_delete/4 instructions](keysearch-delete.txt)
- [Focused check](check-deferred-case-field.ps1)
- [Focused output](probe.txt)
- [Computed case tuple regression](computed-case-tuple-regression.txt)
- [Literal case regression](literal-case-regression.txt)
- [Two-call case regression](two-call-case-regression.txt)
- [Call tuple sequence regression](call-tuple-sequence-regression.txt)
- [Entry tuple binding regression](entry-tuple-binding-regression.txt)
- [Call sequence regression](entry-sequence-regression.txt)
- [Entry field audit](entry-case-fields.txt)
- [Audit script](rank-entry-case-fields.eval)
