// based on org.ayatana.bamf.xml from bamf
using System;
using DBus;

namespace org.ayatana.bamf
{
  public delegate void ActiveApplicationChangedHandler(string old_app, string new_app);

  public delegate void ActiveWindowChangedHandler(string old_win, string new_win);

  public delegate void ViewClosedHandler(string path, string type);

  public delegate void ViewOpenedHandler(string path, string type);

  public delegate void StackingOrderChangedHandler();

  public delegate void RunningApplicationsChangedHandler(string[] opened_desktop_files,
                                                         string[] closed_desktop_files);

  [Interface("org.ayatana.bamf.matcher")]
  public interface IMatcher : org.freedesktop.DBus.Properties
  {
    [return: Argument("xids")]
    uint[] XidsForApplication(string desktop_file);

    [return: Argument("paths")]
    string[] TabPaths();

    [return: Argument("paths")]
    string[] RunningApplications();

    [return: Argument("paths")]
    string[] RunningApplicationsDesktopFiles();

    void RegisterFavorites(string[] favorites);

    [return: Argument("path")]
    string PathForApplication(string desktop_file);

    [return: Argument("paths")]
    string[] WindowPaths();

    [return: Argument("paths")]
    string[] ApplicationPaths();

    [return: Argument("running")]
    bool ApplicationIsRunning(string desktop_file);

    [return: Argument("application")]
    string ApplicationForXid(uint xid);

    [return: Argument("window")]
    string ActiveWindow();

    [return: Argument("application")]
    string ActiveApplication();

    [return: Argument("window_list")]
    string[] WindowStackForMonitor(int monitor_id);

    event ActiveApplicationChangedHandler ActiveApplicationChanged;

    event ActiveWindowChangedHandler ActiveWindowChanged;

    event ViewClosedHandler ViewClosed;

    event ViewOpenedHandler ViewOpened;

    event StackingOrderChangedHandler StackingOrderChanged;

    event RunningApplicationsChangedHandler RunningApplicationsChanged;
  }
}

