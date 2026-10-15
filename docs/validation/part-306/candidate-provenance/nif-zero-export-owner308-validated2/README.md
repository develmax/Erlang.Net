# Validated prepared-image owner derivative 308

This is a separate derivative of immutable source proposal `nif-zero-export-owner308-candidate1`, freeze A8E7529ADF8062A6E0BB4C83367076B560B640A6AEDF598C4C1A9C1A75448C44. Its actual production parent is working305 source32 freeze3 `0B595447CCC12C1735B069F987728D434DFBF1709E04931A23049F70C3115E26`, not published HEAD alone. All original resources and both305 HOST rows are retained. Main/Git were not edited; no native/original run or broad full suite occurred here.

Three production files retain exactly the source proposal's ownership design: immutable ModuleCodeImage.ExecutionOwner, PreparedBeamImage constructs its image with owner=this, and the existing exact-current NIF admission branch accepts reference equality with that owner. No registry map, fake export, public loader, default hook, new production file or public MFA is added. Existing one-argument image construction and all earlier old/foreign/pending/generation guards remain.

## Actual red and green

Two initial HOST preconditions failed before the intended assertion. Their logs and original receipts remain, with separate explicit classifications as **not semantic red**:

* The reused reflection helper searched only nonpublic properties, but the positional BeamExecutionIdentity.Image property is public. The derivative HOST uses public+nonpublic lookup for that record.
* An ordinary registered getenv callback correctly owns its own os image. The derivative registers only this explicit trusted fixture forwarding callback through the existing internal RegisterIntrinsic API, so it observes the genuine interpreted BEAM caller rather than an os delegate frame. This does not install any default runtime callback or OS implementation.

After those harness corrections, HOST-only production-parent validation passed fresh_direct_true and failed fresh_false_false_true with actual NifImageAdmissionException.OwnerMismatch during current reservation. The production-three overlay was then applied. Seven-path SDK/custom formatting retained identical Roslyn token kinds/text.

Green target compilation had zero warnings/errors. All six real compiled-BEAM owner scenarios and the pure synchronous NoInlining prepared-cycle GC control passed. All 21 focused managed groups passed, including existing source/BEAM/caller/admission/lifetime/image/permission/tail/loading/purge/initialization and305 exact-image/raw-six regressions.

The managed fake initializer and binding prove internal ownership composition and once-only cleanup, not native ABI behavior. Pending reservation occurs in a genuine interpreted callback. Current reservation reuses only its captured identity; the actual saved local fun then enters its real BEAM frame, validates the same prepared owner, and gets Reload on a further reservation. Foreign prepared/ticket/temporary rollback owners and unrelated images remain refused. False/false/true paths cover delete, old refusal, purge, retirement and rejection after retirement; direct paths cover runtime shutdown. Global export absence remains intact.

## Handoff

`promotion-map.json` binds exact current parent/result bytes for seven C# files, including one additive Program row. `validation-result.json` and the final source closure bind actual execution evidence. Source-only proposal and its peer receipt remain historical inputs; this derivative requires its own final peer review. The heavy gate was explicitly released to lifecycle's307 validation after focus21 completed.

The pure GC control covers only the new unpublished synchronous Prepared/Image cycle; it makes no whole-runtime collection claim. Pinned original source and full design/lifetime rationale remain in the immutable proposal. No new original zero-export NIF loading observation, public load_nif activation, ABI completion, or performance claim is implied.
