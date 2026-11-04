/* POSIX host boundary for Erlang.Net; OTP-29.1.1 efile contracts (Apache-2.0).
 * No ERTS dependency or OS-specific stat/dirent layout crosses the ABI. */
#define _FILE_OFFSET_BITS 64
#define _POSIX_C_SOURCE 200809L
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <fcntl.h>
#include <dirent.h>
#include <errno.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include "filesystem.h"

int enfs_abi(void) { return (int)sizeof(enfs_info); }
int enfs_open(const char *path, int mode, int *fd) {
    int flags = (mode & ENFS_READ) ? ((mode & ENFS_WRITE) ? O_RDWR : O_RDONLY) : O_WRONLY;
    if (mode & ENFS_WRITE) flags |= O_CREAT;
    if ((mode & ENFS_WRITE) && !(mode & ENFS_READ) && !(mode & ENFS_APPEND)) flags |= O_TRUNC;
    if (mode & ENFS_APPEND) flags |= O_APPEND | O_CREAT;
    if (mode & ENFS_EXCLUSIVE) flags |= O_EXCL | O_CREAT;
    if (mode & ENFS_SYNC) flags |= O_SYNC;
#ifdef O_CLOEXEC
    flags |= O_CLOEXEC;
#endif
    int value = open(path, flags, 0666);
    if (value < 0) return errno;
    struct stat st;
    if (fstat(value, &st) < 0) { int e = errno; close(value); return e; }
    if (S_ISDIR(st.st_mode)) { close(value); return EISDIR; }
    *fd = value;
    return 0;
}
int enfs_read(int fd, uint8_t *buffer, int count, int positioned, int64_t offset, int *actual) {
    ssize_t n;
    do { n = positioned ? pread(fd, buffer, (size_t)count, (off_t)offset) : read(fd, buffer, (size_t)count); } while (n < 0 && errno == EINTR);
    if (n < 0) return errno;
    *actual = (int)n; return 0;
}
int enfs_write(int fd, const uint8_t *buffer, int count, int positioned, int64_t offset) {
    int done = 0;
    while (done < count) {
        ssize_t n = positioned ? pwrite(fd, buffer + done, (size_t)(count-done), (off_t)(offset+done)) : write(fd, buffer + done, (size_t)(count-done));
        if (n < 0) { if (errno == EINTR) continue; return errno; }
        if (n == 0) return EIO;
        done += (int)n;
    }
    return 0;
}
int enfs_seek(int fd, int64_t offset, int origin, int64_t *position) {
    off_t n = lseek(fd, (off_t)offset, origin);
    if (n == (off_t)-1) return errno;
    *position = (int64_t)n; return 0;
}
int enfs_truncate(int fd) {
    off_t position = lseek(fd, 0, SEEK_CUR);
    if (position == (off_t)-1) return errno;
    return ftruncate(fd, position) < 0 ? errno : 0;
}
int enfs_sync(int fd, int data_only) {
#ifdef __APPLE__
    (void)data_only; return fsync(fd) < 0 ? errno : 0;
#else
    return (data_only ? fdatasync(fd) : fsync(fd)) < 0 ? errno : 0;
#endif
}
int enfs_path(int operation, const char *path, const char *target) {
    int result;
    if (operation == ENFS_RENAME) result = rename(path, target);
    else if (operation == ENFS_MKDIR) result = mkdir(path, 0777);
    else result = rmdir(path);
    if (result == 0) return 0;
    int error = errno;
    if (error == ENOTEMPTY) error = EEXIST;
    if (operation == ENFS_RMDIR && error == EEXIST) {
        struct stat a, b;
        if (stat(path,&a)==0 && stat(".",&b)==0 && a.st_ino==b.st_ino && a.st_dev==b.st_dev) error = EINVAL;
    }
    return error;
}
static int handle_access(const struct stat *st) {
    mode_t bits;
    if (st->st_uid == geteuid()) bits = st->st_mode >> 6;
    else {
        int member = st->st_gid == getegid();
        int count = getgroups(0,NULL);
        gid_t *groups = count > 0 ? malloc((size_t)count*sizeof(gid_t)) : NULL;
        if (groups) { count=getgroups(count,groups); for (int i=0;i<count;i++) if(groups[i]==st->st_gid) member=1; free(groups); }
        bits = member ? st->st_mode >> 3 : st->st_mode;
    }
    return ((bits & 4) ? 1 : 0) | ((bits & 2) ? 2 : 0);
}
int enfs_stat(const char *path, int fd, int follow, enfs_info *info) {
    struct stat st;
    int result = fd >= 0 ? fstat(fd,&st) : (follow ? stat(path,&st) : lstat(path,&st));
    if (result < 0) return errno;
    info->type = S_ISDIR(st.st_mode) ? ENFS_DIRECTORY : S_ISREG(st.st_mode) ? ENFS_REGULAR : S_ISLNK(st.st_mode) ? ENFS_SYMLINK : (S_ISCHR(st.st_mode)||S_ISBLK(st.st_mode)) ? ENFS_DEVICE : ENFS_OTHER;
    info->access = fd >= 0 ? handle_access(&st) : ((access(path,R_OK)==0 ? 1 : 0) | (access(path,W_OK)==0 ? 2 : 0));
    info->size=st.st_size; info->atime=st.st_atime; info->mtime=st.st_mtime; info->ctime=st.st_ctime;
    info->mode=st.st_mode; info->links=st.st_nlink; info->dev=st.st_dev; info->rdev=st.st_rdev;
    info->inode=st.st_ino; info->uid=st.st_uid; info->gid=st.st_gid;
    return 0;
}
int enfs_cwd(uint8_t *buffer, int capacity, int *length) {
    if (!getcwd((char*)buffer,(size_t)capacity)) return errno;
    *length=(int)strlen((char*)buffer); return 0;
}
typedef struct { DIR *dir; struct dirent *pending; } enfs_dir;
int enfs_opendir(const char *path, void **result) {
    DIR *dir=opendir(path); if (!dir) return errno;
    enfs_dir *state=calloc(1,sizeof(enfs_dir)); if(!state) { closedir(dir); return ENOMEM; }
    state->dir=dir; *result=state; return 0;
}
int enfs_readdir(void *handle, uint8_t *buffer, int capacity, int *length) {
    enfs_dir *state=handle;
    if(!state->pending) { errno=0; state->pending=readdir(state->dir); if(!state->pending) { *length=-1; return errno; } }
    size_t size=strlen(state->pending->d_name);
    if(size>(size_t)capacity) return ERANGE;
    memcpy(buffer,state->pending->d_name,size); *length=(int)size; state->pending=NULL; return 0;
}
void enfs_closedir(void *handle) { enfs_dir *state=handle; closedir(state->dir); free(state); }
