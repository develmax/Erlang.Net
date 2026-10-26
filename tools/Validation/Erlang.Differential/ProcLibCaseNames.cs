namespace Erlang.Differential;

public static class ProcLibCaseNames
{
    public const string Module = "proc_lib";
    public const string Spawn = "spawn";
    public const string SpawnLink = "spawn_link";
    public const string InitAck = "init_ack";
    public const string Ok = "ok";
    public const string True = "true";
    public const string False = "false";
    public const string FixtureModule = "oracle_proc_lib";
    public const string Child = "child";
    public const string Parent = "proc_lib_parent";
    public const string Prefix = "proc-lib-local-";
    public const string MfaSpawn = Prefix + "mfa-spawn-ancestry-and-initial-call";
    public const string MfaLink = Prefix + "mfa-spawn-link-atomic-link";
    public const string FunSpawn = Prefix + "fun-spawn-ancestry-and-initial-call";
    public const string FunLink = Prefix + "fun-spawn-link-atomic-link";
    public const string AckOne = Prefix + "init-ack-one-arbitrary-return";
    public const string AckTwo = Prefix + "init-ack-two-arbitrary-return";
    public const string Registered = Prefix + "registered-parent-name";
    public const string Inherited = Prefix + "inherited-ancestors-only";
    public const string InvalidAncestors = Prefix + "improper-parent-ancestors-retained";
    public const string MissingAncestors = Prefix + "ack-missing-ancestors-badmatch";
    public const string EmptyAncestors = Prefix + "ack-empty-ancestors-badmatch";
    public const string ImproperAncestors = Prefix + "ack-improper-ancestors-head";
    public const string InvalidFun = Prefix + "spawn-invalid-fun-function-clause";
    public const string InvalidModule = Prefix + "spawn-invalid-module-function-clause";
    public const string InvalidArgs = Prefix + "spawn-improper-arguments-child-badarg";
    public const string NonzeroFun = Prefix + "spawn-nonzero-arity-fails-in-child";
    public const string MissingMfa = Prefix + "missing-mfa-fails-in-child";
    public const string NativeInitialFun = Prefix + "native-initial-call-fun-wrapper";
    public const string NativeInitialMfa = Prefix + "native-initial-call-mfa-wrapper";
    public const string AtomAncestors = Prefix + "nonlist-parent-ancestors-reset";
    public const string AtomArgs = Prefix + "nonlist-arguments-function-clause";
    public const string MissingContract = "Missing direct proc_lib contract";
    public const string Mismatch = "proc_lib direct contract mismatch";
}
