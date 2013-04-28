using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.IO;
using ImageMagick.MagickCore;

namespace ImageMagick.MagickWand
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


    #region Properties

    public ImageType ImageType
    {
      get {
        return MagickGetImageType(mWand);
      }
      set {
        if (!MagickSetImageType(mWand, value)) {
          //TODO - implement exceptions
          throw new Exception();
        }
      }
    }

    #endregion Properties


    #region IClonable implementation

    /// <summary>
    /// Makes an exact copy of the specified wand.
    /// </summary>
    public object Clone()
    {
      return new MagickWand(CloneMagickWand(mWand));
    }

    #endregion IClonable implementation


    public bool EvaluateImage(MagickEvaluateOperator op, double value)
    {
      // TODO - implement exception checking
      return MagickEvaluateImage(mWand, op, value);
    }

    public byte[] GetImageBlob()
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

    public bool ReadImageBlob(byte[] blob)
    {
      GCHandle pinnedArray = GCHandle.Alloc(blob, GCHandleType.Pinned);
      IntPtr pointer = pinnedArray.AddrOfPinnedObject();

      bool result = MagickReadImageBlob(mWand, pointer, (IntPtr)blob.Length);

      pinnedArray.Free();
      // TODO - implement exception checking
      return result;
    }

    public bool ResizeImage(int width, int heigth, FilterType filter, double blur)
    {
      // TODO - implement exception checking
      return MagickResizeImage(mWand, (IntPtr)width, (IntPtr)heigth, filter, blur);
    }

    #region Magic Wand Methods - from magick-wand.c

    [DllImport("libMagickWand", EntryPoint = "ClearMagickWand")]
    private static extern void ClearMagickWand(IntPtr wand);

    [DllImport("libMagickWand", EntryPoint = "CloneMagickWand")]
    private static extern IntPtr CloneMagickWand(IntPtr wand);

    [DllImport("libMagickWand", EntryPoint = "DestroyMagickWand")]
    private static extern IntPtr DestroyMagickWand(IntPtr wand);

    [DllImport("libMagickWand", EntryPoint = "IsMagickWand")]
    private static extern bool IsMagickWand(IntPtr wand);

    [DllImport("libMagickWand", EntryPoint = "MagickClearException")]
    private static extern bool MagickClearException(IntPtr wand);

    [DllImport("libMagickWand", EntryPoint = "MagickGetException")]
    private static extern IntPtr MagickGetException(IntPtr wand,
                                                    ExceptionType severity);

    [DllImport("libMagickWand", EntryPoint = "MagickGetExceptionType")]
    private static extern ExceptionType MagickGetExceptionType(IntPtr wand);

    [DllImport("libMagickWand", EntryPoint = "MagickGetIteratorIndex")]
    private static extern UIntPtr MagickGetIteratorIndex(IntPtr wand);

    [DllImport("libMagickWand", EntryPoint = "MagickQueryConfigureOption")]
    private static extern IntPtr MagickQueryConfigureOption(string option);

    [DllImport("libMagickWand", EntryPoint = "MagickQueryConfigureOptions")]
    private static extern IntPtr MagickQueryConfigureOptions(string pattern,
                                                             [Out] out UIntPtr number_options);

    [DllImport("libMagickWand", EntryPoint = "MagickQueryFontMetrics")]
    private static extern IntPtr MagickQueryFontMetrics(IntPtr wand,
                                                        IntPtr drawing_wand,
                                                        string text);

    [DllImport("libMagickWand", EntryPoint = "MagickQueryMultilineFontMetrics")]
    private static extern IntPtr MagickQueryMultilineFontMetrics(IntPtr wand,
                                                                 IntPtr drawing_wand,
                                                                 string text);

    [DllImport("libMagickWand", EntryPoint = "MagickQueryFonts")]
    private static extern IntPtr MagickQueryFonts(string pattern,
                                                  [Out] out UIntPtr number_fonts);

    [DllImport("libMagickWand", EntryPoint = "MagickQueryFormats")]
    private static extern IntPtr MagickQueryFormats(string pattern,
                                                    [Out] out UIntPtr number_formats);

    [DllImport("libMagickWand", EntryPoint = "MagickRelinquishMemory")]
    private static extern IntPtr MagickRelinquishMemory(IntPtr resource);

    [DllImport("libMagickWand", EntryPoint = "MagickSetFirstIterator")]
    private static extern void MagickSetFirstIterator(IntPtr wand);

    [DllImport("libMagickWand", EntryPoint = "MagickResetIterator")]
    private static extern void MagickResetIterator(IntPtr wand);

    [DllImport("libMagickWand", EntryPoint = "MagickSetIteratorIndex")]
    private static extern bool MagickSetIteratorIndex(IntPtr wand, IntPtr index);

    [DllImport("libMagickWand", EntryPoint = "MagickSetLastIterator")]
    private static extern void MagickSetLastIterator(IntPtr wand);

    [DllImport("libMagickWand", EntryPoint = "MagickWandGenesis")]
    private static extern void MagickWandGenesis();

    [DllImport("libMagickWand", EntryPoint = "MagickWandTerminus")]
    private static extern void MagickWandTerminus();

    [DllImport("libMagickWand", EntryPoint = "NewMagickWand")]
    private static extern IntPtr NewMagickWand();

    [DllImport("libMagickWand", EntryPoint = "NewMagickWandFromImage")]
    private static extern IntPtr NewMagickWandFromImage(IntPtr image);

    #endregion Magic Wand Methods - from magick-wand.c


    [DllImport("libMagickWand", EntryPoint = "MagickEvaluateImage")]
    private static extern bool MagickEvaluateImage(IntPtr wand,
                                                   MagickEvaluateOperator op,
                                                   double value);

    [DllImport("libMagickWand", EntryPoint = "MagickFunctionImage")]
    private static extern bool MagickFunctionImage(IntPtr wand,
                                                   MagickFunction function,
                                                   UIntPtr number_arguments,
                                                   double[] arguments);

    [DllImport("libMagickWand", EntryPoint = "MagickGetImageBlob")]
    private static extern IntPtr MagickGetImageBlob(IntPtr wand,
                                                    [Out] out IntPtr length);

    [DllImport("libMagickWand", EntryPoint = "MagickGetImageHeight")]
    private static extern IntPtr MagickGetImageHeight(IntPtr wand);

    [DllImport("libMagickWand", EntryPoint = "MagickGetImageType")]
    private static extern ImageType MagickGetImageType(IntPtr wand);

    [DllImport("libMagickWand", EntryPoint = "MagickGetImageWidth")]
    private static extern IntPtr MagickGetImageWidth(IntPtr wand);

    [DllImport("libMagickWand", EntryPoint = "MagickResizeImage")]
    private static extern bool MagickResizeImage(IntPtr mgck_wand,
                                                 IntPtr columns,
                                                 IntPtr rows,
                                                 FilterType filter_type,
                                                 double blur);

    [DllImport("libMagickWand", EntryPoint = "MagickReadImageBlob")]
    private static extern bool MagickReadImageBlob(IntPtr wand,
                                                   IntPtr blob,
                                                   IntPtr length);

    [DllImport("libMagickWand", EntryPoint = "MagickSetImageOpacity")]
    private static extern bool MagickSetImageOpacity(IntPtr wand,
                                                     double alpha);

    [DllImport("libMagickWand", EntryPoint = "MagickSetImageType")]
    private static extern bool MagickSetImageType(IntPtr wand,
                                                  ImageType image_type);

  }
}
