using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

using Autodesk.Revit;
using Autodesk.Revit.DB;
using Dynamo.TypeSystem;
using Microsoft.FSharp.Collections;

using Value = Dynamo.FScheme.Value;
using Dynamo.FSchemeInterop;
using Dynamo.Revit;
using Dynamo.Connectors;
using Dynamo.Utilities;

namespace Dynamo.Nodes
{
    [NodeName("Project Point On Curve")]
    [NodeCategory(BuiltinNodeCategories.MODIFYGEOMETRY_INTERSECT)]
    [NodeDescription("Project a point onto a curve.")]
    public class dynProjectPointOnCurve : dynRevitTransactionNode, IDrawable, IClearable
    {
        public dynProjectPointOnCurve()
        {
            InPortData.Add(new PortData("xyz", "The point to be projected.", new ObjectType(typeof(XYZ))));
            InPortData.Add(new PortData("crv", "The curve on which to project the point.", new ObjectType(typeof(Curve))));

            OutPortData.Add(new PortData("xyz", "The nearest point on the curve.", new ObjectType(typeof(XYZ))));
            OutPortData.Add(new PortData("t", "The unnormalized parameter on the curve.", new NumberType()));
            OutPortData.Add(new PortData("d", "The distance from the point to the curve .", new NumberType()));

            RegisterAllPorts();
        }

        public override void Evaluate(FSharpList<Value> args, Dictionary<PortData, Value> outPuts)
        {
            var xyz = (XYZ)((Value.Container)args[0]).Item;
            var crv = (Curve)((Value.Container)args[1]).Item;

            IntersectionResult ir = crv.Project(xyz);
            XYZ pt = ir.XYZPoint;
            double t = ir.Parameter;
            double d = ir.Distance;

            Pts.Add(pt);

            outPuts[OutPortData[0]] = Value.NewContainer(pt);
            outPuts[OutPortData[1]] = Value.NewNumber(t);
            outPuts[OutPortData[2]] = Value.NewNumber(d);
        }

        protected List<XYZ> Pts = new List<XYZ>();
        public override void Draw()
        {
            if (RenderDescription == null)
                RenderDescription = new RenderDescription();
            else
                RenderDescription.ClearAll();

            foreach (XYZ pt in Pts)
                RenderDescription.points.Add(new Point3D(pt.X, pt.Y, pt.Z));
        }

        public void ClearReferences()
        {
            Pts.Clear();
        }
    }

    [NodeName("Project Point On Face")]
    [NodeCategory(BuiltinNodeCategories.MODIFYGEOMETRY_INTERSECT)]
    [NodeDescription("Project a point onto a face.")]
    public class dynProjectPointOnFace : dynRevitTransactionNode, IClearable
    {
        public dynProjectPointOnFace()
        {
            InPortData.Add(new PortData("xyz", "The point to be projected.", new ObjectType(typeof(XYZ))));
            InPortData.Add(new PortData("face", "The face on which to project the point.", new ObjectType(typeof(Face))));

            OutPortData.Add(new PortData("xyz", "The nearest point to the projected point on the face.", new ObjectType(typeof(XYZ))));
            OutPortData.Add(new PortData("uv", "The UV coordinates of the nearest point on the face..", new ObjectType(typeof(UV))));
            OutPortData.Add(new PortData("d", "The distance from the point to the face", new NumberType()));
            OutPortData.Add(new PortData("edge", "The edge if projected point is near an edge.", new ObjectType(typeof(Edge))));
            OutPortData.Add(new PortData("edge t", "The parameter of the nearest point on the edge.", new NumberType()));

            RegisterAllPorts();
        }

        public override void Evaluate(FSharpList<Value> args, Dictionary<PortData, Value> outPuts)
        {
            var xyz = (XYZ)((Value.Container)args[0]).Item;
            var face = (Face)((Value.Container)args[1]).Item;

            IntersectionResult ir = face.Project(xyz);
            XYZ pt = ir.XYZPoint;
            UV uv = ir.UVPoint;
            double d = ir.Distance;
            Edge e = null;
            try
            {
                e = ir.EdgeObject;
            }
            catch { }
            double et = 0;
            try
            {
                et = ir.EdgeParameter;
            }
            catch { }

            outPuts[OutPortData[0]] = Value.NewContainer(xyz);
            outPuts[OutPortData[0]] = Value.NewContainer(uv);
            outPuts[OutPortData[0]] = Value.NewNumber(d);
            outPuts[OutPortData[0]] = Value.NewContainer(e);
            outPuts[OutPortData[0]] = Value.NewNumber(et);

            Pts.Add(pt);
        }

        protected List<XYZ> Pts = new List<XYZ>();
        public override void Draw()
        {
            if (RenderDescription == null)
                RenderDescription = new RenderDescription();
            else
                RenderDescription.ClearAll();

            foreach (XYZ pt in Pts)
                RenderDescription.points.Add(new Point3D(pt.X, pt.Y, pt.Z));
        }

        public void ClearReferences()
        {
            Pts.Clear();
        }
    }
}
