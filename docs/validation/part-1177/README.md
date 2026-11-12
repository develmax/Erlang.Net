# Part 1177 — match flat tuple entries before allocation

The register-based tuple emitter also handles single unguarded clauses. It tests tuple shape and literals before allocation and reserves Y slots only for bindings read by the body. Failure before allocation reaches func_info with the original arguments. Repeated bindings, guards and unsupported field patterns use the existing general path.

The probe passed with both OTP and C# output. The generated frame shrank from four slots to two, matching the OTP frame size. The generated code still extracts tuple fields into X registers before allocating, while OTP extracts the field it uses directly into Y afterward.

The Windows OTP 29.1.1 corpus generated 1,363 of 1,366 images. It recorded 356 Equal, 1,007 Different and 3 SourceRejected. ELDAPv3 remained Equal. Among 329 changed images the Code length gap narrowed in 321 and widened in five. OTP prepared all 1,007 differing images for loading. Three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe assertions](run.eval)
- [Before](before.txt)
- [After](after.txt)
- [OTP](otp.txt)
- [Opcode ranking](ranking.txt)
