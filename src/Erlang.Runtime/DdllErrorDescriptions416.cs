namespace Erlang;

internal static class DdllErrorDescriptions416
{
    internal static string? Atom(string name) => name switch
    {
        "inconsistent" => "Driver name and/or driver options are inconsistent with currently loaded driver",
        "linked_in_driver" => "Driver is statically linked and cannot be loaded/unloaded",
        "permanent" => "DDLL driver is permanent an cannot be unloaded/loaded",
        "not_loaded" => "DDLL driver is not loaded",
        "not_loaded_by_this_process" => "DDLL driver was not loaded by this process",
        "not_pending" => "DDLL load not pending for this driver name",
        "already_loaded" => "DDLL driver is already loaded successfully",
        "pending_reload" => "Driver reloading is already pending",
        "pending_process" => "Driver is loaded by others when attempting option {reload, pending_driver}",
        "no_error" => Code(0),
        "no_driver_init" => Code(-1),
        "driver_init_failed" => Code(-2),
        "bad_driver_name" => Code(-3),
        "driver_name_to_long" => Code(-4),
        "driver_incorrect_version" => Code(-5),
        "no_ddll_available" => Code(-6),
        "unspecified_error" => Code(-7),
        "symbol_not_found" => Code(-8),
        _ => null
    };

    internal static string? Code(int code) => code switch
    {
        0 => "No error",
        -1 => "No driver init in dynamic library",
        -2 => "Driver init failed",
        -3 => "Bad driver name",
        -4 => "Driver name to long",
        -5 => "Driver compiled with incorrect version of erl_driver.h",
        -6 => "DDLL functionality not available on this platform",
        -7 => "Unspecified dynamic library error",
        -8 => "Symbol not found in dynamic library",
        _ => null
    };
}
