# Part896 Immediate function invocation

The parser applies the same postfix call rules to anonymous and named fun expressions, function references and structured expressions. This covers the immediate invocations used in the OTP SSL_LOG and Mnesia CATCHU macros and preserves the existing argument and remote call parsing paths.

The source regression invokes an anonymous closure that captures a value, a named fun, a closure selected by case and a local function reference. The pinned OTP installation returned {13,13,13,local} for the same Erlang fixture.

All four focused compiler cases passed. [Focused report](focused.json).

A separate three-function BEAM probe returned {13,13,13} when loaded on OTP. OTP emits direct returns for these closure calls, while the C# compiler still emits closure allocation and invocation. The probe BEAM sizes were 600 bytes for OTP and 1140 bytes for the C# compiler.

The complete 1,366-image run found 137 Equal, 720 Different, 126 SourceRejected and 383 Unsupported. Of the 38 newly admitted sources, 11 generated images with differences and 27 reached a later unsupported stage. All 17 sources previously reporting the expected-end error passed source admission. Existing byte-for-byte matches were preserved. [Corpus report](otp-corpus.json).
