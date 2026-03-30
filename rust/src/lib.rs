/*
 * (c) FFRI Security, Inc., 2026 / Author: FFRI Security, Inc.
 */
//! dbgeng-rs 版

#![allow(non_snake_case)]
use dbgeng::{
    DEBUG_EXTENSION_VERSION,
    client::DebugClient,
    windows::{
        Win32::{
            Foundation::{E_ABORT, S_OK},
            System::Diagnostics::Debug::Extensions::DEBUG_EXTINIT_HAS_COMMAND_HELP,
        },
        core::{HRESULT, IUnknown, Interface, PCSTR},
    },
};

const N: usize = 32; // 取得する文字数の最大サイズ

// 表示するレジスタ
const REG_NAMES: &[&str] = &[
    "rax", "rbx", "rcx", "rdx", "rsi", "rdi", "rbp", "rsp", "r8", "r9", "r10",
    "r11", "r12", "r13", "r14", "r15",
];

#[unsafe(no_mangle)]
extern "C" fn DebugExtensionInitialize(
    Version: *mut u32,
    Flags: *mut u32,
) -> HRESULT {
    unsafe {
        // 生ポインタ処理のため unsafe
        *Version = DEBUG_EXTENSION_VERSION(1, 0);
        *Flags = DEBUG_EXTINIT_HAS_COMMAND_HELP;
    }
    S_OK
}

/// `%` をエスケープする。
fn escape<S: AsRef<str>>(s: S) -> String {
    s.as_ref().replace('%', "%%")
}

/// 文字列らしいデータを文字列として返す。
fn make_string(buf: &[u8]) -> String {
    let mut ret = String::with_capacity(N);
    for ch in buf {
        let ch = *ch;
        // 0x80 以上は ASCII でない
        if ch >= 0x80 {
            return ret;
        }
        // 0x20 未満はエスケープシーケンス
        // 以下のエスケープシーケンス以外は文字とみなさないこととする
        if ch < 0x20 {
            match ch {
                0x09 => ret += "\\t",
                0x0a => ret += "\\n",
                0x0b => ret += "\\v",
                0x0d => ret += "\\r",
                _ => return ret,
            }
        } else {
            if ch == 0x22 || ch == 0x5c {
                ret.push('\\');
            }
            ret.push(ch as char);
        }
    }
    // ここまで来た場合、文字列の途中で切れた可能性があるため、... を付けて続きを示唆しておく
    ret += "...";
    ret
}

fn show_reg_str(dbg: &DebugClient, reg_name: &str) {
    let val = match dbg.reg64(reg_name) {
        Ok(x) => x,
        Err(e) => {
            // レジスタの取得に失敗
            let _ = dbg.log(escape(format!(
                "Getting {reg_name} value is failed: {e}\n"
            )));
            return;
        }
    };
    let mut buf = [0; N];

    let _ = dbg.read_virtual(val, &mut buf);
    let s = make_string(&buf);
    if s.is_empty() {
        let _ = dbg.log(escape(format!("{reg_name:3} = 0x{val:016x}\n")));
    } else {
        let _ =
            dbg.log(escape(format!("{reg_name:3} = 0x{val:016x} \"{s}\"\n")));
    }
}

/// regstr コマンド
#[unsafe(no_mangle)]
extern "C" fn regstr(
    debug_client: *mut std::ffi::c_void,
    _args: PCSTR,
) -> HRESULT {
    let Some(client) = (unsafe { IUnknown::from_raw_borrowed(&debug_client) })
    else {
        return E_ABORT;
    };

    let Ok(dbg) = DebugClient::new(client) else {
        return E_ABORT;
    };

    for reg_name in REG_NAMES {
        show_reg_str(&dbg, reg_name);
    }
    S_OK
}

/// help コマンド
#[unsafe(no_mangle)]
extern "C" fn help(
    debug_client: *mut std::ffi::c_void,
    _args: PCSTR,
) -> HRESULT {
    // 生ポインタを IUnknown 型にキャスト
    let Some(client) = (unsafe { IUnknown::from_raw_borrowed(&debug_client) })
    else {
        return E_ABORT;
    };

    // DebugClient 内で必要なインタフェースにキャストされる
    let Ok(dbg) = DebugClient::new(client) else {
        return E_ABORT;
    };

    /*
    // 引数を使いたい場合
    let Ok(args) = (unsafe { _args.to_string() }) else {
        return E_ABORT;
    };
    */

    let _ = dbg.log("regstr_rs help!\n");
    S_OK
}
