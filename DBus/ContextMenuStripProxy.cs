using System;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Keebuntu.DBus
{
  public class ContextMenuStripProxy : DefaultMenuItemProxy
  {
    private ContextMenuStrip menu;

    public ContextMenuStripProxy(ContextMenuStrip menu)
    {
      this.menu = menu;
    }

    public override string ChildrenDisplay {
      get {
        return menu.Items.Count > 0 ? "submenu" : base.ChildrenDisplay;
      }
    }

    public override IMenuItemProxy[] GetChildren()
    {
      var itemList = new List<ToolStripItemProxy>();
      foreach(ToolStripItem item in menu.Items)
      {
        itemList.Add(ToolStripItemProxy.GetProxyFromCache(item));
      }
      return itemList.ToArray();
    }
  }
}
