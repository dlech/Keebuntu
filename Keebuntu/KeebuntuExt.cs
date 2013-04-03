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

namespace Keebuntu 
{
  public class KeebuntuExt : Plugin, IKeePassWindow
  {
    private IPluginHost mPluginHost;
    private Thread mGtkThread;
    private ApplicationIndicator mIndicator;
    private Gtk.Menu mAppIndicatorMenu;

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

    public Dictionary<string, string> GetMenuForWindow(){
      return new Dictionary<string, string>();
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

      var gtkWindow = new Gtk.Window(PwDefs.ProductName);
      gtkWindow.Visible = true;

      var gtkMainMenu = new Gtk.MenuBar();
      gtkWindow.Add(gtkMainMenu);
      gtkMainMenu.Show();

      var mainWindowMenu = mPluginHost.MainWindow.MainMenu;
      foreach (System.Windows.Forms.ToolStripMenuItem item in mainWindowMenu.Items) {
        ConvertAndAddMenuItem (item, gtkMainMenu);
      }
      mainWindowMenu.ItemAdded += (sender, e) =>
        InvokeMainWindow (() => ConvertAndAddMenuItem (e.Item, gtkMainMenu));

      mIndicator = new ApplicationIndicator("keepass-appindicator-plugin",
                                             "keepass-locked",
                                             AppIndicator.Category.ApplicationStatus);
#if DEBUG
      mIndicator.IconThemePath =
        Path.GetFullPath("Resources/icons/ubuntu-mono-dark/apps/24");
#endif
      mIndicator.Status = AppIndicator.Status.Active;
      mIndicator.Title = PwDefs.ProductName;
      mAppIndicatorMenu = new Gtk.Menu();
      var trayContextMenu = mPluginHost.MainWindow.TrayContextMenu;
      foreach (System.Windows.Forms.ToolStripItem item in trayContextMenu.Items) {
        ConvertAndAddMenuItem(item, mAppIndicatorMenu);
      }
      trayContextMenu.ItemAdded += (sender, e) =>
        InvokeGtkThread(() => ConvertAndAddMenuItem(e.Item, mAppIndicatorMenu));

      mAppIndicatorMenu.Shown += OnAppIndicatorMenuShown;
      mIndicator.Menu = mAppIndicatorMenu;

      if (Environment.GetEnvironmentVariable ("UBUNTU_MENUPROXY",
                                              EnvironmentVariableTarget.Process) !=
          "libappmenu.so")
      {
        Debug.Fail ("appmenu not supported");
      }

      var sessionBus = DBus.Bus.Session;
      var objectPath = new DBus.ObjectPath("/com/canonical/Unity/Panel/Service");
      sessionBus.Register (objectPath, new Object());
      Gtk.Application.Run();

      mAppIndicatorMenu.Shown -= OnAppIndicatorMenuShown;
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
            InvokeMainWindow (() => ConvertAndAddMenuItem (e.Item, subMenu));
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

