# Part 1234 — match nested list-head patterns and return bound values in registers

Split the 405-module nonempty-list extraction group by the operations following extraction and by branch terminators. The largest next-instruction group has 176 functions across 102 modules. Its reference instructions test a tuple and extract a field. The grouping includes different source forms and provides an upper bound for this class of emitter change.

The compiler keeps tuple shape tests, nested field extraction, exact literal tests and simple alias bindings in X registers for the existing three-clause list scan. It can return a bound register and still tail-call the fallback with the untouched list tail. Both get_componentrelation/1 and the previously aligned any_manual_tag/1 in asn1ct_check match the reference after label normalization.

The Release build generated all 1,366 images. There were 357 Equal, 1,009 Different and zero rejected sources. Eight generated images changed, with the Code size gap falling by 451 bytes in total. OTP accepted all differing pairs.

- [List extraction ranking](list-extraction-ranking.txt)
- [Ranking script](rank-list-extraction.eval)
- [Normalized release instructions](asn1ct-check-normalized.txt)
- [Execution probe](probe.txt)
- [Probe source](list_head_tail_probe.erl)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
