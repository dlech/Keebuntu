using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace Keebuntu
{
  public class DefaultMenuItemProxy : IMenuItemProxy
  {
    private static Dictionary<string, string> mDisplayNameCache;
    private static Dictionary<string, object> mDefaultValueCache;

    public string Type { get { return GetDefaultValue<string>("Type"); } }

    public string Label { get { return GetDefaultValue<string>("Label"); } }

    public bool Enabled { get { return GetDefaultValue<bool>("Enabled"); } }

    public bool Visible { get { return GetDefaultValue<bool>("Visible"); } }

    public string IconName { get { return GetDefaultValue<string>("IconName"); } }

    public byte[] IconData { get { return GetDefaultValue<byte[]>("IconData"); } }

    public string[][] Shortcut { get { return GetDefaultValue<string[][]>("Shortcut"); } }

    public string ToggleType { get { return GetDefaultValue<string>("ToggleType"); } }

    public int ToggleState { get { return GetDefaultValue<int>("ToggleState"); } }

    public string ChildrenDisplay { get { return GetDefaultValue<string>("ChildrenDisplay"); } }

    public string Disposition { get { return GetDefaultValue<string>("Disposition"); } }
       
    public string AccessibleDesc { get { return GetDefaultValue<string>("AccessibleDesc"); } }

    static DefaultMenuItemProxy()
    {
      mDisplayNameCache = new Dictionary<string, string>();
      mDefaultValueCache = new Dictionary<string, object>();

      var menuItemProxyType = typeof(IMenuItemProxy);
      foreach (var property in menuItemProxyType.GetProperties()) {
        foreach (DisplayNameAttribute attribute in
          property.GetCustomAttributes(typeof(DisplayNameAttribute), true)) {
          mDisplayNameCache.Add(attribute.DisplayName, property.Name);
        }
        foreach (DefaultValueAttribute attribute in
          property.GetCustomAttributes(typeof(DefaultValueAttribute), true)) {
          mDefaultValueCache.Add(property.Name, attribute.Value);
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
