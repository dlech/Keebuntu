using System;
using System.Diagnostics;
using System.Threading;
using System.Drawing.Imaging;
using AppIndicator;
using KeePass.Plugins;
using System.IO;
using System.ComponentModel;
using KeePassLib;

namespace Keebuntu
{
  public class KeebuntuExt : Plugin
  {
    private IPluginHost mPluginHost;
    private Thread mGtkThread;
    private ApplicationIndicator mIndicator;
    private Gtk.Menu mMenu;

    public override bool Initialize(IPluginHost host)
    {
      mPluginHost = host;
      try {
        mGtkThread = new Thread (RunGtkApp);
        mGtkThread.SetApartmentState (ApartmentState.STA);
        mGtkThread.Name = "GTK Thread";
        mGtkThread.Start ();
      } catch (Exception ex) {
        Debug.Fail (ex.ToString ());
        return false;
      }
      return true;
    }

    public override void Terminate()
    {
      try {
        InvokeGtkThread (() => Gtk.Application.Quit ());
      } catch (Exception ex) {
        Debug.Fail (ex.ToString ());
      }
    }

    private void OnMenuShown(object sender, EventArgs e)
    {
      try {
        var mainWindowType = mPluginHost.MainWindow.GetType ();
        var onCtxTrayOpeningMethodInfo =
          mainWindowType.GetMethod ("OnCtxTrayOpening",
                                      System.Reflection.BindingFlags.Instance |
                                    System.Reflection.BindingFlags.NonPublic,
                                    Type.DefaultBinder,
                                    new[] {
                                      typeof(object),
                                      typeof(CancelEventArgs)
                                    },
                                      null);
        if (onCtxTrayOpeningMethodInfo != null) {
          InvokeMainWindow (
            () => onCtxTrayOpeningMethodInfo.Invoke (mPluginHost.MainWindow,
                                                     new[] {
                                                       sender,
                                                       new CancelEventArgs ()
                                                     })
            );
        }
      } catch (Exception ex) {
        Debug.Fail (ex.ToString ());
      }
    }

    private void RunGtkApp()
    {
      Gtk.Application.Init ();

      mIndicator = new ApplicationIndicator ("keepass-appindicator-plugin",
                                             "keepass-locked",
                                             AppIndicator.Category.ApplicationStatus);
#if DEBUG
      mIndicator.IconThemePath =
        Path.GetFullPath("Resources/icons/ubuntu-mono-dark/apps/24");
#endif
      mIndicator.Status = AppIndicator.Status.Active;
      mIndicator.Title = PwDefs.ProductName;
      mMenu = new Gtk.Menu ();
      var trayContextMenu = mPluginHost.MainWindow.TrayContextMenu;
      foreach (System.Windows.Forms.ToolStripItem item in trayContextMenu.Items) {
        ConvertAndAddMenuItem (item);
      }
      trayContextMenu.ItemAdded += (sender, e) =>
        InvokeGtkThread (() => ConvertAndAddMenuItem (e.Item));

      mMenu.Shown += OnMenuShown;
      mMenu.ShowAll ();
      mIndicator.Menu = mMenu;

      Gtk.Application.Run ();

      mMenu.Shown -= OnMenuShown;
    }

    private void InvokeMainWindow(Action action)
    {
      var mainWindow = mPluginHost.MainWindow;
      if (mainWindow.InvokeRequired) {
        mainWindow.Invoke (action);
      } else {
        action.Invoke ();
      }
    }

    private void InvokeGtkThread(Action action)
    {
      if (ReferenceEquals (Thread.CurrentThread, mGtkThread)) {
        action.Invoke ();
      } else {
        Gtk.ReadyEvent readyEvent = () => action.Invoke ();
        var threadNotify = new Gtk.ThreadNotify (readyEvent);
        threadNotify.WakeupMain ();
      }
    }

    private void ConvertAndAddMenuItem(System.Windows.Forms.ToolStripItem item)
    {
      if (item is System.Windows.Forms.ToolStripMenuItem) {
        // windows forms use & for mneumonic, gtk uses _
        var gtkMenuItem = new Gtk.ImageMenuItem (item.Text.Replace ("&", "_"));

        if (item.Image != null) {
          var memStream = new MemoryStream ();
          item.Image.Save (memStream, ImageFormat.Png);
          memStream.Position = 0;
          gtkMenuItem.Image = new Gtk.Image (memStream);
        }

        gtkMenuItem.TooltipText = item.ToolTipText;
        gtkMenuItem.Visible = item.Visible;
        gtkMenuItem.Sensitive = item.Enabled;

        gtkMenuItem.Activated += (sender, e) =>
          InvokeMainWindow (item.PerformClick);

        item.TextChanged += (sender, e) => InvokeGtkThread (() =>
        {
          var label = gtkMenuItem.Child as Gtk.Label;
          if (label != null) {
            label.Text = item.Text;
          }
        }
        );
        item.EnabledChanged += (sender, e) =>
          InvokeGtkThread (() => gtkMenuItem.Sensitive = item.Enabled);
        item.VisibleChanged += (sender, e) =>
          InvokeGtkThread (() => gtkMenuItem.Visible = item.Visible);

        mMenu.Insert (gtkMenuItem, item.Owner.Items.IndexOf (item));
      } else if (item is System.Windows.Forms.ToolStripSeparator) {
        mMenu.Insert (new Gtk.SeparatorMenuItem (), item.Owner.Items.IndexOf (item));
      } else {
        Debug.Fail ("Unexpected menu item");
      }
    }
  }
}

