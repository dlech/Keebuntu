using System;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Keebuntu
{
  public class MenuStripProxy : DefaultMenuItemProxy
  {
    private MenuStrip mMenu;

    public MenuStripProxy(MenuStrip menu)
    {
      mMenu = menu;
    }

    public override string ChildrenDisplay {
      get {
        return mMenu.Items.Count > 0 ? "submenu" : base.ChildrenDisplay;
      }
    }

    public override IMenuItemProxy[] GetChildren()
    {
      var itemList = new List<ToolStripItemProxy>();
      foreach(ToolStripMenuItem item in mMenu.Items)
      {
        itemList.Add(ToolStripItemProxy.GetProxyFromCache(item));
      }
      return itemList.ToArray();
    }
  }
}

