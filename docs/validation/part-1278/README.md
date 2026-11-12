# Part 1278 — Build receive call lists from message registers

A terminal flat receive branch can build a two-element list directly from its matched fields before an external call. When the forwarded result is the only dependency of subsequent expressions, the message fields need no stack slots.

The focused probe checks list order, a zero-slot frame and the fallback when a matched field is used later. In gl:rec/1 the frame drops from four slots to one, the release size. Extraction order, register assignment and call locations still differ.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. Only gl changed, with its Code size gap decreasing by 15 bytes.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-receive-list.ps1)
- [gl instruction comparison](gl-receive.txt)
