#include "init_progress582.h"
#ifndef GENERATION582
#define GENERATION582 1
#endif
static int constructor_status582;
#if GENERATION582 == 2
__attribute__((constructor)) static void constructor582(void)
{
    constructor_status582=NativeFailure582;
    if (!event582("constructor_enter\n") || !touch582(ReadyEnvironment582)) return;
    long long start=milliseconds582();
    if (start<0) return;
    for (;;)
    {
        if (exists582(AckEnvironment582))
        {
            if (event582("constructor_ack_seen\n")) constructor_status582=0;
            return;
        }
        long long now=milliseconds582();
        if (now<0 || now-start>=WaitMilliseconds582) break;
        struct timespec delay={0,PollMilliseconds582*NanosecondsPerMillisecond582};
        (void)nanosleep(&delay,NULL);
    }
    (void)event582("constructor_timeout\n");
}
#endif
static int load582(ErlNifEnv *env,void **data,ERL_NIF_TERM info)
{
    (void)env; (void)info;
    int *value=enif_alloc(sizeof(*value));
    if (!value) return NativeFailure582;
    *value=GENERATION582; *data=value;
    if (!event582("load_accepted\n")) {enif_free(value);*data=NULL;return NativeFailure582;}
    return 0;
}
static int upgrade582(ErlNifEnv *env,void **next,void **old,ERL_NIF_TERM info)
{
    if (!event582("upgrade_enter\n")) return NativeFailure582;
    if (constructor_status582 || !old || !*old) return NativeFailure582;
    return load582(env,next,info);
}
static void unload582(ErlNifEnv *env,void *data)
{
    (void)env; enif_free(data);
    (void)event582(GENERATION582==1 ? "unload_v1\n" : "unload_v2\n");
}
static ERL_NIF_TERM status582(ErlNifEnv *env,int argc,const ERL_NIF_TERM argv[])
{
    (void)argc; (void)argv;
    return enif_make_tuple2(env,enif_make_int(env,GENERATION582),enif_make_int(env,constructor_status582));
}
static ErlNifFunc functions582[]={{"status",0,status582,0}};
ERL_NIF_INIT(init_subject582,functions582,load582,NULL,upgrade582,unload582)
