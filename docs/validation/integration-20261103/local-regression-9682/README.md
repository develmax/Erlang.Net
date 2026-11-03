# Exact-source local regression and isolated timeout control

The [manifest](manifest.json) binds the complete unchanged report/log, isolated control and16 running assembly hashes to ad791db4222edddb99a08d0b6e43f0a7e43f193b.

The actual whole runner completed9681/9682 with one failure: `runtime/beam/lifecycle388/original-alternating-nontail-and-full-trace` exceeded the existing15s generic wrapper (15090ms). The same case on the exact same binaries then passed alone in10497ms under the same wrapper. Runtime/compiler source trees are identical to the earlier green0e snapshot, whose case took12865ms. These observations do not establish a specific cause of the timing variation.

No runtime, expected value, original image, timeout or budget selector was changed. The full report remains failed; the isolated report explicitly remains partial. They must never be composed or relabeled as a passed full report. Canonical readiness requires a genuinely complete passing report on the current source, including the independent exact-head CI if successful.
