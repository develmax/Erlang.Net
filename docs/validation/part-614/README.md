# Part614 LDAP tagged patterns, type guards and binary captures

The C# BEAM backend lowers the generated is_choice/1 shape, including a tuple type test, dispatch by tuple arity, tag comparison and a local call followed by a local tail call. The type table describes the tuple register consumed by select_tuple_arity.

A single identity clause can use is_atom/1, is_binary/1 or is_integer/1 as its guard. A failing check branches to the function info label. The binary matcher supports fixed unsigned captures at the start of an exact-width bitstring and a binary tail after fixed-width skipped fields. It emits OTP 29 bs_match commands and restores the original input on failure before raising function_clause.

The deterministic fixture reproduces the exact generated is_choice/1 clauses and tests representative guard and binary capture shapes. It matches the original compiler's complete image after removing the reference Docs chunk for the no-documentation profile. An original OTP VM loaded the C#-generated fixture and verified nine successful calls and four function_clause paths.

These lowering rules do not yet cover the complete generated LDAP codec. Arithmetic in tag_number/1, recursive bit matching with arithmetic, comprehensions and exception handling remain outside this implementation.

Both builds succeeded and all eight selected tests passed. [Focused report](focused.json).

The final corpus run checked all 1,366 installed images. It recorded 18 exact matches, 488 source rejections and 860 modules outside the current BEAM emission profile, including ELDAPv3. Full regression and canonical readiness remain open. [Per-image report](otp-corpus.json).
