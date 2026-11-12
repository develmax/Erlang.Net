# Part 1284 — Format atom names in spec-derived documentation signatures

Apply the existing Erlang atom name formatter to the function name at the start of a spec-derived Docs signature. Explicit documentation signatures retain precedence.

The focused probe compares reserved, quoted and plain names with OTP. The complete Docs section of gl.beam now matches byte for byte. Code, Attr and Line still differ. Line differs only in the instruction count, where the generated image is one lower.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. ELDAPv3 remains Equal. OTP accepted all differing pairs. All six changed modules now match the reference Docs section byte for byte.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-doc-signatures.ps1)
- [gl Line byte comparison](gl-line-bytes.txt)
- [gl section comparison](gl-chunks.txt)
