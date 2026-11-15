# Part 1783

Sort consecutive independent X-to-Y moves by destination slot after branch slot reuse. Preserve original store order when a run writes the same destination twice.

Follow the independent move ordering in beam_block:sort_moves from pinned OTP 29.1.1. No dependency was added.

All 11 focused tests passed. The preceding compiler, this compiler and OTP each passed 15 fixture assertions, including a guard failure that still needs the original input list. The normalized list_case/2 and list_clause/2 instructions match OTP. The compiler from part 1782 fails the tightened emission regression. The broader compiler block was still running at publication.

The full Windows OTP 29.1.1 corpus comparison was still running at publication. The last completed release comparison is in part 1780.

Handle consecutive tuple field projections before frame allocation and compare the remaining register and instruction differences against the release corpus.

[Fixture](beam_entry_patterns.erl) · [Fixture execution](behavior.txt) · [Previous compiler](previous-entry.txt) · [Current compiler](new-entry.txt) · [OTP](otp-entry.txt) · [Focused tests](focused.txt) · [List case comparison](list-case-reference.txt) · [List clause comparison](list-clause-reference.txt) · [Previous compiler regression](negative.txt)
