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
using System.Threading.Tasks;
using System.Windows.Forms;

using DBus;
using KeePass.Plugins;
using KeePass.UI;
using KeePassLib;
using KeePassLib.Utility;
using Keebuntu.DBus;

namespace KeebuntuAppMenu
{
  public class KeebuntuAppMenuExt : Plugin
  {
    const string menuPath = "/com/canonical/menu/{0}";
    const string keebuntuAppMenuWarningSeenId = "KeebuntuAppMenu.WarningSeen";

    IPluginHost pluginHost;
    MenuStripDBusMenu dbusMenu;
    MenuStripDBusMenu emptyDBusMenu;
    com.canonical.AppMenu.Registrar.IRegistrar unityPanelServiceBus;
    IntPtr mainFormXid;
    ObjectPath mainFormObjectPath;
    bool hideMenuInApp;

    public override bool Initialize(IPluginHost host)
    {
      pluginHost = host;

      // mimmic behavior of other ubuntu apps
      hideMenuInApp =
        Environment.GetEnvironmentVariable("APPMENU_DISPLAY_BOTH") != "1";
      bool threadStarted = false;
      try {
        DBusBackgroundWorker.Request();
        threadStarted = true;
        DBusBackgroundWorker.InvokeGtkThread((Action)GtkDBusInit).Wait();

        if (hideMenuInApp)
        {
          pluginHost.MainWindow.MainMenu.Visible = false;
        }
        pluginHost.MainWindow.Activated += MainWindow_Activated;
        GlobalWindowManager.WindowAdded += GlobalWindowManager_WindowAdded;
        GlobalWindowManager.WindowRemoved += GlobalWindowManager_WindowRemoved;
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
        if (threadStarted)
          Terminate();
        return false;
      }
      return true;
    }

    public override void Terminate()
    {
      try {
        pluginHost.MainWindow.Activated -= MainWindow_Activated;
        GlobalWindowManager.WindowAdded -= GlobalWindowManager_WindowAdded;
        GlobalWindowManager.WindowRemoved -= GlobalWindowManager_WindowRemoved;
        DBusBackgroundWorker.InvokeWinformsThread(() => {
          pluginHost.MainWindow.MainMenu.Visible = true;
        });
        DBusBackgroundWorker.Release();
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
      }
    }

    void MainWindow_Activated(object sender, EventArgs e)
    {
      if (hideMenuInApp)
      {
        pluginHost.MainWindow.MainMenu.Visible = false;
      }
      // have to re-register the window each time the main windows is shown
      // otherwise we lose the application menu
      // TODO - sometimes we invoke this unnessasarily. If there is a way to
      // test that we are still registered, that would proably be better.
      // For now, it does not seem to hurt anything.
      DBusBackgroundWorker.InvokeGtkThread(
        () => unityPanelServiceBus.RegisterWindow((uint)mainFormXid.ToInt32(),
                                                   mainFormObjectPath));
    }

    void GtkDBusInit()
    {
      /* setup ApplicationMenu */

      dbusMenu = new MenuStripDBusMenu(pluginHost.MainWindow.MainMenu);
      emptyDBusMenu = new MenuStripDBusMenu(new MenuStrip());

      var sessionBus = Bus.Session;

#if DEBUG
      const string dbusBusPath = "/org/freedesktop/DBus";
      const string dbusBusName = "org.freedesktop.DBus";
      var dbusObjectPath = new ObjectPath(dbusBusPath);
      var dbusService =
        sessionBus.GetObject<org.freedesktop.DBus.IBus>(dbusBusName, dbusObjectPath);
      dbusService.NameAcquired += (name) => Console.WriteLine ("NameAcquired: " + name);
#endif
      const string registrarBusPath = "/com/canonical/AppMenu/Registrar";
      const string registratBusName = "com.canonical.AppMenu.Registrar";
      var registrarObjectPath = new ObjectPath(registrarBusPath);
      unityPanelServiceBus =
        sessionBus.GetObject<com.canonical.AppMenu.Registrar.IRegistrar>(registratBusName,
                                                                         registrarObjectPath);
      mainFormXid = GetWindowXid(pluginHost.MainWindow);
      mainFormObjectPath = new ObjectPath(string.Format(menuPath,
                                                        mainFormXid));
      sessionBus.Register(mainFormObjectPath, dbusMenu);
      try {
        unityPanelServiceBus.RegisterWindow((uint)mainFormXid.ToInt32(),
                                            mainFormObjectPath);
      } catch (Exception) {
        if (!pluginHost.CustomConfig.GetBool(keebuntuAppMenuWarningSeenId, false))
          Task.Run((Action)ShowErrorMessage);
      }
    }

    void ShowErrorMessage()
    {
      DBusBackgroundWorker.Request();
      DBusBackgroundWorker.InvokeGtkThread(() => {
        using (var dialog = new Gtk.Dialog()) {
          dialog.BorderWidth = 6;
          dialog.Resizable = false;
          dialog.HasSeparator = false;
          var message = "<span weight=\"bold\"size=\"larger\">"
            + "Could not register KeebuntuAppMenu with Unity panel service."
              + "</span>\n\n"
              + "This plugin only works with Ubuntu Unity desktop."
              + " If you do not use Unity, you should uninstall the KeebuntuAppMenu plugin."
              + "\n";
          var label = new Gtk.Label(message);
          label.UseMarkup = true;
          label.Wrap = true;
          label.Yalign = 0;
          var icon = new Gtk.Image(Gtk.Stock.DialogError, Gtk.IconSize.Dialog);
          icon.Yalign = 0;
          var contentBox = new Gtk.HBox();
          contentBox.Spacing = 12;
          contentBox.BorderWidth = 6;
          contentBox.PackStart(icon);
          contentBox.PackEnd(label);
          dialog.VBox.PackStart(contentBox);
          dialog.AddButton("Don't show this again", Gtk.ResponseType.Accept);
          dialog.AddButton("OK", Gtk.ResponseType.Ok);
          dialog.DefaultResponse = Gtk.ResponseType.Ok;
          dialog.Response += (o, args) => {
            dialog.Destroy();
            if (args.ResponseId == Gtk.ResponseType.Accept)
              pluginHost.CustomConfig.SetBool(keebuntuAppMenuWarningSeenId, true);
          };
          dialog.ShowAll();
          dialog.KeepAbove = true;
          dialog.Run();
        }
      }).Wait();
      DBusBackgroundWorker.Release();
    }

    void GlobalWindowManager_WindowAdded(object sender, GwmWindowEventArgs e)
    {
      var xid = (uint)GetWindowXid(e.Form);
      var objectPath = new ObjectPath(string.Format(menuPath, xid));
      DBusBackgroundWorker.InvokeGtkThread(() => {
        Bus.Session.Register(objectPath, emptyDBusMenu);
        unityPanelServiceBus.RegisterWindow(xid, objectPath);
      });
    }

    void GlobalWindowManager_WindowRemoved(object sender, GwmWindowEventArgs e)
    {
      var xid = (uint)GetWindowXid(e.Form);
      var objectPath = new ObjectPath(string.Format(menuPath, xid));
      DBusBackgroundWorker.InvokeGtkThread(() => {
        unityPanelServiceBus.UnregisterWindow(xid);
        Bus.Session.Unregister(objectPath);
      });
      if (GlobalWindowManager.WindowCount <= 1)
        DBusBackgroundWorker.InvokeGtkThread(
          () => unityPanelServiceBus.RegisterWindow((uint)mainFormXid.ToInt32(),
                                                     mainFormObjectPath));
    }

    IntPtr GetWindowXid(System.Windows.Forms.Form form)
    {
      var winformsAssm = typeof(System.Windows.Forms.Control).Assembly;
      var hwndType = winformsAssm.GetType("System.Windows.Forms.Hwnd");
      var objectFromHandleMethod =
        hwndType.GetMethod("ObjectFromHandle", BindingFlags.Public | BindingFlags.Static);
      var hwnd =
        objectFromHandleMethod.Invoke(null, new object[] { form.Handle });
      var wholeWindowField = hwndType.GetField("whole_window",
                                               BindingFlags.NonPublic | BindingFlags.Instance);
      return (IntPtr)wholeWindowField.GetValue(hwnd);
    }
  }
}
