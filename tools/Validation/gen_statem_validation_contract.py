"""Pinned input identity and expected dimensions of the focused gen_statem corpus."""
BASELINE = "OTP-29.1.1"
COMMIT = "ad05823719d77c8faee87348ea39513d4e2f99c5"
EXPORT_COUNT = 40
API_CASE_COUNT = 74
ENGINE_SCENARIO_COUNT = 311
BACKENDS = ("Managed", "Beam")
MANAGED_BACKEND = BACKENDS[0]
REPORT_SUFFIX = "-report.json"
PROVENANCE_FILE = "provenance.json"
ORIGINAL_MODULE = "gen_statem"
ORIGINAL_SOURCE = "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp/gen_statem.erl"
MANAGED_SOURCES = "src/Otp/Managed/Erlang.Otp.Stdlib/Processes/GenStatem"
SOURCE_OBJECT = BASELINE + ":lib/stdlib/src/gen_statem.erl"
DEFAULT_AUDIT = "artifacts/gen-statem/source-audit.json"
CORPUS_DIGESTS = (
    ("CorpusSha256", "original.etf"),
    ("EffectsSha256", "effects.etf"),
    ("FeatureBootstrapSha256", "feature-bootstrap.etf"),
)
