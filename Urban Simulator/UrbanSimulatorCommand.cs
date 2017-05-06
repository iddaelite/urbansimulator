﻿using System;
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

            //generateRoadNetwork()                     //Using the precinct, Generate a Road Network
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


    }
}
