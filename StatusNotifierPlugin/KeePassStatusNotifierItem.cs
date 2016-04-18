using System;
using System.Collections.Generic;

using DBus;
using Keebuntu.DBus;
using KeePass.Plugins;
using org.kde.StatusNotifierItem;

namespace KeebuntuStatusNotifier
{
  public class KeePassStatusNotifierItem : IStatusNotifierItem
  {
    IPluginHost pluginHost;

    public KeePassStatusNotifierItem(IPluginHost pluginHost)
    {
      this.pluginHost = pluginHost;
    }

    // TODO - extract org.freedesktop.DBus.IProperties impementation to a base class
    #region org.freedesktop.DBus.IProperties

    public object Get(string @interface, string propname)
    {
      return GetPropertiesForInterface(@interface)[propname];
    }

    public IDictionary<string, object> GetAll(string @interface)
    {
      return GetPropertiesForInterface(@interface);
    }

    public void Set(string @interface, string propname, object value)
    {
      if (GetPropertiesForInterface(@interface)[propname] != value) {
        GetType().GetProperty(propname).SetValue(this, value, null);
      }
    }

    private IDictionary<string, object> GetPropertiesForInterface(string interfaceName)
    {
      var properties = new Dictionary<string, object>();
      foreach (var @interface in GetType().GetInterfaces()) {
        var interfaceAttributes =
          @interface.GetCustomAttributes(typeof(InterfaceAttribute), false);
        // TODO need to check this for DBus.InterfaceAttribute too.
        foreach (InterfaceAttribute attrib in interfaceAttributes) {
          if (string.IsNullOrWhiteSpace(interfaceName) || attrib.Name == interfaceName) {
            foreach (var property in @interface.GetProperties()) {
              // TODO - do we need to handle indexed properties?
              properties.Add(property.Name, property.GetValue(this, null));
            }
          }
        }
      }
      return properties;
    }

    #endregion

    public virtual string Category { get { return "ApplicationStatus"; } }

    public virtual string Id {
      get { return pluginHost.MainWindow.Name; }
    }

    public virtual string Title {
      get { return KeePass.Program.MainForm.Text; }
    }

    public virtual string Status { get { return "Active"; } }

    public virtual uint WindowId { get { return 0; } }

    public virtual bool ItemIsMenu { get { return false; } }

    public virtual string IconName { get { return "keepass2-locked"; } }

    //public virtual KDbusImageVector IconPixmap { get { return null; } }

    //public virtual string OverlayIconName { get { return null; } }

    //public virtual KDbusImageVector OverlayIconPixmap { get { return null; } }

    //public virtual string AttentionIconName { get { return null; } }

    //public virtual KDbusImageVector AttentionIconPixmap { get { return null; } }

    //public virtual string AttentionMovieName { get { return null; } }

//    public virtual Tooltip Tooltip {
//      get {
//        return new Tooltip() {
//          IconName = "keepass2-locked",
//          IconPixmap = KDbusImageVector.None,
//          Title = "title",
//          Description = "tip",
//        };
//      }
//    }

    public virtual void ContextMenu(int x, int y)
    {
      // TODO: show a menu here
      throw new NotImplementedException();
    }

    public virtual void Activate(int x, int y)
    {
      DBusBackgroundWorker.InvokeWinformsThread(() => {
        if (pluginHost.MainWindow.Visible) {
          pluginHost.MainWindow.Hide();
        } else {
          pluginHost.MainWindow.EnsureVisibleForegroundWindow(true, true);
        }
      });
    }

    public virtual void SecondaryActivate(int x, int y)
    {
      throw new NotImplementedException();
    }

    public virtual void Scroll(int delta, string orientation)
    {
      throw new NotImplementedException();
    }

    public virtual event Action NewTitle;

    public virtual event Action NewIcon;

    //public virtual event Action NewAttentionIcon;

    //public virtual event Action NewOverlayIcon;

    //public virtual event Action NewToolTip;

    public virtual event Action<string> NewStatus;

    protected void OnNewTitle()
    {
      if (NewTitle != null) {
        NewTitle.Invoke();
      }
    }

    protected void OnNewIcon()
    {
      if (NewIcon != null) {
        NewIcon.Invoke();
      }
    }

    protected void OnNewStatus(string status)
    {
      if (NewStatus != null) {
        NewStatus.Invoke(status);
      }
    }
  }
}
