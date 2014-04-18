using System;
using System.Diagnostics;
using System.Threading;
using System.Drawing.Imaging;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using KeePass.Plugins;
using KeePassLib;
using Keebuntu.Dbus;
using DBus;
using Keebuntu.DBus;
using KeePassLib.Utility;
using System.Windows.Forms;

namespace KeebuntuAppMenu
{
  public class KeebuntuAppMenuExt : Plugin
  {
    const string menuPath = "/com/canonical/menu/{0}";
    const string keebuntuAppMenuWarningSeenId = "KeebuntuAppMenu.WarningSeen";

    IPluginHost pluginHost;
    MenuStripDBusMenu dbusMenu;

    public override bool Initialize(IPluginHost host)
    {
      pluginHost = host;
      var threadStarted = false;
      try {
        DBusBackgroundWorker.Start();
        threadStarted = true;
        DBusBackgroundWorker.InvokeGtkThread(() => GtkDBusInit());

        // mimmic behavior of other ubuntu apps
        if (Environment.GetEnvironmentVariable("APPMENU_DISPLAY_BOTH") != "1")
        {
          pluginHost.MainWindow.MainMenu.Visible = false;
        }
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
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
      }
    }

    void GtkDBusInit()
    {
      /* setup ApplicationMenu */

      dbusMenu = new MenuStripDBusMenu(pluginHost.MainWindow.MainMenu);

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
      var unityPanelServiceBus =
        sessionBus.GetObject<com.canonical.AppMenu.Registrar.IRegistrar>(registratBusName,
                                                                         registrarObjectPath);
      var mainFormXid = GetWindowXid(pluginHost.MainWindow);
      var mainFormObjectPath = new ObjectPath(string.Format(menuPath,
                                                            mainFormXid));
      sessionBus.Register(mainFormObjectPath, dbusMenu);
      try {
      unityPanelServiceBus.RegisterWindow((uint)mainFormXid.ToInt32(),
                                          mainFormObjectPath);
      } catch (Exception) {
        if (!pluginHost.CustomConfig.GetBool(keebuntuAppMenuWarningSeenId, false)) {
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
        }
        Terminate ();
        return;
      }
      // have to re-register the window each time the main windows is shown
      // otherwise we lose the application menu
      pluginHost.MainWindow.Activated += (sender, e) =>
      {
        // TODO - sometimes we invoke this unnessasarily. If there is a way to
        // test that we are still registered, that would proably be better.
        // For now, it does not seem to hurt anything.
        DBusBackgroundWorker.InvokeGtkThread(
          () => unityPanelServiceBus.RegisterWindow((uint)mainFormXid.ToInt32(),
                                                    mainFormObjectPath));
      };
    }

    IntPtr GetWindowXid(System.Windows.Forms.Form form)
    {
      var typeName = typeof(System.Windows.Forms.Control).AssemblyQualifiedName;
      var hwndTypeName = typeName.Replace("Control", "Hwnd");
      var hwndType = Type.GetType(hwndTypeName);
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

