# Part 1976 option controls

Both failed tests stopped in the public source mapping entry point at the no_ssa_opt directive. The retained harness errors report that BeamSourceForms.FunctionLines does not support the compile attribute. The source correction is test-only. It reuses the existing BeamAdjacentNumericResultChecks.Compile helper for each disabled control. The normal and other negative profiles continue through the same public compilation path and all existing assertions remain in place.

The exact compiler 784C93E6500E67A1E8044EDA282783F0190D3BA37D86D7C3A852FA227B84E9C3, test payload 9557EA65085DFFE23619444B07E7E62F5FF757942ACF6D4C89CF5D0D3E1C7A49 and runtime 7445A7EA8A055C952841AD41A2F4F0138B7FE39EAB6762EB1030E59308A1C8AA were copied into the private fixture directory. Reflection read the test payload's source constants and invoked the same internal predicates directly. The seven checks matched expectations.

| Check | Normal | No SSA | Other controls |
| --- | --- | --- | --- |
| HasOwnedStateBranches | true | false | Overlapping state tags false |
| HasMembershipTable | true | false | Compound and loose numeric false |

The already generated seven images were observed independently on installed OTP 29.1.1. Both normal and disabled SSH variants retained the earlier oracle hash 3B0DDC33F436486E5EF7C07B671D43FA81ED656507837846A6587A312F461E09. The overlap variant retained 8976B728ADA2631D788455E661729321368DCBF08B3512F29539F74DD1B524FF. Guard normal and disabled produced byte-identical ETF terms including the local if_clause at beam_guard_atom_alternatives.erl line 6. The physical Line operands both use the implicit module filename. The managed BeamLineTableReader reconstructs that filename from the module atom. The compound guard catches the failed first comparison with element/2 and reaches alpha; exhausted alternatives have the same error location. The numeric control accepts 1.0 and 1 while retaining their different terms.

The two edited source preimages match the known-groups build receipt. Strict git replay succeeded privately. The exact compiled payload was used only for private fixture compilation, direct structural reflection and native observation of the emitted images. No SDK build, managed harness suite retry, shared edit or OTP checkout change was performed. The corrected managed test assembly still needs the root's authorized build and run.

- [Exact payload identities](payload-identity.json), [structural results](structural-results.json), [native results](native-results.json)
- [Guard original failure](dispatches-atom-only-guard-alternatives.failure.txt), [SSH original failure](partitions-tagged-state-inside-tuple-arity.failure.txt)
- [Tests-only patch](tests-only.patch) and [verified preimages](preimages.json)
