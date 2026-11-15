# Part 1772

The compiler delays frame allocation past safe tagged tuple tests and tuple projections. It preserves the projected field in an X register until a branch needs its frame. The terminal fallback can construct its result in X registers and return without a frame. The stack trimmer maps slots written in a suffix to their new locations and copies only values that remain live at the trim point.

The guarded tagged-tuple fixture passed 11 assertions on the previous compiler, the current compiler and OTP 29.1.1. Disassembly of the recursive tagged-tuple fixture confirms that the trim no longer reads the unwritten Y register. The focused tests, full Windows OTP corpus comparison and broader compiler test block are pending at this checkpoint.

The most recent verified corpus result is 399 of 1,366 byte-identical images from part 1771. The C# compiler emitted all 1,366 images in that run. [Part 1771 evidence](../part-1771/README.md)

[Fixture](beam_entry_frame_probe.erl) · [Fixture execution](fixture-behavior.txt) · [Nested tuple instructions](nested-instructions.txt)
