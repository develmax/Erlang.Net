#include <string.h>
#include "erl_driver.h"
#define PERM445_NAME "permanent_drv445"
#define PERM445_LOCK_COMMAND 445
static ErlDrvEntry entry445;
static ErlDrvData start445(ErlDrvPort port,char *command){(void)command;return (ErlDrvData)port;}
static ErlDrvSSizeT control445(ErlDrvData data,unsigned int command,char *buf,ErlDrvSizeT len,char **result,ErlDrvSizeT space)
{
 int status;(void)buf;(void)len;
 if(command!=PERM445_LOCK_COMMAND)return -1;
 /* Genuine source callback on its actual opened port, outside dynamic init. */
 status=driver_lock_driver((ErlDrvPort)data);
 if(space<1){*result=driver_alloc(1);if(!*result)return -1;}
 (*result)[0]=(char)status;return 1;
}
DRIVER_INIT(permanent_drv445)
{
 memset(&entry445,0,sizeof(entry445));entry445.driver_name=PERM445_NAME;
 entry445.start=start445;entry445.control=control445;
 entry445.extended_marker=ERL_DRV_EXTENDED_MARKER;
 entry445.major_version=ERL_DRV_EXTENDED_MAJOR_VERSION;entry445.minor_version=ERL_DRV_EXTENDED_MINOR_VERSION;
 return &entry445;
}
