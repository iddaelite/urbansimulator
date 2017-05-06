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

            RhinoApp.WriteLine("The Urban Simulator has begun.");

            urbanModel theUrbanModel = new urbanModel();

            if (!getPrecinct(theUrbanModel))             //Ask user to select a surface representing the Precinct
                return Result.Failure;

            generateRoadNetwork(theUrbanModel);         //Using the precinct, Generate a Road Network

            //createBlocks()                            //Using the road network, create blocks
            //subdivideBlocks()                         //Subdivide the blocks into Plots
            //instantiateBuildings()                    //Place buildings on each plot

            RhinoApp.WriteLine("The Urban Simulator is complete.");

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
                
            if(obj.ObjectCount == 1)
                model.precinctSrf = obj.Object(0).Surface();

            return true;
        }

        public bool generateRoadNetwork(urbanModel model)
        {

            //extract the border from the precinct surface - Temp Geometry
            Curve[] borderCrvs = model.precinctSrf.ToBrep().DuplicateNakedEdgeCurves(true, false);

            if(borderCrvs.Length > 0)
            {
                int noBorders = borderCrvs.Length;

                Random rnd = new Random();
                Curve theCrv = borderCrvs[rnd.Next(noBorders)];

                //select a random point on one of the edges
                double t = new Random().NextDouble();
                Plane perpFrm;

                Point3d pt = theCrv.PointAtNormalizedLength(t);
                theCrv.PerpendicularFrameAt(t, out perpFrm);

                Point3d pt2 = Point3d.Add(pt, perpFrm.XAxis);

                //Draw a line perpendicular
                Line ln = new Line(pt, pt2);
                Curve lnExt = ln.ToNurbsCurve().ExtendByLine(CurveEnd.End, borderCrvs);

                RhinoDoc.ActiveDoc.Objects.AddLine(lnExt.PointAtStart,lnExt.PointAtEnd);

                RhinoDoc.ActiveDoc.Objects.AddPoint(pt);

                RhinoDoc.ActiveDoc.Views.Redraw();

                


            }





            //Collect the line and repeat the process (select random pt and draw perp line) - recursion

            return true;
        }



    }
}
