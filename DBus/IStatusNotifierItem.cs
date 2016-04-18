using System;
using DBus;

// documentation comes from:
// https://freedesktop.org/wiki/Specifications/StatusNotifierItem/StatusNotifierItem/

namespace org.kde.StatusNotifierItem
{
  /// <summary>
  /// Icon pixmap.
  /// </summary>
  /// <remarks>
  /// All the icons can be transferred over the bus by a particular serialization
  /// of their data, capabe of representing multiple resolutions of the same image
  /// or a brief animation of images of the same size.
  /// </remarks>
  public struct KDbusImageVector
  {
    /// <summary>
    /// The width.
    /// </summary>
    public int Width;

    /// <summary>
    /// The height.
    /// </summary>
    public int Height;

    /// <summary>
    /// The image data.
    /// </summary>
    /// <remarks>
    /// The data is represented in ARGB32 format and is in the network byte order
    /// </remarks>
    public byte[] Data;

    public static KDbusImageVector None {
      get {
        return new KDbusImageVector() { Data = new byte[0] };
      }
    }
  }

  public struct Tooltip
  {
    /// <summary>
    /// Freedesktop-compliant name for an icon.
    /// </summary>
    public string IconName;

    /// <summary>
    /// The icon data.
    /// </summary>
    public KDbusImageVector IconPixmap;

    /// <summary>
    /// The title.
    /// </summary>
    public string Title;

    /// <summary>
    /// descriptive text for this tooltip.
    /// </summary>
    /// <remarks>
    /// It can contain also a subset of the HTML markup language, for a list of
    /// allowed tags see Section Markup.
    /// </remarks>
    public string Description;
  }

  [Interface("org.kde.StatusNotifierItem")]
  public interface IStatusNotifierItem : org.freedesktop.DBus.Properties
  {
    /// <summary>
    /// Describes the category of this item.
    /// </summary>
    /// <remarks>
    /// The allowed values for the Category property are:
    /// "ApplicationStatus": The item describes the status of a generic application,
    /// for instance the current state of a media player. In the case where the
    /// category of the item can not be known, such as when the item is being
    /// proxied from another incompatible or emulated system, "ApplicationStatus"
    /// can be used a sensible default fallback.
    /// "Communications": The item describes the status of communication oriented
    /// applications, like an instant messenger or an email client.
    /// "SystemServices": The item describes services of the system not seen as
    /// a stand alone application by the user, such as an indicator for the
    /// activity of a disk indexing service.
    /// "Hardware": The item describes the state and control of a particular
    /// hardware, such as an indicator of the battery charge or sound card
    /// volume control.
    /// </remarks>
    string Category { get; }

    /// <summary>
    /// It's a name that should be unique for this application and consistent
    /// between sessions, such as the application name itself.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// It's a name that describes the application, it can be more descriptive
    /// than Id.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Describes the status of this item or of the associated application.
    /// </summary>
    /// <remarks>>
    /// The allowed values for the Status property are:
    /// "Passive": The item doesn't convey important information to the user,
    /// it can be considered an "idle" status and is likely that visualizations
    /// will chose to hide it.
    /// "Active": The item is active, is more important that the item will be
    /// shown in some way to the user.
    /// "NeedsAttention": The item carries really important information for the
    /// user, such as battery charge running out and is wants to incentive the
    /// direct user intervention. Visualizations should emphasize in some way
    /// the items with "NeedsAttention" status.
    /// </remarks>
    string Status { get; }

    /// <summary>
    /// It's the windowing-system dependent identifier for a window, the
    /// application can chose one of its windows to be available trough this
    /// property or just set 0 if it's not interested.
    /// </summary>
    uint WindowId { get; }

    bool ItemIsMenu { get; }

    /// <summary>
    /// The StatusNotifierItem can carry an icon that can be used by the
    /// visualization to identify the item.
    /// </summary>
    /// <remarks>
    /// An icon can either be identified by its Freedesktop-compliant icon name,
    /// carried by this property of by the icon data itself, carried by the
    /// property IconPixmap. Visualizations are encouraged to prefer icon names
    /// over icon pixmaps if both are available (FIXME: still not very defined:
    /// could the the pixmap used as fallback if an icon name is not found?)
    /// </remarks>
    string IconName { get; }

    /// <summary>
    /// Carries an ARGB32 binary representation of the icon, the format of icon
    /// data used in the specification is described in Section Icons
    /// </summary>
    //KDbusImageVector IconPixmap { get; }

    /// <summary>
    /// The Freedesktop-compliant name of an icon. This can be used by the
    /// visualization to indicate extra state information, for instance as an overlay for the main icon.
    /// </summary>
    //string OverlayIconName { get; }

    /// <summary>
    /// ARGB32 binary representation of the overlay icon described in
    /// <see cref="OverlayIconName"/>
    /// </summary>
    //KDbusImageVector OverlayIconPixmap { get; }

    /// <summary>
    /// The Freedesktop-compliant name of an icon. this can be used by the
    /// visualization to indicate that the item is in RequestingAttention state.
    /// </summary>
    //string AttentionIconName { get; }

    /// <summary>
    /// ARGB32 binary representation of the requesting attention icon describe
    /// in <see cref="AttentionIconName"/>.
    /// </summary>
    //KDbusImageVector AttentionIconPixmap { get; }

    /// <summary>
    /// An item can also specify an animation associated to the
    /// "RequestingAttention" state.
    /// </summary>
    /// <remarks>
    /// This should be either a Freedesktop-compliant icon name or a full path.
    /// The visualization can chose between the movie or AttentionIconPixmap
    /// (or using neither of those) at its discretion.
    /// </remarks>
    //string AttentionMovieName { get; }

    /// <summary>
    /// Data structure that describes extra information associated to this item,
    /// that can be visualized for instance by a tooltip (or by any other means
    /// the visualization consider appropriate).
    /// </summary>
    //Tooltip Tooltip { get; }

    /// <summary>
    /// Asks the status notifier item to show a context menu.
    /// </summary>
    /// <remarks>>
    /// This is typically a consequence of user input, such as mouse right click
    /// over the graphical representation of the item.
    ///
    /// The x and y parameters are in screen coordinates and is to be considered
    /// an hint to the item where to show eventual windows (if any).
    /// </remarks>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    void ContextMenu(int x, int y);

    /// <summary>
    /// Asks the status notifier item for activation.
    /// </summary>
    /// <remarks>
    /// This is typically a consequence of user input, such as mouse left click
    /// over the graphical representation of the item. The application will
    /// perform any task is considered appropriate as an activation request.
    ///
    /// The x and y parameters are in screen coordinates and is to be considered
    /// an hint to the item where to show eventual windows (if any).
    /// </remarks>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    void Activate(int x, int y);

    void SecondaryActivate(int x, int y);

    /// <summary>
    /// The user asked for a scroll action.
    /// </summary>
    /// <remarks>
    /// This is caused from input such as mouse wheel over the graphical
    /// representation of the item.
    ///
    /// The delta parameter represent the amount of scroll, the orientation
    /// parameter represent the horizontal or vertical orientation of the scroll
    /// request and its legal values are horizontal and vertical.
    /// </remarks>
    /// <param name="delta">Delta.</param>
    /// <param name="orientation">Orientation.</param>
    void Scroll(int delta, string orientation);

    /// <summary>
    /// The item has a new title.
    /// </summary>
    /// <remarks>
    /// The graphical representation should read it again immediately.
    /// </remarks>
    event Action NewTitle;

    /// <summary>
    /// The item has a new icon.
    /// </summary>
    /// <remarks>
    /// The graphical representation should read it again immediately.
    /// </remarks>
    event Action NewIcon;

    /// <summary>
    /// The item has a new attention icon.
    /// </summary>
    /// <remarks>
    /// The graphical representation should read it again immediately.
    /// </remarks>
    //event Action NewAttentionIcon;

    /// <summary>
    /// The item has a new overlay icon.
    /// </summary>
    /// <remarks>
    /// The graphical representation should read it again immediately.
    /// </remarks>
    //event Action NewOverlayIcon;

    /// <summary>
    /// The item has a new tooltip.
    /// </summary>
    /// <remarks>
    /// The graphical representation should read it again immediately.
    /// </remarks>
    //event Action NewToolTip;

    /// <summary>
    /// The item has a new status, that is passed as an argument of the signal.
    /// </summary>
    event Action<string> NewStatus;
  }
}
