# Four integer parse contracts: remaining large-path audit

Source-only audit of list_to_integer/1,2 and binary_to_integer/1,2 against pinned OTP-29.1.1. No large inputs, subject execution, compiler/build, CI or Main edits. The current167-case packet remains immutable.

The next change is implementable as a dedicated big-path adapter, but a final BigInteger bit-length check alone is insufficient. It must preserve three separate priorities: native SMALL scanning; complete original iodata-to-binary validation for list fallback; then trimmed significant-size prechecks before ASCII digit validation and final65535-limb arithmetic limit. The fallback permits nested iodata and binary tails after the source SMALL threshold. The included10-case ordinary fixture prepares small witnesses for that partition only; it has not run and contains no seeded expected outputs.

The public caps are4194304 significant bytes in all bases and1262611 when radix>=10. The64-bit native limit is65535 limbs/4194240 magnitudebits. Zero trimming precedes limits. Below the public caps malformed digit bytes precede arithmetic overflow; above them system_limit precedes digit validation. Illegal list iodata precedes the public cap after native big selection. Detailed source-backed partitions and implementation order are separate JSON files.

Full acceptance still needs per-case/per-profile bounded heavy original evidence retaining complete arguments and stack metadata; ordinary16MiB decoder budgets must not be silently bypassed. The existing mandatory large-cap plan remains applicable. Long traversal/combination also needs ProcessContext cancellation/yield and resource-cleanup proof. No blanket complete-contract classification is justified by the current60 numeric edge cases alone.
