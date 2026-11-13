# Part 1556 — Preserve known list shape through reverse

Result inference distinguishes a definitely empty list, a definitely nonempty list, and a list of unknown length when compiling calls to lists:reverse/1. This also applies when the call is imported. The normal return from reverse of a cons cell is nonempty, even when the tail is not statically known.

The type annotation on the result of the path lookup now matches OTP in digraph:del_path/3 and digraph:acyclic_add_edge/5. [Delete path](del_path.txt) · [Add edge](acyclic_add_edge.txt).

## Corpus results

The C# compiler accepted and generated all 1,366 modules. The comparison found 390 Equal and 976 Different. Two images changed. Both digraph functions now match after label normalization, and no function lost a match. ELDAPv3 remains byte-identical from C# ASN.1 generation. The OTP loader accepted all 976 differing images. Three selected regression tests passed. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader results](loader-and-code.txt) · [Tests](focused.json).
