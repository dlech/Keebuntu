using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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

namespace GtkStatusIcon
{
  public class GtkStatusIconExt : Plugin
  {
    IPluginHost pluginHost;
    Gtk.StatusIcon statusIcon;
    Gtk.Menu statusIconMenu;
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
      pluginHost.MainWindow.Activated += MainWindow_Activated;
      pluginHost.MainWindow.Resize -= MainWindow_Resize;
      statusIcon.PopupMenu -= OnPopupMenu;
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

    void OnPopupMenu(object sender, Gtk.PopupMenuArgs e)
    {
      try {
        var mainWindowType = pluginHost.MainWindow.GetType();
        var cxtTrayField = mainWindowType.GetField("m_ctxTray",
          BindingFlags.Instance | BindingFlags.NonPublic);
        var ctxTray = cxtTrayField.GetValue(pluginHost.MainWindow);

        // Synthesize menu open events. These are expected by KeePass and
        // other plugins

        var onOpening = ctxTray.GetType().GetMethod("OnOpening",
          BindingFlags.Instance | BindingFlags.NonPublic);
        DBusBackgroundWorker.InvokeWinformsThread(() =>
          onOpening.Invoke(ctxTray, new[] { new CancelEventArgs() }));

        statusIconMenu.Popup(null, null, null, (uint)e.Args[0], (uint)e.Args[1]);

        var onOpened = ctxTray.GetType().GetMethod("OnOpened",
          BindingFlags.Instance | BindingFlags.NonPublic);
        DBusBackgroundWorker.InvokeWinformsThread(() =>
          onOpened.Invoke(ctxTray, new[] { new CancelEventArgs() }));
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
      }
    }

    /// <summary>
    /// Initalize Gtk and DBus stuff
    /// </summary>
    private void GtkDBusInit()
    {
      /* setup StatusIcon */

      statusIcon = new Gtk.StatusIcon();
      statusIcon.IconName = "keepass2-locked";
#if DEBUG
      statusIcon.File = Path.GetFullPath("Resources/icons/hicolor/16x16/apps/keepass2-locked.png");
#endif
      statusIcon.Tooltip = PwDefs.ProductName;

      statusIconMenu = new Gtk.Menu();

      var trayContextMenu = pluginHost.MainWindow.TrayContextMenu;
      // make copy of item list to prevent list changed exception when iterating
      var menuItems =
        new System.Windows.Forms.ToolStripItem[trayContextMenu.Items.Count];
      trayContextMenu.Items.CopyTo(menuItems, 0);
      trayContextMenu.ItemAdded += (sender, e) =>
        DBusBackgroundWorker.InvokeGtkThread
          (() => ConvertAndAddMenuItem(e.Item, statusIconMenu));

      foreach (System.Windows.Forms.ToolStripItem item in menuItems) {
        ConvertAndAddMenuItem(item, statusIconMenu);
      }

      statusIcon.PopupMenu += OnPopupMenu;
      statusIcon.Activate += (sender, e) => {
        DBusBackgroundWorker.InvokeWinformsThread
        (() => pluginHost.MainWindow.EnsureVisibleForegroundWindow(true, true));
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

    byte[] ResizeImage(Image image, int width, int height)
    {
      var stream = new MemoryStream();
      image.Save(stream, ImageFormat.Png);
      try {
        var wand = new MagickWand();
        wand.ReadImageBlob(stream.ToArray());
        wand.ResizeImage(width, height, FilterType.Mitchell, 0.5);
        return wand.GetImageBlob();
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());

        // http://stackoverflow.com/a/24199315/1976323

        var destRect = new Rectangle(0, 0, width, height);
        var destImage = new Bitmap(width, height);

        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        using (var graphics = Graphics.FromImage(destImage))
        {
          graphics.CompositingMode = CompositingMode.SourceCopy;
          graphics.CompositingQuality = CompositingQuality.HighQuality;
          graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
          graphics.SmoothingMode = SmoothingMode.HighQuality;
          graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

          using (var wrapMode = new ImageAttributes())
          {
            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(image, destRect, 0, 0, image.Width,image.Height, GraphicsUnit.Pixel, wrapMode);
          }
        }

        stream.Seek(0, SeekOrigin.Begin);
        destImage.Save(stream, ImageFormat.Png);
      }
      return stream.ToArray();
    }
  }
}
