# Part 1313

Adjacent clauses that test a literal or a tuple in the list head can share the list shape check and extraction when an unguarded variable-head clause covers every remaining nonempty list. The emitted code preserves the head until a test succeeds and keeps the original argument for the non-list path. The successful tuple branch can reuse the head register for its last extracted field.

The focused test compares complete instructions with OTP 29.1.1 and exercises the literal branch, tuple branch, fallback for a nonmatching head, and the original argument fallback. The prior literal head and variable-tail argument tests also passed.

- [Focused command](check-shared-head.ps1)
- [Instruction and execution results](probe.txt)
- [Literal head regression check](literal-head-regression.txt)
- [Variable tail regression check](variable-tail-regression.txt)
- [Adjacent clause audit](adjacent-clause-forms.txt)

The final run accepted all 1,366 sources and generated all 1,366 images. It reported 361 Equal and 1,005 Different. All 20 changed images narrowed their Code size gaps. Five additional function instruction streams match after label normalization. OTP accepted every differing image.

- [Full corpus](otp-corpus.json)
- [Changed images](delta.json)
- [Function comparison](normalized-functions.txt)
- [OTP loader](loader.txt)
- [Atom dispatch in the closest remaining module](near-equal-crypto.txt)
