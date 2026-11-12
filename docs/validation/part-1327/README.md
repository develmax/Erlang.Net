# Part 1327

A lifted anonymous function with captures, one unguarded tuple clause and distinct bindings routes a failed shape test to an OTP-style helper. The helper has the declared fun arity, so function_clause shows the supplied arguments without captured values. The success path still needs stack lifetime work.

The focused test compares the failure branch with OTP and checks the function_clause stack arguments. The corpus changed 159 images. It gained 253 label-normalized instruction matches for failure helpers across 134 modules, with zero lost matches. The absolute Code size gap increased by 2,608 bytes in aggregate as the generated files gained helpers.

The full Windows corpus generated all 1,366 images with 363 Equal, 1,003 Different and zero SourceRejected. OTP accepted all differing images. ELDAPv3 remained Equal.

- [Focused check](check-closure-entry-failure.ps1)
- [Focused output](probe.txt)
- [Full corpus](otp-corpus.json)
- [Changed images](delta.json)
- [Instruction matches](normalized-functions.txt)
- [OTP loader](loader.txt)
