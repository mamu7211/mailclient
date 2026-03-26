#!/bin/bash
# Stops and removes Aspire containers including associated volumes
# Ollama volumes are preserved to avoid re-downloading large AI models
# Supports Docker and Podman

set -e

# ── Colors ───────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
BOLD='\033[1m'
DIM='\033[2m'
RESET='\033[0m'

# ── Detect container runtime ─────────────────────────────────
if command -v podman &>/dev/null; then
    RT="podman"
elif command -v docker &>/dev/null; then
    RT="docker"
else
    echo -e "${RED}✗ Neither podman nor docker found.${RESET}"
    exit 1
fi

echo ""
echo -e "${BOLD}${CYAN}  ╔══════════════════════════════════════════╗${RESET}"
echo -e "${BOLD}${CYAN}  ║     Feirb Aspire Container Cleanup        ║${RESET}"
echo -e "${BOLD}${CYAN}  ╚══════════════════════════════════════════╝${RESET}"
echo -e "  ${DIM}Runtime: ${RT}${RESET}"
echo ""

# ── Find containers ──────────────────────────────────────────
mapfile -t CONTAINERS < <($RT ps -a --format "{{.Names}}" | grep -E "^feirb-" || true)

if [ ${#CONTAINERS[@]} -eq 0 ]; then
    echo -e "  ${YELLOW}⚠  No Feirb containers found.${RESET}"
    echo ""
    cleanup_orphaned_volumes
    exit 0
fi

# ── Helper functions ─────────────────────────────────────────

get_volumes() {
    # Volumes mounted on the container
    local mounted
    mounted=$($RT inspect "$1" --format '{{range .Mounts}}{{if eq .Type "volume"}}{{.Name}} {{end}}{{end}}' 2>/dev/null || true)
    # Additionally match feirb.apphost-* volumes by service name
    # Extract the service name (e.g. feirb-postgres-xxx → feirb-postgres)
    local service_name
    service_name=$(echo "$1" | sed -E 's/-[a-z0-9]{8,}$//')
    local orphaned
    orphaned=$($RT volume ls --format "{{.Name}}" | grep -F "$service_name" || true)
    echo "$mounted $orphaned" | tr ' ' '\n' | sort -u | tr '\n' ' '
}

cleanup_orphaned_volumes() {
    mapfile -t ORPHAN_VOLS < <($RT volume ls --format "{{.Name}}" | grep -E "^feirb\." | grep -v "ollama" || true)
    if [ ${#ORPHAN_VOLS[@]} -eq 0 ]; then
        return
    fi

    echo -e "  ${BOLD}Orphaned volumes:${RESET}"
    echo ""
    for vol in "${ORPHAN_VOLS[@]}"; do
        echo -e "       ${GRAY}└── $vol${RESET}"
    done
    echo ""
    read -rp "  Delete orphaned volumes? [y/N] " vol_choice
    echo ""
    if [[ "$vol_choice" =~ ^[yY]$ ]]; then
        for vol in "${ORPHAN_VOLS[@]}"; do
            echo -e "  ${RED}✗${RESET}  Removing volume ${DIM}$vol${RESET}"
            $RT volume rm "$vol" 2>/dev/null || true
        done
        echo ""
        echo -e "  ${GREEN}✓  Orphaned volumes removed.${RESET}"
    else
        echo -e "  ${YELLOW}Skipped.${RESET}"
    fi
    echo ""
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

is_ollama_container() {
    [[ "$1" == *ollama* ]]
}

remove_container() {
    local container="$1"
    local volumes
    volumes=$(get_volumes "$container")

    echo -e "  ${YELLOW}⏹${RESET}  Stopping ${BOLD}$container${RESET} ..."
    $RT stop "$container" 2>/dev/null || true

    echo -e "  ${RED}✗${RESET}  Removing ${BOLD}$container${RESET} ..."
    $RT rm "$container" 2>/dev/null || true

    if is_ollama_container "$container"; then
        echo -e "  ${CYAN}⏩${RESET}  Keeping Ollama volumes (model data)"
    else
        for vol in $volumes; do
            echo -e "  ${RED}✗${RESET}  Removing volume ${DIM}$vol${RESET}"
            $RT volume rm "$vol" 2>/dev/null || true
        done
    fi
}

# ── List containers ──────────────────────────────────────────
echo -e "  ${BOLD}Aspire containers:${RESET}"
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
echo -e "  ${BOLD}${BLUE}[a]${RESET}  Remove all"
echo -e "  ${BOLD}${BLUE}[q]${RESET}  Cancel"
echo ""

read -rp "  Selection: " choice
echo ""

case "$choice" in
    a|A)
        for container in "${CONTAINERS[@]}"; do
            remove_container "$container"
            echo ""
        done
        echo -e "  ${GREEN}✓  All Feirb containers removed.${RESET}"
        echo ""
        cleanup_orphaned_volumes
        ;;
    q|Q)
        echo -e "  ${YELLOW}Cancelled.${RESET}"
        exit 0
        ;;
    *)
        if [[ "$choice" =~ ^[0-9]+$ ]] && [ "$choice" -ge 1 ] && [ "$choice" -le ${#CONTAINERS[@]} ]; then
            remove_container "${CONTAINERS[$((choice-1))]}"
            echo ""
            echo -e "  ${GREEN}✓  Done.${RESET}"
            echo ""
            cleanup_orphaned_volumes
        else
            echo -e "  ${RED}✗  Invalid selection.${RESET}"
            exit 1
        fi
        ;;
esac

echo ""
