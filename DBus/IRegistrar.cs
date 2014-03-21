using System;
using DBus;
using System.Collections.Generic;

namespace com.canonical.AppMenu.Registrar
{
  /// <summary>
  /// Interface for dbus
  /// </summary>
  [Interface("com.canonical.AppMenu.Registrar")]
  public interface IRegistrar
  {
    [return: Argument("menu")]
    string GetMenuForWindow(uint windowId, ObjectPath menuObjectPath);

    [return: Argument("menus")]
    object GetMenus();

    void RegisterWindow(uint windowId, ObjectPath menuObjectPath);

    void UnregisterWindow(uint windowId);

    event WindowRegisteredHandler WindowRegistered;

    event WindowUnregisteredHandler WindowUnregistered;
  }

  public delegate void WindowRegisteredHandler(uint windowId,
                                               string service,
                                               ObjectPath menuObjectPath);

  public delegate void WindowUnregisteredHandler(uint windowId);

}

