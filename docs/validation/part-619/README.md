# Part619 LDAP choice search and tag acceptance

The C# BEAM backend emits the generated find_choice/2 and accepts/2 bodies. It extracts list heads and descriptor tuple fields, walks references and choices, and preserves arguments across calls. It keeps the case_clause branch when the predicate has no proven Boolean result. The type table records the atom-or-tuple result when a recursive choice search guarantees it.

The deterministic fixture contains both generated function bodies and minimal local schema and tag dependencies. Its BEAM matches the OTP compiler output after removing Docs for the fixture profile. An original OTP VM loaded the generated file and verified seven successful results and three invalid-list function_clause paths.

The isolated audit emits 122 of 130 source function bodies. Eight fail clause shape validation. Whole-module ELDAPv3 emission remains open. [Function audit](eldap-functions.txt).

Both builds and all eight selected tests passed. [Focused report](focused.json).

The final corpus run checked all 1,366 installed images. It recorded 18 exact matches, 488 source rejections and 860 modules outside the emission profile, including ELDAPv3. Full regression and canonical readiness remain open. [Per-image report](otp-corpus.json).
