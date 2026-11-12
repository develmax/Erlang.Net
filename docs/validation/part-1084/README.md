# Part 1084 — retain terminal call results in X0

For a fresh variable bound to a call with simple operands and used immediately in the returned tuple, the general emitter builds the tuple directly from X0, previously saved variables and constants. It emits the call once even when several fields refer to its result. A call with argument bindings retains the existing match handling.

The OTP execution probe covers distinct and repeated result fields, process dictionary updates and preservation of badmatch when an argument binds the variable. In put_value/2 both the new output and OTP allocate two Y slots and omit init_yregs. They still differ in register ordering and the new output reloads the call arguments. In repeated/2 the new output still preserves one input that OTP leaves in X.

- [Probe source](frame_entry_probe.erl)
- [OTP assertions](run.eval)
- [Previous emission](before.txt)
- [New emission](after.txt)
- [OTP emission](otp.txt)
- [Focused compiler tests](focused.json)

The full corpus includes 1,366 files. It produced 1,363 BEAM files, with 348 Equal and 1015 Different. Three sources were rejected. ELDAPv3 is Equal. Compared with part 1082, 49 generated images changed and their Code size gaps narrowed. No gap widened. OTP accepted all 1015 differing images for loading.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Opcode ranking](opcode-ranking.txt)
