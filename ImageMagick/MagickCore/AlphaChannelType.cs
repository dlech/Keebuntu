using System;

namespace ImageMagick.MagickCore
{
  public enum AlphaChannelType
  {
    UndefinedAlphaChannel,
    ActivateAlphaChannel,
    BackgroundAlphaChannel,
    CopyAlphaChannel,
    DeactivateAlphaChannel,
    ExtractAlphaChannel,
    OpaqueAlphaChannel,
    [Obsolete()]
    ResetAlphaChannel, /* deprecated */
    SetAlphaChannel,
    ShapeAlphaChannel,
    TransparentAlphaChannel,
    FlattenAlphaChannel,
    RemoveAlphaChannel
  }
}

