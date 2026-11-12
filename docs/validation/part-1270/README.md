# Part 1270 — Return receive fields without a stack spill

For terminal receive branches returning a variable, move the matched field directly from its scratch X register into X0. A terminal literal or a variable already available in its stack slot needs no scratch spill. Branches that compute other expressions or join with subsequent code retain the existing bindings.

The focused OTP comparison verifies projection, a literal result and timeout. The generated probe contains no move from an X register to a Y slot. In gl:rec/1 the result branch drops the store and reload through Y3. Its frame and remaining instruction differences still need alignment.

The corpus generated all 1,366 images. There were 359 Equal, including ELDAPv3, 1,007 Different and zero source rejections. The OTP loader accepted all differing pairs. All 22 changed files moved closer in Code size, by 90 bytes in aggregate.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-receive-projection.ps1)
- [gl instruction comparison](gl-receive.txt)
