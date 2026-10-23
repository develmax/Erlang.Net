/* The unchanged OTP inet implementation is builtin in ERTS. This dynamic
   container owns its linked TCP/UDP entries for our separate native host. */
#include "inet_drv.c"
#include "packet_parser.c"

static int inet_entry_init455(void)
{
    packet_parser_init();
    return inet_init();
}

static void inet_entry_finish455(void)
{
    /* Ports already retain their actual image leases until native Stop. The
       container must remove its own registrations before its image can exit. */
#ifdef HAVE_UDP
    remove_driver_entry(&udp_inet_driver_entry);
#endif
    remove_driver_entry(&tcp_inet_driver_entry);
}

DRIVER_INIT(inet)
{
    inet_driver_entry.init = inet_entry_init455;
    inet_driver_entry.finish = inet_entry_finish455;
    return &inet_driver_entry;
}
