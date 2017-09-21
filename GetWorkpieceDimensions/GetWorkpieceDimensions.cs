using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NXOpen;
using NXOpen.CAM;
using NXOpen.UF;

namespace GetWorkpieceDimensions
{
    public sealed class GetWorkpieceDimensions
    {
        private static Session _theSession = Session.GetSession();
        private static UFSession _theUfSession = UFSession.GetUFSession();
        private static UI _theUi = UI.GetUI();

        public static int GetDimensions(string[] args)
        {

            IntPtr zero;
            string operationName;

            _theUfSession.Mom.AskMom(IntPtr.Zero, out zero);
            _theUfSession.Mom.AskString(zero, "operation_name", out operationName);

            var workPart = _theSession.Parts.Work;
            var setup = workPart.CAMSetup;
            var operation = setup.CAMOperationCollection.FindObject(operationName);

            CAMObject geometryView = (CAMObject)operation.GetParent(CAMSetup.View.Geometry);
            List<FeatureGeometry> featureGeometryList = new List<FeatureGeometry>();

            GetFeatureGeometryList(geometryView, ref featureGeometryList);

            double[] result = GetWorkpieseDimension(featureGeometryList,setup);

            _theUfSession.Mom.SetDoubleArray(zero,"part_dimensions",3,result);

          
            return -1;
        }

        private static void GetFeatureGeometryList(CAMObject camObject, ref List<FeatureGeometry> featureGeometryList)
        {
            dynamic nextFeature;
            if (camObject.GetType() == typeof(FeatureGeometry))
            {
                featureGeometryList.Add((FeatureGeometry)camObject);
                nextFeature = (FeatureGeometry)camObject;
                GetFeatureGeometryList((CAMObject)nextFeature.GetParent(), ref featureGeometryList);
            }
            else if (camObject.GetType() == typeof(OrientGeometry))
            {
                nextFeature = (OrientGeometry)camObject;
                GetFeatureGeometryList((CAMObject)nextFeature.GetParent(), ref featureGeometryList);
            }
        }

        private static double[] GetWorkpieseDimension(List<FeatureGeometry> featureGeometryList, CAMSetup setup)
        {
            double[] tempArray = new double[6];
            double[] workpieceDimension = new double[3] { -1, -1, -1 };

            if (featureGeometryList.Count < 1)
            {
                Guide.InfoWriteLine("Warning!! The Program Head can't be filled with workpiece dimension!!!");
                Guide.InfoWriteLine("There is also a danger that your machine will be CRASHED!");
                Guide.InfoWriteLine("Exam your workpiece in Operation Setup!!!");
                return workpieceDimension;
            }

            NXOpen.CAM.FeatureGeometry featureGeometry = featureGeometryList[0];
            NXOpen.CAM.MillGeomBuilder geomBuilder = setup.CAMGroupCollection.CreateMillGeomBuilder(featureGeometry);
            TaggedObject taggedObject = geomBuilder.GetCustomizableItemBuilder("Raw Material");
            TaggedObject partGeometry;
            NXOpen.CAM.Geometry geometry;

            if (taggedObject != null)
            {
                //"Hybrid Geometry"
                geometry = (Geometry)taggedObject;
                partGeometry = (TaggedObject)geometry.GeometryList.GetContents()[0].GetItems()[0];
            }
            else
            {
                //Milling Geometry
                partGeometry = (TaggedObject)geomBuilder.PartGeometry.GeometryList.GetContents()[0].GetItems()[0];
            }

            Body body = (Body) partGeometry;

            _theUfSession.Modl.AskBoundingBox(body.Tag, tempArray);

            workpieceDimension[0] = tempArray[3] - tempArray[0];
            workpieceDimension[1] = tempArray[4] - tempArray[1];
            workpieceDimension[2] = tempArray[5] - tempArray[2];

            return workpieceDimension;
        }

        public static int GetUnloadOption(string arg)
        {
            return System.Convert.ToInt32(Session.LibraryUnloadOption.Immediately);
        }
    }
}
