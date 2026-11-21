# Wide integer construction qualification

Source baseline Part1974, compiler CEAA23E07C812C2387EB9CD6BDF20822A9AF8BAC181B5A0BBB5652EC846DC387. Pinned OTP 29.1.1 is ad05823719d77c8faee87348ea39513d4e2f99c5. Ranked StrT evidence is in the sibling `strt-residual-1974-audit` with receipt d5e0292fedf68e1f78b231e154b2a5e201c174f8f23d09968cec47e12db7695d.

## Proof

Pinned `lib/compiler/src/beam_ssa_opt.erl` lines 2443–2457 and 2492–2513 treat wide big-endian integer segments using an arithmetic shift. The first shift count at which a nonnegative input is zero is its bit length. For a negative input, arithmetic right shift produces minus one at the bit length of its complement. Round that width up to a multiple of eight. A whole sign-filled constant stays an integer with its original unit. A nonempty remaining suffix keeps its original truncation and is copied from the already normalized bitstring. The high segment uses unit one when split. The high and low widths sum to the same effective width and the separated low bits are byte-aligned. This draft applies to byte-aligned cases where the compiler already produced a string and the retained high part is at least 64 bits. The OTP width limit is 2^24. The existing normalization and segment numbering carry all original checks and error contexts. Little-endian, native-endian and non-byte-aligned paths have their current lowering. The existing module optimization and SSA options guard the new rule.

The independent model compared the draft formula with OTP's shift search and finite-width integer encoding on 5,460 signed and unsigned cases, including truncation and the 64-bit boundary. This model checks arithmetic equivalence. The managed candidate has no SDK run yet.

## Scope

A full physical scan of the actual Part1974 generated images identified 2 Different modules and 3 functions with the necessary existing output signature. A `bs_create_bin` string contains at least eight contiguous zero or 0xff bytes. Zero of 509 Equal modules have this signature. The two Equal modules with any construction string payload, `ssh_bits:mpint/1` and `erl_posix_msg:message_1/1`, are exact controls. The four-module report map is `scope-identities.tsv`.

- `ssh_transport:encrypt/2` and `decrypt/2` use a wide 64-bit zero prefix. The diagnostic StrT goes from 16 to 8 bytes, matching the pinned 8.
- `dtls_handshake:address_to_bin/2` combines 80 zero bits with a two-byte suffix. The diagnostic StrT goes from 15 to 5 bytes against the pinned 7. The remaining partial difference is `dtls_handshake:enc_handshake/2` and its separately folded compression-method fields.

These are physical operand and table projections, with no module byte equality attributed.

## Checks and retake

1. The compiler and permanent regression patches jointly pass strict `git apply --check --whitespace=error-all` against the copied frozen preimages.
2. Pinned and frozen Part1974 both compile the permanent fixture. `check-frozen-red.eval` proves the old four wide starts are strings while pinned starts with integer operands. The retained native fixture reports `ok badarg` for pinned and actual frozen Part1974. It verifies sign, suffix, truncation, small widths, dynamic size and a dynamic operand failure. This uses the frozen compiler through the Windows PowerShell managed host, without an SDK build.
3. After integrating, build and run `compiler/beam/keeps-wide-bit-construction-sign-fill-compact` and existing bitstring construction/match controls. Compile the four scope modules with isolated generated output and use existing raw whole-image and normalized comparisons. Load the two generated affected modules in pinned native OTP, and pass the actual newly generated fixture path to `native-fixture.eval`. Capture full stderr from both loaders and keep reports per SHA.

