using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.IO;

namespace ImageMagick
{
  public class MagickWand : ICloneable
  {
    // Inital impementation based on:
    // http://www.toptensoftware.com/Articles/17/High-Quality-Image-Resampling-in-Mono-Linux

    protected IntPtr mWand = IntPtr.Zero;

    static MagickWand()
    {
      MagickWandGenesis();
      AppDomain.CurrentDomain.ProcessExit += (sender, e) => MagickWandTerminus();
    }

    protected MagickWand(IntPtr wand)
    {
      mWand = wand;
    }

    public MagickWand() : this(NewMagickWand()) { }

    /// <summary>
    /// Deallocates memory associated with an MagickWand
    /// </summary>
    ~MagickWand()
    {
      if (mWand != IntPtr.Zero) {
        mWand = DestroyMagickWand(mWand);
      }
    }

    #region IClonable implementation

    /// <summary>
    /// Makes an exact copy of the specified wand.
    /// </summary>
    public object Clone()
    {
      return new MagickWand(CloneMagickWand(mWand));
    }

    #endregion IClonable implementation


    public static byte[] ResizeImage(System.Drawing.Image image,
                                     int newWidth, int newHeight)
    {
      var stream = new MemoryStream();
      image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
      var wand = new MagickWand();
      wand.ReadImageBlob(stream.ToArray());
      MagickResizeImage(wand.mWand,
                        (IntPtr)newWidth,
                        (IntPtr)newHeight,
                        Filter.Mitchell,
                        1.0);
      return wand.GetImageBlob();
    }

    public enum Filter
    {
      Undefined,
      Point,
      Box,
      Triangle,
      Hermite,
      Hanning,
      Hamming,
      Blackman,
      Gaussian,
      Quadratic,
      Cubic,
      Catrom,
      Mitchell,
      Lanczos,
      Bessel,
      Sinc,
      Kaiser,
      Welsh,
      Parzen,
      Lagrange,
      Bohman,
      Bartlett,
      SincFast
    };

    public enum InterpolatePixel
    {
      Undefined,
      Average,
      Bicubic,
      Bilinear,
      Filter,
      Integer,
      Mesh,
      NearestNeighbor,
      Spline
    };

    private enum ExceptionType
    {
      UndefinedException,
      WarningException = 300,
      ResourceLimitWarning = 300,
      TypeWarning = 305,
      OptionWarning = 310,
      DelegateWarning = 315,
      MissingDelegateWarning = 320,
      CorruptImageWarning = 325,
      FileOpenWarning = 330,
      BlobWarning = 335,
      StreamWarning = 340,
      CacheWarning = 345,
      CoderWarning = 350,
      FilterWarning = 352,
      ModuleWarning = 355,
      DrawWarning = 360,
      ImageWarning = 365,
      WandWarning = 370,
      RandomWarning = 375,
      XServerWarning = 380,
      MonitorWarning = 385,
      RegistryWarning = 390,
      ConfigureWarning = 395,
      PolicyWarning = 399,
      ErrorException = 400,
      ResourceLimitError = 400,
      TypeError = 405,
      OptionError = 410,
      DelegateError = 415,
      MissingDelegateError = 420,
      CorruptImageError = 425,
      FileOpenError = 430,
      BlobError = 435,
      StreamError = 440,
      CacheError = 445,
      CoderError = 450,
      FilterError = 452,
      ModuleError = 455,
      DrawError = 460,
      ImageError = 465,
      WandError = 470,
      RandomError = 475,
      XServerError = 480,
      MonitorError = 485,
      RegistryError = 490,
      ConfigureError = 495,
      PolicyError = 499,
      FatalErrorException = 700,
      ResourceLimitFatalError = 700,
      TypeFatalError = 705,
      OptionFatalError = 710,
      DelegateFatalError = 715,
      MissingDelegateFatalError = 720,
      CorruptImageFatalError = 725,
      FileOpenFatalError = 730,
      BlobFatalError = 735,
      StreamFatalError = 740,
      CacheFatalError = 745,
      CoderFatalError = 750,
      FilterFatalError = 752,
      ModuleFatalError = 755,
      DrawFatalError = 760,
      ImageFatalError = 765,
      WandFatalError = 770,
      RandomFatalError = 775,
      XServerFatalError = 780,
      MonitorFatalError = 785,
      RegistryFatalError = 790,
      ConfigureFatalError = 795,
      PolicyFatalError = 799
    }

    struct SemaphoreInfo
    {
      public IntPtr mutex;
      public int id;
      public IntPtr reference_count;
      public UIntPtr signature;
    }

    private struct ExceptionInfo
    {
      public ExceptionType severity;
      public int error_number;
      public string reason;
      public string description;
      public IntPtr exceptions;
      public bool relinquish;
      public SemaphoreInfo semaphore;
      public UIntPtr signature;
    }

    #region Magic Wand Methods - from magick-wand.c

    [DllImport("libMagickWand.so.5", EntryPoint = "ClearMagickWand")]
    private static extern void ClearMagickWand(IntPtr wand);

    [DllImport("libMagickWand.so.5", EntryPoint = "CloneMagickWand")]
    private static extern IntPtr CloneMagickWand(IntPtr wand);

    [DllImport("libMagickWand.so.5", EntryPoint = "DestroyMagickWand")]
    private static extern IntPtr DestroyMagickWand(IntPtr wand);

    [DllImport("libMagickWand.so.5", EntryPoint = "IsMagickWand")]
    private static extern bool IsMagickWand(IntPtr wand);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickClearException")]
    private static extern bool MagickClearException(IntPtr wand);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickGetException")]
    private static extern IntPtr MagickGetException(IntPtr wand,
                                                    ExceptionType severity);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickGetExceptionType")]
    private static extern ExceptionType MagickGetExceptionType(IntPtr wand);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickGetIteratorIndex")]
    private static extern UIntPtr MagickGetIteratorIndex(IntPtr wand);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickQueryConfigureOption")]
    private static extern IntPtr MagickQueryConfigureOption(string option);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickQueryConfigureOptions")]
    private static extern IntPtr MagickQueryConfigureOptions(string pattern,
                                                             [Out] out UIntPtr number_options);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickQueryFontMetrics")]
    private static extern IntPtr MagickQueryFontMetrics(IntPtr wand,
                                                        IntPtr drawing_wand,
                                                        string text);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickQueryMultilineFontMetrics")]
    private static extern IntPtr MagickQueryMultilineFontMetrics(IntPtr wand,
                                                                 IntPtr drawing_wand,
                                                                 string text);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickQueryFonts")]
    private static extern IntPtr MagickQueryFonts(string pattern,
                                                  [Out] out UIntPtr number_fonts);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickQueryFormats")]
    private static extern IntPtr MagickQueryFormats(string pattern,
                                                    [Out] out UIntPtr number_formats);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickRelinquishMemory")]
    private static extern IntPtr MagickRelinquishMemory(IntPtr resource);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickSetFirstIterator")]
    private static extern void MagickSetFirstIterator(IntPtr wand);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickResetIterator")]
    private static extern void MagickResetIterator(IntPtr wand);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickSetIteratorIndex")]
    private static extern bool MagickSetIteratorIndex(IntPtr wand, IntPtr index);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickSetLastIterator")]
    private static extern void MagickSetLastIterator(IntPtr wand);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickWandGenesis")]
    private static extern void MagickWandGenesis();

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickWandTerminus")]
    private static extern void MagickWandTerminus();

    [DllImport("libMagickWand.so.5", EntryPoint = "NewMagickWand")]
    private static extern IntPtr NewMagickWand();

    [DllImport("libMagickWand.so.5", EntryPoint = "NewMagickWandFromImage")]
    private static extern IntPtr NewMagickWandFromImage(IntPtr image);

    #endregion Magic Wand Methods - from magick-wand.c


    [DllImport("libMagickWand.so.5", EntryPoint = "MagickGetImageBlob")]
    private static extern IntPtr MagickGetImageBlob(IntPtr wand,
                                                    [Out] out IntPtr length);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickGetImageHeight")]
    private static extern IntPtr MagickGetImageHeight(IntPtr wand);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickGetImageWidth")]
    private static extern IntPtr MagickGetImageWidth(IntPtr wand);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickResizeImage")]
    private static extern bool MagickResizeImage(IntPtr mgck_wand,
                                                 IntPtr columns,
                                                 IntPtr rows,
                                                 Filter filter_type,
                                                 double blur);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickReadImageBlob")]
    private static extern bool MagickReadImageBlob(IntPtr wand,
                                                   IntPtr blob,
                                                   IntPtr length);

    [DllImport("libMagickWand.so.5", EntryPoint = "MagickSetImageOpacity")]
    private static extern bool MagickSetImageOpacity(IntPtr wand,
                                                     double alpha);



    // Interop
    private bool ReadImageBlob(byte[] blob)
    {
      GCHandle pinnedArray = GCHandle.Alloc(blob, GCHandleType.Pinned);
      IntPtr pointer = pinnedArray.AddrOfPinnedObject();

      bool result = MagickReadImageBlob(mWand, pointer, (IntPtr)blob.Length);

      pinnedArray.Free();

      return result;
    }

    // Interop
    private byte[] GetImageBlob()
    {
      // Get the blob
      IntPtr len;
      IntPtr buf=MagickGetImageBlob(mWand, out len);

      // Copy it
      var dest=new byte[len.ToInt32()];
      Marshal.Copy(buf, dest, 0, len.ToInt32());

      // Relinquish
      MagickRelinquishMemory(buf);

      return dest;
    }
  }
}
