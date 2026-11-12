# Part609 OTP corpus source preparation

The C# source compiler accepts additional warning controls, including local and remote signature lists, and the legacy no_native flag. A diagnostic for a remaining unsupported compile option identifies that option. Tests cover valid controls, malformed lists and unsupported parse transforms.

The corpus runner passes the kernel include directory when compiling stdlib, matching its OTP build recipe. It also generates the missing ELDAPv3.erl and companion header from the installed ELDAPv3.asn1 using the admitted OTP ASN.1 generator with noobj, legacy_erlang_types and ber. Generation takes place outside the reference installation, with outputs retained beside the report. The C# compiler receives the generated Erlang source and performs the .erl-to-BEAM step. The released image remains the comparison reference.

The focused build and all eight selected tests passed. [Focused report](focused.json).

The complete 1,366-image corpus recorded 14 exact matches, 488 source rejections and 864 modules outside the current BEAM emission profile. Thirteen previously rejected sources now reach the BEAM emission profile check, and ELDAPv3 joins them with the generated source. All eight logger.hrl lookup diagnostics are gone. ELDAPv3 has no byte comparison yet. The full suite and canonical readiness remain open. [Per-image report](otp-corpus.json).
