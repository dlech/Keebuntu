using System;

namespace ImageMagick.MagickCore
{
  public enum FilterType
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
    Jinc,
    Sinc,
    SincFast,
    Kaiser,
    Welsh,
    Parzen,
    Bohman,
    Bartlett,
    Lagrange,
    Lanczos,
    LanczosSharp,
    Lanczos2,
    Lanczos2Sharp,
    Robidoux,
    RobidouxSharp,
    Cosine,
    Spline,
    LanczosRadius,
    Sentinel /* a count of all the filters, not a real filter */
  };
}

