using System;
using System.Diagnostics;
using System.Threading;
using System.Drawing.Imaging;
using AppIndicator;
using KeePass.Plugins;
using System.IO;
using System.ComponentModel;
using KeePassLib;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Keebuntu 
{
  public class KeebuntuExt : Plugin
  {
    private IPluginHost mPluginHost;
    private Thread mGtkThread;
    private ApplicationIndicator mIndicator;
    private Gtk.Menu mAppIndicatorMenu;
    private WinformsDBusMenu mDBusMenu;

    public override bool Initialize(IPluginHost host)
    {
      mPluginHost = host;
      try {
        mGtkThread = new Thread(RunGtkApp);
        mGtkThread.SetApartmentState(ApartmentState.STA);
        mGtkThread.Name = "GTK Thread";
        mGtkThread.Start();
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
        return false;
      }
      return true;
    }

    public override void Terminate()
    {
      try {
        InvokeGtkThread(() => Gtk.Application.Quit());
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
      }
    }

    private void OnAppIndicatorMenuShown(object sender, EventArgs e)
    {
      try {
        var mainWindowType = mPluginHost.MainWindow.GetType();
        var onCtxTrayOpeningMethodInfo =
          mainWindowType.GetMethod("OnCtxTrayOpening",
                                    System.Reflection.BindingFlags.Instance |
                                      System.Reflection.BindingFlags.NonPublic,
                                    Type.DefaultBinder,
                                    new[] {
                                      typeof(object),
                                      typeof(CancelEventArgs)
                                    },
                                      null);
        if (onCtxTrayOpeningMethodInfo != null) {
          InvokeMainWindow(
            () => onCtxTrayOpeningMethodInfo.Invoke(mPluginHost.MainWindow,
                                                     new[] {
                                                       sender,
                                                       new CancelEventArgs()
                                                     }
          )
          );
        }
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
      }
    }

    private void RunGtkApp()
    {
      DBus.BusG.Init();
      Gtk.Application.Init();

      /* setup ApplicationIndicator */

      mIndicator = new ApplicationIndicator("keepass-appindicator-plugin",
                                            "keepass2",
                                            AppIndicator.Category.ApplicationStatus);
#if DEBUG
      mIndicator.IconThemePath = Path.GetFullPath("Resources/icons");
#endif
      mIndicator.Title = PwDefs.ProductName;
      mIndicator.Status = AppIndicator.Status.Active;
      
      mAppIndicatorMenu = new Gtk.Menu();
      var trayContextMenu = mPluginHost.MainWindow.TrayContextMenu;
      foreach (System.Windows.Forms.ToolStripItem item in trayContextMenu.Items) {
        ConvertAndAddMenuItem(item, mAppIndicatorMenu);
      }
      trayContextMenu.ItemAdded += (sender, e) =>
        InvokeGtkThread(() => ConvertAndAddMenuItem(e.Item, mAppIndicatorMenu));

      // might not be needed since we are monitoring via dbus too
      mAppIndicatorMenu.Shown += OnAppIndicatorMenuShown;

      mIndicator.Menu = mAppIndicatorMenu;

      /* setup ApplicationMenu */

      mDBusMenu = new WinformsDBusMenu(mPluginHost.MainWindow.MainMenu);

      var sessionBus = DBus.Bus.Session;

#if DEBUG
      var dbusBusPath = "/org/freedesktop/DBus";
      var dbusBusName = "org.freedesktop.DBus";
      var dbusObjectPath = new DBus.ObjectPath(dbusBusPath);
      var dbusService =
        sessionBus.GetObject<org.freedesktop.DBus.IBus>(dbusBusName, dbusObjectPath);
      dbusService.NameAcquired += (name) => Console.WriteLine ("NameAcquired: " + name);
#endif
      var busPath = "/com/canonical/AppMenu/Registrar";
      var busName = "com.canonical.AppMenu.Registrar";
      var objPath = new DBus.ObjectPath(busPath);
      var unityPanelServiceBus =
        sessionBus.GetObject<com.canonical.AppMenu.Registrar>(busName, objPath);

      // TODO - extract getting xid to separate function
      var typeName = typeof(System.Windows.Forms.Control).AssemblyQualifiedName;
      var hwndTypeName = typeName.Replace("Control", "Hwnd");
      var hwndType = Type.GetType(hwndTypeName);
      var objectFromHandleMethod =
        hwndType.GetMethod("ObjectFromHandle", BindingFlags.Public | BindingFlags.Static);
      var hwnd =
        objectFromHandleMethod.Invoke(null, new object[] { mPluginHost.MainWindow.Handle });
      var wholeWindowField = hwndType.GetField("whole_window",
                                               BindingFlags.NonPublic | BindingFlags.Instance);
      var xid = (IntPtr)wholeWindowField.GetValue(hwnd);

      var menuPath = "/com/canonical/menu/{0}";
      var windowObjectPath = new DBus.ObjectPath(string.Format(menuPath, xid));
      sessionBus.Register(windowObjectPath, mDBusMenu);
      unityPanelServiceBus.RegisterWindow((uint)xid.ToInt32(), windowObjectPath);

      /* ApplicationIndicator dbus */

      var panelServiceBusName = "com.canonical.Unity.Panel.Service";
      var panelServiceBusPath = "/com/canonical/Unity/Panel/Service";
      var panelServiceObjectPath = new DBus.ObjectPath(panelServiceBusPath);
      var panelService =
        sessionBus.GetObject<com.canonical.Unity.Panel.IService>(panelServiceBusName,
                                                                 panelServiceObjectPath);
      // TODO - this could be improved by filtering on entry_id == ?
      panelService.EntryActivated += (entry_id, entry_geometry) =>
        OnAppIndicatorMenuShown(this, new EventArgs());

      /* run gtk event loop */

      Gtk.Application.Run();

      /* cleanup */

      mAppIndicatorMenu.Shown -= OnAppIndicatorMenuShown;
      sessionBus.Unregister(windowObjectPath);
    }

    private void InvokeMainWindow(Action action)
    {
      var mainWindow = mPluginHost.MainWindow;
      if (mainWindow.InvokeRequired) {
        mainWindow.Invoke(action);
      } else {
        action.Invoke();
      }
    }

    private void InvokeGtkThread(Action action)
    {
      if (ReferenceEquals(Thread.CurrentThread, mGtkThread)) {
        action.Invoke();
      } else {
        Gtk.ReadyEvent readyEvent = () => action.Invoke();
        var threadNotify = new Gtk.ThreadNotify(readyEvent);
        threadNotify.WakeupMain();
      }
    }

    private void ConvertAndAddMenuItem(System.Windows.Forms.ToolStripItem item,
                                       Gtk.MenuShell gtkMenuShell)
    {
      if (item is System.Windows.Forms.ToolStripMenuItem) {

        var winformMenuItem = item as System.Windows.Forms.ToolStripMenuItem;

        // windows forms use & for mneumonic, gtk uses _
        var gtkMenuItem = new Gtk.ImageMenuItem(winformMenuItem.Text.Replace("&", "_"));

        if (winformMenuItem.Image != null) {
          var memStream = new MemoryStream();
          winformMenuItem.Image.Save(memStream, ImageFormat.Png);
          memStream.Position = 0;
          gtkMenuItem.Image = new Gtk.Image(memStream);
        }

        gtkMenuItem.TooltipText = winformMenuItem.ToolTipText;
        gtkMenuItem.Visible = winformMenuItem.Visible;
        gtkMenuItem.Sensitive = winformMenuItem.Enabled;

        gtkMenuItem.Activated += (sender, e) =>
          InvokeMainWindow(winformMenuItem.PerformClick);

        winformMenuItem.TextChanged += (sender, e) => InvokeGtkThread(() =>
        {
          var label = gtkMenuItem.Child as Gtk.Label;
          if (label != null) {
            label.Text = winformMenuItem.Text;
          }
        }
        );
        winformMenuItem.EnabledChanged += (sender, e) =>
          InvokeGtkThread(() => gtkMenuItem.Sensitive = winformMenuItem.Enabled);
        winformMenuItem.VisibleChanged += (sender, e) =>
          InvokeGtkThread(() => gtkMenuItem.Visible = winformMenuItem.Visible);

        gtkMenuItem.Show();
        gtkMenuShell.Insert(gtkMenuItem, winformMenuItem.Owner.Items.IndexOf(winformMenuItem));


        if (winformMenuItem.HasDropDownItems) {
          var subMenu = new Gtk.Menu();
          foreach(System.Windows.Forms.ToolStripItem dropDownItem in
                  winformMenuItem.DropDownItems)
          {
            ConvertAndAddMenuItem (dropDownItem, subMenu);
          }
          gtkMenuItem.Submenu = subMenu;

          winformMenuItem.DropDown.ItemAdded += (sender, e) =>
            InvokeGtkThread (() => ConvertAndAddMenuItem (e.Item, subMenu));
        }
      } else if (item is System.Windows.Forms.ToolStripSeparator) {
        var gtkSeparator = new Gtk.SeparatorMenuItem();
        gtkSeparator.Show ();
        gtkMenuShell.Insert(gtkSeparator, item.Owner.Items.IndexOf(item));
      } else {
        Debug.Fail("Unexpected menu item");
      }
    }
  }
}

