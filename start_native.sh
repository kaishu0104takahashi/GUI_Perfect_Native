#!/bin/bash

# 1. .NETの場所を明確に指定 (これが足りていませんでした)
export DOTNET_ROOT=$HOME/.dotnet

# 2. パスも通しておく
export PATH=$PATH:$HOME/.dotnet:$HOME/.dotnet/tools

# 3. プロジェクトフォルダへ移動
cd /home/shikoku-pc/avalonia/GUI_Perfect

# 4. ビルド済みアプリを直接実行
./bin/Debug/net9.0/GUI_Perfect
