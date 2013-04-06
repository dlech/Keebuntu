using System;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using DBus;
using com.canonical.dbusmenu;
using System.Diagnostics;
using System.IO;
using System.Drawing.Imaging;

namespace Keebuntu
{
  public class WinformsDBusMenu : com.canonical.dbusmenu.IDbusMenu
  {
    private readonly string[] supportedMenuItemProperties =
    {
      "type",
      "label",
      "enabled",
      "visible",
      "icon-name",
      "icon-data",
      "shortcut",
      "toggle-type",
      "toggle-state",
      "children-display",
      "disposition",
      "accessible-desc"
    };
    private List<ToolStripItem> mMenuItemList;
    private Form mMenuParentForm;
    private uint mRevision = 0;
    private object mLockObject = new object();

    // TODO - extract org.freedesktop.DBus.IProperties impementation to a base class
    #region org.freedesktop.DBus.IProperties

    public object Get(string @interface, string propname)
    {
      return GetPropertiesForInterface(@interface)[propname];
    }

    public IDictionary<string, object> GetAll(string @interface)
    {
      return GetPropertiesForInterface(@interface);
    }

    public void Set(string @interface, string propname, object value)
    {
      if (GetPropertiesForInterface(@interface)[propname] != value) {
        GetType().GetProperty(propname).SetValue(this, value, null);
      }
    }

    private Dictionary<string, object> GetPropertiesForInterface(string interfaceName)
    {
      var properties = new Dictionary<string, object>();
      foreach (var @interface in GetType().GetInterfaces()) {
        var interfaceAttributes =
          @interface.GetCustomAttributes(typeof(DBus.InterfaceAttribute), false);
        // TODO need to check this for DBus.InterfaceAttribute too.
        foreach (DBus.InterfaceAttribute attrib in interfaceAttributes) {
          if (string.IsNullOrWhiteSpace(interfaceName) || attrib.Name == interfaceName) {
            foreach (var property in @interface.GetProperties()) {
              // TODO - do we need to handle indexed properties?
              properties.Add(property.Name, property.GetValue(this, null));
            }
          }
        }
      }
      return properties;
    }

    #endregion

    public uint Version {
      // TODO - need to verify that this is OK
      get { return 3; }
    }

    public string TextDirection {
      get
      {
        return mMenuParentForm.RightToLeft == RightToLeft.Yes ? "rtl" : "ltr";
      }
    }

    public string Status {
      get {
        // TODO implement "notice" and property change support
        return "normal";
      }
    }

    public string[] IconThemePath { 
      get { return new string[0]; }
    }

    public WinformsDBusMenu(MenuStrip menu)
    {
      if (menu == null) {
        throw new ArgumentNullException("menu");
      }
      lock (mLockObject) {
        mMenuParentForm = menu.FindForm();

        mMenuItemList = new List<ToolStripItem>();

        var rootMenuItem = new ToolStripMenuItem();
        rootMenuItem.DropDownItems.AddRange(menu.Items);
        mMenuItemList.Insert(0, rootMenuItem);
        AddItemsToMenuItemList(menu.Items);

        if (Environment.GetEnvironmentVariable("APPMENU_DISPLAY_BOTH") != "1")
        {
          menu.Visible = false;
        }
      }
    }

    private void AddItemsToMenuItemList(ToolStripItemCollection items)
    {
      foreach (ToolStripItem item in items) {
        if (!mMenuItemList.Contains(item))
        {
          mMenuItemList.Add(item);
        }
        var dropDownItem = item as ToolStripDropDownItem;
        if (dropDownItem != null) {
          AddItemsToMenuItemList(dropDownItem.DropDownItems);
          dropDownItem.DropDown.ItemAdded += (sender, e) => 
          {
            if (!mMenuItemList.Contains(e.Item)) {
              mMenuItemList.Add(e.Item);
            }
            if (dropDownItem.DropDownItems.Count == 1) {
              OnItemPropertyUpdated(e.Item, "children-display");
            }
          };
          dropDownItem.DropDown.ItemRemoved += (sender, e) => 
          {
            // set to null instead of removing because we are using the index
            // as the id and we don't want to mess it up.
            var itemIndex = mMenuItemList.IndexOf(e.Item);
            mMenuItemList[itemIndex] = null;
            if (dropDownItem.DropDownItems.Count == 0) {
              OnItemPropertyUpdated(e.Item, "children-display");
            }
          };
          var menuItem = dropDownItem as ToolStripMenuItem;
          if (menuItem != null) {
            menuItem.CheckStateChanged += (sender, e) => 
              OnItemPropertyUpdated(menuItem, "toggle-state");
          }
        }
        item.TextChanged += (sender, e) =>
          OnItemPropertyUpdated(item, "label");
        item.EnabledChanged += (sender, e) =>
          OnItemPropertyUpdated(item, "enabled");
        item.AvailableChanged += (sender, e) =>
          OnItemPropertyUpdated(item, "visible");
      }
    }

    /// <summary>
    /// Gets the dbus property for an item.
    /// </summary>
    /// <returns>
    /// The item property.
    /// </returns>
    /// <param name='item'>
    /// Item.
    /// </param>
    /// <param name='property'>
    /// Property.
    /// </param>
    private object GetItemProperty(ToolStripItem item, string property)
    {
      if (item == null) {
        throw new ArgumentNullException("item");
      }
      if (property == null) {
        throw new ArgumentNullException("property");
      }

      ToolStripMenuItem menuItem;

      switch (property) {
        case "type":
          return item is ToolStripSeparator ? "separator" : "standard";
        case "label":
          return item.Text == null ? string.Empty : item.Text.Replace("&", "_");
        case "enabled":
          return item.Enabled;
        case "visible":
          return item.Available;
        case "icon-name":
          return String.Empty;
        case "icon-data":
          if (item.Image == null) {
            return new byte[0];
          }
          var memStream = new MemoryStream();
          item.Image.Save(memStream, ImageFormat.Png);
          return memStream.ToArray();
        case "shortcut":
          var keyList = new List<string>();
          menuItem = item as ToolStripMenuItem;
          if (menuItem != null) {
            if (menuItem.ShortcutKeys.HasFlag(Keys.Alt)) {
              keyList.Add("Alt");
            }
            if (menuItem.ShortcutKeys.HasFlag(Keys.Control)) {
              keyList.Add("Control");
            }
            if (menuItem.ShortcutKeys.HasFlag(Keys.Shift)) {
              keyList.Add("Shift");
            }
            var keyCode = menuItem.ShortcutKeys & Keys.KeyCode;
            if (keyCode != Keys.None) {
              keyList.Add(keyCode.ToString());
            }

          }
          var shortcutList = new string[1][];
          shortcutList[0] = keyList.ToArray();
          return shortcutList;
        case "toggle-type":
          menuItem = item as ToolStripMenuItem;
          if (menuItem != null) {
            if (menuItem.CheckOnClick) {
              return "checkmark";
            }
          }
          return String.Empty;
        case "toggle-state":
          menuItem = item as ToolStripMenuItem;
          if (menuItem != null) {             
            switch (menuItem.CheckState) {
              case CheckState.Checked:
                return 1;
              case CheckState.Unchecked:
                return 0;
              case CheckState.Indeterminate:
                return 2; // just for fun
              default:
                break;
            }
          }
          return -1;
        case "children-display":
          var dropDownItem = item as ToolStripDropDownItem;
          if (dropDownItem != null) {
            if (dropDownItem.HasDropDownItems) {
              return "submenu";
            }
          }
          return String.Empty;
        case "disposition":
          return "normal";
        case "accessible-desc":
          return item.AccessibleDescription ?? string.Empty;
      }
      throw new ArgumentException("Invalid property name", property);
    }

    private bool IsDefaultValue(string property, object value)
    {
      if (property == null) {
        throw new ArgumentNullException("property");
      }
      if (value == null) {
        throw new ArgumentNullException("value");
      }

      switch (property) {
        case "type":
          return value.Equals("standard");
        case "label":
          return value.Equals(String.Empty);
        case "enabled":
          return value.Equals(true);
        case "visible":
          return value.Equals(true);
        case "icon-name":
          return value.Equals(String.Empty);
        case "icon-data":
          return value is byte[] && (value as byte[]).Length == 0;
        case "shortcut":
          return value is string[][] && (value as string[][])[0].Length == 0;
        case "toggle-type":
          return value.Equals(String.Empty);
        case "toggle-state":
          return value.Equals(-1);
        case "children-display":
          return value.Equals(String.Empty);
        case "disposition":
          return value.Equals("normal");
        case "accessible-desc":
          return value.Equals(String.Empty);
      }
      throw new ArgumentException("Invalid property name", property);
    }

    public void GetLayout(int parentId, int recursionDepth, string[] propertyNames,
                          out uint revision, out MenuItemLayout layout)
    {
#if DEBUG
      Console.WriteLine("GetLayout - parentId:{0}, recursionDepth:{1}, propertyNames:{2}",
                        parentId, recursionDepth, string.Join(", ", propertyNames));
#endif
      if (propertyNames.Length == 0) {
        propertyNames = supportedMenuItemProperties;
      }

      revision = mRevision;

      var item = mMenuItemList[parentId];
      layout = CreateMenuItemLayout(item, 0, recursionDepth, propertyNames);
    }

    private MenuItemLayout CreateMenuItemLayout(ToolStripItem item,
                                                int depth, int maxDepth,
                                                string[] propertyNames)
    {    
#if DEBUG
      //Console.WriteLine("CreateMenuItemLayout - item:{0}, depth:{1}, maxDepth:{2}, propertyNames:{3}",
      //                  item, depth, maxDepth, string.Join(", ", propertyNames));
#endif
      var layout = new MenuItemLayout();
      layout.id = mMenuItemList.IndexOf(item);
      layout.properties = new Dictionary<string, object>();
      foreach (var property in propertyNames) {
        try {
          var value = GetItemProperty(item, property);
          if (!IsDefaultValue(property, value))
          {
            layout.properties.Add(property, value);
          }
        } catch (Exception ex) {
          Debug.Fail(ex.ToString());
        }
      }
      var childList = new List<object>();
      if (maxDepth < 0 || depth < maxDepth) {
        var dropDownItem = item as ToolStripDropDownItem;
        if (dropDownItem != null) {
          foreach (ToolStripItem childItem in dropDownItem.DropDownItems) {
            childList.Add(CreateMenuItemLayout(childItem, depth + 1, maxDepth,
                                               propertyNames)
            );
          }
        }
      }
      layout.childeren = childList.ToArray();
      return layout;
    }
   
    public com.canonical.dbusmenu.MenuItem[] GetGroupProperties(int[] ids,
                                                                string[] propertyNames)
    {
#if DEBUG
      Console.WriteLine("GetGroupProperties - ids:{0}, propertyNames:{1}",
                        string.Join(", ", ids), string.Join(", ", propertyNames));
#endif
      if (propertyNames.Length == 0) {
        propertyNames = supportedMenuItemProperties;
      }
      var itemList = new List<com.canonical.dbusmenu.MenuItem>();
      foreach (var id in ids) {
        var item = mMenuItemList[id];
        var menuItem = new com.canonical.dbusmenu.MenuItem();
        menuItem.id = id;
        menuItem.properties = new Dictionary<string, object>();
        foreach (var property in propertyNames) {
          try {
            var value = GetItemProperty(item, property);
            if (!IsDefaultValue(property, value))
            {
              menuItem.properties.Add(property, value);
            }
          } catch (Exception ex) {
            Debug.Fail(ex.ToString());
          }
        }
        itemList.Add(menuItem);
      }

      return itemList.ToArray();
    }
   
    public object GetProperty(int id, string name)
    {
#if DEBUG
      Console.WriteLine("GetProperty - id:{0}, name:{1}", id, name);
#endif
      var item = mMenuItemList[id];
      return GetItemProperty(item, name);
    }
   
    public void Event(int id, string eventId, object data, uint timestamp)
    {
#if DEBUG
      Console.WriteLine("Event - id:{0}, eventId:{1}, data:{2}, timestamp:{3}",
                        id, eventId, data, timestamp);
#endif
      var item = mMenuItemList[id];
      switch (eventId) {
        case "clicked":
          InvokeParentForm(() => item.PerformClick());
          break;
        case "hovered":
          // TODO - hack hovered event?
          break;
        case "opened":
          break;
        case "closed":
          break;
      }
    }

    public int[] EventGroup(MenuEvent[] events)
    {
#if DEBUG
      Console.WriteLine("EventGroup - events:{0}", events.Length);
#endif
      var errorList = new List<int>();
      foreach (var @event in events) {
        try {
          Event(@event.id,  @event.eventId,  @event.data,  @event.timestamp);
        } catch (Exception) {
          errorList.Add(@event.id);
        }
      }
      return errorList.ToArray();
    }

    public bool AboutToShow(int id)
    {
#if DEBUG
      Console.WriteLine("AboutToShow - id:{0}", id);
#endif
      var item = mMenuItemList[id];
      // try to use item so we throw and exception if it is null
      var dummy = item.Name;
      // TODO - is there anything in winforms here?
      return true;
    }

    public void AboutToShowGroup(int[] ids, out int[] updatesNeeded, out int[] idErrors)
    {
#if DEBUG
      Console.WriteLine("AboutToShowgroup - ids:{0}", ids.Length);
#endif
      var needsUpdateList = new List<int>();
      var errorList = new List<int>();
      foreach (var id in ids) {
        try {
          if (AboutToShow(id)) {
            needsUpdateList.Add(id);
          }
        } catch (Exception) {
          errorList.Add(id);
        }
      }
      updatesNeeded = needsUpdateList.ToArray();
      idErrors = errorList.ToArray();
    }
       
    public event ItemsPropertiesUpdatedHandler ItemsPropertiesUpdated;

    /// <summary>
    /// Helper class for single property change. 
    /// Raises the items properties updated event.
    /// </summary>
    private void OnItemPropertyUpdated(ToolStripItem item, string property)
    {
      var properties = new com.canonical.dbusmenu.MenuItem();
      properties.id = mMenuItemList.IndexOf(item);
      properties.properties = new Dictionary<string, object>();
      properties.properties.Add(property, GetItemProperty(item, property));
      OnItemsPropertiesUpdated(new[] { properties }, null);
      OnLayoutUpdated(mMenuItemList.IndexOf(item));
    }

    private void OnItemsPropertiesUpdated(com.canonical.dbusmenu.MenuItem[] updatedProps,
                                          MenuItemPropertyDescriptor[] removedProps)
    {
#if DEBUG
      Console.WriteLine("OnItemsPropertiesUpdated - ");
      Console.Write("  updatedProps - ");
      if (updatedProps == null) {
        Console.WriteLine("Empty");
      } else {
        foreach(var prop in updatedProps)
        {
          Console.WriteLine("id:{0}, properties:{1}",
                            prop.id, string.Join(", ", prop.properties));

        }
      }
      Console.Write("  removedProps - ");
      if (removedProps == null) {
        Console.WriteLine("Empty");
      } else {
        foreach(var prop in removedProps)
        {
          Console.WriteLine("id:{0}, properties:{1}",
                            prop.id, string.Join(", ", prop.properties));

        }
      }
#endif
      if (ItemsPropertiesUpdated != null) {
        if (updatedProps == null) {
          updatedProps = new com.canonical.dbusmenu.MenuItem[0];
        }
        if (removedProps == null) {
          removedProps = new MenuItemPropertyDescriptor[0];
        }
        foreach (var method in ItemsPropertiesUpdated.GetInvocationList()) {
          try {
            method.DynamicInvoke(updatedProps, removedProps);
          } catch (Exception ex) {
            Debug.Fail(ex.ToString());
          }
        }
      }
    }

    public event LayoutUpdatedHandler LayoutUpdated;

    private void OnLayoutUpdated(int parent)
    {
#if DEBUG
      Console.WriteLine("OnLayoutUpdated - parent:{0}", parent);
#endif
      if (LayoutUpdated != null) {
        foreach (var method in LayoutUpdated.GetInvocationList()) {
          try {
            method.DynamicInvoke(mRevision++, parent);
          } catch (Exception ex) {
            Debug.Fail(ex.ToString());
          }
        }
      }
    }

    public event ItemActivationRequestedHandler ItemActivationRequested;

    private void OnItemActivationRequested(int id)
    {
#if DEBUG
      Console.WriteLine("OnItemActivationRequested - id:{0}", id);
#endif
      if (ItemActivationRequested != null) {
        foreach (var method in ItemActivationRequested.GetInvocationList()) {
          try {
            method.DynamicInvoke(id, (uint)DateTime.Now.Ticks);
          } catch (Exception ex) {
            Debug.Fail(ex.ToString());
          }
        }
      }
    }

    private void InvokeParentForm(Action action)
    {
      if (mMenuParentForm.InvokeRequired) {
        mMenuParentForm.Invoke(action);
      } else {
        action.Invoke();
      }
    }
  }
}