# Part893 Fixed-width bit patterns

The general emitter matches fixed integer and binary fields and a final unsized binary tail. It emits signed and endian flags, checks total width or tail alignment, and applies pattern tests to the extracted fields. The existing byte-specialized paths remain in use.

The deterministic fixture extracts a signed little-endian integer. Rebuild with `erlc +deterministic beam_fixed_bits_probe.erl`. The complete fixture matched byte for byte. [OTP assembly](otp-direct-disassembly.S).

The compound test decodes a four-bit field, a twelve-bit little-endian field and an aligned binary tail. Both images returned `{{10,288,<<255>>},no,no}` on OTP for valid, short and unaligned inputs. General registers and dynamic-width patterns remain open.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned installation and report paths. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases. All three passed. Full runtime and canonical readiness checks remain pending. [Focused report](focused.json).

The full 1,366-file corpus reported 100 Equal, 746 Different, 164 SourceRejected and 356 Unsupported. Fourteen modules moved from Unsupported to Different, all previously stopped in BinaryPattern. Among the Different modules, 37 already match the release Code chunk. [Corpus report](otp-corpus.json).
