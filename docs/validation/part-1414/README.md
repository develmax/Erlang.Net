# Part 1414 — Check consecutive call-result tuple matches

The existing saved-call planner handles untagged tuple checks with at most one bound field per call. It tests the type and arity of the original value and projects the live field directly into the next argument register when possible. The same check works before a tail call, a tuple return, or a call-based case. The tuple return checks the heap before projection, and a case assigns failure labels after the branch labels.

The deterministic fixture matches the complete OTP image. OTP executed both images through successful calls, both case branches, a non-tuple result and a tuple with the wrong arity. The fixture and three focused compiler checks passed with the compiler assembly used for the corpus.

The Windows OTP 29.1.1 corpus generated all 1,366 images. It has 380 Equal and 986 Different, including an Equal ELDAPv3 from C#-generated source. No source was rejected and all differing images passed OTP loader preparation. Seven release images changed and the combined Code size gap shrank by 185 bytes. Two functions gained normalized instruction equality, mnesia_recover:what_happened_remotely/3 and snmp_notification_mib:find_targets/0. No normalized matches were lost in the changed images.

The next group needs multiple live fields from one checked result. For example, asn1ct_imm:combine_imms/2 and asn1ct_parser2:parse_Intersections/1 both consume two fields from the first call. The new plan currently accepts one bound field and uses existing lowering for the other shapes. Code has a size mismatch in 985 release images. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Corpus](otp-corpus.json), [status changes](status-changes.txt) and [section size counts](chunk-sizes.txt)
- [Normalized instructions](normalized-functions.txt) and [loader and Code sizes](loader-and-code.txt)
- [Oracle](check-tuple-chain.ps1), [probe](probe.txt), [fixture hashes](fixture-hashes.txt) and [focused tests](focused.json)
