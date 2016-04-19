How to hack on Keebuntu
=======================

This assumes you are running Ubuntu 16.04.

## Installing Prerequisites

    sudo apt-add-repository ppa:dlech/keepass2-plugins-beta
    sudo sed -i 's/#deb-src/deb-src/' /etc/apt/sources.list.d/dlech-ubuntu-keepass2-plugins-beta-xenial.list
    sudo apt-get update
    sudo apt-get build-dep keepass2-plugin-ubuntu
    sudo apt-get install git

## Getting the source code

    git clone https://github.com/dlech/Keebuntu

## Building

    cd Keebuntu
    xbuild /property:Configuration=Release Keebuntu.sln

## IDE

    sudo apt-get install monodevelop
    monodevelop Keebuntu.sln
