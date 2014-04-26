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
using Keebuntu.Dbus;

namespace KeebuntuAppIndicator
{
  public class KeebuntuAppIndicatorExt : Plugin
  {
    static int instanceCount = 0;

    IPluginHost pluginHost;
    System.Drawing.Icon notifyIcon;
    ApplicationIndicator mIndicator;
    string mEntryId;
    Gtk.Menu mAppIndicatorMenu;
    bool mActiveateWorkaroundNeeded;
    System.Windows.Forms.Timer mActivateWorkaroundTimer;
    com.canonical.Unity.Panel.Service.IService mPanelService;
    com.canonical.Unity.Panel.Service.Desktop.IService mPanelService2;

    public override bool Initialize(IPluginHost host)
    {
      pluginHost = host;
      notifyIcon = pluginHost.MainWindow.MainNotifyIcon.Icon;
      // the prevents the System.Windows.Forms.NotifyIcon from being shown.
      pluginHost.MainWindow.MainNotifyIcon.Icon = null;
      mActivateWorkaroundTimer = new System.Windows.Forms.Timer();
      mActivateWorkaroundTimer.Interval = 100;
      mActivateWorkaroundTimer.Tick += OnActivateWorkaroundTimerTick;

      var threadStarted = false;
      try {
        DBusBackgroundWorker.Start();
        threadStarted = true;
        DBusBackgroundWorker.InvokeGtkThread(() => GtkDBusInit());

        pluginHost.MainWindow.Activated += (sender, e) =>
        {
          if (mActiveateWorkaroundNeeded) {
            // see explanation in OnActivateWorkaroundTimerTick()
            mActivateWorkaroundTimer.Start();
            mActiveateWorkaroundNeeded = false;
          }
        };

        pluginHost.MainWindow.Resize += (sender, e) =>
        {
          if (!pluginHost.MainWindow.Visible &&
              pluginHost.MainWindow.WindowState ==
              System.Windows.Forms.FormWindowState.Minimized)
          {
            // see explanation in OnActivateWorkaroundTimerTick()
            mActiveateWorkaroundNeeded = true;
          }
        };
        pluginHost.MainWindow.UIStateUpdated += MainWindow_UIStateUpdated;
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
      pluginHost.MainWindow.UIStateUpdated -= MainWindow_UIStateUpdated;
      pluginHost.MainWindow.MainNotifyIcon.Icon = notifyIcon;
      try {
        DBusBackgroundWorker.Stop();
        // Mono tends to lock up sometimes when trying to hide/remove the
        // notification icon on shutdown (the System.Windows.Forms.NotifyIcon,
        // not our ApplicationIndicator). We fake the private variable so
        // that mono does not call the HideSystray() method since it is not
        // shown anyway.
        var notifyIconType = pluginHost.MainWindow.MainNotifyIcon.GetType();
        var notifyIconVisibleField =
          notifyIconType.GetField("visible", BindingFlags.Instance |
                                             BindingFlags.NonPublic);
        notifyIconVisibleField.SetValue(pluginHost.MainWindow.MainNotifyIcon,
                                        false);
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
      }
    }

    void MainWindow_UIStateUpdated(object sender, EventArgs e)
    {
      // remove the tooltip from the notify icon because it causes issues
      // with Unity app menus
      pluginHost.MainWindow.MainNotifyIcon.Text = string.Empty;
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

      var trayContextMenu = pluginHost.MainWindow.TrayContextMenu;
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

      // This only works on non-Unity desktops
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
                                     pluginHost.MainWindow.Visible ))
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

      // This is a workaround for Unity. In Unity, the underlying GTK menu does
      // not trigger the Shown event when the menu is shown. We can simulate
      // this by using a private Unity API

      const string panelServiceBusName = "com.canonical.Unity.Panel.Service";
      const string panelServiceBusPath = "/com/canonical/Unity/Panel/Service";
      var panelServiceObjectPath = new DBus.ObjectPath(panelServiceBusPath);
      try {
        mPanelService = sessionBus
          .GetObject<com.canonical.Unity.Panel.Service.IService>(
            panelServiceBusName, panelServiceObjectPath);
        if (mPanelService != null) {
          mPanelService.EntryActivated += (entry_id, entry_geometry) => {
            if (mEntryId == null)
              mEntryId = mPanelService.Sync()
                .Where(args => args.name_hint == mIndicator.ID)
                .SingleOrDefault().id;
            else
              mEntryId = string.Empty;
            if (!String.IsNullOrEmpty(mEntryId) && mEntryId != entry_id)
              return;
            OnAppIndicatorMenuShown(this, new EventArgs());
          };
        }
      } catch (Exception) {
        // ignored
      }

      // Since this is a private API, Unity does not care about changing it,
      // which was done in trusty. So, the very similar looking code above
      // will work pre-trusty and this will work for trusty and beyond (until
      // the API is changed again).
      const string panelServiceDesktopBusName =
      "com.canonical.Unity.Panel.Service.Desktop";
      try {
        mPanelService2 = sessionBus
          .GetObject<com.canonical.Unity.Panel.Service.Desktop.IService>(
            panelServiceDesktopBusName, panelServiceObjectPath);
        if (mPanelService2 != null) {
          mPanelService2.EntryActivated += (panel_id, entry_id, entry_geometry) => {
            if (mEntryId == null)
              mEntryId = mPanelService2.Sync()
                .Where(args => args.name_hint == mIndicator.ID)
                .SingleOrDefault().id;
            else
              mEntryId = string.Empty;
            if (!String.IsNullOrEmpty(mEntryId) && mEntryId != entry_id)
              return;
            OnAppIndicatorMenuShown(this, new EventArgs());
          };
        }
      } catch (Exception) {
        // ignored
      }
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

        //gtkMenuItem.TooltipText = winformMenuItem.ToolTipText;
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
        (() => pluginHost.MainWindow.Activate());
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
