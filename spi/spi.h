// Susie 32bit Plug-in header
#ifndef SUSIE_PLUGIN_API_HEADER_
#define SUSIE_PLUGIN_API_HEADER_

#ifndef NOMINMAX
#define NOMINMAX
#endif
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>

#if defined(_MSC_VER)
#define	SPI_API __declspec(dllexport)
#else
#error must define for other compiler
#endif

#define SPI_CALL __cdecl
#define SPI_CALLBACK __stdcall

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

#include <pshpack1.h> // align 1 byte
    typedef struct PictureInfo
    {
        long left, top;	  /// 画像を展開する位置
        long width;       /// 画像の幅(pixel)
        long height;      /// 画像の高さ(pixel)
        WORD x_density;   /// 画素の水平方向密度
        WORD y_density;	  /// 画素の垂直方向密度
        short colorDepth; /// 画素当たりのbit数
        HLOCAL hInfo;      /// 画像内のテキスト情報
    } PictureInfo;

    typedef struct FileInfo
    {
        unsigned char method[8]; // 圧縮法の種類
        unsigned long position;  // ファイル上での位置
        unsigned long compsize;  // 圧縮されたサイズ
        unsigned long filesize;  // 元のファイルサイズ
        time_t timestamp;        // ファイルの更新日時
        char path[200];          // 相対パス
        char filename[200];      // ファイルネーム
        unsigned long crc;       // CRC
    } FileInfo;
#include <poppack.h>

    /// Error Code
    enum {
        Susie_Success = 0,                // 正常終了
        Susie_NotImplemented = -1,        // その機能はインプリメントされていない
        Susie_FailedToProcess = 1,        // コールバック関数が非0を返したので展開を中止した
        Susie_UnknownFormat = 2,          // 未知のフォーマット
        Susie_CorruptedData = 3,          // データが壊れている
        Susie_FailedToAllocateMemory = 4, // メモリーが確保できない
        Susie_MemoryError = 5,            // メモリーエラー(Lock出来ない、等)
        Susie_FailedToReadFile = 6,       // ファイルリードエラー
        Susie_Reserved = 7,               // 予約
        Susie_InternalError = 8,          // 内部エラー
    };

    /// ConfigurationDlg
    enum {
        Susie_ConfigurationDlg_About = 0,    /// Plug-inのaboutダイアログ表示(必要であれば)
        Susie_ConfigurationDlg_Settings = 1, /// 設定ダイアログ表示
        Susie_ConfigurationDlg_Reserved = 2, /// 予約
    };

    // Common functions
    SPI_API int SPI_CALL GetPluginInfo(int infono, LPSTR buf, int buflen);
    SPI_API int SPI_CALL IsSupported(LPSTR filename, DWORD dw);
    SPI_API int SPI_CALL ConfigurationDlg(HWND parent, int fnc);


    // 00IN fcuntions
    SPI_API int SPI_CALL GetPictureInfo(LPSTR buf, long len, unsigned int flag, PictureInfo* lpInfo);
    SPI_API int SPI_CALL GetPicture(LPSTR buf, long len, unsigned int flag, HANDLE* pHBInfo, HANDLE* pHBm,
        FARPROC lpPrgressCallback, long lData);
    SPI_API int SPI_CALL GetPreview(LPSTR buf, long len, unsigned int flag, HANDLE* pHBInfo, HANDLE* pHBm,
        FARPROC lpPrgressCallback, long lData);

    // 00AM functions
    SPI_API int SPI_CALL GetArchiveInfo(LPSTR buf, long len, unsigned int flag, HLOCAL* lphInf);
    SPI_API int SPI_CALL GetFileInfo(LPSTR buf, long len, LPSTR filename, unsigned int flag, FileInfo* lpInfo);
    SPI_API int SPI_CALL GetFile(LPSTR src, long len, LPSTR dest, unsigned int flag, FARPROC prgressCallback, long lData);

    typedef int (SPI_CALLBACK* PrgressCallback)(int nNum, int nDenom, long lData);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // SUSIE_PLUGIN_API_HEADER_
