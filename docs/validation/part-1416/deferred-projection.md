# Deferred tuple projection

The current planner preserves individual fields across a call. It writes each surviving field to an initialized Y slot while X0 still contains the checked result. The registers used only by the next call are prepared separately, with the source tuple kept intact until the last projection.

In the remaining both/1 probe, both fields are first used after lists:reverse/1. OTP keeps the checked tuple in one slot, reuses the argument slot, trims the frame and extracts both fields just before the final lists:zip3/3 call. The C# compiler uses two field slots. The resulting maximum frames are two for OTP and three for C#. Compare the whole-tuple live range with the live ranges of its fields when extending the shared planner.

The previous Attr size delta in dialyzer_contracts is the default vsn. Its decoded attribute lists contain only that value in the reference and in both tested builds. The emitter derives it from the MD5 of the essential chunks. The integer size in ETF can change as those chunks converge.

- [Probe instructions](deferred-projection.txt)
- [Decoded attributes](attribute-followup.txt)
