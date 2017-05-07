using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace Urban_Simulator
{
    public class urbanModel
    {

        public string name = "Urban Model";
        public Surface precinctSrf;
        public List<Curve> roadNetwork;
        public List<block> blocks;

        public urbanModel()
        {

        }

    }
   
    public class block
    {

        public int type; //0 = park, 1 = low rise, 2 = mid rise, 3 = high rise
        public Brep blockSrf;
        public List<plot> plots;

        public block(Brep inpSrf, int inpType)
        {
            this.blockSrf = inpSrf;
            this.type = inpType;
        }

    }

    public class plot
    {
        public Brep plotSrf;
        public Curve buildingOutline;
        public Extrusion building;
        public Random bldHeight = new Random();

        public plot (Brep inpSrf, int inpPlotType)
        {
            this.plotSrf = inpSrf;
            this.createBuilding(inpPlotType);
        }

        public bool createBuilding(int inpPlotType)
        {

            if (this.plotSrf.GetArea() < 50)
                return false;

            if(inpPlotType > 0) // Skip Parks
            {
                int minBldHeight = 0;
                int maxBldHeight = 3;

                if (inpPlotType == 1) //Low Rise
                {
                    minBldHeight = 3;
                    maxBldHeight = 9;
                }

                if (inpPlotType == 2) //Mid Rise
                {
                    minBldHeight = 15;
                    maxBldHeight = 32;
                }

                if (inpPlotType == 3) //High Rise
                {
                    minBldHeight = 60;
                    maxBldHeight = 120;
                }

                double actBuildingHeight = this.bldHeight.Next(minBldHeight, maxBldHeight);

                System.Drawing.Color bldCol = System.Drawing.Color.White;

                if (actBuildingHeight < 6)
                    bldCol = System.Drawing.Color.FromArgb(168, 126, 198);
                else if (actBuildingHeight < 12)
                    bldCol = System.Drawing.Color.FromArgb(255, 173, 194);
                else if (actBuildingHeight < 36)
                    bldCol = System.Drawing.Color.FromArgb(243, 104, 75);
                else if (actBuildingHeight < 92)
                    bldCol = System.Drawing.Color.FromArgb(225, 164, 24);
                else if (actBuildingHeight > 92)
                    bldCol = System.Drawing.Color.FromArgb(254, 255, 51);

                ObjectAttributes oa = new ObjectAttributes();
                oa.ColorSource = ObjectColorSource.ColorFromObject;
                oa.ObjectColor = bldCol;
               
                Curve border = Curve.JoinCurves(this.plotSrf.DuplicateNakedEdgeCurves(true, false))[0];

                this.buildingOutline = Curve.JoinCurves(border.Offset(Plane.WorldXY, -4, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, CurveOffsetCornerStyle.None))[0];

                this.building = Extrusion.Create(this.buildingOutline, actBuildingHeight, true);

                RhinoDoc.ActiveDoc.Objects.AddCurve(buildingOutline);
                RhinoDoc.ActiveDoc.Objects.AddExtrusion(this.building, oa);
            }

            return true;

           
        }

    }


}
