# Part618 BER tag matching and continuation

The C# BEAM backend emits the generated LDAP tag_number/1 and long_tag/2 bodies. It reads the class and tag fields, handles BER continuation octets, restores the original argument for function_clause and uses typed bitwise arithmetic. It forwards a mutable match context only to a recognized continuation. Other local targets receive a materialized binary tail.

The deterministic probe contains both actual generated function bodies. Its image matches the OTP reference after removing Docs for the fixture profile. The original OTP VM compared 289 valid and invalid inputs against the reference. Inputs covered all short tags and class bits, multi-octet tags, truncated tags, non-byte-aligned inputs, large accumulators and invalid argument types.

The isolated audit emits 120 of 130 source function bodies. Ten fail clause shape validation. Whole-module ELDAPv3 emission remains open. [Function audit](eldap-functions.txt).

Both builds and all eight selected tests passed. [Focused report](focused.json).

The final corpus run checked all 1,366 installed images. It recorded 18 exact matches, 488 source rejections and 860 modules outside the emission profile, including ELDAPv3. Full regression and canonical readiness remain open. [Per-image report](otp-corpus.json).
