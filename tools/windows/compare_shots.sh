#!/usr/bin/env bash
# Capturas comparables de una build (vistas fijas): arranque, tio de frente, tio de perfil, arrastre hasta la camioneta.
# Uso (Git Bash, desde la raiz del repo): tools/windows/compare_shots.sh <carpeta_build> <carpeta_salida> [calidad low|high]
set -u
B="$1"; OUT="$2"; Q="${3:-high}"
mkdir -p "$OUT"; O=$(cygpath -w "$(cd "$OUT" && pwd)")
EXE="$B/UncleExtraction.exe"; W="-screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -ueQuality $Q"
run() { "$EXE" -ueScene Bar_LastRound $W "$@" >/dev/null 2>&1 & local pid=$!; sleep "$WAIT"; taskkill //F //IM UncleExtraction.exe >/dev/null 2>&1; wait $pid 2>/dev/null; }
WAIT=13; run -ueAutopilot drag -ueAutopilotDelay 60 -ueShot "$O\\01_arranque.png@3" -logFile "$O\\01.log"
WAIT=13; run -ueAutopilot drag -ueAutopilotDelay 60 -ueFaceUncle 2 -ueShot "$O\\02_tio_frente.png@3.5" -logFile "$O\\02.log"
WAIT=13; run -ueAutopilot drag -ueAutopilotDelay 60 -ueFaceUncle 2 -ueFaceAngle 90 -ueShot "$O\\03_tio_perfil.png@3.5" -logFile "$O\\03.log"
WAIT=24; run -ueAutopilot drag -ueShot "$O\\04_arrastre.png@11" -ueShot "$O\\05_camioneta.png@15" -ueShot "$O\\06_vereda.png@19" -logFile "$O\\04.log"
ls "$OUT"/*.png
