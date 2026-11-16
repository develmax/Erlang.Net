# Part 1108 — project a tagged call result directly in X0

The general emitter handles a terminal case that projects a single field from a tagged tuple returned by an ordinary call. The call consumes prepared entry operands, the frame needs zero Y slots, and the fallback returns a constant. When a preceding clause matches the same field and converts lists to atoms, both clauses share the tuple check and field extraction.

The pinned OTP probe verified direct projection, list conversion, return of a non-list field, fallback and badarg propagation. The unguarded projection matched the OTP instruction list. In the guarded probe OTP inferred the tag and tuple size from the local callee, so its code used an is_tuple check with type information where the general C# emitter used is_tagged_tuple.

- [Probe source](beam_entry_tagged_projection_probe.erl)
- [Execution assertions](run.eval)
- [OTP and generated instructions](disassembly.txt)

The release form_default/3 instructions match the generated function except for label numbers. Both now use a zero-slot frame and one projection. The full corpus generated 1,363 images. There were 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. The Code length gap narrowed in all eight changed images.

- [Release comparison](form-default.txt)
- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
