# Part622 LDAP list comprehensions

The C# backend emits the generated octets/1 and encode_tagged/3 functions and their local list comprehension helpers. It supports a single generator with an unfiltered remote unary map or a filtered local unary map over a tagged pair. Helpers preserve result order and raise bad_generator on malformed input. The backend assigns helper labels before emitting call sites.

The deterministic fixture contains both generated function bodies and the original generated helper names. Its BEAM matches OTP after removing Docs for the fixture profile. The original OTP VM compared eleven valid and invalid inputs, covering filtered and recursive octets, malformed lists and tagged encoding paths.

The isolated audit provides the generated helper labels and emits 127 of 130 source function bodies. Three fail clause shape validation. Whole-module ELDAPv3 emission remains open. [Function audit](eldap-functions.txt).

Both builds and all eight selected tests passed. [Focused report](focused.json).

The final corpus run checked all 1,366 installed images. It recorded 18 exact matches, 488 source rejections and 860 modules outside the emission profile, including ELDAPv3. Full regression and canonical readiness remain open. [Per-image report](otp-corpus.json).
