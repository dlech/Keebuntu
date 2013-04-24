using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace Keebuntu.DBus
{
  public class DefaultMenuItemProxy : IMenuItemProxy
  {
    private static Dictionary<string, string> mDisplayNameCache;
    private static Dictionary<string, object> mDefaultValueCache;

    #region IMenuItemProxy implementation

    public virtual string Type {
      get { return GetDefaultValue<string>("Type"); }
    }

    public virtual string Label {
      get { return GetDefaultValue<string>("Label"); }
    }

    public virtual bool Enabled {
      get { return GetDefaultValue<bool>("Enabled"); }
    }

    public virtual bool Visible {
      get { return GetDefaultValue<bool>("Visible"); }
    }

    public virtual string IconName {
      get { return GetDefaultValue<string>("IconName"); }
    }

    public virtual byte[] IconData {
      get { return GetDefaultValue<byte[]>("IconData"); }
    }

    public virtual string[][] Shortcut {
      get { return GetDefaultValue<string[][]>("Shortcut"); }
    }

    public virtual string ToggleType {
      get { return GetDefaultValue<string>("ToggleType"); }
    }

    public virtual int ToggleState {
      get { return GetDefaultValue<int>("ToggleState"); }
    }

    public virtual string ChildrenDisplay {
      get { return GetDefaultValue<string>("ChildrenDisplay"); }
    }

    public virtual string Disposition {
      get { return GetDefaultValue<string>("Disposition"); }
    }

    public virtual string AccessibleDesc {
      get { return GetDefaultValue<string>("AccessibleDesc"); }
    }

    public object GetValue(string propertyName)
    {
      var property = GetType().GetProperty(propertyName);
      if  (property == null)
      {
        if (mDisplayNameCache.TryGetValue(propertyName, out propertyName))
        {
          property = GetType().GetProperty(propertyName);
        }
      }
      if (property == null)
      {
        throw new ArgumentException("Unknown property: " + propertyName, "propertyName");
      }
      return property.GetValue(this, null);
    }

    public virtual IMenuItemProxy[] GetChildren()
    {
      return new IMenuItemProxy[0];
    }

    public virtual void OnEvent(string eventId, object data, uint timestamp)
    {
      return;
    }

    public virtual bool OnAboutToShow()
    {
      return false;
    }

    #endregion IMenuItemProxy implementation

    static DefaultMenuItemProxy()
    {
      mDisplayNameCache = new Dictionary<string, string>();
      mDefaultValueCache = new Dictionary<string, object>();
      UpdateCaches<IMenuItemProxy>();
    }

    protected static void UpdateCaches<T>() where T : IMenuItemProxy
    {
      foreach (var property in typeof(T).GetProperties()) {
        foreach (DisplayNameAttribute attribute in
          property.GetCustomAttributes(typeof(DisplayNameAttribute), true)) {
          mDisplayNameCache[attribute.DisplayName] = property.Name;
        }
        foreach (DefaultValueAttribute attribute in
          property.GetCustomAttributes(typeof(DefaultValueAttribute), true)) {
          mDefaultValueCache[property.Name] = attribute.Value;
        }
      }
    }

    public static T GetDefaultValue<T>(string propertyName)
    {
      return (T)GetDefaultValue(propertyName);
    }

    public static object GetDefaultValue(string propertyName)
    {
      object value;

      // special case for 'shortcut' since we can only pass a single dimensional
      // array in the DefaultValueAttribute
      if (propertyName.Equals("shortcut", StringComparison.OrdinalIgnoreCase))
      {
        return new string[0][];
      }

      if (mDefaultValueCache.TryGetValue(propertyName, out value))
      {
        return value;
      }
      string realPropertyName;
      if (mDisplayNameCache.TryGetValue(propertyName, out realPropertyName))
      {
        if (mDefaultValueCache.TryGetValue(realPropertyName, out value))
        {
          return value;
        }
      }
      throw new ArgumentException(
        string.Format("Unknown property: {0}", propertyName),
        "propertyName");
    }

    public static bool IsDefaultValue(string propertyName, object value)
    {
      if (propertyName == null) {
        throw new ArgumentNullException("propertyName");
      }
      if (value == null) {
        throw new ArgumentNullException("value");
      }

      var array = value as Array;
      if (array != null) {
        if (array.Length == 1) {
          var nestedArray = array.GetValue(0) as Array;
          if (nestedArray != null) {
            return nestedArray.Length == 0;
          }
        }
        return array.Length == 0;
      }
      return GetDefaultValue(propertyName).Equals(value);
    }

    public static string[] GetAllDisplayNames()
    {
      var names = new string[mDisplayNameCache.Count];
      mDisplayNameCache.Keys.CopyTo(names, 0);
      return names;
    }
  }
}
