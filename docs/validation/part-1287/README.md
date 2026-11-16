# Part 1287 — Substitute a leading constant used in case branches

Fold a leading constant in a zero-argument function when the remaining sequence uses supported expressions and case patterns cannot rebind the constant. Reuse the existing constant substitution for calls and constructors.

The probe checks both case arms and confirms that the constant is never stored in a Y slot. gl:init_nif/0 now allocates two slots, like OTP. The branch result forwarding, slot assignment and initialization still differ.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. Only gl changed. Its frame now matches, and its Code size gap increased by two bytes.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-case-constant.ps1)
- [gl instruction comparison](gl-init.txt)
