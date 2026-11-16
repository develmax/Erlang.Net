# Part620 LDAP sequence field encoding and decoding

The C# BEAM backend lowers the generated encode_fields/2 and decode_fields/2 functions. It extracts list and tuple components, skips absent optional values and false defaults, and preserves unread input across the predicate and conversion calls. Unknown predicate results retain the case_clause path. A missing mandatory field still follows the source error path.

The deterministic fixture contains both generated function bodies and controlled local dependencies. Its BEAM matches the OTP compiler output after removing the reference Docs chunk for the fixture profile. An original OTP VM loaded the C# image and verified ten successful and failing paths, including preservation of the unread tail and mismatched inputs.

The isolated audit emits 124 of 130 source function bodies. Six fail clause shape validation. Whole-module ELDAPv3 emission remains open. [Function audit](eldap-functions.txt).

Both builds and all eight selected tests passed. [Focused report](focused.json).

The final corpus run checked all 1,366 installed images. It recorded 18 exact matches, 488 source rejections and 860 modules outside the emission profile, including ELDAPv3. Full regression and canonical readiness remain open. [Per-image report](otp-corpus.json).
