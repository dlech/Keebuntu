Keebuntu
========

KeePass 2.x plugins that provide Ubuntu (Unity) integration.


Includes three plugins:

New (in beta)! Launcher Quicklist
=================================

Takes menu items from notification tray icon and displays them in the Unity Launcher menu.

Tested with the following desktops:
* Ubuntu Unity
* elementary OS Pantheon

####Background

The built-in notification tray icon for KeePass does not display in the panel. This is because the Ubuntu Unity
Desktop only supports application indicator type tray icons. This plugin provides an alternitive means of accessing the menu items of the tray icon.

####Usage

Right-click on the KeePass 2.x icon in the launcher. You will find menu items such as Lock/Unlock Workspace and Generate Password….


Application Indicator
=====================

Provides an application indicator tray icon for KeePass.

Tested with the following desktops:
* Cinnamon
* GNOME Shell (requires [appindicator plugin](https://extensions.gnome.org/extension/615/appindicator-support/))
* Unity
* Xfce

####Background

The built-in notification tray icon for KeePass does not display in the panel. This is because the Ubuntu Unity
Desktop only supports application indicator type tray icons.


####Usage

Right or left-clicking the icon displays the menu. Hovering the mouse over the icon and scrolling down with the
scroll wheel will minimize KeePass to the tray. Scrolling up will resore KeePass from the tray.


Application Menu
================

Shows the KeePass application menu in the panel instead of the application window. NOTE: This only works on Unity - not other desktops.


####Background


Most applications, when used in the Ubuntu Unity desktop, have their menus shown in a common area in the panel rather
than in the application window.


####Usage

Setting the environment variable `APPMENU_DISPLAY_BOTH=1` before starting KeePass will show the menu both on the panel
and in the KeePass application window.

####Known Issues

* Causes KeePassHttp to crash. I have submitted a patch to KeePassHttp to fix this, but it seems that it is no longer being maintained. So, I have packaged a patched version of KeePassHttp which can be obtained by running `sudo apt-get install keepass2-plugin-keepasshttp`. Be sure to uninstall any old version of KeePassHttp first. See below on how to setup the ppa.

-----

Binary Packages
===============

On Ubuntu and derivative systems, you can install via ppa:

```
sudo apt-add-repository ppa:dlech/keepass2-plugins
sudo apt-get update
sudo apt-get install keepass2-plugin-application-indicator keepass2-plugin-application-menu
```

The latest versions are in the beta ppa:
```
sudo apt-add-repository ppa:dlech/keepass2-plugins-beta
sudo apt-get update
sudo apt-get install keepass2-plugin-launcher keepass2-plugin-application-indicator keepass2-plugin-application-menu
```

On Arch Linux, you can try https://aur.archlinux.org/packages/keebuntu-git/ (outdated)
