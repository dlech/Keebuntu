using System;
using System.Reflection;
using System.Collections.Generic;
using DBus;
using com.canonical.dbusmenu;

namespace Keebuntu
{
  public class FakeDBusMenu : com.canonical.dbusmenu.IDbusMenu
  {
    [return: Argument ("value")]
    public object Get (string @interface, string propname)
    {
      return GetPropertiesForInterface(@interface)[propname];
    }

    [return: Argument ("props")]
    public IDictionary<string, object> GetAll (string @interface)
    {
      return GetPropertiesForInterface(@interface);
    }

    public void Set (string @interface, string propname, object value)
    {
      if (GetPropertiesForInterface(@interface)[propname] != value)
      {
        GetType().GetProperty(propname).SetValue(this, value, null);
      }
    }

    private Dictionary<string, object> GetPropertiesForInterface(string interfaceName)
    {
      var properties = new Dictionary<string, object>();
      foreach (var @interface in GetType().GetInterfaces())
      {
        var interfaceAttributes =
          @interface.GetCustomAttributes(typeof(DBus.InterfaceAttribute), false);
        // TODO need to check this for DBus.InterfaceAttribute too.
        foreach (DBus.InterfaceAttribute attrib in interfaceAttributes)
        {
          if (string.IsNullOrWhiteSpace(interfaceName) || attrib.Name == interfaceName)
          {
            foreach(var property in @interface.GetProperties())
            {
              // TODO - do we need to handle indexed properties?
              properties.Add(property.Name, property.GetValue(this, null));
            }
          }
        }
      }
      return properties;
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

    public FakeDBusMenu()
    {
    }

    public void GetLayout(int parentId, int recursionDepth, string[] propertyNames,
                          out uint revision, out MenuItemLayout layout)
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
      item.properties.Add("visible", true);
      item.properties.Add("enabled", true);
      item.childeren = new object[0];
      items[0] = item;

      var rootMenuItemLayout = new MenuItemLayout();
      rootMenuItemLayout.id = 0;
      rootMenuItemLayout.properties = new Dictionary<string, object>();
      rootMenuItemLayout.properties.Add("label", "Label Empty");
      rootMenuItemLayout.properties.Add("visible", true);
      rootMenuItemLayout.properties.Add("enabled", true);
      rootMenuItemLayout.properties.Add("children-display", "submenu");
      rootMenuItemLayout.childeren = items;

      revision = 26;
      layout = rootMenuItemLayout;
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
      var properties = new MenuItemProperties[ids.Length];
      for (int i = 0; i < ids.Length; i++) {
        properties[i].id = ids[i];
        properties[i].properties = new Dictionary<string, object>();
        properties[i].properties.Add("children-display", "submenu");
        if (properties[i].id == 2)
          properties[i].properties.Add("label", "_File");
      }
      return properties;
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