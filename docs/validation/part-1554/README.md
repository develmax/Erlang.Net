# Part 1554 — Propagate case selector types to returned bindings

Call-result inference carries the selector type into a variable case pattern. It joins the types of values returned by the branches and recognizes empty list syntax and the proper-list return type of lists:reverse/1. Recursive selectors use an unknown type until the result can be established from other return expressions.

The instructions of inet_db:stripdot/1 now match OTP after label normalization, including the atom-or-list type on its comparison operand. The comparison in inet_config:try_get_rc/1 still lacks that type because the list is bound by a tuple pattern. The standalone fixture compares six successful and failing inputs on the OTP VM. Three selected regression tests passed. [Matching case](stripdot.txt) · [Tuple pattern case](try_get_rc.txt) · [Fixture](beam_case_result_type.erl) · [Results](fixture.txt) · [Tests](focused.json).

## Corpus results

All 1,366 sources were accepted and generated. The comparison found 390 Equal and 976 Different. Four images changed. Two functions gained a full instruction match after normalization and none lost a match. ELDAPv3 remains byte-identical from the C# ASN.1 generator. The OTP loader accepted all 976 differing images. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader results](loader-and-code.txt).
