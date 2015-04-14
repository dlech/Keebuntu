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

####Package

`keepass2-plugin-launcher`

####Usage

Right-click on the KeePass 2.x icon in the launcher. You will find menu items such as Lock/Unlock Workspace and Generate Password….

New (in beta)! Tray Icon
========================

Provides a notification tray icon for KeePass. 

Tested with the following desktops:
* Cinnamon

Does not work with:
* Ubuntu Unity

####Background

The built-in notification tray icon for KeePass does not display in the panel. This is because
notification tray support for WinForms applications is broken in Mono.

####Package

`keepass2-plugin-tray-icon`

Note: this package conflicts with `keepas2-plugin-application-indicator` (you cannot have both
installed at the same time). Compare the usages to decide which package you want to install.

####Usage

Left-clicking the icon will activate the KeePass window. Right-clicking the icon displays the menu.

Application Indicator
=====================

Provides an application indicator tray icon for KeePass.

Tested with the following desktops:
* Cinnamon
* GNOME Shell (requires [appindicator plugin](https://extensions.gnome.org/extension/615/appindicator-support/))
* KDE
* Unity
* Xfce

####Background

The built-in notification tray icon for KeePass does not display in the panel. This is
because notification tray support for WinForms applications is broken in Mono. Additionally,
even if it did work, it would still not be displayed in Ubuntu Unity Desktop because it
only supports application indicator type tray icons.

####Package

`keepass2-plugin-application-indicator`

Note: this package conflicts with `keepas2-plugin-tray-icon` (you cannot have both
installed at the same time). Compare the usages to decide which package you want to install.

####Usage

Right or left-clicking the icon displays the menu. Hovering the mouse over the icon and scrolling down with the
scroll wheel will minimize KeePass to the tray. Scrolling up will resore KeePass from the tray.


Application Menu
================

Shows the KeePass application menu in the panel instead of the application window. NOTE: This only works on Unity - not other desktops.

####Background

Most applications, when used in the Ubuntu Unity desktop, have their menus shown in a common area in the panel rather
than in the application window.

####Package

`keepass2-plugin-application-menu`

####Usage

Setting the environment variable `APPMENU_DISPLAY_BOTH=1` before starting KeePass will show the menu both on the panel
and in the KeePass application window.

####Known Issues

* Causes KeePassHttp to crash. I have submitted a patch to KeePassHttp to fix this, ~~but it seems that it is no longer being maintained~~. [Update: the patch has been merged, but not sure if it has been released yet.] So, I have packaged a patched version of KeePassHttp which can be obtained by running `sudo apt-get install keepass2-plugin-keepasshttp`. Be sure to uninstall any old version of KeePassHttp first. See below on how to setup the ppa.

-----

Binary Packages
===============

On Ubuntu and derivative systems, you can install via ppa:

```
sudo apt-add-repository ppa:dlech/keepass2-plugins
sudo apt-get update
sudo apt-get install <list-of-package-names>
```

The latest versions are in the beta ppa:
```
sudo apt-add-repository ppa:dlech/keepass2-plugins-beta
sudo apt-get update
sudo apt-get install <list-of-package-names>
```

On Arch Linux, you can try https://aur.archlinux.org/packages/keebuntu-git/ (outdated)
