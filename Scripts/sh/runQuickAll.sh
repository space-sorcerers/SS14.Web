#!/bin/sh

DIR="$(dirname "$0")"
"$DIR"/runQuickAuth.sh "$@" &
"$DIR"/runQuickHub.sh "$@" &
"$DIR"/runQuickWeb.sh "$@" &
