using System;
using System.Diagnostics;
using System.Threading;
using System.Drawing.Imaging;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using AppIndicator;
using KeePass.Plugins;
using KeePassLib;
using Keebuntu.Dbus;

namespace KeebuntuAppIndicator 
{
  public class KeebuntuAppIndicatorExt : Plugin
  {
    private IPluginHost mPluginHost;
    private Thread mGtkThread;
    private ApplicationIndicator mIndicator;
    private Gtk.Menu mAppIndicatorMenu;
    private bool mActiveateWorkaroundNeeded;
    private System.Windows.Forms.Timer mActivateWorkaroundTimer;
    private object mStartupThreadLock = new object();

    public override bool Initialize(IPluginHost host)
    {
      mPluginHost = host;
      mActivateWorkaroundTimer = new System.Windows.Forms.Timer();
      mActivateWorkaroundTimer.Interval = 100;
      mActivateWorkaroundTimer.Tick += OnActivateWorkaroundTimerTick;

      try {
        mGtkThread = new Thread(RunGtkDBusThread);
        mGtkThread.SetApartmentState(ApartmentState.STA);
        mGtkThread.Name = "KeebuntuAppIndicator GTK/DBus Thread";
        lock(mStartupThreadLock) {
          mGtkThread.Start();
          if (!Monitor.Wait(mStartupThreadLock, 5000)) {
            mGtkThread.Abort();
            throw new Exception("KeebuntuAppIndicator Gtk/DBus Thread failed to start");
          }
        }

        mPluginHost.MainWindow.Activated += (sender, e) =>
        {
          if (mActiveateWorkaroundNeeded) {
            // see explanation in OnActivateWorkaroundTimerTick()
            mActivateWorkaroundTimer.Start();
            mActiveateWorkaroundNeeded = false;
          }
        };

        mPluginHost.MainWindow.Resize += (sender, e) =>
        {
          if (!mPluginHost.MainWindow.Visible &&
              mPluginHost.MainWindow.WindowState ==
              System.Windows.Forms.FormWindowState.Minimized)
          {
            // see explanation in OnActivateWorkaroundTimerTick()
            mActiveateWorkaroundNeeded = true;
          }
        };

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
          InvokeMainWindowAsync(
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

    private void RunGtkDBusThread()
    {
      try {
        lock (mStartupThreadLock) {
          DBus.BusG.Init();
          Gtk.Application.Init();

          /* setup ApplicationIndicator */

          mIndicator = new ApplicationIndicator("keepass2-plugin-appindicator",
                                                "keepass2-locked",
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

          var sessionBus = DBus.Bus.Session;

#if DEBUG
          var dbusBusPath = "/org/freedesktop/DBus";
          var dbusBusName = "org.freedesktop.DBus";
          var dbusObjectPath = new DBus.ObjectPath(dbusBusPath);
          var dbusService =
            sessionBus.GetObject<org.freedesktop.DBus.IBus>(dbusBusName, dbusObjectPath);
          dbusService.NameAcquired += (name) => Console.WriteLine ("NameAcquired: " + name);
#endif

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

          Monitor.Pulse(mStartupThreadLock);
        }

        /* run gtk event loop */
        Gtk.Application.Run();

        /* cleanup */

        mAppIndicatorMenu.Shown -= OnAppIndicatorMenuShown;
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
      }
    }

    private void InvokeMainWindowAsync(Action action)
    {
      var mainWindow = mPluginHost.MainWindow;
      if (mainWindow.InvokeRequired) {
        mainWindow.BeginInvoke(action);
      } else {
        action.BeginInvoke(null, null);
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
          InvokeMainWindowAsync(winformMenuItem.PerformClick);

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

    private void OnActivateWorkaroundTimerTick(object sender, EventArgs e)
    {
      // There seems to be a bug? in Mono where if you change Visible from
      // false to true and WindowState from Minimized to !Minimized and then call
      // Activate() all in the same method call, then the icon in the launcher
      // is resored, but the window is not shown. To work around this, we use a
      // timer to invoke Activate() a second time to get the window to show.

      mActivateWorkaroundTimer.Stop();
      InvokeMainWindowAsync(() => mPluginHost.MainWindow.Activate());
    }
  }
}
