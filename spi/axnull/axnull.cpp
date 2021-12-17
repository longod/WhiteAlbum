// Susie 32bit Plug-in
// stab of import filter
#include "../spi.h"

#ifdef __cplusplus
extern "C" {
#endif // __cplusplus

    int SPI_CALL GetPluginInfo(int infono, LPSTR buf, int buflen) {
        switch (infono)
        {
        case 0: // plugin version
            if (buflen >= 4) {
                buf[0] = '3';
                buf[1] = '4';
                buf[2] = 'A';
                buf[3] = 'M';
                return 4;
            }
        case 1: // plugin name
        {
            const char pluginName[] = "axnull";
            const int count = ARRAYSIZE(pluginName); // not byte-size but equals
            if (buflen >= count) {
                memcpy(buf, pluginName, count);
                return count;
            }
        }
        case 2: // plugin extension
        {
            const char ext[] = "*.null";
            const int count = ARRAYSIZE(ext); // not byte-size but equals
            if (buflen >= count) {
                memcpy(buf, ext, count);
                return count;
            }
        }
        case 3: // plugin format
        {
            const char format[] = "null";
            const int count = ARRAYSIZE(format); // not byte-size but equals
            if (buflen >= count) {
                memcpy(buf, format, count);
                return count;
            }
        }
        default:
            break;
        }
        return 0;
    }

    int SPI_CALL IsSupported(LPSTR filename, DWORD dw) {
        return 1;
    }

    int SPI_CALL ConfigurationDlg(HWND parent, int fnc) {
        return 0;
    }

    int SPI_CALL GetPictureInfo(LPSTR buf, long len, unsigned int flag, PictureInfo* lpInfo) {
        return 0;
    }

    int SPI_CALL GetPicture(LPSTR buf, long len, unsigned int flag, HANDLE* pHBInfo, HANDLE* pHBm,
        FARPROC lpPrgressCallback, long lData) {
        return 0;
    }

    int SPI_CALL GetPreview(LPSTR buf, long len, unsigned int flag, HANDLE* pHBInfo, HANDLE* pHBm,
        FARPROC lpPrgressCallback, long lData) {
        return 0;
    }

    int SPI_CALL GetArchiveInfo(LPSTR buf, long len, unsigned int flag, HLOCAL* lphInf) {
        return 0;
    }

    int SPI_CALL GetFileInfo(LPSTR buf, long len, LPSTR filename, unsigned int flag, FileInfo* lpInfo) {
        return 0;
    }

    int GetFile(LPSTR src, long len, LPSTR dest, unsigned int flag, FARPROC prgressCallback, long lData) {
        return 0;
    }

    BOOL APIENTRY DllMain(HMODULE hModule,
        DWORD  ul_reason_for_call,
        LPVOID lpReserved
    )
    {
        switch (ul_reason_for_call)
        {
        case DLL_PROCESS_ATTACH:
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
        case DLL_PROCESS_DETACH:
            break;
        }
        return TRUE;
    }

#ifdef __cplusplus
}
#endif // __cplusplus
