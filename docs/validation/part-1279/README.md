# Part 1279 — Share extraction between two receive clauses

When two unguarded three-field receive clauses share a tag, extract their fields once if the first tests a bound variable and the second binds a fresh one. The tag test can fail directly to the receive loop. A failed bound-variable comparison reuses the extracted fields in the second clause.

The focused probe verifies the shared extraction, equality fallthrough, list order, timeout and retention of unrelated messages. In gl:rec/1 the remaining instruction difference is a repeated line before the reporting call.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. Only gl changed, with its Code size gap decreasing by 18 bytes.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-receive-shared.ps1)
- [gl instruction comparison](gl-receive.txt)
