/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
// WdbgExts 実装
#define KDEXT_64BIT // 64 ビット版であることを明示

#include <Windows.h>
#include <WDBGEXTS.H>

#define N 32 // 最大バイト数

// 表示するレジスタ
const char* const REG_NAMES[] = {
    "rax",
    "rbx",
    "rcx",
    "rdx",
    "rsi",
    "rdi",
    "rbp",
    "rsp",
    "r8",
    "r9",
    "r10",
    "r11",
    "r12",
    "r13",
    "r14",
    "r15",
};

// APIバージョン情報
EXT_API_VERSION ApiVersion = {
    1,                        // MajorVersion (拡張機能のメジャーバージョン)
    0,                        // MinorVersion (拡張機能のマイナーバージョン)
    EXT_API_VERSION_NUMBER64, // Revision     (リビジョン)
    0                         // Reserved     (予約)
};

// 使用するAPIバージョン情報を返す関数
LPEXT_API_VERSION ExtensionApiVersion(void) {
    return &ApiVersion;
}

WINDBG_EXTENSION_APIS ExtensionApis;

// 拡張機能の初期化用関数
VOID WinDbgExtensionDllInit(
    PWINDBG_EXTENSION_APIS lpExtensionApis,
    USHORT MajorVersion,
    USHORT MinorVersion
) {
    UNREFERENCED_PARAMETER(MajorVersion);
    UNREFERENCED_PARAMETER(MinorVersion);
    ExtensionApis = *lpExtensionApis;
}

// 文字列らしいデータまでを文字列として返す。
PSTR MakeString(PCSTR Buffer, const ULONG BufferLength)
{
    const PSTR ret = (PSTR)malloc((size_t)BufferLength * 2 * sizeof(char) + 4);
    if (!ret) return NULL;
    PSTR it = ret;
    for (ULONG i = 0; i < BufferLength; i++)
    {
        const CHAR ch = Buffer[i];
        // 0x80 以上はASCIIでない
        if ((UCHAR)ch >= 0x80) goto Finish;
        if (ch < 0x20)
        {
            // 0x20 未満はエスケープシーケンス
            // 以下のエスケープシーケンス以外は文字とみなさないこととする
            switch (ch)
            {
            case '\t':
                *it++ = '\\';
                *it++ = 't';
                break;
            case '\n':
                *it++ = '\\';
                *it++ = 'n';
                break;
            case '\v':
                *it++ = '\\';
                *it++ = 'v';
                break;
            case '\r':
                *it++ = '\\';
                *it++ = 'r';
                break;
            default:
                goto Finish;
            }
        }
        else
        {
            if (ch == '"' || ch == '\\') *it++ = '\\';
            *it++ = ch;
        }
    }
    // ここまで来た場合、文字列の途中で切れた可能性があるため、... を付けて続きを示唆しておく。
    *it++ = '.';
    *it++ = '.';
    *it++ = '.';
Finish:
    *it = '\0';
    return ret;
}

void ShowRegStr(PCSTR RegName)
{
    /*
    // コンテキストから取る方法もある (が、レジスタ名で検索する場合は面倒)
    CONTEXT ctx;
    GetContext(0, &ctx, sizeof(ctx));

    ULONG64 RegValue;
    if (!strcmp(RegName, "rax")) RegValue = ctx.Rax;
    else if (!strcmp(RegName, "rbx")) RegValue = ctx.Rbx;
    else if (!strcmp(RegName, "rcx")) RegValue = ctx.Rcx;
    else if (!strcmp(RegName, "rdx")) RegValue = ctx.Rdx;
    else if (!strcmp(RegName, "rdi")) RegValue = ctx.Rdi;
    else if (!strcmp(RegName, "rsi")) RegValue = ctx.Rsi;
    else if (!strcmp(RegName, "r8")) RegValue = ctx.R8;
    else if (!strcmp(RegName, "r9")) RegValue = ctx.R9;
    else if (!strcmp(RegName, "r10")) RegValue = ctx.R10;
    else if (!strcmp(RegName, "r11")) RegValue = ctx.R11;
    else if (!strcmp(RegName, "r12")) RegValue = ctx.R12;
    else if (!strcmp(RegName, "r13")) RegValue = ctx.R13;
    else if (!strcmp(RegName, "r14")) RegValue = ctx.R14;
    else if (!strcmp(RegName, "r15")) RegValue = ctx.R15;
    else return;
    */
    char fmt[8] = "@";
    strcat_s(fmt, _countof(fmt), RegName);
    // "@rax" 等を式評価
    const ULONG64 RegValue = GetExpression(fmt);

    char buf[N] = { 0 };
    ULONG nr = 0;
    // 読み取りに失敗した場合も続行
    ReadMemory(RegValue, buf, N, &nr);

    // bufを文字列に変換する
    const PSTR s = MakeString(buf, N);
    if (!s)
    {
        dprintf("Insufficient memory\n");
        return;
    }
    if (s[0] == '\0')
    {
        dprintf("%-3s = 0x%016I64x\n", RegName, RegValue);
    }
    else
    {
        // 先頭1文字でも正当な文字であれば表示する
        dprintf("%-3s = 0x%016I64x \"%s\"\n", RegName, RegValue, s);
    }
    free(s);
}

DECLARE_API(regstr)
{
    UNREFERENCED_PARAMETER(hCurrentProcess);
    UNREFERENCED_PARAMETER(hCurrentThread);
    UNREFERENCED_PARAMETER(dwCurrentPc);
    UNREFERENCED_PARAMETER(dwProcessor);
    UNREFERENCED_PARAMETER(args);

    for (size_t i = 0; i < _countof(REG_NAMES); i++) ShowRegStr(REG_NAMES[i]);
}

DECLARE_API(help)
{
    UNREFERENCED_PARAMETER(hCurrentProcess);
    UNREFERENCED_PARAMETER(hCurrentThread);
    UNREFERENCED_PARAMETER(dwCurrentPc);
    UNREFERENCED_PARAMETER(dwProcessor);
    UNREFERENCED_PARAMETER(args);
    dprintf("regstr_c help!\n");
}
