using System;
using DBus;

// based on panel-main.c from unity (pre-trusty)

namespace com.canonical.Unity.Panel.Service
{
  public struct ObjectInfo
  {
    public string indicator_id;
    public string id;
    public string name_hint;
    public string label_text;
    public bool label_is_sensitive;
    public bool label_is_visible;
    public uint image_type;
    public string image_data; // base64 encoded
    public bool image_is_sensitive;
    public bool image_is_visible;
    public int priority;
  }

  public struct Geometry
  {
    public string entry_id;
    public int x;
    public int y;
    public int width;
    public int height;
  }

  public struct EntryGeometry
  {
    public int x;
    public int y;
    public uint width;
    public uint height;
  }

  public delegate void EntryActivatedHandler(string entry_id,
                                             EntryGeometry entry_geometry);

  public delegate void ReSyncHandler(string indicator_id);

  public delegate void EntryActivateRequestHandler(string entry_id);

  public delegate void EntryShowNowChangedHandler(string entry_id, byte show_now_state);

  [Interface("com.canonical.Unity.Panel.Service")]
  public interface IService
  {
    [return: Argument("state")]
    ObjectInfo[] Sync();

    [return: Argument("state")]
    ObjectInfo[] SyncOne(string indicator_id);

    void SyncGeometries(string panel_id, Geometry[] geometries);

    void ShowEntry(string entry_id, uint xid, int x, int y, uint button);

    void ShowAppMenu(uint xid, int x, int y);

    void SecondaryActivateEntry(string entry_id);

    void ScrollEntry(string entry_id, int delta);

    event EntryActivatedHandler EntryActivated;

    event ReSyncHandler ReSync;

    event EntryActivateRequestHandler EntryActivateRequest;

    event EntryShowNowChangedHandler EntryShowNowChanged;
  }
}

