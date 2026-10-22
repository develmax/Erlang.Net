namespace Erlang;

internal static class PrimTtyNames
{
    internal const string Module = "prim_tty", Stdout = "stdout", Stderr = "stderr", Stdin = "stdin";
    internal const string Ok = "ok", Error = "error", NoSupport = "enotsup", Closed = "closed", BadDescriptor = "ebadf";
    internal const string True = "true", False = "false", Input = "input", Raw = "raw", Utf8 = "utf8", Utf16 = "utf16", Little = "little";
    internal const string Select = "select", ReadyInput = "ready_input";
    internal const int ReadMaximum = 1024, WcharBytes = 2, RetryMilliseconds = 5, PollMilliseconds = 10;
    internal const int IoNotFound = 1168;
    internal const uint ThreadTerminate = 1, MessageFlags = 0x1300;
    internal const string ReadFile = "ReadFile", WriteFile = "WriteFile", ReadConsole = "ReadConsoleW";
    internal const string UnsupportedSelector = "The redirected TTY profile currently requires stdout or stderr; original unknown-selector NUL behavior remains held";
    internal const string Foreign = "TTY resource belongs to a different runtime or NIF owner";
    internal const string FunctionIsatty = "isatty";
    internal const string FunctionTtyCreate = "tty_create";
    internal const string FunctionTtyInit = "tty_init";
    internal const string FunctionSetlocale = "setlocale";
    internal const string FunctionTtyIsOpen = "tty_is_open";
    internal const string FunctionTtySelect = "tty_select";
    internal const string FunctionTtyWindowSize = "tty_window_size";
    internal const string FunctionWriteNif = "write_nif";
    internal const string FunctionTtyEncoding = "tty_encoding";
    internal const string FunctionReadNif = "read_nif";
    internal const string FunctionIsprint = "isprint";
    internal const string FunctionWcwidth = "wcwidth";
    internal const string FunctionWcswidth = "wcswidth";
    internal const string FunctionSizeofWchar = "sizeof_wchar";
    internal const string FunctionSetuptermNif = "setupterm_nif";
    internal const string FunctionTigetnumNif = "tigetnum_nif";
    internal const string FunctionTigetflagNif = "tigetflag_nif";
    internal const string FunctionTinfoNif = "tinfo_nif";
    internal const string FunctionTigetstrNif = "tigetstr_nif";
    internal const string FunctionTputsNif = "tputs_nif";
    internal static readonly (string Name, int Arity)[] Signatures =
    [
        (FunctionIsatty,1),(FunctionTtyCreate,1),(FunctionTtyInit,2),(FunctionSetlocale,1),(FunctionTtyIsOpen,2),
        (FunctionTtySelect,2),(FunctionTtyWindowSize,1),(FunctionWriteNif,2),(FunctionTtyEncoding,1),(FunctionReadNif,3),
        (FunctionIsprint,1),(FunctionWcwidth,1),(FunctionWcswidth,1),(FunctionSizeofWchar,0),(FunctionSetuptermNif,0),
        (FunctionTigetnumNif,1),(FunctionTigetflagNif,1),(FunctionTinfoNif,0),(FunctionTigetstrNif,1),(FunctionTputsNif,2)
    ];
}
