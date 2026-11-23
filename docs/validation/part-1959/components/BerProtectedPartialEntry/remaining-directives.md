# Remaining BER type and context dependencies

The pinned runtime source is OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. The three generated owners are OTP-PKIX, OTP-PKIX-Relaxed and PKIX1Explicit-2009. The two directive sources common to all three are the generated partial decoder entry with `[mandatory,{undecoded,[16]}]` and the generated suppressions helper with `[{undecoded,0},{alt_parts,0}]`. The second helper remains part of the compiled image and contributes a source-typed call edge.

| Recursive function | Current instructions | OTP instructions |
| --- | ---: | ---: |
| decode_primitive_incomplete/2 | 419 | 69 |
| decode_constructed_incomplete/2 | 197 | 106 |
| decode_incomplete2/5 | 61 | 38 |
| incomplete_choice_alt/2 | 26 | 18 |
| decode_constr_indef_incomplete/3 | 50 | 46 |

Each count applies to all three owners. The pinned `decode_primitive_incomplete/2` begins with a list projection, classifies its head as an atom or tuple, and retains the undecoded and tag-zero alt_parts paths. It also keeps both nil and nonempty alternatives for the tuple-headed tail. Concrete execution of the public and suppressions seeds alone is insufficient to predict these instructions. The next source pass needs an explicit abstract proper-list element and tail join, tuple-tag and arity projections, and propagation over the recursive component. The pinned `incomplete_choice_alt/2` still admits both integer zero and nonempty-list tuple keys at its comparison, while collapsing its nested-list head clause.

The independent shared header requires proof of the match-context argument established by the boxed binary entry and forwarded through its slow tag path. Pinned 64-bit resume with equal registers is a no-op. The existing generic header emits a binary tail at that boundary. A standalone resume emission would violate that representation contract. Keep concrete binary ABI across the current projection and protected dispatch passes.
