# Part 1263 — Match mixed entry guards and tuple tail argument moves

Allow exact integer alternatives and tuple arity checks in the constant-module call path. Emit integer selection and tuple checks before frame allocation. In a simple single-clause tuple tail wrapper, extract the final used member into the tuple input register and use the existing parallel move scheduler for the call operands.

The probe checks exact integer matching, tuple arity failure, and tuple tail argument order. In the standalone `gl` rebuild, `color3bv/1`, `clipPlane/2` and `colorMask/4` match after label normalization. The count rises from 645 to 745 matching functions. `Dbgi` remains byte-identical, and generated `Code` falls from 83,021 to 79,294 bytes.

Remaining wrapper groups include a `length/1` binding before the constant-module call, mixed type alternatives, and tuple forwarding with multiple arguments.

Keep the final field in its extraction register when it is already a call argument destination. The TLS probe confirms that `new_session_ticket_base/1` retains the baseline instructions. The two other changed TLS functions, `stateful_use/4` and `stateless_use/4`, now match OTP after label normalization even though their encoded instructions use more bytes than the baseline.

The part 1262 ranking finds 4,350 release list comprehension function names and arities absent from the corresponding generated images across 515 modules. It includes naming differences as well as missing helpers.

The final run generated all 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. Nineteen images changed. The Code length moved closer to OTP in 18 images, with a net reduction in the size gap of 3,790 bytes. The TLS length gap grew six bytes while two normalized functions became equal. OTP accepted all 1,007 differing pairs.

- [Probe source](mixed_guard_probe.erl)
- [Probe command](check-mixed-guards.ps1)
- [Execution result](probe.txt)
- [Normalized gl ranking](gl-functions.txt)
- [Focused gl functions](gl-targets.txt)
- [Standalone gl sections](gl-chunks.txt)
- [TLS tuple forwarding comparison](tls-corrected.txt)
- [Comprehension name ranking](comprehension-ranking.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
