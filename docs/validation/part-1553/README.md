# Part 1553 — Return directly from short-circuit calls

When a local call is proven to return only Boolean values, the terminal andalso and orelse forms can return the left operand directly or tail-call the right operand. Literal entry clauses still dispatch before either call. For an unproven left operand, the generic path keeps both tests and raises the OTP error tuple with the actual invalid value.

The is_pchar/1 instruction sequence matches OTP after label normalization. The standalone probe compares seven outcomes, including skipped effects, a non-Boolean right operand, and the error tuple for an invalid left operand. [Disassembly](is_pchar.txt) · [Fixture](beam_terminal_predicates.erl) · [Observed outcomes](fixture.txt).

## Corpus results

All 1,366 sources were accepted and generated. The comparison found 390 Equal and 976 Different. ELDAPv3 remains byte-identical from the C# ASN.1 generator. Five additional functions in inet and uri_string match after label and closure identifier normalization, and no matching functions were lost. The OTP loader accepted all 976 differing images. Three selected regression checks passed. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader results](loader-and-code.txt) · [Tests](focused.json).
