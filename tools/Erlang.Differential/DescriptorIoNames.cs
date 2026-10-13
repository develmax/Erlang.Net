namespace Erlang.Differential;

public static class DescriptorIoNames
{
    public const string Command = "--fd";
    public const string ModulePrefix = "-module(fd_io_contract). -export([run/0]). run()->";
    public const string ModuleSuffix = ",done.";
    public const string InputToken = "$INPUT$";
    public const string OutputToken = "$OUTPUT$";
    public const string TemporaryPrefix = "erlang-net-fd-";
    public const string OutputFile = "output.bin";
    public const string InputFile = "input.bin";
    public const string Shell = "/bin/bash";
    public const string ShellOption = "-c";
    public const string ShellScript = "exec 3< <(cat -- \"$2\"); exec 4>\"$3\"; exec \"$1\" -noshell -noinput -eval \"$4\"";
    public const string ShellLabel = "fd-oracle";
    public const string Failure = "Descriptor IO independent contract failed";
    public const int ReferenceInput = 3;
    public const int ReferenceOutput = 4;
    public const int TimeoutSeconds = 15;
    public const string CancellationTest = "runtime/port-fd-io/cancel-idle-borrowed-reader";
    public const string CancellationBody = "P=open_port({fd,$INPUT$,$OUTPUT$},[binary,eof]),receive after 50->ok end,true=erlang:port_close(P)";
}
