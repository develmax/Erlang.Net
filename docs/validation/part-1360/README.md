# Part 1360 — Project entry fields into case selector calls

A case selector can take a single named field from a function-head tuple or record. The case planner reads the field directly into the outgoing argument register if other arguments leave the source intact. It keeps the entry shape check unless local callers prove the shape. When a conversion call leaves a deferred result field live, the planner keeps its tuple slot when moving it would not reduce the frame.

The focused fixture compares three decoded functions with OTP after label normalization. Runtime probes cover wrong tuple and record shapes, guard failure before the selector runs, and a shadowed closure argument that still raises function_clause. Six earlier probes passed.

The final Windows OTP 29.1.1 run generated all 1,366 images. It found 365 Equal, 1,001 Different and zero rejected sources. ELDAPv3 is Equal. OTP accepted all 1,001 differing images for loading. At least two BEAM sections differ in size in 989 modules. Code length differs in 1,000 modules, Type length in 901 and Line length in 836.

All three changed images have narrower Code size gaps, by 97 bytes in total. ssl_gen_statem:handshake_cancel/1 gained a normalized instruction match, with none lost. Peak frame size fell to OTP's count in three functions. In xmerl_xpath:expanded_name/3 it fell from eight slots to two. The decoded instructions of that function agree after label normalization apart from the entry xmlContext shape check. The caller passes its context through another function without a locally proven shape.

The next source audit finds 71 candidate functions across 64 differing modules whose private constant arguments are used in case branches. It finds at least one private constant argument candidate in 281 modules. Full runtime and readiness checks were not run.

- [Full corpus](otp-corpus.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Code size changes](code-delta.json)
- [OTP loader check](loader.txt)
- [expanded_name/3 instructions](expanded-name.txt)
- [Focused check](check-entry-case-field.ps1)
- [Focused output](probe.txt)
- [Deferred case field regression](deferred-case-field-regression.txt)
- [Computed case tuple regression](computed-case-tuple-regression.txt)
- [Literal case regression](literal-case-regression.txt)
- [Two-call case regression](two-call-case-regression.txt)
- [Call tuple sequence regression](call-tuple-sequence-regression.txt)
- [Entry tuple binding regression](entry-tuple-binding-regression.txt)
- [Private constant argument audit](private-constant-arguments-final.txt)
- [Audit script](rank-private-constant-arguments.eval)
