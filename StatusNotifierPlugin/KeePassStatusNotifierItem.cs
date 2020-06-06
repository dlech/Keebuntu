using System;
using System.Collections.Generic;

using DBus;
using Keebuntu.DBus;
using KeePass.Plugins;
using org.kde.StatusNotifierItem;
using KeePassLib;

namespace KeebuntuStatusNotifier
{
  public class KeePassStatusNotifierItem : MenuStripDBusMenu, IStatusNotifierItem
  {
    IPluginHost pluginHost;
    ObjectPath menuPath;

    public KeePassStatusNotifierItem(IPluginHost pluginHost, ObjectPath menuPath)
      : base (pluginHost.MainWindow.TrayContextMenu, pluginHost.MainWindow)
    {
      this.pluginHost = pluginHost;
      this.menuPath = menuPath;
    }



    #region IStatusNotifierItem implementation

    public string Category { get { return "ApplicationStatus"; } }

    public string Id {
      get { return PwDefs.ShortProductName; }
    }

    public string Title {
      get { return KeePass.Program.MainForm.Text; }
    }

    string IStatusNotifierItem.Status { get { return "Active"; } }

    public uint WindowId { get { return 0; } }

    public bool ItemIsMenu { get { return false; } }

    public ObjectPath Menu { get { return menuPath; } }

    public string IconName { get { return "keepass2-locked"; } }

    //public KDbusImageVector IconPixmap { get { return null; } }

    // public string OverlayIconName { get { return null; } }

    //public KDbusImageVector OverlayIconPixmap { get { return null; } }

    //public string AttentionIconName { get { return null; } }

    //public KDbusImageVector AttentionIconPixmap { get { return null; } }

    //public string AttentionMovieName { get { return null; } }

//    public Tooltip Tooltip {
//      get {
//        return new Tooltip() {
//          IconName = "keepass2-locked",
//          IconPixmap = KDbusImageVector.None,
//          Title = "title",
//          Description = "tip",
//        };
//      }
//    }

    public void ContextMenu(int x, int y)
    {
      // This is not called since we have implemented the Menu property
      throw new NotImplementedException();
    }

    public void Activate(int x, int y)
    {
      DBusBackgroundWorker.InvokeWinformsThread(() => {
        if (pluginHost.MainWindow.Visible) {
          pluginHost.MainWindow.Hide();
        } else {
          pluginHost.MainWindow.EnsureVisibleForegroundWindow(true, true);
        }
      });
    }

    public void SecondaryActivate(int x, int y)
    {
      throw new NotImplementedException();
    }

    public void Scroll(int delta, string orientation)
    {
      throw new NotImplementedException();
    }

    public event Action NewTitle;

    public event Action NewIcon;

    //public event Action NewAttentionIcon;

    public event Action NewOverlayIcon;

    //public event Action NewToolTip;

    public event Action<string> NewStatus;

    #endregion

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

    protected void OnNewOverlayIcon()
    {
      if (NewOverlayIcon != null) {
        NewOverlayIcon.Invoke();
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
