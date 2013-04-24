using System;
using System.ComponentModel;

namespace Keebuntu.DBus
{
  public interface IMenuItemProxy
  {
    [DisplayName("type")]
    [DefaultValue("standard")]
    string Type { get; }

    [DisplayName("label")]
    [DefaultValue("")]
    string Label { get; }

    [DisplayName("enabled")]
    [DefaultValue(true)]
    bool Enabled { get; }

    [DisplayName("visible")]
    [DefaultValue(true)]
    bool Visible { get; }

    [DisplayName("icon-name")]
    [DefaultValue("")]
    string IconName { get; }

    [DisplayName("icon-data")]
    [DefaultValue(new byte[0])]
    byte[] IconData { get; }

    [DisplayName("shortcut")]
    [DefaultValue(new string[0])]
    string[][] Shortcut { get; }

    [DisplayName("toggle-type")]
    [DefaultValue("")]
    string ToggleType { get; }

    [DisplayName("toggle-state")]
    [DefaultValue(-1)]
    int ToggleState { get; }

    [DisplayName("children-display")]
    [DefaultValue("")]
    string ChildrenDisplay { get; }

    [DisplayName("disposition")]
    [DefaultValue("normal")]
    string Disposition { get; }
       
    [DisplayName("accessible-desc")]
    [DefaultValue("")]
    string AccessibleDesc { get; }

    object GetValue(string propertyName);
    IMenuItemProxy[] GetChildren();

    void OnEvent(string eventId, object data, uint timestamp);
    bool OnAboutToShow();
  }
}

