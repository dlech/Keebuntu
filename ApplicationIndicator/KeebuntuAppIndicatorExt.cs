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
using ImageMagick.MagickCore;
using ImageMagick.MagickWand;
using Keebuntu.DBus;

namespace KeebuntuAppIndicator
{
  public class KeebuntuAppIndicatorExt : Plugin
  {
    private static int instanceCount = 0;

    private IPluginHost mPluginHost;
    private ApplicationIndicator mIndicator;
    private Gtk.Menu mAppIndicatorMenu;
    private bool mActiveateWorkaroundNeeded;
    private System.Windows.Forms.Timer mActivateWorkaroundTimer;

    public override bool Initialize(IPluginHost host)
    {
      mPluginHost = host;
      mActivateWorkaroundTimer = new System.Windows.Forms.Timer();
      mActivateWorkaroundTimer.Interval = 100;
      mActivateWorkaroundTimer.Tick += OnActivateWorkaroundTimerTick;

      var threadStarted = false;
      try {
        DBusBackgroundWorker.Start();
        threadStarted = true;
        DBusBackgroundWorker.InvokeGtkThread(() => GtkDBusInit());

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
        if (threadStarted) {
          Terminate();
        }
        return false;
      }
      return true;
    }

    public override void Terminate()
    {
      try {
        DBusBackgroundWorker.Stop();
        // Mono tends to lock up sometimes when trying to hide/remove the
        // notification icon on shutdown (the System.Windows.Forms.NotifyIcon,
        // not our ApplicationIndicator). We fake the private variable so
        // that mono does not call the HideSystray() method since it is not
        // shown anyway.
        var notifyIconType = mPluginHost.MainWindow.MainNotifyIcon.GetType();
        var notifyIconVisibleField =
          notifyIconType.GetField("visible", BindingFlags.Instance |
                                             BindingFlags.NonPublic);
        notifyIconVisibleField.SetValue(mPluginHost.MainWindow.MainNotifyIcon,
                                        false);
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
          DBusBackgroundWorker.InvokeWinformsThread
            (() => onCtxTrayOpeningMethodInfo.Invoke(mPluginHost.MainWindow,
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

      mIndicator =
        new ApplicationIndicator("keepass2-plugin-appindicator" + instanceCount++,
                                 "keepass2-locked",
                                 AppIndicator.Category.ApplicationStatus);
#if DEBUG
      mIndicator.IconThemePath = Path.GetFullPath("Resources/icons");
#endif
      mIndicator.Title = PwDefs.ProductName;
      mIndicator.Status = AppIndicator.Status.Active;

      mAppIndicatorMenu = new Gtk.Menu();

      var trayContextMenu = mPluginHost.MainWindow.TrayContextMenu;
      // make copy of item list to prevent list changed exception when iterating
      var menuItems =
        new System.Windows.Forms.ToolStripItem[trayContextMenu.Items.Count];
      trayContextMenu.Items.CopyTo(menuItems, 0);
      trayContextMenu.ItemAdded += (sender, e) =>
        DBusBackgroundWorker.InvokeGtkThread
          (() => ConvertAndAddMenuItem(e.Item, mAppIndicatorMenu));

      foreach (System.Windows.Forms.ToolStripItem item in menuItems) {
        ConvertAndAddMenuItem(item, mAppIndicatorMenu);
      }

      // might not be needed since we are monitoring via dbus too
      mAppIndicatorMenu.Shown += OnAppIndicatorMenuShown;

      mIndicator.Menu = mAppIndicatorMenu;

      // when mouse cursor is over application indicator, scroll up will untray
      // and scroll down will tray KeePass
      mIndicator.ScrollEvent += (o, args) =>
      {
        /* Workaround for bug in mono/appindicator-sharp.
         *
         * args.Direction throws InvalidCastException
         * Can't cast args.Arg[1] to Gdk.ScrollDirection for some reason, so we
         * have to cast to uint first (that is the underlying data type) and
         * then cast to Gdk.ScrollDirection
         */
        var scrollDirectionUint = (uint)args.Args[1];
        var scrollDirection = (Gdk.ScrollDirection)scrollDirectionUint;

        var trayMenuItem = trayContextMenu.Items["m_ctxTrayTray"];
        if (trayMenuItem.Enabled && (scrollDirection == Gdk.ScrollDirection.Up ^
                                     mPluginHost.MainWindow.Visible ))
        {
          DBusBackgroundWorker.InvokeWinformsThread
            (() => trayMenuItem.PerformClick());
        }
      };

      var sessionBus = DBus.Bus.Session;

#if DEBUG
      const string dbusBusPath = "/org/freedesktop/DBus";
      const string dbusBusName = "org.freedesktop.DBus";
      var dbusObjectPath = new DBus.ObjectPath(dbusBusPath);
      var dbusService =
        sessionBus.GetObject<org.freedesktop.DBus.IBus>(dbusBusName, dbusObjectPath);
      dbusService.NameAcquired += (name) => Console.WriteLine ("NameAcquired: " + name);
#endif

      /* ApplicationIndicator dbus */

      const string panelServiceBusName = "com.canonical.Unity.Panel.Service";
      const string panelServiceBusPath = "/com/canonical/Unity/Panel/Service";
      var panelServiceObjectPath = new DBus.ObjectPath(panelServiceBusPath);
      var panelService =
        sessionBus.GetObject<com.canonical.Unity.Panel.IService>(panelServiceBusName,
                                                                 panelServiceObjectPath);
      // TODO - this could be improved by filtering on entry_id == ?
      panelService.EntryActivated += (entry_id, entry_geometry) =>
        OnAppIndicatorMenuShown(this, new EventArgs());
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
          DBusBackgroundWorker.InvokeWinformsThread(winformMenuItem.PerformClick);

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

      mActivateWorkaroundTimer.Stop();
      DBusBackgroundWorker.InvokeWinformsThread
        (() => mPluginHost.MainWindow.Activate());
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
