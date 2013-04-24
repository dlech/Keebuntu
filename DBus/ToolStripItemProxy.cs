using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Drawing.Imaging;

namespace Keebuntu.DBus
{
  public class ToolStripItemProxy : DefaultMenuItemProxy
  {
    private ToolStripItem mItem;
    private static Dictionary<ToolStripItem, ToolStripItemProxy> mProxyCache;

    static ToolStripItemProxy()
    {
      mProxyCache = new Dictionary<ToolStripItem, ToolStripItemProxy>();
    }

    public ToolStripItemProxy(ToolStripItem item)
    {
      mItem = item;
    }

    public override string Type {
      get {
        if (mItem is ToolStripSeparator)
        {
          return "separator";
        }
        return base.Type;
      }
    }

    public override string Label {
      get {
        return mItem.Text == null ? base.Label : mItem.Text.Replace("&", "_");
      }
    }

    public override bool Enabled {
      get {
        return mItem.Enabled;
      }
    }

    public override bool Visible {
      get {
        return mItem.Available;
      }
    }

    public override string IconName {
      get {
        return base.IconName;
      }
    }

    public override byte[] IconData {
      get {
        if (mItem.Image == null) {
            return new byte[0];
          }
          var memStream = new MemoryStream();
          mItem.Image.Save(memStream, ImageFormat.Png);
          // TODO - use imagemagick or something to gray icon if menu item is disabled
          return memStream.ToArray();
      }
    }

    public override string[][] Shortcut {
      get {
        var keyList = new List<string>();
          var menuItem = mItem as ToolStripMenuItem;
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
      }
    }

    public override string ToggleType {
      get {
        var menuItem = mItem as ToolStripMenuItem;
          if (menuItem != null) {
            if (menuItem.CheckOnClick) {
              return "checkmark";
            }
          }
        return base.ToggleType;
      }
    }

    public override int ToggleState {
      get {
        var menuItem = mItem as ToolStripMenuItem;
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
        return base.ToggleState;
      }
    }

    public override string ChildrenDisplay {
      get {
        var dropDownItem = mItem as ToolStripDropDownItem;
          if (dropDownItem != null) {
            if (dropDownItem.HasDropDownItems) {
              return "submenu";
            }
          }
        return base.ChildrenDisplay;
      }
    }

    public override string Disposition {
      get {
        return base.Disposition;
      }
    }

    public override string AccessibleDesc {
      get {
        return mItem.AccessibleDescription ?? base.AccessibleDesc;
      }
    }

    public override IMenuItemProxy[] GetChildren()
    {
      var dropDownItem = mItem as ToolStripDropDownItem;
      if (dropDownItem != null)
      {
        var itemList = new List<ToolStripItemProxy>();
        foreach(ToolStripItem item in dropDownItem.DropDownItems)
        {
          itemList.Add(GetProxyFromCache(item));
        }
        return itemList.ToArray();
      }
      return base.GetChildren();
    }

    public static ToolStripItemProxy GetProxyFromCache(ToolStripItem item)
    {
      ToolStripItemProxy proxy;
      if (!mProxyCache.TryGetValue(item, out proxy))
      {
        proxy = new ToolStripItemProxy(item);
        mProxyCache.Add(item, proxy);
      }
      return proxy;
    }

    public override void OnEvent(string eventId, object data, uint timestamp)
    {
      switch (eventId) {
        case "clicked":
          InvokeWinformsThread(() => mItem.PerformClick());
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

    private void InvokeWinformsThread(Action action)
    {
      var parent = mItem.GetCurrentParent();
      if (parent.InvokeRequired) {
        parent.Invoke(action);
      } else {
        action.Invoke();
      }
    }
  }
}

