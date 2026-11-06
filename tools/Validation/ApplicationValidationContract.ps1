# Oracle admission and capture contracts. Observer source stays unchanged so
# the same Erlang observations and source locations apply to both backends.
$ApplicationOracle = @{
    DefaultAdmission = 'artifacts/library376-official-compiler/official-admission.json'
    DefaultOutput = 'artifacts/application/oracle-001'
    Release = '29.1.1'
    Commit = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
    Launcher = 'bin/erl.exe'
    RuntimeClosurePattern = '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$'
    LibraryClosurePattern = '^lib/(stdlib-[^/]+|kernel-[^/]+|erts-[^/]+)/(ebin|src)/'
    MinimumClosureFiles = 15
    ProvenanceJsonDepth = 8
    ObserverSource = 'tools/Validation/Erlang.ApplicationCompatibility/application_port_cases.erl'
    CompanionSource = 'tools/Validation/Erlang.ApplicationCompatibility/application_port_callback.erl'
    UpstreamSource = 'tools/Validation/Erlang.ApplicationCompatibility/Upstream/application_SUITE.erl'
    ObserverBeam = 'application_port_cases.beam'
    CompanionBeam = 'application_port_callback.beam'
    Exports = 'exports.etf'
    Corpus = 'original.etf'
    Effects = 'effects.etf'
    FeatureBootstrap = 'feature-bootstrap.etf'
    Log = 'oracle.log'
    Provenance = 'provenance.json'
    NoShellOption = '-noshell'
    EvalOption = '-eval'
    CaptureExpression = '{ok,application_port_callback}=compile:file("{0}",[{outdir,"{2}"}]),{ok,application_port_cases}=compile:file("{1}",[{outdir,"{2}"}]),{ok,application_SUITE}=compile:file("{3}",[{outdir,"{2}"}]),true=code:add_patha("{2}"),ok=application_port_cases:write("{2}"),halt().'
    AdmissionFailure = 'application requires the admitted official OTP 29.1.1 provider.'
    IncompleteClosure = 'Incomplete application oracle closure.'
    InputChanged = 'application oracle input changed.'
    ExistingOutput = 'Choose a new output directory.'
    UnsupportedPath = 'Unsupported output path.'
    CaptureFailure = 'application oracle capture failed.'
}
