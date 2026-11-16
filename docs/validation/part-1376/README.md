# Part 1376 — Forward call results into returned tuples

The entry planner can forward the first result directly into a call inside the returned tuple. It validates the other tuple fields as constants or surviving entry arguments. The existing register moves prepare the next call, and the tuple constructor reads its result from X0. If the tuple also needs the original result, the planner preserves its slot.

The oracle fixture exercises local and external calls, surviving tuple fields, a constructed call argument and a tuple that also uses the first result. All five functions match the OTP instructions after removing function labels. Four use one fewer stack slot. The release comparison shows asn1ct:common_imports/2 moving from allocate 1 to allocate 0 and matching OTP after label normalization.

The complete Windows OTP 29.1.1 corpus generated all 1,366 images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal from the C# ASN.1 generator. OTP accepted all 997 differing images for loading. Four changed images narrowed their Code size gaps by a combined 31 bytes, and their four changed frames reach the OTP size. Three functions gained normalized matches and none lost a match. In cth_conn_log:init/2 the remaining instruction gap is a trim after the last Y slot is loaded and the resulting deallocate operand.

The new list audit classifies 3,249 source-backed functions whose first generated instruction is allocation where OTP first checks for a nonempty list. Its largest identified clause group has a variable cons head and a case body in 280 functions across 157 modules. Call arguments containing a cons construction affect 258 functions across 146 modules. Groups overlap. The broader frame audit counts 3,462 functions across 574 modules in the nonempty-list group.

The selected compiler checks, direct whole-module check and three prior focused oracles passed. The full runtime suite, canonical readiness regeneration and publication remain pending. The layout checker reports existing formatting differences outside this change in the shared emitter.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Remaining function and chunk ranking](remaining-functions.txt)
- [List entry audit](rank-list-entry-prefixes.escript)
- [List entry groups](list-entry-prefixes.txt)
- [Fixture source](forwarded_tuple_probe.erl)
- [Focused check](check-forwarded-tuple.ps1)
- [Focused output](probe.txt)
- [Release instructions](release-functions.txt)
- [Remaining trim difference](cth-conn-log.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
