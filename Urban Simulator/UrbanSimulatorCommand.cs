using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace Urban_Simulator
{
    [System.Runtime.InteropServices.Guid("82dbcba5-8bf9-49fb-ac3c-fa4ad71f1c95")]
    public class UrbanSimulatorCommand : Command
    {
        public UrbanSimulatorCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static UrbanSimulatorCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "UrbanSimulator"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            RhinoDoc.ActiveDoc.Views.RedrawEnabled = false;

            RhinoApp.WriteLine("The Urban Simulator has begun.");

            urbanModel theUrbanModel = new urbanModel();

            if (!getPrecinct(theUrbanModel))             //Ask user to select a surface representing the Precinct
                return Result.Failure;

            if (!generateRoadNetwork(theUrbanModel))     //Using the precinct, Generate a Road Network
                return Result.Failure;

            createBlocks(theUrbanModel);                //Using the road network, create blocks

            subdivideBlocks(theUrbanModel, 30, 20);     //Subdivide the blocks into Plots

            RhinoApp.WriteLine("The Urban Simulator is complete.");

            RhinoDoc.ActiveDoc.Views.RedrawEnabled = true;

            RhinoDoc.ActiveDoc.Views.Redraw();

            return Result.Success;
        }


        public bool getPrecinct(urbanModel model)
        {

            GetObject obj = new GetObject();
            obj.GeometryFilter = ObjectType.Surface;
            obj.SetCommandPrompt("Please select a Surface representing your Precinct");

            GetResult res = obj.Get();

            if (res != GetResult.Object)
            {
                RhinoApp.WriteLine("User failed to select a surface.");
                return false;
            }

            if (obj.ObjectCount == 1)
                model.precinctSrf = obj.Object(0).Surface();

            return true;
        }

        public bool generateRoadNetwork(urbanModel model)
        {

            int noIterations = 6;

            Random rndRoadT = new Random();

            List<Curve> obstCrvs = new List<Curve>();

            //extract the border from the precinct surface - Temp Geometry
            Curve[] borderCrvs = model.precinctSrf.ToBrep().DuplicateNakedEdgeCurves(true, false);

            foreach (Curve itCrv in borderCrvs)
                obstCrvs.Add(itCrv);


            if (borderCrvs.Length > 0)
            {
                int noBorders = borderCrvs.Length;

                Random rnd = new Random();
                Curve theCrv = borderCrvs[rnd.Next(noBorders)];

                recursivePerpLine(theCrv, ref obstCrvs, rndRoadT, 1, noIterations);

            }

            model.roadNetwork = obstCrvs;

            if (obstCrvs.Count > borderCrvs.Length)
                return true;
            else
                return false;

        }

        public bool recursivePerpLine(Curve inpCrv, ref List<Curve> inpObst, Random inpRnd, int dir, int cnt)
        {
            if (cnt < 1)
                return false;

            //select a random point on one of the edges
            double t = inpRnd.Next(20, 80) / 100.0;
            Plane perpFrm;

            Point3d pt = inpCrv.PointAtNormalizedLength(t);
            inpCrv.PerpendicularFrameAt(t, out perpFrm);

            Point3d pt2 = Point3d.Add(pt, perpFrm.XAxis * dir);

            //Draw a line perpendicular
            Line ln = new Line(pt, pt2);
            Curve lnExt = ln.ToNurbsCurve().ExtendByLine(CurveEnd.End, inpObst);

            if (lnExt == null)
                return false;

            inpObst.Add(lnExt);

            //RhinoDoc.ActiveDoc.Objects.AddLine(lnExt.PointAtStart, lnExt.PointAtEnd);
            //RhinoDoc.ActiveDoc.Objects.AddPoint(pt);
            //RhinoDoc.ActiveDoc.Views.Redraw();

            recursivePerpLine(lnExt, ref inpObst, inpRnd, 1, cnt - 1);
            recursivePerpLine(lnExt, ref inpObst, inpRnd, -1, cnt - 1);

            return true;

        }

        public bool createBlocks(urbanModel model)
        {

            Random blockType = new Random();

            Brep precinctPolySurface = model.precinctSrf.ToBrep().Faces[0].Split(model.roadNetwork, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);

            List<block> blocks = new List<block>();

            foreach (BrepFace itBF in precinctPolySurface.Faces)
            {
                Brep itBlock = itBF.DuplicateFace(false);
                itBlock.Faces.ShrinkFaces();

                int theBlockType = blockType.Next(4);

                blocks.Add(new block(itBlock, theBlockType));

                RhinoDoc.ActiveDoc.Objects.AddBrep(itBlock);
            }

            if (blocks.Count > 0)
            {
                model.blocks = blocks;
                return true;
            }
            else
            {
                return false;
            }

        }

        public bool subdivideBlocks(urbanModel model, int minPlotDepth, int maxPlotWidth)
        {
           
            foreach(block itBlock in model.blocks)
            {

                Brep itSrf = itBlock.blockSrf;
                itBlock.plots = new List<plot>();

                Curve[] borderCrvs = itSrf.DuplicateNakedEdgeCurves(true, false);

                List<Curve> splitLines = new List<Curve>();

                itSrf.Faces[0].SetDomain(0, new Interval(0, 1));
                itSrf.Faces[0].SetDomain(1, new Interval(0, 1));

                Point3d pt1 = itSrf.Faces[0].PointAt(0, 0);
                Point3d pt2 = itSrf.Faces[0].PointAt(0, 1);
                Point3d pt3 = itSrf.Faces[0].PointAt(1, 1);
                Point3d pt4 = itSrf.Faces[0].PointAt(1, 0);

                double length = pt1.DistanceTo(pt2);
                double width = pt1.DistanceTo(pt4);

                Point3d sdPt1 = new Point3d();
                Point3d sdPt2 = new Point3d();

                if (length > width) //Length is wider
                {
                    if( width > (minPlotDepth * 2) ) //Suitable for Subdivision
                    {
                        //Create a subdividing line
                        sdPt1 = itSrf.Surfaces[0].PointAt(0.5, 0);
                        sdPt2 = itSrf.Surfaces[0].PointAt(0.5, 1);
                    }
                }
                else //Width is wider
                {
                    if (length > (minPlotDepth * 2)) //Suitable for Subdivision
                    {
                        //Create a subdividing line
                        sdPt1 = itSrf.Surfaces[0].PointAt(0, 0.5);
                        sdPt2 = itSrf.Surfaces[0].PointAt(1, 0.5);
                    }
                }


                Line subDLine = new Line(sdPt1, sdPt2);
                Curve subDCrv = subDLine.ToNurbsCurve();

                splitLines.Add(subDCrv);

                double crvLength = subDCrv.GetLength();
                double noPlots = Math.Floor(crvLength / maxPlotWidth);

                for(int t = 0; t < noPlots; t++)
                {
                    double tVal = t * (1 / noPlots);

                    Plane perpFrm;

                    Point3d evalPt = subDCrv.PointAtNormalizedLength(tVal);
                    subDCrv.PerpendicularFrameAt(tVal, out perpFrm);

                    Point3d ptPer2Up = Point3d.Add(evalPt, perpFrm.XAxis);
                    Point3d ptPer2Down = Point3d.Add(evalPt, -perpFrm.XAxis);

                    //Draw a line perpendicular
                    Line ln1 = new Line(evalPt, ptPer2Up);
                    Line ln2 = new Line(evalPt, ptPer2Down);

                    Curve lnExt1 = ln1.ToNurbsCurve().ExtendByLine(CurveEnd.End, borderCrvs);
                    Curve lnExt2 = ln2.ToNurbsCurve().ExtendByLine(CurveEnd.End, borderCrvs);

                    splitLines.Add(lnExt1);
                    splitLines.Add(lnExt2);
                    
                }

                Brep plotPolySurface = itSrf.Faces[0].Split(splitLines, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);

                foreach (BrepFace itBF in plotPolySurface.Faces)
                {
                    Brep itPlot = itBF.DuplicateFace(false);
                    itPlot.Faces.ShrinkFaces();
                    itBlock.plots.Add(new plot(itPlot, itBlock.type));
                    RhinoDoc.ActiveDoc.Objects.AddBrep(itPlot);
                }

                RhinoDoc.ActiveDoc.Views.Redraw();

            }

            return true;
        }

    }
}
