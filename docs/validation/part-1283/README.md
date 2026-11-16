# Part 1283 — Tail-call with the selected case result

The case-result forwarding path emits an external tail call and reports that the decoder sequence has completed its return. Other decoder paths retain their existing frame return.

The gl comparison confirms call_ext_last for erlang:load_nif/2. The frame still has three slots, compared with two in OTP. The behavioral probe exercises both branches through the ordinary case call path. The separate Docs comparison finds two signature differences, both missing quotes around reserved atom names.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. Six of the seven changed images moved closer in Code size, with the aggregate gap decreasing by ten bytes.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-case-result.ps1)
- [gl instruction comparison](gl-init.txt)
- [Decoded gl documentation differences](gl-docs.txt)
