# Part 1276 — Share the heap check for a tagged list element

For a tagged two-element tuple whose value comes from a static remote call, reserve space for the tuple and enclosing list cells together. Evaluate the call first, retain X0 across the check, then construct the tuple and list.

The focused probe checks one seven-word heap allocation for the tuple and two list cells. The same check now matches the release in gl:rec/1. Its frame remains at four slots, compared with one in OTP.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. The Code size gap decreased in all 68 changed images, by 403 bytes in aggregate.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-list-heap.ps1)
- [gl instruction comparison](gl-receive.txt)
