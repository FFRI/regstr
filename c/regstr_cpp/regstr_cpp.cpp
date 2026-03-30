/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
// DbgEng 実装
#include <DbgEng.h>
#include <string>

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

EXTERN_C HRESULT CALLBACK
DebugExtensionInitialize(_Out_ PULONG Version,
    _Out_ PULONG Flags)
{
    // 拡張機能のバージョンを指定
    *Version = DEBUG_EXTENSION_VERSION(1, 0);
    // help コマンドを定義するため、フラグを立てておく
    *Flags = DEBUG_EXTINIT_HAS_COMMAND_HELP;

    return S_OK;
}

static std::string MakeString(PCSTR Buffer, const ULONG BufferLength)
{
    std::string ret;
    ret.reserve(BufferLength);
    for (ULONG i = 0; i < BufferLength; i++)
    {
        const CHAR ch = Buffer[i];
        // 0x80 以上 はASCII でない
        if (static_cast<UCHAR>(ch) >= 0x80) return ret;
        if (ch < 0x20)
        {
            // 0x20 未満はエスケープシーケンス
            // 以下のエスケープシーケンス以外は文字とみなさないこととする
            switch (ch)
            {
            case '\t':
                ret += "\\t";
                break;
            case '\n':
                ret += "\\n";
                break;
            case '\v':
                ret += "\\v";
                break;
            case '\r':
                ret += "\\r";
                break;
            default:
                return ret;
            }
        }
        else
        {
            if (ch == '"' || ch == '\\') ret += '\\';
            ret += ch;
        }
    }
    // ここまで来た場合、文字列の途中で切れた可能性があるため、... を付けて続きを示唆しておく。
    ret += "...";
    return ret;
}

// RegName に対応するレジスタの値を ULONG64 型で返す。
HRESULT GetRegisterValueUlong64ByName(IDebugControl7* Control, IDebugRegisters2* Registers, PCSTR RegName, PULONG64 Value)
{
    ULONG Register;

    // RegName に対応するレジスタのインデックスを取得する
    HRESULT hr = Registers->GetIndexByName(RegName, &Register);
    if (FAILED(hr)) return hr;

    // 取得したレジスタインデックスから値を取り出す
    DEBUG_VALUE DebugValue, DebugValueOut;
    hr = Registers->GetValue(Register, &DebugValue);
    if (FAILED(hr)) return hr;

    if (DebugValue.Type == DEBUG_VALUE_INT64) {
        // タイプが一致していればそのまま使用する
        // DEBUG_VALUE から対応する型を取り出して返す
        *Value = DebugValue.I64;
    }
    else {
        // タイプが一致していなければ合わせる
        hr = Control->CoerceValue(&DebugValue, DEBUG_VALUE_INT64, &DebugValueOut);
        if (FAILED(hr)) return hr;
        *Value = DebugValueOut.I64;
    }

    return S_OK;
}

void ShowRegStr(IDebugControl7* Control, IDebugDataSpaces4* DataSpaces, IDebugRegisters2* Registers, PCSTR RegName)
{
    ULONG64 RegValue;
    if (FAILED(GetRegisterValueUlong64ByName(Control, Registers, RegName, &RegValue)))
    {
        // 取得失敗
        Control->Output(DEBUG_OUTPUT_NORMAL, "Failed to get register %s value\n", RegName);
        return;
    }

    // RegValue をアドレスとみなしてメモリの読み取りを試みる
    // 読み取りの成否は気にしない
    char buf[N] = { 0 };
    ULONG nr;
    DataSpaces->ReadVirtual(RegValue, buf, N, &nr);
    const std::string s = MakeString(buf, N);
    if (s.empty())
    {
        Control->Output(DEBUG_OUTPUT_NORMAL, "%-3s = 0x%016I64x\n", RegName, RegValue);
    }
    else
    {
        // 先頭 1 文字でも正当な文字であれば表示する
        Control->Output(DEBUG_OUTPUT_NORMAL, "%-3s = 0x%016I64x \"%s\"\n", RegName, RegValue, s.c_str());
    }
}

EXTERN_C HRESULT CALLBACK
regstr(PDEBUG_CLIENT Client, PCSTR Args)
{
    UNREFERENCED_PARAMETER(Args);

    HRESULT hr;

    // 必要なインターフェースを取得する
    IDebugControl7* Control = nullptr;
    IDebugDataSpaces4* DataSpaces = nullptr;
    IDebugRegisters2* Registers = nullptr;
    hr = Client->QueryInterface(__uuidof(IDebugControl7), reinterpret_cast<PVOID*>(&Control));
    if (FAILED(hr)) goto Finish;
    hr = Client->QueryInterface(__uuidof(IDebugDataSpaces4), reinterpret_cast<PVOID*>(&DataSpaces));
    if (FAILED(hr)) goto Finish;
    hr = Client->QueryInterface(__uuidof(IDebugRegisters2), reinterpret_cast<PVOID*>(&Registers));
    if (FAILED(hr)) goto Finish;

    for (size_t i = 0; i < _countof(REG_NAMES); i++) ShowRegStr(Control, DataSpaces, Registers, REG_NAMES[i]);
    hr = S_OK;

Finish:
    if (FAILED(hr))
    {
        // 必要なインターフェース取得に失敗したらエラーを返す
        if (Control) Control->Output(DEBUG_OUTPUT_NORMAL, "Failed to get interfaces!\n");
    }
    if (Control) Control->Release();
    if (DataSpaces) DataSpaces->Release();
    if (Registers) Registers->Release();

    return hr;
}

EXTERN_C HRESULT CALLBACK
help(PDEBUG_CLIENT Client, PCSTR Args)
{
    UNREFERENCED_PARAMETER(Args);

    HRESULT hr;

    // 必要なインターフェースを取得する
    IDebugControl7* Control = nullptr;
    hr = Client->QueryInterface(__uuidof(IDebugControl7), reinterpret_cast<PVOID*>(&Control));
    if (FAILED(hr)) goto Finish;

    Control->Output(DEBUG_OUTPUT_NORMAL, "regstr_cpp help!\n");
    hr = S_OK;

Finish:
    if (FAILED(hr))
    {
        // 必要なインターフェース取得に失敗したらエラーを返す
        if (Control) Control->Output(DEBUG_OUTPUT_NORMAL, "Failed to get interfaces!\n");
    }
    // インターフェースの解放
    if (Control) Control->Release();
    return hr;
}
