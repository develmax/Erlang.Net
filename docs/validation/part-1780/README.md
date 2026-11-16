# Part 1780

Move the list shape test and guarded extraction of both fields before frame allocation. Preserve the head and tail in separate X registers across the successful path. The default path constructs its result and returns without a frame.

The previous compiler, current compiler and OTP passed all twelve fixture checks. The successful branch in the new compiler still allocates three slots. The reference allocates two.

All 1,366 Windows OTP 29.1.1 sources were accepted and emitted BEAM images. 400 files matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 966 differing pairs.

Compared 173 changed images. 5 normalized instruction matches were gained and 0 were lost. Absolute Code-size differences changed by 4896 bytes in total.

All 11 focused cases passed. The preceding compiler failed the register-projection assertion. The compiler block passed 3722 of 3830. The same 108 cases failed on part 1779.

Reuse branch-local coloring after the list projections. Remove the projected head store when its uses are satisfied directly from X.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First instruction differences](first.txt) · [Fixture](beam_entry_patterns.erl) · [Fixture execution](fixture-behavior.txt) · [C# entry instructions](new-entry.txt) · [OTP entry instructions](otp-entry.txt) · [Corpus entry changes](entry-differences.txt) · [Previous compiler regression](negative.txt) · [Focused tests](focused.txt) · [Compiler block](compiler-block-summary.txt)
