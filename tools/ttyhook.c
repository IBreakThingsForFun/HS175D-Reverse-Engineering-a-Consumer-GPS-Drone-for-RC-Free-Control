/*
 * ttyhook.c — LD_PRELOAD shim for the HS175D / Syma X30 Pro flight bridge (img_trans_app).
 *
 * Hooks open()/read()/write() so that every byte img_trans_app exchanges with the flight
 * controller on /dev/ttyS1 is logged to /tmp/ttyhook.log WITH DIRECTION — including the
 * SoC->MCU (W) direction a passive `cat /dev/ttyS1` can never see. Lossless: we sit inside
 * the process's own syscalls, so there's no second reader competing for bytes.
 *
 *   W = bytes img_trans_app SENDS to the MCU (sticks / optical-flow / commands)  <-- the new view
 *   R = bytes img_trans_app RECEIVES from the MCU (telemetry: 0x58 8a/8b/8c frames)
 *
 * --- build (on a PC with an armv7 hard-float cross toolchain; e.g. WSL: apt install gcc-arm-linux-gnueabihf) ---
 *   arm-linux-gnueabihf-gcc -shared -fPIC -O2 -o ttyhook.so ttyhook.c -ldl -lpthread
 *
 * --- deploy (adb root shell) ---
 *   adb -s 192.168.100.1:5555 push ttyhook.so /tmp/ttyhook.so
 *   adb -s 192.168.100.1:5555 shell
 *     kill $(pidof img_trans_app)
 *     cd /img_trans && LD_PRELOAD=/tmp/ttyhook.so ./bin/img_trans_app &
 *     tail -f /tmp/ttyhook.log         # watch both directions, perfectly framed
 *
 * Restarting img_trans_app briefly drops video/WiFi-control, then it comes back. /tmp is RAM
 * (gone on reboot) — fine for capture. Nothing here changes flight behaviour; it only observes.
 */
#define _GNU_SOURCE
#include <dlfcn.h>
#include <stdio.h>
#include <string.h>
#include <stdarg.h>
#include <fcntl.h>
#include <unistd.h>
#include <time.h>
#include <pthread.h>

static ssize_t (*real_read)(int, void *, size_t);
static ssize_t (*real_write)(int, const void *, size_t);
static int     (*real_open)(const char *, int, ...);

static int  fc_fd = -1;                 /* the /dev/ttyS1 descriptor, learned at open() */
static FILE *lg;
static pthread_mutex_t mx = PTHREAD_MUTEX_INITIALIZER;

static void init(void)
{
    if (!real_read)  real_read  = dlsym(RTLD_NEXT, "read");
    if (!real_write) real_write = dlsym(RTLD_NEXT, "write");
    if (!real_open)  real_open  = dlsym(RTLD_NEXT, "open");
    if (!lg)         lg         = fopen("/tmp/ttyhook.log", "a");
}

static void dump(const char *dir, const unsigned char *b, ssize_t n)
{
    if (n <= 0 || !lg) return;
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    pthread_mutex_lock(&mx);
    fprintf(lg, "%ld.%03ld %s %zd:", (long)ts.tv_sec, ts.tv_nsec / 1000000, dir, n);
    for (ssize_t i = 0; i < n; i++) fprintf(lg, " %02x", b[i]);
    fputc('\n', lg);
    fflush(lg);
    pthread_mutex_unlock(&mx);
}

int open(const char *path, int flags, ...)
{
    init();
    va_list ap; va_start(ap, flags);
    mode_t m = va_arg(ap, int); va_end(ap);
    int fd = real_open(path, flags, m);
    if (fd >= 0 && path && strstr(path, "ttyS1")) {
        fc_fd = fd;
        if (lg) { fprintf(lg, "# open %s fd=%d\n", path, fd); fflush(lg); }
    }
    return fd;
}

ssize_t read(int fd, void *buf, size_t cnt)
{
    init();
    ssize_t n = real_read(fd, buf, cnt);
    if (fd == fc_fd) dump("R", buf, n);     /* MCU -> SoC (telemetry) */
    return n;
}

ssize_t write(int fd, const void *buf, size_t cnt)
{
    init();
    if (fd == fc_fd) dump("W", buf, cnt);   /* SoC -> MCU (commands) — the unseen direction */
    return real_write(fd, buf, cnt);
}
