/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
// EngExtCpp 実装
#include "engextcpp.hpp"
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

class EXT_CLASS : public ExtExtension
{
    void ShowRegStr(PCSTR RegName);
public:
    EXT_COMMAND_METHOD(regstr);
};

EXT_DECLARE_GLOBALS(); // 拡張機能に必要なインスタンスを用意するマクロ

// 文字列らしいデータまでを文字列として返す。
static std::string MakeString(PCSTR Buffer, const ULONG BufferLength)
{
    std::string ret;
    ret.reserve(BufferLength);
    for (ULONG i = 0; i < BufferLength; i++)
    {
        const CHAR ch = Buffer[i];
        // 0x80 以上は ASCII でない
        if (ch < 0) return ret;
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
    // ここまで来た場合、文字列の途中で切れた可能性があるため、... を付けて続きを示唆しておく
    ret += "...";
    return ret;
}

void EXT_CLASS::ShowRegStr(PCSTR RegName)
{
    const ULONG64 RegValue = GetRegisterU64(RegName);
    char buf[N] = { 0 };
    try
    {
        ExtRemoteData data;
        data.Set(RegValue, N);
        // data.GetString(buf, N); // これでも良い
        data.ReadBuffer(buf, N, false);
    }
    catch (...)
    {
        // 何もしない
    }
    // buf を文字列に変換する
    const std::string s = MakeString(buf, N);
    if (s.empty())
    {
        Out("%-3s = 0x%016I64x\n", RegName, RegValue);
    }
    else
    {
        // 先頭 1 文字でも正当な文字であれば表示する
        Out("%-3s = 0x%016I64x \"%s\"\n", RegName, RegValue, s.c_str());
    }
}

EXT_COMMAND(regstr, "Print register string", "")
{
    for (size_t i = 0; i < _countof(REG_NAMES); i++) ShowRegStr(REG_NAMES[i]);
}
