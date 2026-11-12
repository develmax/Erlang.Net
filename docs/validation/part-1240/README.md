# Part 1240 — emit abstract attributes from included headers

Use the same attribute writer for main sources and included files. Header tokens already carry source coordinates and file transitions. The existing attribute handlers and default declaration check still determine whether each form can be written.

The probe compares all decoded abstract forms with OTP after normalizing file paths. The included dialyzer attribute, spec, function and file transitions match.

The full release run generated all 1,366 images. It retained 357 Equal, including ELDAPv3, with 1,009 Different and zero rejected. Only Dbgi changed in the 13 affected modules. The restored otp_internal chunk matches byte for byte. OTP accepted all differing image pairs. The strict audit has 19 remaining failures among the original 116 candidates.

- [Abstract form comparison](probe.txt)
- [Probe source](debug_header_attribute_probe.erl)
- [Included attributes and function](debug_header_attribute_probe.hrl)
- [Full corpus](otp-corpus.json)
- [Chunk delta](debug-delta.txt)
- [Strict diagnostics](strict.json)
- [OTP loader](loader.txt)
