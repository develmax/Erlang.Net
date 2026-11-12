# Part 1269 — Return directly from terminal receive branches

When a receive is the entire function clause body, emit a return or tail call inside each successful branch and inside the timeout branch. Receive expressions that feed subsequent code still join their branches. Reuse the existing tail call lowering for local calls, eligible external calls and the one-argument exception BIFs.

The gl:rec/1 comparison confirms direct recursion, a tail call to erlang:error/1 and a direct return for the two-element tuple branch. ExpT and LocT match again. The Code gap falls from 80 to 65 bytes, and Line has the release length. The remaining instruction differences include frame size and tuple field extraction.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. ELDAPv3 remains Equal. The OTP loader accepted all 1,007 differing pairs. There were 179 changed images, with the aggregate absolute Code size difference decreasing by 5,611 bytes.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-receive-tail.ps1)
- [gl instruction comparison](gl-receive.txt)
- [gl section comparison](gl-chunks.txt)
