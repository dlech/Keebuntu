using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Keebuntu.DBus
{
  /// <summary>
  /// Runs a GTK Application loop for use in Winforms applications.
  /// </summary>
  public static class DBusBackgroundWorker
  {
    static BackgroundWorker worker;
    static Thread gtkThread;
    static List<TaskCompletionSource<object>> taskList =
      new List<TaskCompletionSource<object>> ();

    public static int ReferenceCount { get; private set; }

    /// <summary>
    /// Notifies DBusBackgroundWorker that we are using it.
    /// </summary>
    /// <remarks>
    /// Starts the GTK thread if it is not already running, otherwise, just
    /// increases the reference count.
    /// </remarks>
    public static void Request()
    {
      if (worker == null) {
        worker = new System.ComponentModel.BackgroundWorker();
        worker.WorkerReportsProgress = true;
        worker.DoWork += mWorker_DoWork;
        worker.ProgressChanged += mWorker_ReportProgress;
      }
      if (!worker.IsBusy) {
        worker.RunWorkerAsync();
      }
      ReferenceCount++;
    }

    /// <summary>
    /// Notifies DBusBackgroundWorker that we are done using it.
    /// </summary>
    /// <remarks>
    /// Decreases the reference count. If the reference count is 0, the GTK
    /// thread is stopped.
    /// </remarks>
    public static void Release()
    {
      if (ReferenceCount <= 0) {
        // TODO - should this be an error/exception?
        Debug.Fail("DBusBackgroundWorker was released without being requested.");
        return;
      }
      ReferenceCount--;
      if (ReferenceCount > 0) {
        return;
      }
      InvokeGtkThread(() => Gtk.Application.Quit());
    }

    public static Task InvokeGtkThread(Action action)
    {
      Func<object> func = () => {
        action.Invoke();
        return null;
      };
      return InvokeGtkThread(func);
    }

    public static Task<object> InvokeGtkThread(Func<object> func)
    {
      if (worker == null || !worker.IsBusy)
      {
        throw new Exception("DBusBackgroundWorker not running.");
      }
      var completionSource = new TaskCompletionSource<object>();
      taskList.Add(completionSource);
      Gtk.ReadyEvent readyEvent = () => {
        try {
          completionSource.TrySetResult(func.Invoke());
        } catch (Exception ex) {
          completionSource.TrySetException(ex);
        } finally {
          taskList.Remove(completionSource);
        }
      };
      if (ReferenceEquals(Thread.CurrentThread, gtkThread)) {
        readyEvent.Invoke();
      } else {
        var threadNotify = new Gtk.ThreadNotify(readyEvent);
        threadNotify.WakeupMain();
      }
      return completionSource.Task;
    }

    public static Task InvokeWinformsThread(Action action)
    {
      Func<object> func = () => {
        action.Invoke();
        return null;
      };
      return InvokeWinformsThread(func);
    }

    public static Task<object> InvokeWinformsThread(Func<object> func)
    {
      if (worker == null || !worker.IsBusy)
      {
        throw new Exception("DBusBackgroundWorker not running.");
      }
      var completionSource = new TaskCompletionSource<object>(func);
      taskList.Add(completionSource);
      worker.ReportProgress(0, completionSource);
      return completionSource.Task;
    }

    private static void mWorker_DoWork(object sender, DoWorkEventArgs e)
    {
      try {
        gtkThread = Thread.CurrentThread;

        global::DBus.BusG.Init();
        Gtk.Application.Init();

        /* run gtk event loop */
        Gtk.Application.Run();

      } catch (Exception ex) {
        Debug.Fail(ex.ToString());
      }
    }

    // ReportProgress event is used as a callback to the Winforms thread
    private static void mWorker_ReportProgress(object sender,
                                               ProgressChangedEventArgs e)
    {
      var completionSource = e.UserState as TaskCompletionSource<object>;
      if (completionSource == null)
        return;
      var func = completionSource.Task.AsyncState as Func<object>;
      if (func == null)
        return;

      try {
        completionSource.TrySetResult(func.Invoke());
      } catch (Exception ex) {
        completionSource.TrySetException(ex);
      } finally {
        taskList.Remove(completionSource);
      }
    }
  }
}

