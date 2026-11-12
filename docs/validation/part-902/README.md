# Part902 Targeted inlining of expression bodies

Targeted inlining accepts a single unconditional clause with a binding-free expression body. It substitutes the supplied variables and constants through constructors, calls and operators, including nested call sites and chains of inline targets. Calls with potentially effectful arguments and targets that require new local bindings remain outside this lowering path.

The deterministic fixture adds a helper that constructs a tagged tuple from its argument.

The deterministic tagged-tuple fixture matched the complete OTP image. A separate chained inline probe returned {[ok,42],42} from both images on OTP. Its nested construction instructions still differ.

All three focused compiler checks passed. [Focused report](focused.json).

The full 1,366-image corpus found 137 Equal, 815 Different, 105 SourceRejected and 309 Unsupported. Three previously unsupported modules emitted differing images. [Corpus report](otp-corpus.json).
