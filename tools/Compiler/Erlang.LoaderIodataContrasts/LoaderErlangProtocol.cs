namespace LoaderIodataContrasts;

internal static class LoaderErlangProtocol
{
    internal const string RuntimeInspectionPrefix = "V={erlang:system_info(otp_release),erlang:system_info(version),erlang:system_info(system_architecture),erlang:system_info(build_type),erlang:system_info(emu_flavor),code:root_dir()},";
    internal const string RuntimeFileWritePrefix = "ok=file:write_file(";
    internal const string RuntimeBinaryToTextBridge = ",term_to_binary(V)),ok=file:write_file(";
    internal const string RuntimeTextCaptureSuffix = ",iolist_to_binary(io_lib:format(\"~tp~n\",[V]))),";
    internal const string RuntimeAssertionTuplePrefix = "{";
    internal const string RuntimeAssertionSeparator = ",";
    internal const string RuntimeAssertionJitFields = ",_,_,jit,";
    internal const string RuntimeAssertionHaltSuffix = "}=V,halt(0).";
    internal const string DisassemblyPrefix = "D=beam_disasm:file(";
    internal const string DisassemblyFileBridge = "),ok=file:write_file(";
    internal const string DisassemblyTextSuffix = ",iolist_to_binary(io_lib:format(\"~tp~n\",[D]))),";
    internal const string DisassemblyShapeAssertion = "{beam_file,loader_iodata_wrapper,_,_,_,Fs}=D,{function,run,3,_,_}=lists:keyfind(run,2,Fs),halt(0).";
    internal const string CaseCallPrefix = ":run(";
    internal const string CaseArgumentSeparator = ",";
    internal const string CaseCallSuffix = ").";
    internal const string Quote = "\"";
    internal const string Backslash = "\\";
    internal const string EscapedBackslash = "\\\\";
    internal const string EscapedQuote = "\\\"";
    internal const string CarriageReturn = "\r";
    internal const string EscapedCarriageReturn = "\\r";
    internal const string LineFeed = "\n";
    internal const string EscapedLineFeed = "\\n";
}
