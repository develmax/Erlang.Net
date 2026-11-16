# Part 1377 — Plan list entry with aliased tuple arguments

A list clause can now match an aliased tuple in another input register and retain both the full tuple and any bound fields for its body. The planner can check a nonempty list before a disjoint empty-list clause even when the nonempty clause has tuple arguments. It keeps the nil test when failure can come from a tuple check. List clauses with an if expression can also enter their body through a branch-local frame.

The fixture compares the old and new compiler with OTP for recursive cases and if expressions, proper and improper lists, wrong tuple shapes and a private function reached from a proper-list caller. The two public list functions start with is_nonempty_list on the new compiler. The private test omits the nonempty check because its caller provides a cons. An earlier candidate also omitted the nil check after a tuple failure and returned empty. The final compiler retains the nil check and raises function_clause. In asn1ct_check:leading_attr_index1/5 the empty-list return needs no frame. The nonempty path still allocates twelve slots where OTP allocates three, and the record check and case comparison still occur after allocation.

The complete Windows OTP 29.1.1 run generated all 1,366 images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal from the C# ASN.1 generator. OTP accepted all 997 differing images for loading. Of 56 changed images, 48 narrowed their Code size gaps, five widened them and three kept the same gap. The net reduction was 1,018 bytes. The normalized comparison gained inet:getifaddrs/1 and lost no matches. Peak frames moved closer to OTP in all 72 changed functions across 51 modules. Four reached the OTP size.

The broad nonempty-list first-difference group falls from 3,462 functions across 574 modules to 3,417 across 572. The source-backed case body group falls from 280 functions across 157 modules to 273 across 155.

The selected compiler checks, direct whole-module check and three prior focused probes passed with the compiler used for the verified corpus. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Remaining function and chunk ranking](remaining-functions.txt)
- [List entry groups](list-entry-prefixes.txt)
- [Fixture source](list_alias_case_probe.erl)
- [Focused check](check-list-alias-case.ps1)
- [Focused output](probe.txt)
- [Release instructions](release-functions.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
