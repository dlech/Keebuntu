using System;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;

namespace Keebuntu.DBus
{
  /// <summary>
  /// Runs Gtk Application loop for non-Gtk programs
  /// </summary>
  public static class DBusBackgroundWorker
  {
    static BackgroundWorker mWorker;
    static Thread mGtkThread;
    static object mStartupThreadLock = new object();

    public static int UserCount { get; set; }

    /// <summary>
    /// Starts the the thread if it is not already running, otherwise, just
    /// increases the count of users of this thread.
    /// </summary>
    public static void Start()
    {
      if (mWorker == null) {
        mWorker = new System.ComponentModel.BackgroundWorker();
        mWorker.WorkerReportsProgress = true;
        mWorker.DoWork += mWorker_DoWork;
        mWorker.ProgressChanged += mWorker_ReportProgress;
      }
      if (!mWorker.IsBusy) {
        mWorker.RunWorkerAsync();
      }
      UserCount++;
    }

    /// <summary>
    /// Stop the thread if this is the last user to call for the thread to stop,
    /// otherwise, just decreases the count of users of this thread
    /// </summary>
    public static void Stop()
    {
      if (UserCount <= 0) {
        // TODO - should this be an error/exception?
        return;
      }
      UserCount--;
      if (UserCount > 0) {
        return;
      }
      InvokeGtkThread(() => Gtk.Application.Quit());
    }

    public static void InvokeGtkThread(Action action)
    {
      if (mWorker == null || !mWorker.IsBusy)
      {
        throw new Exception("DBusBackgroundWorker not running.");
      }
      if (ReferenceEquals(Thread.CurrentThread, mGtkThread)) {
        action.Invoke();
      } else {
        Gtk.ReadyEvent readyEvent = () => action.Invoke();
        var threadNotify = new Gtk.ThreadNotify(readyEvent);
        threadNotify.WakeupMain();
      }
    }

    public static void InvokeWinformsThread(Action action)
    {
      if (mWorker == null || !mWorker.IsBusy)
      {
        throw new Exception("DBusBackgroundWorker not running.");
      }
      mWorker.ReportProgress(0, action);
    }

    private static void mWorker_DoWork(object sender, DoWorkEventArgs e)
    {
      try {
        mGtkThread = Thread.CurrentThread;

        global::DBus.BusG.Init();
        Gtk.Application.Init();

        /* run gtk event loop */
        Gtk.Application.Run();

      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
      }
    }

    private static void mWorker_ReportProgress(object sender,
                                               ProgressChangedEventArgs e)
    {
      var action = e.UserState as Action;
      if (action != null) {
        action.Invoke();
      }
    }
  }
}

