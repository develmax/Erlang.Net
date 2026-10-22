#include <string.h>
#include <windows.h>
#include "ddll_visibility438_names.h"
#include "erl_driver.h"
#ifndef DRIVER_NAME_TEXT
#error DRIVER_NAME_TEXT must name this actual fixture
#endif
#ifndef ROOT_FAIL
#define ROOT_FAIL 0
#endif
#ifndef CHILD_FAIL
#define CHILD_FAIL 0
#endif
static ErlDrvEntry root_entry, child_a, child_b;
static volatile LONG gate_failure438;
static int read438(const char *key,char *value) {size_t size=VIS438_NAME_BYTES;return erl_drv_getenv(key,value,&size);}
static int signal438(const char *key)
{
 char name[VIS438_NAME_BYTES];HANDLE event;
 if(read438(key,name)!=0)return 0;
 event=OpenEventA(EVENT_MODIFY_STATE,FALSE,name);if(!event)return -1;
 if(!SetEvent(event)){CloseHandle(event);return -1;}CloseHandle(event);return 0;
}
static int hold438(void)
{
 char entered_name[VIS438_NAME_BYTES],release_name[VIS438_NAME_BYTES];HANDLE entered,release;DWORD result;
 int first=read438(VIS438_ENTER_KEY,entered_name),second=read438(VIS438_RELEASE_KEY,release_name);
 if(first!=0&&second!=0)return 0;
 if(first!=0||second!=0)return -1;
 entered=OpenEventA(EVENT_MODIFY_STATE,FALSE,entered_name);
 release=OpenEventA(SYNCHRONIZE,FALSE,release_name);
 if(!entered||!release){if(entered)CloseHandle(entered);if(release)CloseHandle(release);return -1;}
 if(!SetEvent(entered)){CloseHandle(entered);CloseHandle(release);return -1;}
 result=WaitForSingleObject(release,VIS438_WAIT_MILLISECONDS);CloseHandle(entered);CloseHandle(release);
 return result==WAIT_OBJECT_0?signal438(VIS438_COMPLETED_KEY):-1;
}
static int cross_remove438(void)
{
 char module[VIS438_NAME_BYTES];HMODULE image;FARPROC exported;
 typedef ErlDrvEntry *(__cdecl *get_entry438)(unsigned int);
 if(read438(VIS438_REMOVE_KEY,module)!=0)return 0;
 image=GetModuleHandleA(module);if(!image)return -1;
 exported=GetProcAddress(image,VIS438_ENTRY_EXPORT);if(!exported)return -1;
 return remove_driver_entry(((get_entry438)exported)(VIS438_CHILD_B))==1?0:-1;
}
static int child_init(void)
{
 int failed;add_driver_entry(&child_b);failed=cross_remove438();
 if(hold438()!=0)failed=-1;if(failed!=0)InterlockedExchange(&gate_failure438,1);
 return failed||CHILD_FAIL?-1:0;
}
static int root_init(void)
{
 char phase[VIS438_NAME_BYTES];
 if(read438(VIS438_PHASE_KEY,phase)==0&&strcmp(phase,VIS438_ROOT_PHASE)==0&&hold438()!=0)
 {InterlockedExchange(&gate_failure438,1);return -1;}
 return ROOT_FAIL?-1:0;
}
/* This runs as a genuine native start callback, outside the DDLL list lock.
   First add acquires it and installs TSD; child_a.init can then add child_b. */
static void add_children438(void)
{if(signal438(VIS438_ATTEMPT_KEY)!=0)InterlockedExchange(&gate_failure438,1);add_driver_entry(&child_a);}
static ErlDrvData root_start(ErlDrvPort port,char *command)
{
 char phase[VIS438_NAME_BYTES];(void)command;
 /* Explicit fixture-only mode: both real ports must exist before either
    initializer holds the list. Normal serial/start roles remain unchanged. */
 if(read438(VIS438_PHASE_KEY,phase)!=0||strcmp(phase,VIS438_CONTROL_PHASE)!=0)add_children438();
 return (ErlDrvData)port;
}
/* stop runs before DDLL unload takes its driver-list lock. Never call the
   driver-list API from finish: original do_unload_driver_entry holds that lock. */
static void root_stop(ErlDrvData data)
{ (void)data;(void)remove_driver_entry(&child_b);(void)remove_driver_entry(&child_a); }
static void root_finish(void) { /* No driver-list calls under the unload lock. */ }
__declspec(dllexport) ErlDrvEntry *ddll_visibility_entry438(unsigned int entry)
{return entry==VIS438_CHILD_B?&child_b:&child_a;}
static ErlDrvSSizeT root_control438(ErlDrvData data,unsigned int command,char *buf,ErlDrvSizeT len,char **result,ErlDrvSizeT space)
{
 (void)data;(void)buf;(void)len;
 if(command==VIS438_ADD_COMMAND)add_children438();
 else if(command!=VIS438_STATUS_COMMAND)return -1;
 if(space<1){*result=driver_alloc(1);if(!*result)return -1;}
 (*result)[0]=(char)InterlockedCompareExchange(&gate_failure438,0,0);return 1;
}
DRIVER_INIT(ddll_init_prefix435)
{
 memset(&root_entry,0,sizeof(root_entry));memset(&child_a,0,sizeof(child_a));memset(&child_b,0,sizeof(child_b));
 InterlockedExchange(&gate_failure438,0);
 root_entry.control=root_control438;
 root_entry.driver_name=DRIVER_NAME_TEXT;root_entry.init=root_init;root_entry.start=root_start;root_entry.stop=root_stop;root_entry.finish=root_finish;
 root_entry.extended_marker=ERL_DRV_EXTENDED_MARKER;root_entry.major_version=ERL_DRV_EXTENDED_MAJOR_VERSION;root_entry.minor_version=ERL_DRV_EXTENDED_MINOR_VERSION;
 child_a.driver_name=DRIVER_NAME_TEXT "_a";child_a.init=child_init;
 child_a.extended_marker=ERL_DRV_EXTENDED_MARKER;child_a.major_version=ERL_DRV_EXTENDED_MAJOR_VERSION;child_a.minor_version=ERL_DRV_EXTENDED_MINOR_VERSION;
 child_b.driver_name=DRIVER_NAME_TEXT "_b";
 child_b.extended_marker=ERL_DRV_EXTENDED_MARKER;child_b.major_version=ERL_DRV_EXTENDED_MAJOR_VERSION;child_b.minor_version=ERL_DRV_EXTENDED_MINOR_VERSION;
 return &root_entry;
}
