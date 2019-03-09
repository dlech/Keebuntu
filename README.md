Keebuntu
========

KeePass 2.x plugins that provide Linux Desktop integration. These are
primarily targeted for Ubuntu (but can work on other distros as well).


Status Notifier
===============

Provides a notification tray icon for KeePass on Plasma/KDE5. Also works with
GNOME desktop via [gnome-shell-extension-appindicator][1] (installed by default
as a dependency of the `ubuntu-desktop` package).

[1]: https://packages.ubuntu.com/source/bionic/gnome/gnome-shell-extension-appindicator

![Plasma status notifier screenshot](doc/images/plasma-status-notifier-screenshot.png)

#### Background

The built-in notification tray icon for KeePass does not display in the panel.
This is because notification tray support for WinForms applications is broken
in Mono.

#### Package

`keepass2-plugin-status-notifier`

**Note:** this package conflicts with `keepass2-plugin-tray-icon` (you can only
have one of these installed at a time). Compare the usages to decide which
package you want to install.

#### Usage

Left-clicking the icon trays and untrays the KeePass application. Right-clicking
the icon displays the menu.


Classic Tray Icon
=================

Provides a notification tray icon for KeePass.

![MATE tray icon screenshot](doc/images/mate-tray-icon-screenshot.png)

Tested with the following desktops:

* Cinnamon
* MATE

Does not work with:

* Ubuntu Unity

#### Background

The built-in notification tray icon for KeePass does not display in the panel.
This is because notification tray support for WinForms applications is broken
in Mono.

#### Package

`keepass2-plugin-tray-icon`

**Note:** this package conflicts with `keepass2-plugin-application-indicator`
(you cannot have both installed at the same time). Compare the usages to decide
which package you want to install.

#### Usage

Left-clicking the icon will activate the KeePass window. Right-clicking the
icon displays the menu.


Launcher Quicklist
==================

Takes menu items from notification tray icon and displays them in the Unity
Launcher menu. It also works with the [plank][3] dock (installed by default in
elementary OS).

[3]: https://packages.ubuntu.com/bionic/plank

![Ubuntu launcher screenshot](doc/images/ubuntu-launcher-screenshot.png)

#### Background

The built-in notification tray icon for KeePass does not display in the panel.
This is because the Ubuntu Unity Desktop only supports application indicator
type tray icons. This plugin provides an alternative means of accessing the
menu items of the tray icon.

#### Package

`keepass2-plugin-launcher`

#### Usage

Right-click on the KeePass 2.x icon in the launcher. You will find menu items
such as Lock/Unlock Workspace and Generate Password….


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
