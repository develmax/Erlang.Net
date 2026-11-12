# Part 1271 — Keep tuple receive scratch registers out of the frame

A receive whose top-level clauses all match tuples can keep the message in a scratch register. No clause can bind the whole message, so no stack slot is reserved for it.

For unguarded flat tuple branches that directly return a variable or literal, exclude pattern scratch registers from stack planning. The focused projection probe uses a zero-slot frame, down from four. It checks projection, literal return and timeout against the OTP build.

The full corpus generated all 1,366 images with 359 Equal, including ELDAPv3, 1,007 Different and zero rejected sources. OTP accepted all 1,007 differing pairs. The change affected 215 images. The Code size gap decreased in 213 and remained the same in two, with an aggregate decrease of 1,275 bytes.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-receive-projection.ps1)
