#!/bin/bash

# TODO: Get build version from command-line argument
DNX_BUILD_VERSION=dev

dnu build ./src/HTTPlease* ./test/HTTPlease* --quiet
