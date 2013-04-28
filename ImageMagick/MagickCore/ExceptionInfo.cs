using System;

namespace ImageMagick.MagickCore
{
  public struct ExceptionInfo
  {
    public ExceptionType severity;
    public int error_number;
    public IntPtr reason;
    public IntPtr description;
    public IntPtr exceptions;
    public bool relinquish;
    public SemaphoreInfo semaphore;
    public UIntPtr signature;
  }
}
