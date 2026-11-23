# Rejected full validation — Part 1897

The frozen compiler produced 411 Equal, 955 Different, 0 Unsupported and 0 SourceRejected across 1,366 uncached Windows OTP 29.1.1 inputs in 1,108.879 seconds. It gained `prim_eval.beam` and lost `diameter_callback.beam` against Part 1894. Preserve both outcomes and the local acceptance gate that rejected the byte regression.

The strong validator found zero cases of the typed update failure across the 20 affected modules. Native ERTS confirmed the real Megaco good-record path and the pre-existing failing-record stack line 83. The native reference reports line 87. Overall raw CInf, Docs and Meta are exact. The normalized comparison gained 76 functions and lost `diameter_callback:cb/4`.

The detailed rank had an ERTS heap allocation failure after 524.646 seconds. The original error is in rank.stderr.log. The raw inventory, loader and normalized comparison completed independently.
