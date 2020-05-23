#!/bin/bash

# install ghvs if not on path
if [[ ! $(which ghvs) ]]; then
    dotnet tool update ghvs -g
fi

if [[ $(uname) == "Linux" ]]; then
    if [[ ! $DOTNET_ROOT ]]; then
        export DOTNET_ROOT=$(dirname $(realpath $(which dotnet)))
    fi

    if [[ ! $(which ghvs) ]]; then
        export PATH=$PATH:$HOME/.dotnet/tools
    fi
fi

ghvs $@
