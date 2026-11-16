# Tuple fields live across calls

Pinned OTP 29.1.1 implements tuple projection sinking in lib/compiler/src/beam_ssa_opt.erl. Its comments at line 2762 describe moving each projection to a block that still dominates all uses. It also accounts for the retention cost of keeping the original tuple alive, using mapfoldl as its example.

Two release functions illustrate distinct layouts:

- asn1ct_imm:combine_imms/2 has a two-slot frame. The first projection to Y0 is emitted before the second call. A swap saves the original tuple in Y1 while placing the second call's argument in X0. Later calls extract from the saved tuple and reuse both stack slots.
- asn1ct_parser2:parse_Intersections/1 has a one-slot frame. It extracts the live field directly to Y0 before the second call, then extracts the second call's fields into X1 and X0 before the nil test.

Both use exact return-shape facts for local producers, so the release has no tuple type or arity tests in either function. The current change handles those facts for the immediate-consumer case. The next planner needs separate live ranges for the original tuple and its projected fields, with placement based on the points where the individual fields are needed.

[OTP release instructions](asn1-release-instructions.txt)
