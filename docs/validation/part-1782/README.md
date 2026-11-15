# Part 1782

Write the list tail back to the source X register when liveness proves that every outgoing path can discard the original input. The fixture allocates two slots with three live X registers.

Use the existing branch liveness analysis and the register layout from pinned OTP 29.1.1. No dependency was added.

All 11 focused tests passed. The preceding compiler, this compiler and OTP each passed 12 fixture assertions. The broader compiler block was not run for this intermediate build.

The full Windows OTP 29.1.1 corpus comparison was still running at publication. The last completed release comparison is in part 1780.

Order independent entry stack stores by the destination slot.

[Fixture](beam_entry_patterns.erl) · [Fixture execution](behavior.txt) · [Previous compiler](previous-entry.txt) · [Current compiler](new-entry.txt) · [OTP](otp-entry.txt) · [Focused tests](focused.txt)
