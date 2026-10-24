using Erlang;

namespace Erlang.Tests;

internal static class LibraryDependencyOriginalNames
{
    internal const string Prefix = "Erlang.Tests.LibraryDependencies392.";
    internal const string Manifest = "manifest.json";
    internal const string Dependencies = "dependencies.json";
    internal const string Beam = ".beam";
    internal const string Run = "run";
    internal const string Stacks = "run_stacks";
    internal const string Test0 = "runtime/library392/original-catch-whole-terms";
    internal const string Test1 = "runtime/library392/original-bif3-whole-terms";
    internal const string Test2 = "runtime/library392/original-wide-whole-terms";
    internal const string Test3 = "runtime/library392/original-pretty-whole-terms";
    internal const string Test4 = "runtime/library392/original-unicode-whole-terms";
    internal const string Test5 = "runtime/library392/original-receive-whole-terms";
    internal const string Test6 = "runtime/library392/original-foreign-malformed-native-helper-whole-terms";
    internal const string FixedTest = "runtime/library392/original-fixed-binary-whole-terms";
    internal const string DefaultDepth = "default";
    internal const string ValueSuffix = "-values.etf";
    internal const string StackSuffix = "-stacks.etf";
    internal const string UnicodeFieldsTest = "runtime/library392/original-unicode-fields-whole-terms";
    internal const string MetadataTest = "runtime/library392/original-ordinary-metadata-whole-terms";
    internal const string Small393Test = "runtime/library393/original-is-ne-fnegate-whole-terms";
    internal const string FloatString393Test = "runtime/library393/original-float-string-whole-terms";
    internal const string Math394Test = "runtime/library394/original-math-whole-terms";
    internal const string Math394OwnerTest = "runtime/library394/original-math-strict-startup-owner";
    internal const string FileModule = "file";
    internal const string MathModule = "math";
    internal const string Binary395Test = "runtime/library395/original-binary-whole-terms";
    internal const string Bif395Test = "runtime/library395/original-bif-whole-terms";
    internal const string Frame395Test = "runtime/library395/original-frame-whole-terms";
    internal const string Receive396Test = "runtime/library396/original-receive-whole-terms";
    internal const string Encoding396Test = "runtime/library396/original-encoding-whole-terms";
    internal const string Handler397Test = "runtime/library397/original-handler-whole-terms";
    internal const string Map397Test = "runtime/library397/original-map-whole-terms";
    internal const string Context397Test = "runtime/library397/original-context-whole-terms";
    internal const string Position398Test = "runtime/library398/original-position-whole-terms";
    internal const string Receive398Test = "runtime/library398/original-receive-whole-terms";
    internal const string WarningDefault398Test = "runtime/library398/original-warning-default-whole-terms";
    internal const string WarningInfo398Test = "runtime/library398/original-warning-info-whole-terms";
    internal const string WarningError398Test = "runtime/library398/original-warning-error-whole-terms";
    internal const string WarningWarning398Test = "runtime/library398/original-warning-warning-whole-terms";
    internal const string Sofs398Test = "runtime/library398/original-sofs-relation-whole-terms";
    internal const string Callback399Test = "runtime/library399/original-callback-whole-terms";
    internal const string Nested399Test = "runtime/library399/original-nested-whole-terms";
    internal const string Os399Test = "runtime/library399/original-os-windows31-whole-terms";
    internal const string OsModule = "os";
    internal const string OsPutEnv = "putenv";
    internal const string OsSeedKey = "ERLANG399_SEED";
    internal const string OsSeedValue = "seed_value";
    internal static readonly LibraryDependencyOriginalSuite Os399 = new(
        "os399",
        "beam_os_deterministic_observer399",
        ["beam_os_deterministic_observer399", "beam_os_subject399"],
        [OsModule],
        31,
        3
    );
    internal const string Port399Test = "runtime/library399/original-port-windows6-whole-terms";
    internal static readonly LibraryDependencyOriginalSuite Port399 = new(
        "port399",
        "beam_windows_port_successor399",
        ["beam_windows_port_successor399"],
        [OsModule],
        6,
        2
    );
    internal const string NetKernel400Test = "runtime/library400/original-net-kernel-native-whole-terms";
    internal const string NetKernelModule = "net_kernel";
    internal static readonly LibraryDependencyOriginalSuite NetKernel400 = new(
        "netkernel400",
        "beam_net_kernel_observer400",
        ["beam_net_kernel_observer400"],
        [NetKernelModule],
        23,
        2
    );
    internal const string NewConnection400Test = "runtime/library400/original-new-connection-native-whole-terms";
    internal static readonly LibraryDependencyOriginalSuite NewConnection400 = new(
        "newconnection400",
        "beam_new_connection_observer400",
        ["beam_new_connection_observer400"],
        [],
        17,
        2
    );
    internal const string Failure = "Original dependency-library full-term comparison failed";
    internal const string Code = "Code";
    internal const string Structure = "Structure";
    internal const string Atoms = "Atoms";
    internal const string ExportChunk = "ExpT";
    internal const string Unicode = "unicode";
    internal const string UnicodeTrap = "characters_to_binary_int";
    internal static readonly string[] Libraries = ["gen", "sys", "proc_lib", "io_lib", "io_lib_format", "io_lib_fread", "io_lib_pretty", "lists", "string", "unicode_util"];
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(180);
    internal static readonly LibraryDependencyOriginalSuite Catch = new(
        "catch",
        "beam_catch_observer392",
        ["beam_catch_observer392", "beam_catch_subject392"],
        [],
        18,
        3
    );
    internal static readonly LibraryDependencyOriginalSuite Bif3 = new(
        "bif3",
        "beam_bif3_observer392",
        ["beam_bif3_observer392", "beam_bif3_subject392"],
        [],
        13,
        3
    );
    internal static readonly LibraryDependencyOriginalSuite Wide = new(
        "wide",
        "beam_wide_integer_observer392",
        ["beam_wide_integer_observer392"],
        [],
        8,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite Pretty = new(
        "pretty",
        "beam_pretty_observer392",
        ["beam_pretty_observer392"],
        ["unicode", "epp"],
        10,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite UnicodeSuite = new(
        "unicode",
        "beam_unicode_observer392",
        ["beam_unicode_observer392"],
        ["unicode"],
        99,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite Receive = new(
        "receive",
        "beam_catch_mailbox_observer392",
        ["beam_catch_mailbox_observer392", "beam_catch_mailbox_subject392"],
        [],
        9,
        3
    );
    internal static readonly LibraryDependencyOriginalSuite Fixed = new(
        "fixed",
        "beam_unicode_match_observer392",
        ["beam_unicode_match_observer392"],
        ["unicode"],
        7,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite UnicodeFields = new(
        "unicode-fields",
        "beam_unicode_fields_observer392",
        ["beam_unicode_fields_observer392"],
        ["unicode"],
        21,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite Metadata = new(
        "metadata",
        "beam_module_info_observer392",
        ["beam_module_info_observer392", "beam_module_info_subject392"],
        [],
        22,
        3
    );
    internal static readonly LibraryDependencyOriginalSuite Small393 = new(
        "small393",
        "small_opcodes_observer393d",
        ["small_opcodes_observer393d"],
        [],
        19,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite FloatString393 = new(
        "float-string393",
        "beam_float_string_observer392",
        ["beam_float_string_observer392"],
        [],
        28,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite Math394 = new(
        "math394",
        "beam_math_observer392",
        ["beam_math_observer392"],
        [MathModule],
        248,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite Binary395 = new(
        "binary395",
        "beam_binary_flags_observer395",
        ["beam_binary_flags_observer395"],
        [],
        40,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite Bif395 = new(
        "bif395",
        "beam_infallible_bif_observer394",
        ["beam_infallible_bif_observer394", "beam_infallible_bif_subject394"],
        [],
        8,
        3
    );
    internal static readonly LibraryDependencyOriginalSuite Frame395 = new(
        "frame395",
        "frame_join_observer394",
        ["frame_join_observer394", "frame_join394"],
        [],
        10,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite Receive396 = new(
        "receive396",
        "beam_pure_receive_observer396",
        ["beam_pure_receive_observer396"],
        [],
        18,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite Encoding396 = new(
        "encoding396",
        "beam_filename_observer395",
        ["beam_filename_observer395", "beam_filename_subject395"],
        ["file"],
        5,
        3
    );
    internal static readonly LibraryDependencyOriginalSuite Handler397 = new(
        "handler397",
        "handler_routing_observer396",
        ["handler_routing_observer396"],
        [],
        7,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite Map397 = new(
        "map397",
        "beam_receive_map_observer397",
        ["beam_receive_map_observer397"],
        [],
        18,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite Context397 = new(
        "context397",
        "context_forwarding_observer397",
        ["context_forwarding_observer397"],
        [],
        8,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite Position398 = new(
        "position398",
        "binary_position_observer398",
        ["binary_position_observer398"],
        [],
        10,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite Receive398 = new(
        "receive398",
        "beam_receive_binary_observer398",
        ["beam_receive_binary_observer398"],
        [],
        18,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite WarningDefault398 = new(
        "warning398-default",
        "beam_warning_observer398",
        ["beam_warning_observer398", "beam_warning_subject398"],
        ["error_logger"],
        5,
        3
    );
    internal static readonly LibraryDependencyOriginalSuite WarningInfo398 = new(
        "warning398-info",
        "beam_warning_observer398",
        ["beam_warning_observer398", "beam_warning_subject398"],
        ["error_logger"],
        5,
        3,
        RuntimeWarningMapping.Info
    );
    internal static readonly LibraryDependencyOriginalSuite WarningError398 = new(
        "warning398-error",
        "beam_warning_observer398",
        ["beam_warning_observer398", "beam_warning_subject398"],
        ["error_logger"],
        5,
        3,
        RuntimeWarningMapping.Error
    );
    internal static readonly LibraryDependencyOriginalSuite WarningWarning398 = new(
        "warning398-warning",
        "beam_warning_observer398",
        ["beam_warning_observer398", "beam_warning_subject398"],
        ["error_logger"],
        5,
        3,
        RuntimeWarningMapping.Warning
    );
    internal static readonly LibraryDependencyOriginalSuite Sofs398 = new(
        "sofs398",
        "original_sofs_observer398",
        ["original_sofs_observer398"],
        ["sofs"],
        10,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite Callback399 = new(
        "callback399",
        "beam_receive_callback_observer399",
        ["beam_receive_callback_observer399"],
        ["prim_eval"],
        28,
        2
    );
    internal static readonly LibraryDependencyOriginalSuite Nested399 = new(
        "nested399",
        "beam_receive_callback_nested399",
        ["beam_receive_callback_nested399"],
        ["prim_eval"],
        2,
        2
    );
    internal static readonly string[] UnicodeNative = ["bin_is_7bit/1", "characters_to_binary/2", "characters_to_list/2"];
}
