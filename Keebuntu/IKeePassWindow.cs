using System;
using DBus;
using System.Collections.Generic;

namespace Keebuntu
{
  /// <summary>
  /// Interface for dbus
  /// </summary>
  [Interface("org.dlech.Keebuntu")]
  public interface IKeePassWindow
  {
    Dictionary<string, string> GetMenuForWindow();
  }
}

