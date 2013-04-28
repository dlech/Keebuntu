using System;

namespace ImageMagick.MagickCore
{
  public struct SemaphoreInfo
  {
    public IntPtr mutex;
    public int id;
    public IntPtr reference_count;
    public UIntPtr signature;
  }
}

