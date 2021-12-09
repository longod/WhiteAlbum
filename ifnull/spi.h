#pragma once

#ifdef WIN32
#ifndef NOMINMAX
#define NOMINMAX
#endif
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>
#endif

#if defined(_MSC_VER)
#define	SPI_API __declspec(dllexport)
#else
#error must define for other compiler
#endif

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

#include <pshpack1.h>
    typedef struct PictureInfo
    {
        long left, top;			/* 画像を展開する位置 */
        long width;				/* 画像の幅(pixel) */
        long height;			/* 画像の高さ(pixel) */
        WORD x_density;			/* 画素の水平方向密度 */
        WORD y_density;			/* 画素の垂直方向密度 */
        short colorDepth;		/* 画素当たりのbit数 */
        HLOCAL hInfo;			/* 画像内のテキスト情報[呼び出し側が解放] */
    } PictureInfo;

    typedef struct fileInfo
    {
        unsigned char method[8];	/* 圧縮法の種類 */
        unsigned long position;		/* ファイル上での位置 */
        unsigned long compsize;		/* 圧縮されたサイズ */
        unsigned long filesize;		/* 元のファイルサイズ */
        time_t timestamp;			/* ファイルの更新日時 */
        char path[200];				/* 相対パス */
        char filename[200];			/* ファイルネーム */
        unsigned long crc;			/* CRC */
    } fileInfo;
#include <poppack.h>

    // for ConfigurationDlg
    enum {
        SUSIE_CONFIGDLG_ABOUT = 0,
        SUSIE_CONFIGDLG_SETTING,
        SUSIE_CONFIGDLG_RESERVED
    };

    /* Common Function */
    SPI_API int GetPluginInfo(int infono, LPSTR buf, int buflen);
    SPI_API int IsSupported(LPSTR filename, DWORD dw);
    SPI_API int ConfigurationDlg(HWND parent, int fnc);


    /* '00IN'の関数 */
    SPI_API int GetPictureInfo(LPSTR buf, long len, unsigned int flag, PictureInfo* lpInfo);
    SPI_API int GetPicture(LPSTR buf, long len, unsigned int flag, HANDLE* pHBInfo, HANDLE* pHBm,
        FARPROC lpPrgressCallback, long lData);
    SPI_API int GetPreview(LPSTR buf, long len, unsigned int flag, HANDLE* pHBInfo, HANDLE* pHBm,
        FARPROC lpPrgressCallback, long lData);

    /* '00AM'の関数 */
    SPI_API int GetArchiveInfo(LPSTR buf, long len, unsigned int flag, HLOCAL* lphInf);
    SPI_API int GetFileInfo(LPSTR buf, long len, LPSTR filename, unsigned int flag, fileInfo* lpInfo);
    SPI_API int GetFile(LPSTR src, long len, LPSTR dest, unsigned int flag, FARPROC prgressCallback, long lData);

    typedef int (PASCAL* PrgressCallback)(int nNum, int nDenom, long lData);

#ifdef __cplusplus
}
#endif // __cplusplus

