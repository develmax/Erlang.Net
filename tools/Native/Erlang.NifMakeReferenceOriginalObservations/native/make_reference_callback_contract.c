#include "erl_nif.h"
#ifdef _WIN32
#include <windows.h>
#else
#include <pthread.h>
#endif

enum { REFERENCE_LIMIT = 65 };
static ErlNifEnv *retained;
static ERL_NIF_TERM retained_ref;
static int load_valid;
struct work { ErlNifEnv *env; ERL_NIF_TERM refs[2]; int allocate; };
static void perform(struct work *job)
{
    if (job->allocate) job->env = enif_alloc_env();
    job->refs[0] = enif_make_ref(job->env);
    job->refs[1] = enif_make_ref(job->env);
}
#ifdef _WIN32
static DWORD WINAPI worker(LPVOID pointer) { perform((struct work *)pointer); return 0; }
static int joined(struct work *job)
{
    HANDLE thread = CreateThread(NULL,0,worker,job,0,NULL);
    DWORD status;
    if (!thread) return 0;
    status = WaitForSingleObject(thread,INFINITE);
    CloseHandle(thread);
    return status == WAIT_OBJECT_0;
}
#else
static void *worker(void *pointer) { perform((struct work *)pointer); return NULL; }
static int joined(struct work *job)
{
    pthread_t thread;
    if (pthread_create(&thread,NULL,worker,job)) return 0;
    return pthread_join(thread,NULL) == 0;
}
#endif
static int load(ErlNifEnv *env, void **data, ERL_NIF_TERM info)
{
    ERL_NIF_TERM ref = enif_make_ref(env);
    (void)data; (void)info;
    load_valid = enif_is_ref(env,ref);
    retained = enif_alloc_env();
    retained_ref = enif_make_ref(retained);
    return 0;
}
static void unload(ErlNifEnv *env, void *data)
{
    (void)env; (void)data;
    enif_free_env(retained);
    retained = NULL;
}
static ERL_NIF_TERM references(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM refs[REFERENCE_LIMIT];
    int count,i;
    if (argc != 1 || !enif_get_int(env,argv[0],&count) || count < 0 || count > REFERENCE_LIMIT)
        return enif_make_badarg(env);
    for (i=0;i<count;++i) refs[i]=enif_make_ref(env);
    return enif_make_list_from_array(env,refs,(unsigned)count);
}
static ERL_NIF_TERM independent(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM refs[3],ref;
    ErlNifEnv *other;
    int mode;
    struct work job = {0};
    if (argc != 1 || !enif_get_int(env,argv[0],&mode)) return enif_make_badarg(env);
    if (mode == 0) return enif_make_copy(env,retained_ref);
    if (mode == 1) return enif_make_int(env,load_valid);
    if (mode == 2 || mode == 3) {
        other=enif_alloc_env();
        ref=enif_make_ref(other);
        refs[0]=enif_make_copy(env,ref);
        refs[1]=enif_make_copy(env,ref);
        if (mode == 3) {
            enif_clear_env(other);
            ref=enif_make_ref(other);
        }
        refs[2]=enif_make_copy(env,ref);
        enif_free_env(other);
        return enif_make_list_from_array(env,refs,3);
    }
    if (mode == 4 || mode == 5 || mode == 6) {
        job.allocate = mode == 4;
        if (!job.allocate) job.env=enif_alloc_env();
        if (!joined(&job)) return enif_make_badarg(env);
        refs[0]=enif_make_copy(env,job.refs[0]);
        refs[1]=enif_make_copy(env,job.refs[1]);
        if (mode == 6) {
            if (!joined(&job)) { enif_free_env(job.env); return enif_make_badarg(env); }
            refs[2]=enif_make_copy(env,job.refs[0]);
        } else refs[2]=enif_make_copy(env,job.refs[0]);
        enif_free_env(job.env);
        return enif_make_list_from_array(env,refs,3);
    }
    return enif_make_badarg(env);
}
static ERL_NIF_TERM widths(ErlNifEnv *env, int argc, const ERL_NIF_TERM argv[])
{
    ERL_NIF_TERM values[4];
    (void)argc; (void)argv;
    values[0]=enif_make_int(env,(int)sizeof(int));
    values[1]=enif_make_int(env,(int)sizeof(long));
    values[2]=enif_make_int(env,(int)sizeof(size_t));
    values[3]=enif_make_int(env,(int)sizeof(ErlNifBinary));
    return enif_make_list_from_array(env,values,4);
}
static ErlNifFunc functions[] = {{"references",1,references,0},{"independent",1,independent,0},{"widths",0,widths,0}};
ERL_NIF_INIT(make_reference_callback_contract,functions,load,NULL,NULL,unload)
