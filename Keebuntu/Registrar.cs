using System;
using DBus;
using System.Collections.Generic;

namespace com.canonical.AppMenu
{
  /// <summary>
  /// Interface for dbus
  /// </summary>
  [Interface("com.canonical.AppMenu.Registrar")]
  public interface Registrar
  {
    string GetMenuForWindow(uint windowId, ObjectPath menuObjectPath);
      object GetMenus();
    void RegisterWindow(uint windowId, ObjectPath menuObjectPath);
    void UnregisterWindow(uint windowId);

    event WindowRegisteredHandler WindowRegistered;
    event WindowUnregisteredHandler WindowUnregistered;

  }

  public delegate void WindowRegisteredHandler(uint windowId, string service, ObjectPath menuObjectPath);
  public delegate void WindowUnregisteredHandler(uint windowId);

}

