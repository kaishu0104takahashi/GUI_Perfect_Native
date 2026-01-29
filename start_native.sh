#!/bin/bash

# アプリ名
APP_NAME="GUI_Perfect"

# すでに起動しているかチェック (プロセスIDを探す)
if pgrep -x "$APP_NAME" > /dev/null
then
    # 起動していたら何もしないで終了
    echo "$APP_NAME is already running."
    sleep 1 # ターミナルが一瞬で消えると不安になる場合のため少し待つ（任意）
    exit 0
fi

# まだ起動していなければ起動する
export DISPLAY=:0

# ビルドしたバイナリがあるフォルダへ移動
cd /home/shikoku-pc/gui_bin

# アプリ実行
./$APP_NAME
