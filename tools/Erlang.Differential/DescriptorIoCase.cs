namespace Erlang.Differential;

public sealed record DescriptorIoCase(
    string Name,
    string Body,
    string InputHex,
    string OutputHex,
    string Settings
);
