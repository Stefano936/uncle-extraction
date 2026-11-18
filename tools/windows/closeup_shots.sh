#!/usr/bin/env bash
# Primeros planos comparables (sesion 16): barra, puerta, carrito, tio de frente y de perfil, sin HUD.
# Uso: tools/windows/closeup_shots.sh <carpeta_build> <carpeta_salida>
set -u
B="$1"; OUT="$2"; mkdir -p "$OUT"; O=$(cygpath -w "$(cd "$OUT" && pwd)"); EXE="$B/UncleExtraction.exe"
W="-screen-fullscreen 0 -screen-width 1600 -screen-height 900 -ueCleanCapture -ueNoHud -ueAutopilot drag -ueAutopilotDelay 60"
run() { "$EXE" -ueScene Bar_LastRound $W "$@" >/dev/null 2>&1 & local pid=$!; sleep 17; taskkill //F //IM UncleExtraction.exe >/dev/null 2>&1; wait $pid 2>/dev/null; }
run -uePose "2.6,0,7.3,40,25@2" -ueShot "$O\c1_barra.png@3.5" -logFile "$O\c1.log"
run -uePose "0.9,0,-0.6,215,8@2" -ueShot "$O\c2_puerta.png@3.5" -logFile "$O\c2.log"
run -uePose "4.6,0,1.1,40,32@2" -ueShot "$O\c3_carrito.png@3.5" -logFile "$O\c3.log"
run -ueFaceUncle 2 -ueShot "$O\c4_tio_frente.png@3.6" -logFile "$O\c4.log"
run -ueFaceUncle 2 -ueFaceAngle 90 -ueShot "$O\c5_tio_perfil.png@3.6" -logFile "$O\c5.log"
ls "$OUT"/*.png
