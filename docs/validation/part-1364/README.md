# Part 1364 — Forward tagged payloads through call sequences

A call sequence can start with a tagged match and forward its payload from X0 to the next call. The ordinary call chain scheduler saves entry arguments used later, trims expired slots and keeps the original term for badmatch. When a longer continuation uses the general emitter, one tagged tuple test replaces separate tuple, arity and tag checks. The emitter saves the extracted payload directly in its binding slot. Previously bound fields retain their equality checks.

The focused oracle matches three complete functions after label normalization. It also checks branch continuations, the whole returned match value, failure on a previously bound field, wrong tags, wrong arities and non-tuples. Six previous probes passed.

The final Windows OTP 29.1.1 run generated all 1,366 images. It found 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 and every previous exact image remain Equal. OTP accepted all 997 differing images for loading. Code size gaps narrowed in all 299 changed modules by 22,777 bytes in total. Peak stack use fell in 609 functions across 264 modules and reached the OTP count in seven. The normalized instruction comparison records zero gained and zero lost function matches in the release corpus.

The source audit finds 77 modules with a tagged payload referenced after an intervening call binding and 43 with a terminal case after the match. In ct_release_test:copy_file/3 the generated entry now tests the return value in X0 and the frame has five slots instead of nine. OTP uses three slots. Full runtime and readiness checks were not run.

- [Full corpus](otp-corpus.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Code size changes](code-delta.json)
- [OTP loader check](loader.txt)
- [Focused check](check-tagged-sequence.ps1)
- [Focused output](probe.txt)
- [Tagged call match regression](tagged-call-match.txt)
- [wx text command regression](wx-text-command.txt)
- [Private constants regression](private-case-constants.txt)
- [Entry case field regression](entry-case-field.txt)
- [Deferred case field regression](deferred-case-field.txt)
- [Computed case tuple regression](computed-case-tuple.txt)
- [Tagged continuation audit](tagged-continuation-audit.txt)
- [Continuation instructions](continuation-candidates.txt)
- [ASN.1 payload lifetime](asn1-payload-lifetime.txt)
- [Audit script](rank-tagged-continuations.eval)
