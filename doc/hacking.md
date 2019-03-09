How to hack on Keebuntu
=======================

This assumes you are running Ubuntu 18.04.

## Installing Prerequisites

    sudo apt-add-repository ppa:dlech/keepass2-plugins-beta
    sudo sed -i 's/#deb-src/deb-src/' /etc/apt/sources.list.d/dlech-ubuntu-keepass2-plugins-beta-bionic.list
    sudo apt-get update
    sudo apt-get build-dep keepass2-plugin-ubuntu
    sudo apt-get install git

## Getting the source code

    git clone https://github.com/dlech/Keebuntu

## Building

    cd Keebuntu
    xbuild /property:Configuration=Release Keebuntu.sln

## IDE

    Open the folder in [Visual Studio Code](https://code.visualstudio.com/).
