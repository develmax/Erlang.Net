# Part 1781

Allow branch-local stack-slot reuse after unframed list or tuple projections. Keep the default return outside the frame. The list fixture now allocates two slots.

Reuse the existing branch-local stack allocator and validate the entry-path layout against pinned OTP 29.1.1. No dependency was added.

All 11 focused tests passed. The preceding compiler, this compiler and OTP each passed 12 fixture assertions. The broader compiler block was not run for this intermediate build.

The full Windows OTP 29.1.1 corpus comparison was still running at publication. The last completed release comparison is in part 1780.

Reuse the original input register after the last use of the source list.

[Fixture](beam_entry_patterns.erl) · [Fixture execution](behavior.txt) · [Previous compiler](previous-entry.txt) · [Current compiler](new-entry.txt) · [OTP](otp-entry.txt) · [Focused tests](focused.txt)
