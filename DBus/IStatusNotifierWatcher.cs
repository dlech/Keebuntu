using System;
using System.ComponentModel;

using DBus;
using org.freedesktop.DBus;

// Documentation comes from:
// https://freedesktop.org/wiki/Specifications/StatusNotifierItem/StatusNotifierWatcher/

namespace org.kde
{
  /// <summary>
  /// Status notifier watcher interface.
  /// </summary>
  /// <remarks>
  /// There will be a single org.freedesktop.StatusNotifierWatcher service
  /// instance registered on the session but at any given time. The
  /// StatusNotifierWatcher service is used to keep track of StatusNotifierItem
  /// instances, enumerate them and notify when new ones are registered or old
  /// ones are unregistered.
  ///
  /// It is also used to keep track of org.freedesktop.StatusNotifierHost
  /// instances, to have an easy way to know if there is at least one service
  /// registered as the visualization host for the status notifier items.
  /// </remarks>
  [Interface("org.kde.StatusNotifierWatcher")]
  public interface IStatusNotifierWatcher : Introspectable, Properties
  {
    /// <summary>
    /// Register a StatusNotifierItem into the StatusNotifierWatcher
    /// </summary>
    /// <param name="service">Service in the form of its full name on the
    /// session bus, for instance org.freedesktop.StatusNotifierItem-4077-1.
    /// </param>
    /// <remarks>
    /// A StatusNotifierItem instance must be registered to the watcher in
    /// order to be noticed from both the watcher and the StatusNotifierHost
    /// instances. If the registered StatusNotifierItem goes away from the
    /// session bus, the StatusNotifierWatcher should automatically notice it
    /// and remove it from the list of registered services.\
    /// </remarks>
    void RegisterStatusNotifierItem(string service);

    /// <summary>
    /// Register a StatusNotifierHost into the StatusNotifierWatcher.
    /// </summary>
    /// <param name="service">
    /// Servicein the form of its full name on the session bus, for instance
    /// org.freedesktop.StatusNotifierHost-4005.
    /// </param>
    /// <remarks>
    /// Every NotficationHost instance that intends to display StatusNotifierItem
    /// representations should register to StatusNotifierWatcher with this method.
    /// The StatusNotifierWatcher should automatically notice if an instance of
    /// StatusNotifierHost goes away.
    /// </remarks>
    void RegisterStatusNotifierHost(string service);

    /// <summary>
    /// List containing all the registered instances of StatusNotifierItem.
    /// </summary>
    /// <remarks>
    /// All elements of the array should correspond to services actually running
    /// on the session bus at the moment of the method call.
    /// </remarks>
    string[] RegisteredStatusNotifierItems { get; }

    /// <summary>
    /// True if at least one StatusNotifierHost has been registered with the
    /// Section called RegisterStatusNotifierHost and is currently running.
    /// </summary>
    /// <remarks>
    /// If no StatusNotifierHost are registered and running, all
    /// StatusNotifierItem instances should fall back using the Freedesktop
    /// System tray specification.
    /// </remarks>
    bool IsStatusNotifierHostRegistered { get; }

    /// <summary>
    /// The version of the protocol the StatusNotifierWatcher instance implements.
    /// </summary>
    int ProtocolVersion { get; }

    /// <summary>
    /// A new StatusNotifierItem has been registered.
    /// </summary>
    /// <remarks>
    /// The argument of the signal is the session bus name of the instance.
    /// StatusNotifierHost implementation should listen this signal to know
    /// when they should update their representation of the items.
    /// </remarks>
    event Action<string> StatusNotifierItemRegistered;

    /// <summary>
    /// A StatusNotifierItem instance has disappeared from the bus.
    /// </summary>
    /// <remarks>
    /// The argument of the signal is the session bus name of the instance.
    /// StatusNotifierHost implementation should listen this signal to know
    /// when they should update their representation of the items.
    /// </remarks>
    event Action<string> StatusNotifierItemUnregistered;

    /// <summary>
    /// A new StatusNotifierHost has been registered.
    /// </summary>
    /// <remarks>
    /// The StatusNotifierItem instances knows that they can use this protocol
    /// instead of the Freedesktop System tray protocol.
    /// </remarks>
    event Action StatusNotifierHostRegistered;

    event Action StatusNotifierHostUnregistered;
  }
}
