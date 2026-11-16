# Part 1555 — Infer types of returned tuple fields

Result inference follows local calls to tuple constructors and reads the selected field of tuples that can match the case pattern. Literal mismatches exclude incompatible tuple alternatives. Imported lists:reverse/1 contributes the same proper-list result as its qualified form.

In inet_config:try_get_rc/1, the is_eq_exact operand now carries the same atom-or-list type as in OTP. The standalone fixture covers recursive tuple producers, distinct success and error tags, and an imported reverse call. Five outcomes matched on the OTP VM and three selected regression tests passed. [Disassembly](try_get_rc.txt) · [Fixture](beam_tuple_result_type.erl) · [Results](fixture.txt) · [Tests](focused.json).

## Corpus results

The compiler accepted and generated all 1,366 modules. The comparison found 390 Equal and 976 Different. Two images changed. inet_config:try_get_rc/1 gained a full instruction match after label normalization and no function lost a match. ELDAPv3 remains byte-identical from the C# ASN.1 generator. OTP accepted all 976 differing images for loading. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader results](loader-and-code.txt).
