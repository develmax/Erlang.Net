# Guarded terminal call-result projection

The independently generated 4,012-byte megaco_tcp_sup.beam is byte-identical to the pinned Windows OTP-29.1.1 release image. The common SHA-256 is 763AC8A08073103F8C111D36FDA9641741F8416F96577C2CED5BDED5F3D05866. The standard BeamCorpus comparison independently reports Equal. The standalone artifact generator preserves the newly equal image because BeamCorpus only saves different files.

Baseline is the exact-init candidate from the adjacent directory. It uses the frozen Part1850 archive (AE9AF63F43D2874A7120045251EB1A9BE230A7EAFE55E3A323F462326FCD6B71) and the earlier construction-scheduling patch. Baseline compiler SHA-256 is 7428C95FF50DA50A31221330B1355B4CD6ECA1E6DC7E0D29664C1DD2524696A7. Candidate compiler SHA-256 is 0D40EFF23F8A90BEE83F8364931E6EA0BFADBB932150A7F704CF7A77E668CC04. identity.json contains the input and output hashes.

OTP-29.1.1 commit ad05823719d77c8faee87348ea39513d4e2f99c5 already emits the three remaining differences at beam_ssa_codegen. The local OTP listings show the same choices through beam_trim. The C# rule identifies the ordinary call pair, the three outgoing argument assignments and the guarded terminal projection. It emits the independent constant first, then clears the saved input after the final proven read. It roots the two available X registers and projects the guarded field to X0 after the heap check. The existing stack-copy analysis proves the old slot has no value reads on any later path. The check leaves the original code in place when a failure arm reads the saved input. The rule uses decoded BEAM operands and the existing codec and liveness machinery. No dependency or upstream code was added.

| Scoped check | Exact-init baseline | Candidate |
|---|---|---|
| Equal | 6 | 7 |
| Different | 3 | 2 |
| Unsupported | 0 | 0 |
| SourceRejected | 0 | 0 |
| New / lost full matches | | 1 / 0 |

The one-worker candidate comparison took 4.91 seconds. Baseline reports from the earlier three- and six-input scans cover the same exact module set and source inputs. Fresh native preparation and execution covered supervisor init and positive and missing child lookups. The permanent fixture verified the register, root and stack instructions and managed preparation. It reports the intended assertion failure with the baseline and passes using the candidate. Before/after fixture execution also matched for both successful returns, both missing paths and malformed nested tuple exceptions.

The compiler and isolated focused harness built cleanly. The SDK-backed source layout check and patch forward/reverse application checks passed. The patch SHA-256 is 1424167C77B35EFD70EB140291F0358A6F5FC7F7C5DFCB664D9A4269C21F8D05. The full corpus and full regression belong to the integration lane. There were no GitHub writes.
