using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using ImageMagick.MagickCore;
using ImageMagick.MagickWand;
using KeePass.Plugins;
using KeePassLib;
using Keebuntu.DBus;
using Unity;
using Dbusmenu;

namespace KeebuntuUnityLauncher
{
  public class KeebuntuUnityLauncherExt : Plugin
  {
    IPluginHost pluginHost;
    LauncherEntry launcher;
    System.Windows.Forms.Timer updateUITimer;
    System.Windows.Forms.Timer finishInitDelaytimer;

    public override bool Initialize(IPluginHost host)
    {
      pluginHost = host;
      updateUITimer = new System.Windows.Forms.Timer();
      updateUITimer.Interval = 500;
      updateUITimer.Tick += On_updateUITimer_Tick;
      finishInitDelaytimer = new System.Windows.Forms.Timer();

      var threadStarted = false;
      try {
        DBusBackgroundWorker.Request();
        threadStarted = true;
        DBusBackgroundWorker.InvokeGtkThread((Action)GtkDBusInit).Wait();
        pluginHost.MainWindow.UIStateUpdated += On_MainWindow_UIStateUpdated;
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
        if (threadStarted) {
          Terminate();
        }
        return false;
      }
      return true;
    }

    public override void Terminate()
    {
      pluginHost.MainWindow.UIStateUpdated -= On_MainWindow_UIStateUpdated;
      try {
        DBusBackgroundWorker.Release();
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
      }
    }

    void On_MainWindow_UIStateUpdated(object sender, System.EventArgs e)
    {
      // Calling OnCtyTrayOpening triggers a UIStateUpdated event, so we use
      // a timer to throttle calls.
      updateUITimer.Start();
    }

    void On_updateUITimer_Tick(object sender, System.EventArgs e)
    {
      try {
        var mainWindowType = pluginHost.MainWindow.GetType();
        var onCtxTrayOpeningMethodInfo =
          mainWindowType.GetMethod("OnCtxTrayOpening",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic, null,
            new[] { typeof(object), typeof(CancelEventArgs) }, null);
        if (onCtxTrayOpeningMethodInfo != null) {
          DBusBackgroundWorker.InvokeWinformsThread(() =>
            onCtxTrayOpeningMethodInfo.Invoke(pluginHost.MainWindow,
            new[] { sender, new CancelEventArgs() }));
        }
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
      } finally {
        updateUITimer.Stop();
      }
    }

    /// <summary>
    /// Initalize Gtk stuff
    /// </summary>
    private void GtkDBusInit()
    {
      launcher = LauncherEntry.GetForDesktopId("keepass2.desktop");
      var rootMenuItem = new Dbusmenu.Menuitem();
      var trayContextMenu = pluginHost.MainWindow.TrayContextMenu;
      // make copy of item list to prevent list changed exception when iterating
      var menuItems =
        new System.Windows.Forms.ToolStripItem[trayContextMenu.Items.Count];
      trayContextMenu.Items.CopyTo(menuItems, 0);
      trayContextMenu.ItemAdded += (sender, e) =>
        DBusBackgroundWorker.InvokeGtkThread(() =>
          ConvertAndAddMenuItem(e.Item, rootMenuItem));
      foreach (System.Windows.Forms.ToolStripItem item in menuItems) {
        if (item.Name == "m_ctxTrayTray" || item.Name == "m_ctxTrayFileExit")
          continue;
        ConvertAndAddMenuItem(item, rootMenuItem);
      }
      // the launcher may not be listening yet, so we delay setting the properties
      // to give it extra time
      finishInitDelaytimer.Tick += (sender, e) =>
      {
        finishInitDelaytimer.Stop();
        DBusBackgroundWorker.InvokeGtkThread(() =>
          launcher.Quicklist = rootMenuItem);
      };
      finishInitDelaytimer.Interval = 1000;
      finishInitDelaytimer.Start();
    }

    private void ConvertAndAddMenuItem(System.Windows.Forms.ToolStripItem item,
      Dbusmenu.Menuitem parent)
    {
      if (item is System.Windows.Forms.ToolStripMenuItem) {

        var winformMenuItem = item as System.Windows.Forms.ToolStripMenuItem;

        var dbusMenuItem = new Dbusmenu.Menuitem();
        dbusMenuItem.PropertySet("label", winformMenuItem.Text.Replace("&", ""));
        // VisibleChanged does not seem to be firing, so make everything visible for now
        //dbusMenuItem.PropertySetBool("visible", winformMenuItem.Visible);
        dbusMenuItem.PropertySetBool("enabled", winformMenuItem.Enabled);

        dbusMenuItem.ItemActivated += (sender, e) =>
          DBusBackgroundWorker.InvokeWinformsThread((Action)winformMenuItem.PerformClick);

        winformMenuItem.TextChanged +=
          (sender, e) => DBusBackgroundWorker.InvokeGtkThread(() =>
            dbusMenuItem.PropertySet("label", winformMenuItem.Text.Replace("&", "")));
        winformMenuItem.EnabledChanged += (sender, e) =>
          DBusBackgroundWorker.InvokeGtkThread(() =>
            dbusMenuItem.PropertySetBool("enabled", winformMenuItem.Enabled));
        winformMenuItem.VisibleChanged += (sender, e) =>
          DBusBackgroundWorker.InvokeGtkThread(() =>
            dbusMenuItem.PropertySetBool("visible", winformMenuItem.Visible));

        parent.ChildAppend(dbusMenuItem);
      } else if (item is System.Windows.Forms.ToolStripSeparator) {
        // Ignore separator for now because there are too many of them
//        var dbusMenuItem = new DbusmenuMenuitem();
//        dbusMenuItem.PropertySet("type", "separator");
//        parent.ChildAppend(dbusMenuItem);
      } else {
        Debug.Fail("Unexpected menu item");
      }
    }
  }
}