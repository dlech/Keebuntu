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

using AppIndicator;
using ImageMagick.MagickCore;
using ImageMagick.MagickWand;
using KeePass.Plugins;
using KeePassLib;
using Keebuntu.DBus;
using KeePassLib.Utility;

namespace KeebuntuAppIndicator
{
  public class KeebuntuAppIndicatorExt : Plugin
  {
    static int instanceCount = 0;

    IPluginHost pluginHost;
    ApplicationIndicator indicator;
    Gtk.Menu appIndicatorMenu;
    GLib.Signal aboutToShowSignal;
    bool activateWorkaroundNeeded;
    System.Windows.Forms.Timer activateWorkaroundTimer;

    public override bool Initialize(IPluginHost host)
    {
      pluginHost = host;

      // purposely break the winforms tray icon so it is not displayed
      var mainWindowType = pluginHost.MainWindow.GetType();
      var ntfTrayField = mainWindowType.GetField("m_ntfTray",
                               BindingFlags.Instance | BindingFlags.NonPublic);
      var ntfField = ntfTrayField.FieldType.GetField("m_ntf",
                               BindingFlags.Instance | BindingFlags.NonPublic);
      ntfField.SetValue(ntfTrayField.GetValue(pluginHost.MainWindow), null);

      activateWorkaroundTimer = new System.Windows.Forms.Timer();
      activateWorkaroundTimer.Interval = 100;
      activateWorkaroundTimer.Tick += OnActivateWorkaroundTimerTick;

      var threadStarted = false;
      try {
        DBusBackgroundWorker.Request();
        threadStarted = true;
        DBusBackgroundWorker.InvokeGtkThread((Action)GtkDBusInit).Wait();

        pluginHost.MainWindow.Activated += MainWindow_Activated;
        pluginHost.MainWindow.Resize += MainWindow_Resize;
      } catch (Exception ex) {
        MessageService.ShowWarning(
          "KeebuntuAppIndicator plugin failed to start.",
          ex.ToString());
        if (threadStarted) {
          Terminate();
        }
        return false;
      }
      return true;
    }

    public override void Terminate()
    {
      pluginHost.MainWindow.Activated += MainWindow_Activated;
      pluginHost.MainWindow.Resize -= MainWindow_Resize;
      if (aboutToShowSignal != null) {
        aboutToShowSignal.RemoveDelegate((EventHandler)OnAppIndicatorMenuShown);
      }
      if (appIndicatorMenu != null) {
        appIndicatorMenu.Shown -= OnAppIndicatorMenuShown;
      }
      try {
        DBusBackgroundWorker.Release();
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
      }
    }

    void MainWindow_Activated(object sender, EventArgs e)
    {
      if (activateWorkaroundNeeded) {
        // see explanation in OnActivateWorkaroundTimerTick()
        activateWorkaroundTimer.Start();
        activateWorkaroundNeeded = false;
      }
    }

    void MainWindow_Resize(object sender, EventArgs e)
    {
      if (!pluginHost.MainWindow.Visible &&
          pluginHost.MainWindow.WindowState ==
          System.Windows.Forms.FormWindowState.Minimized)
      {
        // see explanation in OnActivateWorkaroundTimerTick()
        activateWorkaroundNeeded = true;
      }
    }

    private void OnAppIndicatorMenuShown(object sender, EventArgs e)
    {
      try {
        var mainWindowType = pluginHost.MainWindow.GetType();
        var onCtxTrayOpeningMethodInfo =
          mainWindowType.GetMethod("OnCtxTrayOpening",
                                    System.Reflection.BindingFlags.Instance |
                                      System.Reflection.BindingFlags.NonPublic,
                                    null,
                                    new[] {
                                      typeof(object),
                                      typeof(CancelEventArgs)
                                    },
                                    null);
        if (onCtxTrayOpeningMethodInfo != null) {
          DBusBackgroundWorker.InvokeWinformsThread
            (() => onCtxTrayOpeningMethodInfo.Invoke(pluginHost.MainWindow,
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

    /// <summary>
    /// Initalize Gtk and DBus stuff
    /// </summary>
    private void GtkDBusInit()
    {
      /* setup ApplicationIndicator */

      indicator =
        new ApplicationIndicator("keepass2-plugin-appindicator" + instanceCount++,
                                 "keepass2-locked",
                                 AppIndicator.Category.ApplicationStatus);
#if DEBUG
      indicator.IconThemePath = Path.GetFullPath("Resources/icons");
#endif
      indicator.Title = PwDefs.ProductName;
      indicator.Status = AppIndicator.Status.Active;

      appIndicatorMenu = new Gtk.Menu();

      var trayContextMenu = pluginHost.MainWindow.TrayContextMenu;
      // make copy of item list to prevent list changed exception when iterating
      var menuItems =
        new System.Windows.Forms.ToolStripItem[trayContextMenu.Items.Count];
      trayContextMenu.Items.CopyTo(menuItems, 0);
      trayContextMenu.ItemAdded += (sender, e) =>
        DBusBackgroundWorker.InvokeGtkThread
          (() => ConvertAndAddMenuItem(e.Item, appIndicatorMenu));

      foreach (System.Windows.Forms.ToolStripItem item in menuItems) {
        ConvertAndAddMenuItem(item, appIndicatorMenu);
      }

      indicator.Menu = appIndicatorMenu;
      try {
        // This is a hack to get the about-to-show event from the dbusmenu
        // that is created by the appindicator.
        var getPropertyMethod =
          typeof(GLib.Object).GetMethod("GetProperty",
                                        BindingFlags.NonPublic | BindingFlags.Instance);
        var dbusMenuServer =
          (GLib.Value)getPropertyMethod.Invoke(indicator,
                                               new object[] { "dbus-menu-server" });
        var rootNode =
          (GLib.Value)getPropertyMethod.Invoke(dbusMenuServer.Val,
                                               new object[] { "root-node" });
        aboutToShowSignal =
          GLib.Signal.Lookup((GLib.Object)rootNode.Val, "about-to-show");
        aboutToShowSignal.AddDelegate((EventHandler)OnAppIndicatorMenuShown);
      } catch (Exception ex) {
        Debug.Fail(ex.Message);
        // On desktops that don't support application indicators, libappinidicator
        // creates a fallback GtkStatusIcon. This event only fires in that case.
        appIndicatorMenu.Shown += OnAppIndicatorMenuShown;
      }

      // when mouse cursor is over application indicator, scroll up will untray
      // and scroll down will tray KeePass
      indicator.ScrollEvent += (o, args) =>
      {
        // Workaround for bug in libappindicator
        // https://bugs.launchpad.net/ubuntu/+source/libappindicator/+bug/1071738
        Gdk.ScrollDirection scrollDirection;
        try {
          // this works for versions >= 12.10.1+13.10.20130920-0ubuntu4.1
          scrollDirection = (Gdk.ScrollDirection)args.Args[1];
        } catch (InvalidCastException) {
          // older versions will throw exception because the old type was uint
          var scrollDirectionUint = (uint)args.Args[1];
          scrollDirection = (Gdk.ScrollDirection)scrollDirectionUint;
        }

        var trayMenuItem = trayContextMenu.Items["m_ctxTrayTray"];
        if (trayMenuItem.Enabled && (scrollDirection == Gdk.ScrollDirection.Up ^
                                     pluginHost.MainWindow.Visible ))
        {
          DBusBackgroundWorker.InvokeWinformsThread
            ((Action)trayMenuItem.PerformClick);
        }
      };
    }

    private void ConvertAndAddMenuItem(System.Windows.Forms.ToolStripItem item,
                                       Gtk.MenuShell gtkMenuShell)
    {
      if (item is System.Windows.Forms.ToolStripMenuItem) {

        var winformMenuItem = item as System.Windows.Forms.ToolStripMenuItem;

        // windows forms use '&' for mneumonic, gtk uses '_'
        var gtkMenuItem = new Gtk.ImageMenuItem(winformMenuItem.Text.Replace("&", "_"));

        if (winformMenuItem.Image != null) {
          MemoryStream memStream;
          var image = winformMenuItem.Image;
          if (image.Width != 16 || image.Height != 16) {
            var newImage = ResizeImage(image, 16, 16);
            memStream = new MemoryStream(newImage);
          } else {
            memStream = new MemoryStream();
            image.Save(memStream, ImageFormat.Png);
            memStream.Position = 0;
          }
          gtkMenuItem.Image = new Gtk.Image(memStream);
        }

        gtkMenuItem.TooltipText = winformMenuItem.ToolTipText;
        gtkMenuItem.Visible = winformMenuItem.Visible;
        gtkMenuItem.Sensitive = winformMenuItem.Enabled;

        gtkMenuItem.Activated += (sender, e) =>
          DBusBackgroundWorker.InvokeWinformsThread((Action)winformMenuItem.PerformClick);

        winformMenuItem.TextChanged +=
          (sender, e) => DBusBackgroundWorker.InvokeGtkThread(() =>
        {
          var label = gtkMenuItem.Child as Gtk.Label;
          if (label != null) {
            label.Text = winformMenuItem.Text;
          }
        }
        );
        winformMenuItem.EnabledChanged += (sender, e) =>
          DBusBackgroundWorker.InvokeGtkThread
            (() => gtkMenuItem.Sensitive = winformMenuItem.Enabled);
        winformMenuItem.VisibleChanged += (sender, e) =>
          DBusBackgroundWorker.InvokeGtkThread
            (() => gtkMenuItem.Visible = winformMenuItem.Visible);

        gtkMenuItem.Show();
        gtkMenuShell.Insert(gtkMenuItem,
                            winformMenuItem.Owner.Items.IndexOf(winformMenuItem));

        if (winformMenuItem.HasDropDownItems) {
          var subMenu = new Gtk.Menu();
          foreach(System.Windows.Forms.ToolStripItem dropDownItem in
                  winformMenuItem.DropDownItems)
          {
            ConvertAndAddMenuItem (dropDownItem, subMenu);
          }
          gtkMenuItem.Submenu = subMenu;

          winformMenuItem.DropDown.ItemAdded += (sender, e) =>
            DBusBackgroundWorker.InvokeGtkThread
              (() => ConvertAndAddMenuItem (e.Item, subMenu));
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

      activateWorkaroundTimer.Stop();
      DBusBackgroundWorker.InvokeWinformsThread
        ((Action)pluginHost.MainWindow.Activate);
    }

    private byte[] ResizeImage(System.Drawing.Image image, int width, int height)
    {
      var stream = new MemoryStream();
      image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
      try {
        var wand = new MagickWand();
        wand.ReadImageBlob(stream.ToArray());
        wand.ResizeImage(width, height, FilterType.Mitchell, 0.5);
        return wand.GetImageBlob();
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
      }
      return stream.ToArray();
    }
  }
}
