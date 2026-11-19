# In-place tuple projection and heap checks

The existing heap-placement pass now moves an immediately adjacent in-place tuple projection after the heap check when its X register is already rooted. It keeps the existing live prefix and treats ordinary calls as barriers. The tuple remains reachable during a collection before its field is selected. This follows pinned OTP 29.1.1 beam_ssa_codegen, where get_tuple_element is neutral for heap need. The compiler and test use existing APIs and add no dependencies.

| Scoped evidence | Part1855 baseline | Isolated candidate |
|---|---|---|
| Whole-file Equal | 5 | 5 |
| Different | 7 | 7 |
| Unsupported | 0 | 0 |
| SourceRejected | 0 | 0 |
| Comparison time, one worker | 31.34 s | 32.76 s |

All five controls retained their exact images, including megaco_tcp_sup and ssh_channel_sup. Seven other images changed. Their Code chunk lengths stayed the same. Across these images, 30 disassembled functions changed. Seven gained an exact body after normalizing label numbers, and none lost one. One gained exact disassembly with original label numbers and no function lost exact disassembly. The seven gains are recorded in function-gains.log. No byte-equal whole files or independently compared sections were gained or lost. The raw comparison identifies the changed Code, Attr and FunT sections. It also records the unmodified modules and exact SHA-256 identities.

Pinned ERTS prepared all seven changed images. The native fixture matched the previous compiler and OTP for in-place X0, in-place X1, a call barrier and invalid tuple input. Native SSH open and error returns were checked through an in-process handler stub. EDoc returned the expected text and malformed URI error. Both release module observations matched the C# baseline and candidate. The standalone regression reproduced its intended assertion with the frozen baseline and passed with the candidate and managed preparation.

The compiler built cleanly in 87.45 seconds. The scoped worker used BelowNormal priority with one .NET processor after the other managed pilot released its slot. The root's separate four full corpus worker processes were active at launch and through the scoped comparison. The compiler syntax and repository source layout gate were checked on the isolated files. Patch application and reversal passed. The full package regression and complete corpus for this candidate remain with root integration.

The patch SHA-256 is 76B92AD192642AC48425871D8E1C96D9B01F4AE0BA8A104B88ECDF4B447D5379. The compiler SHA-256 is 311378DE418A9F9842701889C50BDDB601AB405D6649A45E174FD8D62CA5DA38. Raw errors, the Part1853 motif analysis, module lists, profiles, before/after comparisons and input identities remain in this directory.
