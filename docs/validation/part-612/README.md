# Part612 named and bounded specs and LDAP schema dispatch

The release metadata writer emits named arguments, bounded functions with subtype constraints, and overloaded specs in the OTP debug representation. Docs uses argument names from a single unambiguous spec, and falls back to the function clause for overloaded specs. A synthetic reference compiled by OTP compares the decoded spec attributes and the raw Docs payload.

The BEAM backend dispatches from atom or integer keys to compound literals, merges identical result blocks in the OTP order, and emits the structured erlang:error/1 fallback. A synthetic whole-image comparison exercises the repeated return value and the error branch. The test fixture was compiled with deterministic and its Docs chunk removed to match the no-documentation fixture profile.

An isolated module containing the actual 50-entry schema/1 function from the C#-generated ELDAPv3 source produced the same 50 descriptors and unknown-type error as OTP. Its final BEAM also matched the OTP compilation of that same isolated source after removing Docs from the reference. Both files had SHA-256 B95E7270975B6C5E49B3449F65D4CBBCCCE0C84A0D4ECCDF15E47B076E47A35F. This comparison covers the generated schema function. The remaining codec functions and the original release image are still outside the BEAM emission profile.

The expanded literal result lowering enables snmpm_user_old:behaviour_info/1. Its admitted release profile records the manager source directory, include paths, macro definitions, warning flags and inserted app_vsn attribute. All 1,968 bytes matched the installed release. The source and reference fixture hashes match the admitted inventory.

All eight selected tests passed. The generated snmpm_user_old loaded in original OTP and returned the callback list and fallback value. [Focused report](focused.json).

The final run checked all 1,366 installed images. It recorded 17 exact matches, 488 source rejections and 861 modules outside the BEAM emission profile. ELDAPv3 remains in the latter group. Full regression and canonical readiness remain open. [Per-image report](otp-corpus.json).
