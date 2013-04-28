using System;

namespace ImageMagick.MagickCore
{
  public enum InterpolatePixelMethod
  {
    UndefinedInterpolatePixel,
    AverageInterpolatePixel, /* Average 4 nearest neighbours */
    BicubicInterpolatePixel, /* Catmull-Rom interpolation */
    BilinearInterpolatePixel, /* Triangular filter interpolation */
    FilterInterpolatePixel, /* Use resize filter - (very slow) */
    IntegerInterpolatePixel, /* Integer (floor) interpolation */
    MeshInterpolatePixel, /* Triangular mesh interpolation */
    NearestNeighborInterpolatePixel, /* Nearest neighbour only */
    SplineInterpolatePixel, /* Cubic Spline (blurred) interpolation */
    Average9InterpolatePixel, /* Average 9 nearest neighbours */
    Average16InterpolatePixel, /* Average 16 nearest neighbours */
    BlendInterpolatePixel, /* blend of nearest 1, 2 or 4 pixels */
    BackgroundInterpolatePixel, /* just return background color */
    CatromInterpolatePixel /* Catmull-Rom interpolation */
  }
}

