# Part897 Quoted atoms and parenthesized type forms

The type parser distinguishes operator tokens from atoms quoted with the same spelling. The source parser accepts parenthesized type declarations and specs. The release spec reader accepts the same parenthesized spec form while retaining the original source annotations.

The deterministic OTP fixture combines a parenthesized type and spec with quoted '+', '-' and 'bnot' type alternatives. The release metadata fixture uses a parenthesized bounded spec. The pinned OTP parser produced identical spec forms, including locations, for the updated fixture and the existing reference image.

The EUnit transform remains open. The pinned release has an export and generated test/0 with annotation 0, and a hidden Docs entry for test/0 with the same annotation.

All three focused compiler cases passed. [Focused report](focused.json).

The complete 1,366-image run found 137 Equal, 723 Different, 115 SourceRejected and 391 Unsupported. Eleven previously rejected modules passed source admission. Three generated differing images and eight reached unsupported generation. [Corpus report](otp-corpus.json).
