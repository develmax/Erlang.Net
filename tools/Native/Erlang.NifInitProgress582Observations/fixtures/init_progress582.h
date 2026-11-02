#ifndef INIT_PROGRESS582_H
#define INIT_PROGRESS582_H
#define _POSIX_C_SOURCE 200809L
#include "erl_nif.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <fcntl.h>
#include <unistd.h>
#include <errno.h>
enum { PathCapacity582=4096, WaitMilliseconds582=2000,
    PollMilliseconds582=5, NanosecondsPerMillisecond582=1000000,
    MillisecondsPerSecond582=1000, NativeFailure582=71 };
#define LogEnvironment582 "ERLANG_INIT582_LOG"
#define AckEnvironment582 "ERLANG_INIT582_ACK"
#define ReadyEnvironment582 "ERLANG_INIT582_READY"
static int event582(const char *text)
{
    const char *path=getenv(LogEnvironment582);
    if (!path || !*path) return 0;
    int fd=open(path,O_WRONLY|O_CREAT|O_APPEND,0600);
    if (fd<0) return 0;
    size_t n=strlen(text); ssize_t result=write(fd,text,n);
    int closed=close(fd);
    return result==(ssize_t)n && closed==0;
}
static int touch582(const char *key)
{
    const char *path=getenv(key);
    if (!path || !*path) return 0;
    int fd=open(path,O_WRONLY|O_CREAT|O_EXCL,0600);
    if (fd<0) return 0;
    return close(fd)==0;
}
static int exists582(const char *key)
{
    const char *path=getenv(key);
    return path && *path && access(path,F_OK)==0;
}
static long long milliseconds582(void)
{
    struct timespec now;
    if (clock_gettime(CLOCK_MONOTONIC,&now)!=0) return -1;
    return (long long)now.tv_sec*MillisecondsPerSecond582
        +now.tv_nsec/NanosecondsPerMillisecond582;
}
#endif
