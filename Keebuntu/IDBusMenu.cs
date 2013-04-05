/* Derived from dbus-menu.xml in libdbusmenu-glib
 */

using System;
using DBus;
using System.Collections.Generic;

namespace com.canonical.dbusmenu
{
  public struct MenuItem
  {
    /// <summary>
    /// A unique numeric identifier.
    /// </summary>
    public int id;

    /// <summary>
    /// The menu item properties.
    /// </summary>
    /// <remarks>
    /// Availible Properties:
    /// 
    /// Name              Type        Default       Description
    /// ------------------------------------------------------------------------
    /// type              string      "standard"    One of ("standard", "separator")
    /// label             string      ""            Text label of item, "_" preceeds access key
    /// enabled           bool        true          Item can be activated
    /// visible           bool        true          Item is visible
    /// icon-name         string      ""            Icon name (freedesktop.org spec)
    /// icon-data         byte[]      byte[0]       PNG data for icon
    /// shortcut          string[][]  string[0][0]  Shortcuts for item, {{"Control", "Alt", "Shift", "Super", <key>}, {...}, ... }
    /// toggle-type       string      ""            If item can be toggled, "checkmark"|"radio"
    /// toggle-state      int         -1            0 = off, 1 = on, else = indeterminate
    /// children-display  string      ""            If item has childeren, "submenu"
    /// disposition       string      "normal"      How item should be presented, "normal", "informative", "warning", "alert"
    /// 
    /// </remarks>
    public Dictionary<string, object> properties;
  }

  public struct MenuItemLayout
  {
    /// <summary>
    /// A unique numeric identifier.
    /// </summary>
    public int id;

    /// <summary>
    /// The menu item properties.
    /// </summary>
    /// <remarks>
    /// See <see cref="MenuItemProperties"/> for a list of valid properties.
    /// </remarks>
    public Dictionary<string, object> properties;

    /// <summary>
    /// The childeren menu items of this menu item.
    /// </summary>
    /// <remarks>
    /// Type should be MenuItemLayout, but this causes stack overflow
    /// </remarks>
    public object[] childeren;
  }

  public struct MenuItemPropertyDescriptor
  {
    int id;
    string[] properties;
  }

  public struct MenuEvent
  {
    public int id;
    public string eventId;
    public object data;
    public uint timestamp;
  }

  public delegate void ItemsPropertiesUpdatedHandler(MenuItem[] updatedProps,
                                                     MenuItemPropertyDescriptor[] removedProps);

  /// <param name="revision">
  /// The revision of the layout that we're currently on.
  /// </param>
  /// <param name="parent">
  /// If the layout update is only of a subtree, this is the
  /// parent item for the entries that have changed.  It is zero if
  /// the whole layout should be considered invalid.
  /// </param>
  public delegate void LayoutUpdatedHandler(uint revision, int parent);

  /// <param name="id">
  /// ID of the menu that should be activated.
  /// </param>
  /// <param name="timestamp">
  /// The time that the event occured
  /// </param>
  public delegate void ItemActivationRequestedHandler(int id, uint timestamp);

  [Interface("com.canonical.dbusmenu")]
  public interface IDbusMenu : org.freedesktop.DBus.Properties 
  { 
    /// <summary>
    /// Provides the version of the DBusmenu API that this API is implementing.
    /// </summary>
    uint Version { get; }

    /// Represents the way the text direction of the application.  This
    /// allows the server to handle mismatches intelligently.  For left-
    /// to-right the string is "ltr" for right-to-left it is "rtl".
    string TextDirection { get; }

    /// <summary>
    /// Tells if the menus are in a normal state or they believe that they
    /// could use some attention.  Cases for showing them would be if help
    /// were referring to them or they accessors were being highlighted.
    /// This property can have two values: "normal" in almost all cases and
    /// "notice" when they should have a higher priority to be shown.
    /// </summary>
    string Status { get; }

    /// <summary>
    /// A list of directories that should be used for finding icons using
    /// the icon naming spec.  Idealy there should only be one for the icon
    /// theme, but additional ones are often added by applications for
    /// app specific icons.
    /// </summary>
    string[] IconThemePath { get; }

    /// <summary>
    /// Gets the layout of the menu.
    /// </summary>
    /// <param name='parentId'>
    /// The ID of the parent node for the layout.  For grabbing the layout
    /// from the root node use zero.
    /// </param>
    /// <param name='recursionDepth'>
    /// The amount of levels of recursion to use.  This affects the content 
    /// of the second variant array.
    ///   - -1: deliver all the items under the <paramref name="parentId"/>.
    ///   - 0: no recursion, the array will be empty.
    ///   - n: array will contains items up to 'n' level depth.
    /// </param>
    /// <param name='propertyNames'>
    /// The list of item properties we are interested in.  If there are
    /// no entries in the list all of the properties will be sent.
    /// </param>
    /// <param name="revision">
    /// The revision number of the layout.
    /// For matching with <see cref="LayoutUpdated"/> signals.
    /// </param>
    /// <param name="layout">
    /// The layout as a recursive structure.
    /// </param>
    /// <remarks>
    /// Provides the layout and properties that are attached to the entries
    /// that are in the layout.  It only gives the items that are children
    /// of the item that is specified in <paramref name="parentId"/>.
    /// It will return all of the properties or specific ones depending of the
    /// value in <paramref name="propertyNames"/>.
    ///
    /// The format is recursive, where the second 'v' is in the same format
    /// as the original 'a(ia{sv}av)'.  Its content depends on the value
    /// of <paramref name="recursionDepth"/>.
    /// </remarks>
    //public MenuLayout GetLayout(int parentId, int recursionDepth, string[] propertyNames)
    void GetLayout(int parentId, int recursionDepth, string[] propertyNames,
                   out uint revision, out MenuItemLayout layout);
  
    /// <summary>
    /// Gets list of items which are children of <paramref name="parentId"/>.
    /// </summary>
    /// <returns>
    /// An array of property values.
    /// </returns>
    /// <param name='ids'>
    /// A list of ids that we should be finding the properties on.
    /// If the list is empty, all menu items should be sent.
    /// </param>
    /// <param name='propertyNames'>
    /// The list of item properties we are interested in.  If there are
    /// no entries in the list all of the properties will be sent.
    /// Property names.
    /// </param>
    [return: Argument("properties")]
    MenuItem[] GetGroupProperties(int[] ids, string[] propertyNames);

    /// <summary>
    /// Get a signal property on a single item.  This is not useful if you're
    /// going to implement this interface, it should only be used if you're
    /// debugging via a commandline tool.
    /// </summary>
    /// <returns>
    /// The value of the property.
    /// </returns>
    /// <param name='id'>
    /// The id of the item which received the event.
    /// </param>
    /// <param name='name'>
    /// The name of the property to get.
    /// </param>
    [return: Argument("property")]
    object GetProperty(int id, string name);

    /// <summary>
    /// This is called by the applet to notify the application an event
    /// happened on a menu item.
    /// </summary>
    /// <param name='id'>
    /// The id of the item which received the event.
    /// </param>
    /// <param name='eventId'>
    /// The type of event. One of "clicked"|"hovered"|"opened"|"closed"
    /// </param>
    /// <param name='data'>
    /// Event-specific data.
    /// </param>
    /// <param name='timestamp'>
    /// The time that the event occured if available or the time the message was
    /// sent if not.
    /// </param>
    void Event(int id, string eventId, object data, uint timestamp);

    /// <summary>
    /// Used to pass a set of events as a single message for possibily several
    /// different menuitems.  This is done to optimize DBus traffic.
    /// </summary>
    /// <returns>
    /// A list of menuitem IDs that couldn't be found.  If none of the ones
    /// in the list can be found, a DBus error is returned.
    /// </returns>
    /// <param name='events'>
    /// An array of all the events that should be passed.
    /// </param>
    [return: Argument("idErrors")]
    int[] EventGroup(MenuEvent[] events);

    /// <summary>
    /// This is called by the applet to notify the application that it is about
    /// to show the menu under the specified item.
    /// </summary>
    /// <returns>
    /// Whether this AboutToShow event should result in the menu being updated.
    /// </returns>
    /// <param name='id'>
    /// Id of the parent menu item of the item about to be shown.
    /// </param>
    [return: Argument("needUpdate")]
    bool AboutToShow(int id);

    /// <summary>
    /// A function to tell several menus being shown that they are about to
    /// be shown to the user.  This is likely only useful for programitc purposes
    /// so while the return values are returned, in general, the singular function
    /// should be used in most user interacation scenarios.
    /// </summary>
    /// <param name='ids'>
    /// The IDs of the menu items who's submenus are being shown.
    /// </param>
    /// <param name='updatesNeeded'>
    /// The IDs of the menus that need updates.  Note: if no update information
    /// is needed the DBus message should set the no reply flag.
    /// </param>
    /// <param name='idErrors'>
    /// A list of menuitem IDs that couldn't be found.  If none of the ones
    /// in the list can be found, a DBus error is returned.
    /// </param>
    void AboutToShowGroup(int[] ids, out int[] updatesNeeded, out int[] idErrors);

    /// <summary>
    /// Triggered when there are lots of property updates across many items
    /// so they all get grouped into a single dbus message.  The format is
    /// the ID of the item with a hashtable of names and values for those
    /// properties.
    /// </summary>
    event ItemsPropertiesUpdatedHandler ItemsPropertiesUpdated;

    /// <summary>
    /// Triggered by the application to notify display of a layout update,
    /// up to revision.
    /// </summary>
    event LayoutUpdatedHandler LayoutUpdated;

    /// <summary>
    /// Occurs when item activation requested.
    /// </summary>
    event ItemActivationRequestedHandler ItemActivationRequested;
  }
}