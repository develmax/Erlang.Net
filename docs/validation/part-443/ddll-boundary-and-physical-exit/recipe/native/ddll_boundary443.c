#include <string.h>
#include "erl_driver.h"
/* Adapted from the actual435/438 start/control fixture. All list operations
   execute in native start/control/stop, never dynamic init or finish. */
#define B443_NAME "ddll_boundary443"
#define B443_COMMAND_BASE 4430
#define B443_COMMAND_COUNT 8
#define B443_RESULT_BYTES 3
static ErlDrvEntry root_entry, outer_a, nested_b, prior_c, prior_d;
static unsigned int mode443;
static int removed_c443, removed_d443;
static int outer_init443(void)
{
    unsigned int removal = mode443 & 3;
    add_driver_entry(&nested_b);
    if (removal == 1 || removal == 3) removed_c443 = remove_driver_entry(&prior_c);
    if (removal == 2 || removal == 3) removed_d443 = remove_driver_entry(&prior_d);
    return mode443 >= 4 ? -1 : 0;
}
static ErlDrvData start443(ErlDrvPort port, char *command)
{
    (void)command;
    /* The outer's immediate old boundary is C, followed by D and the real
       loaded dynamic root. No fake descriptor or prior-entry address. */
    add_driver_entry(&prior_d);
    add_driver_entry(&prior_c);
    return (ErlDrvData)port;
}
static void stop443(ErlDrvData data)
{
    (void)data;
    (void)remove_driver_entry(&nested_b);
    (void)remove_driver_entry(&outer_a);
    (void)remove_driver_entry(&prior_c);
    (void)remove_driver_entry(&prior_d);
}
static ErlDrvSSizeT control443(ErlDrvData data, unsigned int command,
    char *buffer, ErlDrvSizeT length, char **result, ErlDrvSizeT space)
{
    (void)data; (void)buffer; (void)length;
    if (command < B443_COMMAND_BASE || command >= B443_COMMAND_BASE + B443_COMMAND_COUNT) return -1;
    mode443 = command - B443_COMMAND_BASE;
    removed_c443 = 0; removed_d443 = 0;
    add_driver_entry(&outer_a);
    if (space < B443_RESULT_BYTES) {
        *result = driver_alloc(B443_RESULT_BYTES);
        if (!*result) return -1;
    }
    (*result)[0] = (char)mode443;
    (*result)[1] = (char)removed_c443;
    (*result)[2] = (char)removed_d443;
    return B443_RESULT_BYTES;
}
static void configure443(ErlDrvEntry *entry, char *name)
{
    memset(entry, 0, sizeof(*entry));
    entry->driver_name = name;
    entry->extended_marker = ERL_DRV_EXTENDED_MARKER;
    entry->major_version = ERL_DRV_EXTENDED_MAJOR_VERSION;
    entry->minor_version = ERL_DRV_EXTENDED_MINOR_VERSION;
}
DRIVER_INIT(ddll_boundary443)
{
    configure443(&root_entry, B443_NAME);
    configure443(&outer_a, B443_NAME "_a");
    configure443(&nested_b, B443_NAME "_b");
    configure443(&prior_c, B443_NAME "_c");
    configure443(&prior_d, B443_NAME "_d");
    root_entry.start = start443; root_entry.stop = stop443; root_entry.control = control443;
    outer_a.init = outer_init443;
    return &root_entry;
}
