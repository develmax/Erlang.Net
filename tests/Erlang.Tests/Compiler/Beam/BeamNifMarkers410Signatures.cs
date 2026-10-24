namespace Erlang.Tests;

internal static class BeamNifMarkers410Signatures
{
    internal static readonly (string Name, int Arity)[] Expected =
    [
        ("open_nif", 2),
        ("close_nif", 1),
        ("read_nif", 2),
        ("write_nif", 2),
        ("pread_nif", 3),
        ("pwrite_nif", 3),
        ("seek_nif", 3),
        ("sync_nif", 2),
        ("truncate_nif", 1),
        ("allocate_nif", 3),
        ("advise_nif", 4),
        ("read_handle_info_nif", 1),
        ("make_hard_link_nif", 2),
        ("make_soft_link_nif", 2),
        ("rename_nif", 2),
        ("read_info_nif", 2),
        ("set_permissions_nif", 2),
        ("set_owner_nif", 3),
        ("set_time_nif", 4),
        ("read_link_nif", 1),
        ("list_dir_nif", 1),
        ("make_dir_nif", 1),
        ("del_file_nif", 1),
        ("del_dir_nif", 1),
        ("get_device_cwd_nif", 1),
        ("set_cwd_nif", 1),
        ("get_cwd_nif", 0),
        ("ipread_s32bu_p32bu_nif", 3),
        ("read_file_nif", 1),
        ("get_handle_nif", 1),
        ("delayed_close_nif", 1),
        ("altname_nif", 1),
        ("file_desc_to_ref_nif", 1),
    ];
}
