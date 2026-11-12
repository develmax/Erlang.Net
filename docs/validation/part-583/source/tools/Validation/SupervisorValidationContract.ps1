# Oracle admission and capture contracts. Observer source stays unchanged so
# the same Erlang observations and source locations apply to both backends.
$SupervisorOracle = @{
    DefaultAdmission = 'artifacts/library376-official-compiler/official-admission.json'
    DefaultOutput = 'artifacts/supervisor/oracle-001'
    Release = '29.1.1'
    Commit = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
    Launcher = 'bin/erl.exe'
    RuntimeClosurePattern = '^erts-[^/]+/bin/(erl.exe|beam.smp.dll)$'
    LibraryClosurePattern = '^lib/(stdlib-[^/]+|kernel-[^/]+|erts-[^/]+)/(ebin|src)/'
    MinimumClosureFiles = 15
    ProvenanceJsonDepth = 8
    ObserverSource = 'tools/Validation/Erlang.SupervisorCompatibility/supervisor_port_cases.erl'
    CompanionSource = 'tools/Validation/Erlang.SupervisorCompatibility/supervisor_port_second.erl'
    ObserverBeam = 'supervisor_port_cases.beam'
    CompanionBeam = 'supervisor_port_second.beam'
    Exports = 'exports.etf'
    Corpus = 'original.etf'
    Effects = 'effects.etf'
    FeatureBootstrap = 'feature-bootstrap.etf'
    Log = 'oracle.log'
    Provenance = 'provenance.json'
    NoShellOption = '-noshell'
    EvalOption = '-eval'
    CaptureExpression = 'Files=filelib:wildcard("tools/Validation/Erlang.SupervisorCompatibility/Reference/*.erl"),lists:foreach(fun(F)->{ok,_}=compile:file(F,[{outdir,"{2}"}]) end,Files),{ok,supervisor_port_second}=compile:file("{0}",[{outdir,"{2}"}]),{ok,supervisor_port_cases}=compile:file("{1}",[{outdir,"{2}"}]),true=code:add_patha("{2}"),logger:set_primary_config(level,none),ok=supervisor_port_cases:write("{2}"),halt().'
    AdmissionFailure = 'supervisor requires the admitted official OTP 29.1.1 provider.'
    IncompleteClosure = 'Incomplete supervisor oracle closure.'
    InputChanged = 'supervisor oracle input changed.'
    ExistingOutput = 'Choose a new output directory.'
    UnsupportedPath = 'Unsupported output path.'
    CaptureFailure = 'supervisor oracle capture failed.'
}
