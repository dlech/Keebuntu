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
using KeePassLib.Utility;
using DBus;
using org.kde;
using org.freedesktop.DBus;

namespace KeebuntuStatusNotifier
{
  public class KeebuntuStatusNotifierExt : Plugin
  {
    IPluginHost pluginHost;
    KeePassStatusNotifierItem statusNotifier;

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

      var threadStarted = false;
      try {
        DBusBackgroundWorker.Request();
        threadStarted = true;
        DBusBackgroundWorker.InvokeGtkThread((Action)GtkDBusInit).Wait();
      } catch (Exception ex) {
        MessageService.ShowWarning(
          "KeebuntuStatusNotifier plugin failed to start.",
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
      try {
        DBusBackgroundWorker.Release();
      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
      }
    }

    /// <summary>
    /// Initalize Gtk and DBus stuff
    /// </summary>
    private void GtkDBusInit()
    {
      /* setup StatusNotifierItem */

      const string sniWatcherServiceName = "org.kde.StatusNotifierWatcher";
      const string sniWatcherPath = "/StatusNotifierWatcher";
      const string applicationPathTemplate = "/org/keepass/KeePass{0}";

      var watcher = Bus.Session.GetObject<IStatusNotifierWatcher>(
        sniWatcherServiceName, new ObjectPath(sniWatcherPath));
#if DEBUG
      watcher.StatusNotifierItemRegistered += (obj) =>
        Console.WriteLine(obj);
#endif

      var mainWindowType = pluginHost.MainWindow.GetType();
      var cxtTrayField = mainWindowType.GetField("m_ctxTray",
        BindingFlags.Instance | BindingFlags.NonPublic);
      var ctxTray = cxtTrayField.GetValue(pluginHost.MainWindow);
      var onOpening = ctxTray.GetType().GetMethod("OnOpening",
        BindingFlags.Instance | BindingFlags.NonPublic);
      var onOpened = ctxTray.GetType().GetMethod("OnOpened",
        BindingFlags.Instance | BindingFlags.NonPublic);

      var applicationPath = new ObjectPath(string.Format(applicationPathTemplate,
        pluginHost.MainWindow.Handle));
      statusNotifier = new KeePassStatusNotifierItem(pluginHost, applicationPath);

      // Synthesize menu open events. These are expected by KeePass and
      // other plugins
      statusNotifier.Showing += (sender, e) => {
        DBusBackgroundWorker.InvokeWinformsThread(() =>
          onOpening.Invoke(ctxTray, new[] { new CancelEventArgs() }));
      };
      statusNotifier.Shown += (sender, e) => {
        DBusBackgroundWorker.InvokeWinformsThread(() =>
          onOpened.Invoke(ctxTray, new[] { new CancelEventArgs() }));
      };

      Bus.Session.Register(applicationPath, statusNotifier);
      watcher.RegisterStatusNotifierItem(applicationPath.ToString());
    }
  }
}
