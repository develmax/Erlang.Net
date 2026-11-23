# Part 2005 publication source closure

The complete tracked test project builds with the pinned .NET 10.0.400 SDK. Two compiler inputs and four already registered fixtures were restored byte for byte from the validated Part 2003 source snapshot. The [full compiler input comparison](compiler-inputs.json) verifies that the repository contains the same complete compiler source, including the two restored files. Two prepared fixture updates from the snapshot were also retained. Runtime tests from the newer mainline history retain their current content.

The missing closure was exposed by building the published tree, since the scoped compiler had used a complete frozen source tree. The [first build errors](initial-compiler-closure.stdout) and [test build errors](initial-harness-compiler-diagnostics.stdout) are preserved. The fixture repairs use the existing test boundary constants, opcode names and synchronous test adapter. Corrected examples include explicit model compilation for no_ssa_opt, a valid opaque remote call, and atom-table sizes that include the greatest encoded atom index.

The [complete test project build](complete-harness-build.json) passed in 33.065 s. The actual registered harness ran 60 selected cases. After nine fixture repairs, [58 passed and two failed](focused-report.json). Both failures expose remaining compiler differences against OTP 29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5.

- The captured list product uses an untyped Y operand with live X count 5. OTP emits an X operand with the proved finite range and live count 3. The current projection placement bypasses the annotation pass.
- The incomplete tagged terminal group consumes the first clause before the full flat group can factor the shared projections and emit the tag selector. OTP shares all four tuple reads and selects before allocation.

The assertions stay active and record the current problems. The [layout check](fixture-layout.stdout) passed for changed source inputs. No new complete corpus or package-regression result is claimed.
