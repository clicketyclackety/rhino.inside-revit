using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RhinoInside.Revit.External.DB.Extensions
{
  using static NumericTolerance;

  internal static class CurveEqualityComparer
  {
    /// <summary>
    /// IEqualityComparer for <see cref="Autodesk.Revit.DB.Curve"/>
    /// that compares <see cref="Autodesk.Revit.DB.Curve.Reference"/>.
    /// </summary>
    /// <remarks>
    /// Reference <see cref="Reference.GlobalPoint"/> and <see cref="Reference.UVPoint"/> are ignored.
    /// </remarks>
    public static readonly IEqualityComparer<Curve> Reference = default(ReferenceComparer);

    struct ReferenceComparer : IEqualityComparer<Curve>
    {
      public bool Equals(Curve x, Curve y)
      {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        var xReference = x.Reference;
        var yReference = y.Reference;

        if (ReferenceEquals(xReference, yReference)) return true;
        if (xReference is null || xReference is null) return false;

        if (xReference.ElementReferenceType != yReference.ElementReferenceType) return false;
        if (xReference.ElementId != yReference.ElementId) return false;
        if (xReference.LinkedElementId != yReference.LinkedElementId) return false;
        return true;
      }

      public int GetHashCode(Curve value)
      {
        if (value is null) return 0;
        var reference = value.Reference;
        if (reference is null) return 0;

        int hashCode = 1861411795;
        hashCode = hashCode * -1521134295 + reference.ElementReferenceType.GetHashCode();
        hashCode = hashCode * -1521134295 + reference.ElementId.GetHashCode();
        hashCode = hashCode * -1521134295 + reference.LinkedElementId.GetHashCode();
        return hashCode;
      }
    }
  }

  public static class CurveExtension
  {
    public static bool IsSameKindAs(this Curve self, Curve other)
    {
      return self.IsBound == other.IsBound && self.GetType() == other.GetType();
    }

    public static bool GetRawParameters(this Curve curve, out double min, out double max)
    {
      if (curve.IsBound)
      {
        min = curve.GetEndParameter(CurveEnd.Start);
        max = curve.GetEndParameter(CurveEnd.End);
      }
      else if (curve.IsCyclic)
      {
        min = 0.0;
        max = curve.Period;
      }
      else switch (curve)
      {
        case Line line:
          min = 0.0;
          max = line.Direction.GetLength();
          break;

        case HermiteSpline hermite:
          using (var parameters = hermite.Parameters)
          {
            min = parameters.get_Item(0);
            max = parameters.get_Item(parameters.Size-1);
          }
          break;

        case NurbSpline spline:
          using (var knots = spline.Knots)
          {
            min = knots.get_Item(0);
            max = + knots.get_Item(knots.Size-1);
          }
          break;

        default:
          throw new NotImplementedException($"{nameof(GetRawParameters)} is not implemented for {curve.GetType()}.");
      }

      return curve.IsBound;
    }

    public static double GetNormalizedParameter(this Curve curve, double rawParameter)
    {
      curve.GetRawParameters(out var min, out var max);
      var mid = 0.5 * (min + max);
      var factor = 1.0 / (max - min);
      return rawParameter < mid ?
        (rawParameter - min) * factor + 0.0 :
        (rawParameter - max) * factor + 1.0;
    }

    public static double GetRawParameter(this Curve curve, double normalizedParameter)
    {
      curve.GetRawParameters(out var min, out var max);
      return min + normalizedParameter * (max - min);
    }

    public static Curve CreateBounded(this Curve crv, double startParameter, double endParameter)
    {
      crv = crv.CreateTransformed(Transform.Identity);
      crv.MakeBound(startParameter, endParameter);
      return crv;
    }

    public static IEnumerable<Curve> ToBoundedCurves(this Curve curve)
    {
      if (!curve.IsBound)
      {
        switch (curve)
        {
          case Line line:
            yield return line.CreateBounded(-15_000.0, +15_000.0);
            yield break;

          case Arc arc:
            yield return arc.CreateBounded(0.0, Math.PI);
            yield return arc.CreateBounded(Math.PI, 2.0 * Math.PI);
            yield break;

          case Ellipse ellipse:
            yield return ellipse.CreateBounded(0.0, Math.PI);
            yield return ellipse.CreateBounded(Math.PI, 2.0 * Math.PI);
            yield break;

          default:
            throw new NotImplementedException($"{nameof(ToBoundedCurves)} is not implemented for {curve.GetType()}.");
        }
      }

      yield return curve;
    }

    public static CurveArray ToCurveArray(this IEnumerable<Curve> curves)
    {
      var curveArray = new CurveArray();
      foreach (var curve in curves)
        curveArray.Append(curve);

      return curveArray;
    }

    #region TryGetLocation
    public static bool TryGetLocation(this Line curve, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      var curveDirection = curve.Direction;
      if (!curveDirection.IsAlmostEqualTo(XYZExtension.Zero, DefaultTolerance))
      {
        if (curve.IsBound)
        {
          origin = curve.Evaluate(0.5, true);
          basisX = curveDirection.Normalize(0D);
          basisY = basisX.PerpVector();
          return true;
        }
        else
        {
          origin = curve.Origin;
          basisX = curveDirection.Normalize(0D);
          basisY = basisX.PerpVector();
          return true;
        }
      }

      origin = basisX = basisY = default;
      return false;
    }

    public static bool TryGetLocation(this Arc curve, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      if (curve.IsBound)
      {
        var start = curve.GetEndPoint(0);
        var end = curve.GetEndPoint(1);
        var curveDirection = end - start;

        if (!curveDirection.IsAlmostEqualTo(XYZExtension.Zero, DefaultTolerance))
        {
          origin = start + (curveDirection * 0.5);
          basisX = curveDirection.Normalize(0D);
          basisY = curve.Normal.CrossProduct(basisX).Normalize(0D);
          return true;
        }
      }
      else
      {
        origin = curve.Center;
        basisX = curve.XDirection;
        basisY = curve.YDirection;
        return true;
      }

      origin = basisX = basisY = default;
      return false;
    }

    public static bool TryGetLocation(this Ellipse curve, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      if (curve.IsBound)
      {
        var start = curve.GetEndPoint(0);
        var end = curve.GetEndPoint(1);
        var curveDirection = end - start;

        if (!curveDirection.IsAlmostEqualTo(XYZExtension.Zero, DefaultTolerance))
        {
          origin = start + (curveDirection * 0.5);
          basisX = curveDirection.Normalize(0D);
          basisY = curve.Normal.CrossProduct(basisX).Normalize(0D);
          return true;
        }
      }
      else
      {
        origin = curve.Center;
        basisX = curve.XDirection;
        basisY = curve.YDirection;
        return true;
      }

      origin = basisX = basisY = default;
      return false;
    }

    public static bool TryGetLocation(this CylindricalHelix curve, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      origin = curve.BasePoint;
      basisX = curve.XVector;
      basisY = curve.YVector;

      return true;
    }

    public static bool TryGetLocation(this NurbSpline curve, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      if (!curve.IsBound)
        throw new NotImplementedException();

      var start = curve.GetEndPoint(CurveEnd.Start);
      var end = curve.GetEndPoint(CurveEnd.End);
      var curveDirection = end - start;

      var ctrlPoints = curve.CtrlPoints;
      var cov = Transform.Identity;
      int closed = curveDirection.IsZeroLength() ? 1 : 0;
      cov.SetCovariance(ctrlPoints.Skip(closed));

      if (closed == 0)
      {
        basisX = curveDirection.Normalize(0D);

        if (cov.TryGetInverse(out var inverse))
        {
          var basisZ = inverse.GetPrincipalComponent(0D);
          origin = new PlaneEquation(cov.Origin, basisZ).Project(start + (curveDirection * 0.5));

          basisY = basisZ.CrossProduct(basisX).Normalize(0D);
        }
        else
        {
          origin = start + (curveDirection * 0.5);

          var principal = cov.GetPrincipalComponent(0D);
          var plane = new PlaneEquation(principal, 0.0);
          for (int p = 0; p < ctrlPoints.Count; ++p)
            ctrlPoints[p] = plane.Project(ctrlPoints[p]);

          cov.SetCovariance(ctrlPoints, plane.Project(cov.Origin));
          basisY = cov.GetPrincipalComponent(0D);

          if (basisY.IsZeroLength())
            basisY = basisX.PerpVector();
        }
      }
      else
      {
        origin = cov.Origin;
        basisX = cov.GetPrincipalComponent(0D);

        if (cov.TryGetInverse(out var inverse))
        {
          var basisZ = inverse.GetPrincipalComponent(0D);
          basisY = basisZ.CrossProduct(basisX).Normalize(0D);
        }
        else
        {
          var plane = new PlaneEquation(basisX, 0.0);
          for (int p = 0; p < ctrlPoints.Count; ++p)
            ctrlPoints[p] = plane.Project(ctrlPoints[p]);

          cov.SetCovariance(ctrlPoints.Skip(closed), plane.Project(cov.Origin));
          basisY = cov.GetPrincipalComponent(0D);

          if (basisY.IsZeroLength())
            basisY = basisX.PerpVector();
        }
      }

      return true;
    }

    public static bool TryGetLocation(this PolyLine curve, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      switch (curve.NumberOfCoordinates)
      {
        case 0:
          origin = basisX = basisY = default;
          return false;

        case 1:
          origin = curve.GetCoordinate(0);
          basisX = XYZExtension.BasisX;
          basisY = XYZExtension.BasisY;
          return true;

        default:
          var start = curve.GetCoordinate(0);
          var end = curve.GetCoordinate(curve.NumberOfCoordinates - 1);
          if (start.IsAlmostEqualTo(end))
          {
            origin = XYZExtension.ComputeMeanPoint(curve.GetCoordinates());
            var axis = start - origin;
            basisX = axis.Normalize(0D);
            basisY = basisX.PerpVector();
          }
          else
          {
            var axis = end - start;
            origin = start + (axis * 0.5);
            basisX = axis.Normalize(0D);
            basisY = basisX.PerpVector();
          }
          return true;
      }
    }

    public static bool TryGetLocation(this Curve curve, out XYZ origin, out XYZ basisX, out XYZ basisY)
    {
      switch (curve)
      {
        case Line line:               return line.TryGetLocation(out origin, out basisX, out basisY);
        case Arc arc:                 return arc.TryGetLocation(out origin, out basisX, out basisY);
        case Ellipse ellipse:         return ellipse.TryGetLocation(out origin, out basisX, out basisY);
        case CylindricalHelix helix:  return helix.TryGetLocation(out origin, out basisX, out basisY);
        case NurbSpline spline:       return spline.TryGetLocation(out origin, out basisX, out basisY);
        default: throw new NotImplementedException();
      }
    }
    #endregion

    #region TryGetPlane
    public static bool TryGetPlane(this Curve curve, out Plane plane)
    {
      using(var loop = new CurveLoop())
      {
        loop.Append(curve);
        if (loop.HasPlane())
        {
          plane = loop.GetPlane();
          return true;
        }
      }

      plane = default;
      return default;
    }
    #endregion

    #region TryGetCentroid
    public static bool TryGetCentroid(this IEnumerable<Curve> curves, out XYZ centroid)
    {
      centroid = XYZExtension.Zero;
      var count = 0;
      foreach (var curve in curves)
      {
        var t0 = curve.IsBound ? curve.GetEndParameter(CurveEnd.Start) : 0.0;
        var t1 = curve.IsBound ? curve.GetEndParameter(CurveEnd.End) : curve.IsCyclic ? curve.Period : 1.0;
        centroid += curve.Evaluate(t0, normalized: false);
        centroid += curve.Evaluate(t1, normalized: false);
        count += 2;
      }

      centroid /= count;
      return count > 0;
    }
    #endregion
  }
}
