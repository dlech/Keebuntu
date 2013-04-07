using System;
using DBus;

// TODO - find docs on this - only function tested/used is EntryActivated event

// based on panel-main.c from unity

namespace com.canonical.Unity.Panel
{
  public struct ObjectInfo
  {
    public string a;
    public string b;
    public string c;
    public string d;
    public byte e;
    public byte f;
    public uint g;
    public string h;
    public byte i;
    public byte j;
    public int k;
  }

  public struct Geometry
  {
    public string a;
    public int b;
    public int c;
    public int d;
    public int e;
  }

  public struct EntryGeometry
  {
    public int a;
    public int b;
    public uint c;
    public uint d;
  }

  public delegate void EntryActivatedHandler(string entry_id,
                                             EntryGeometry entry_geometry);

  public delegate void ReSyncHandler(string indicator_id);

  public delegate void EntryActivateRequestHandler(string entry_id);

  public delegate void EntryShowNowChangedHaneler(string entry_id, byte show_now_state);

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

    event EntryShowNowChangedHaneler EntryShowNowChanged;
  }
}

