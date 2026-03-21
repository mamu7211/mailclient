#!/bin/bash
# Stoppt und entfernt Aspire-Container inkl. zugehöriger Volumes
# Unterstützt Docker und Podman

set -e

# ── Farben ──────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
BOLD='\033[1m'
DIM='\033[2m'
RESET='\033[0m'

# ── Container-Runtime erkennen ──────────────────────────────
if command -v podman &>/dev/null; then
    RT="podman"
elif command -v docker &>/dev/null; then
    RT="docker"
else
    echo -e "${RED}✗ Weder podman noch docker gefunden.${RESET}"
    exit 1
fi

echo ""
echo -e "${BOLD}${CYAN}  ╔══════════════════════════════════════════╗${RESET}"
echo -e "${BOLD}${CYAN}  ║     Feirb Aspire Container Cleanup        ║${RESET}"
echo -e "${BOLD}${CYAN}  ╚══════════════════════════════════════════╝${RESET}"
echo -e "  ${DIM}Runtime: ${RT}${RESET}"
echo ""

# ── Container suchen ────────────────────────────────────────
mapfile -t CONTAINERS < <($RT ps -a --format "{{.Names}}" | grep -E "^feirb-" || true)

if [ ${#CONTAINERS[@]} -eq 0 ]; then
    echo -e "  ${YELLOW}⚠  Keine Feirb-Container gefunden.${RESET}"
    echo ""
    exit 0
fi

# ── Hilfsfunktionen ─────────────────────────────────────────

get_volumes() {
    # Volumes die am Container gemountet sind
    local mounted
    mounted=$($RT inspect "$1" --format '{{range .Mounts}}{{if eq .Type "volume"}}{{.Name}} {{end}}{{end}}' 2>/dev/null || true)
    # Zusätzlich feirb.apphost-* Volumes die zum Service-Namen passen
    local orphaned
    orphaned=$($RT volume ls --format "{{.Name}}" | grep -E "^feirb\.apphost-.*-${1%%-*}" || true)
    echo "$mounted $orphaned" | tr ' ' '\n' | sort -u | tr '\n' ' '
}

get_status() {
    local status
    status=$($RT inspect "$1" --format '{{.State.Status}}' 2>/dev/null || echo "unknown")
    case "$status" in
        running)  echo -e "${GREEN}●${RESET}" ;;
        exited)   echo -e "${RED}●${RESET}" ;;
        *)        echo -e "${YELLOW}●${RESET}" ;;
    esac
}

remove_container() {
    local container="$1"
    local volumes
    volumes=$(get_volumes "$container")

    echo -e "  ${YELLOW}⏹${RESET}  Stoppe ${BOLD}$container${RESET} ..."
    $RT stop "$container" 2>/dev/null || true

    echo -e "  ${RED}✗${RESET}  Entferne ${BOLD}$container${RESET} ..."
    $RT rm "$container" 2>/dev/null || true

    for vol in $volumes; do
        echo -e "  ${RED}✗${RESET}  Lösche Volume ${DIM}$vol${RESET}"
        $RT volume rm "$vol" 2>/dev/null || true
    done
}

# ── Container auflisten ────────────────────────────────────
echo -e "  ${BOLD}Aspire-Container:${RESET}"
echo ""

for i in "${!CONTAINERS[@]}"; do
    status=$(get_status "${CONTAINERS[$i]}")
    volumes=$(get_volumes "${CONTAINERS[$i]}")
    echo -e "  ${BOLD}${BLUE}[$((i+1))]${RESET}  $status  ${BOLD}${CONTAINERS[$i]}${RESET}"
    for vol in $volumes; do
        echo -e "       ${GRAY}└── $vol${RESET}"
    done
done

echo ""
echo -e "  ${BOLD}${BLUE}[a]${RESET}  Alle entfernen"
echo -e "  ${BOLD}${BLUE}[q]${RESET}  Abbrechen"
echo ""

read -rp "  Auswahl: " choice
echo ""

case "$choice" in
    a|A)
        for container in "${CONTAINERS[@]}"; do
            remove_container "$container"
            echo ""
        done
        echo -e "  ${GREEN}✓  Alle Feirb-Container wurden entfernt.${RESET}"
        ;;
    q|Q)
        echo -e "  ${YELLOW}Abgebrochen.${RESET}"
        exit 0
        ;;
    *)
        if [[ "$choice" =~ ^[0-9]+$ ]] && [ "$choice" -ge 1 ] && [ "$choice" -le ${#CONTAINERS[@]} ]; then
            remove_container "${CONTAINERS[$((choice-1))]}"
            echo ""
            echo -e "  ${GREEN}✓  Fertig.${RESET}"
        else
            echo -e "  ${RED}✗  Ungültige Auswahl.${RESET}"
            exit 1
        fi
        ;;
esac

echo ""
