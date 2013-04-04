using System;
using DBus;
using System.Collections.Generic;

namespace com.canonical
{
  [Interface("com.canonical.dbusmenu")]
  public class DbusMenu
  {
    public struct MenuLayout
    {
      /// <summary>
      /// The revision number of the layout.
      /// For matching with layoutUpdated signals.
      /// </summary>
      public uint revision;

      /// <summary>
      /// The layout of the menu item as a recursive structure.
      /// </summary>
      public MenuItemLayout layout;
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
      /// Availible Properties:
      /// 
      /// Name              Type        Default       Description
      /// ------------------------------------------------------------------------
      /// type              string      "standard"    One of ("standard", "separator")
      /// label             string      ""            Text label of item, "_" preceeds access key
      /// enabled           bool        true          Item can be activated
      /// visible           bool        true          Item is visible
      /// icon-name         string      ""            Icon name (freedesktop.org spec)
      /// icon-data         byte[]      null          PNG data for icon
      /// shortcut          string[][]  null          Shortcuts for item, {{"Control"|"Alt"|"Shift"|"Super", <key>}, ...}
      /// toggle-type       string      ""            If item can be toggled, "checkmark"|"radio"
      /// toggle-state      int         -1            0 = off, 1 = on, else = indeterminate
      /// children-display  string      ""            If item has childeren, "submenu"
      /// 
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

    public struct MenuItemProperties
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
      /// icon-data         byte[]      null          PNG data for icon
      /// shortcut          string[][]  null          Shortcuts for item, {{"Control"|"Alt"|"Shift"|"Super", <key>}, ...}
      /// toggle-type       string      ""            If item can be toggled, "checkmark"|"radio"
      /// toggle-state      int         -1            0 = off, 1 = on, else = indeterminate
      /// children-display  string      ""            If item has childeren, "submenu"
      /// 
      /// </remarks>
      public Dictionary<string, object> properties;
    }

    public uint Version {
      get { return 3; }
    }

    public string Status {
      get {
        // TODO implement "notice" and property change support
        return "normal";
      }
    }

    public DbusMenu()
    {
    }

    /// <summary>
    /// Gets the layout.
    /// </summary>
    /// <returns>
    /// The layout of the menu.
    /// </returns>
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
    /// <remarks>
    /// Provides the layout and propertiers that are attached to the entries
    /// that are in the layout.  It only gives the items that are children
    /// of the item that is specified in <paramref name="parentId"/>.
    /// It will return all of the properties or specific ones depending of the
    /// value in <paramref name="propertyNames"/>.
    ///
    /// The format is recursive, where the second 'v' is in the same format
    /// as the original 'a(ia{sv}av)'.  Its content depends on the value
    /// of <paramref name="recursionDepth"/>.
    /// </remarks>
    public MenuLayout GetLayout(int parentId, int recursionDepth, string[] propertyNames)
    {
      // TODO implement
      Console.WriteLine("GetLayout - parentId: {0}; recursionDepth: {1}; propertyNames: {2}",
                        parentId,
                        recursionDepth,
                        propertyNames == null ? "" : string.Join(", ", propertyNames));

      var items = new object[1];
      var item = new MenuItemLayout();
      item.id = 2;
      item.properties = new Dictionary<string, object>();
      item.properties.Add("label", "_File");
      item.childeren = new object[0];
      items[0] = item;

      var rootMenuItemLayout = new MenuItemLayout();
      rootMenuItemLayout.id = 0;
      rootMenuItemLayout.properties = new Dictionary<string, object>();
      rootMenuItemLayout.properties.Add("children-display", "submenu");
      rootMenuItemLayout.childeren = items;

      var menuLayout = new MenuLayout();
      menuLayout.revision = 0;
      menuLayout.layout = rootMenuItemLayout;
      return menuLayout;
    }
  
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
    public MenuItemProperties[] GetGroupProperties(int[] ids, string[] propertyNames)
    {
      // TODO implement
      Console.WriteLine("GetGroupProperties - ids: {0}; propertyNames: {1}",
                        string.Join(", ", ids), string.Join(", ", propertyNames));
      return new MenuItemProperties[0];
    }

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
    public object GetProperty(int id, string name)
    {
      // TODO implement
      Console.WriteLine("GetProperty - id: {0}; name: {1}", id, name);
      return "property";
    }

    /// <summary>
    /// This is called by the applet to notify the application an event
    /// happened on a menu item.
    /// </summary>
    /// <param name='id'>
    /// The id of the item which received the event.
    /// </param>
    /// <param name='eventId'>
    /// The type of event. One of "clicked"|"hovered"
    /// </param>
    /// <param name='data'>
    /// Event-specific data.
    /// </param>
    /// <param name='timestamp'>
    /// The time that the event occured if available or the time the message was
    /// sent if not.
    /// </param>
    public void Event(int id, string eventId, object data, uint timestamp)
    {
      // TODO implement
      Console.WriteLine("Event - id: {0}; eventId: {1}; data: {2}; timestamp: {3}",
                        id, eventId, data, timestamp);
    }

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
    public bool AboutToShow(int id)
    {
      // TODO implement
      Console.WriteLine("AboutToShow - id: {0}", id);
      return true;
    }

    public struct PropertyDescriptor {
      int id;
      string[] properties;
    }


    public delegate void ItemsPropertiesUpdatedHandler(MenuItemProperties[] updatedProps,
                                                       PropertyDescriptor[] removedProps);

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

    /// <summary>
    /// Triggered when there are lots of property updates across many items
    /// so they all get grouped into a single dbus message.  The format is
    /// the ID of the item with a hashtable of names and values for those
    /// properties.
    /// </summary>
    public event ItemsPropertiesUpdatedHandler ItemsPropertiesUpdated;

    /// <summary>
    /// Triggered by the application to notify display of a layout update,
    /// up to revision.
    /// </summary>
    public event LayoutUpdatedHandler LayoutUpdated;

    /// <summary>
    /// Occurs when item activation requested.
    /// </summary>
    public event ItemActivationRequestedHandler ItemActivationRequested;
  }
}