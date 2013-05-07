using System;
using System.Threading;
using System.Diagnostics;

namespace Keebuntu.DBus
{
  /// <summary>
  /// Runs Gtk Application loop for non-Gtk programs
  /// </summary>
  public static class GtkThread
  {
    static Thread mGtkThread;
    static object mStartupThreadLock = new object();

    public static int UserCount { get; set; }

    /// <summary>
    /// Starts the the thread if it is not already running, otherwise, just
    /// increases the count of users of this thread.
    /// </summary>
    public static void Start()
    {
      Monitor.Enter(mStartupThreadLock);
      try {
        UserCount++;
        if (UserCount != 1) {
          return;
        }
        mGtkThread = new Thread(RunGtkDBusThread);
        mGtkThread.SetApartmentState(ApartmentState.STA);
        mGtkThread.Name = "Keebuntu DBus Thread";
        mGtkThread.Start();
        if (!Monitor.Wait(mStartupThreadLock, 5000) || !mGtkThread.IsAlive) {
          mGtkThread.Abort();
          throw new Exception("Gtk Thread failed to start");
        }
      } finally {
        Monitor.Exit(mStartupThreadLock);
      }
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
      Invoke(() => Gtk.Application.Quit());
    }

    public static void Invoke(Action action)
    {
      if (mGtkThread == null ||
          mGtkThread.ThreadState != System.Threading.ThreadState.Running)
      {
        throw new Exception("Gtk Thread not Running.");
      }
      if (ReferenceEquals(Thread.CurrentThread, mGtkThread)) {
        action.Invoke();
      } else {
        Gtk.ReadyEvent readyEvent = () => action.Invoke();
        var threadNotify = new Gtk.ThreadNotify(readyEvent);
        threadNotify.WakeupMain();
      }
    }

    private static void RunGtkDBusThread()
    {
      Monitor.Enter(mStartupThreadLock);
      try {
        global::DBus.BusG.Init();
        Gtk.Application.Init();

        Monitor.Pulse(mStartupThreadLock);
        Monitor.Exit(mStartupThreadLock);

        /* run gtk event loop */
        Gtk.Application.Run();

      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
        Monitor.Pulse(mStartupThreadLock);
        Monitor.Exit(mStartupThreadLock);
      }
    }
  }
}

