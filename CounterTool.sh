#!/bin/sh
printf '\033c\033]0;%s\a' CounterTool
base_path="$(dirname "$(realpath "$0")")"
"$base_path/CounterTool.x86_64" "$@"
