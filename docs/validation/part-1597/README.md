# Part 1597 — Count live registers for direct function value calls

The baseline comparison found one remaining function whose only normalized instruction difference was the live X count in allocate. The lifted closure in xmerl_xpath_pred allocated with three live registers in C# and two in OTP. [Baseline](live-baseline.txt).

Direct calls through function values now size the live X prefix from the callback and variable arguments read after allocation. The calculation also counts operands already in their destination registers. This covers incoming variable operands and literals for the direct-call path.

The deterministic fixture matches OTP byte for byte. Both images return done for a successful call and report badfun for an invalid target. Three focused tests passed. The test build used a larger .NET thread stack and disabled shared compilation and analyzers. The corpus tool built with its ordinary settings. [Fixture](beam_live_apply.erl) · [Disassembly](fixture-disassembly.txt) · [SHA-256](fixture-sha256.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and generated. There were 392 Equal and 974 Different. ELDAPv3 remains Equal. The only changed image is xmerl_xpath_pred. Its lifted closure now matches OTP after label normalization. The module still differs elsewhere and its Code size is unchanged. OTP accepted all 974 differing images for loading. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Disassembly](xmerl-disassembly.txt) · [Loader](loader-and-code.txt).
