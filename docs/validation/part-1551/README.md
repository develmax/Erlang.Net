# Part 1551 — Construct short literal list prefixes before tail calls

The register tail-call emitter folds a proper literal prefix of up to four items, matching the threshold in OTP's sys_core_fold. It constructs the result with test_heap and put_list directly in the argument registers. An empty prefix forwards the tail as-is.

The normalized instructions of xmerl_xsd_type:set_sign/2 match OTP. The probe compared seven outcomes on the OTP VM, including an empty prefix, an improper tail, a prefix above the folding threshold and conversion errors. [Disassembly](set_sign.txt) · [Probe](beam_literal_append_prefix.erl) · [Results](fixture.txt).

## Corpus results

The C# compiler accepted and generated all 1,366 modules. The comparison found 390 Equal and 976 Different. Only xmerl_xsd_type changed, gaining one matching function after label normalization. ELDAPv3 remains byte-identical from the C# ASN.1 generator. The OTP loader accepted all 976 differing images. All three selected regression checks passed. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader results](loader-and-code.txt) · [Tests](focused.json).
