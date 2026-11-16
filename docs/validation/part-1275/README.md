# Part 1275 — Keep the last computed list element in X0

For a list with a constant tail, evaluate a final call or tuple element into X0 and use it directly in the first put_list instruction. Earlier operands keep their existing evaluation order. The heap check keeps X0 live.

The focused probe verifies direct construction, including a constant suffix, and checks the resulting list and call order against OTP. In gl:rec/1 the report tuple goes directly into the list and the frame falls from five slots to four. OTP uses one slot.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. ELDAPv3 remains Equal. OTP accepted all differing pairs. The Code size gap decreased in 664 of the 665 changed images and increased in one. The aggregate gap decreased by 30,924 bytes.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-list-register.ps1)
- [gl instruction comparison](gl-receive.txt)
