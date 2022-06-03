using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using FlatRedBall.Content.Polygon;

using FlatRedBall.Math;
using FileManager = FlatRedBall.IO.FileManager;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Content.Math.Geometry
{
    /// <summary>
    /// A serializable class storing shape collection data. If saves as XML using the .schx file format, it 
    /// can be used by FlatRedBall tools.
    /// </summary>
    public class ShapeCollectionSave
    {
        #region Fields
        public List<AxisAlignedRectangleSave> AxisAlignedRectangleSaves = new List<AxisAlignedRectangleSave>();

        public List<AxisAlignedCubeSave> AxisAlignedCubeSaves = new List<AxisAlignedCubeSave>();

        public List<PolygonSave> PolygonSaves = new List<PolygonSave>();

        public List<CircleSave> CircleSaves = new List<CircleSave>();
        public List<SphereSave> SphereSaves = new List<SphereSave>();

        string mFileName;
        #endregion

        #region Properties

        [XmlIgnore]
        public string FileName
        {
            get { return mFileName; }
            set { mFileName = value; }
        }

        #endregion

        #region Methods

        public void AddAxisAlignedRectangleList(PositionedObjectList<FlatRedBall.Math.Geometry.AxisAlignedRectangle> axisAlignedRectanglesToAdd)
        {
            foreach (FlatRedBall.Math.Geometry.AxisAlignedRectangle rectangle in axisAlignedRectanglesToAdd)
            {
                AxisAlignedRectangleSave rectangleSave = AxisAlignedRectangleSave.FromAxisAlignedRectangle(rectangle);
                AxisAlignedRectangleSaves.Add(rectangleSave);
            }
        }

        public void AddCircleList(PositionedObjectList<Circle> circlesToAdd)
        {
            foreach (Circle circle in circlesToAdd)
            {
                CircleSave circleSave = CircleSave.FromCircle(circle);
                CircleSaves.Add(circleSave);
            }
        }

        public void AddSphereList(PositionedObjectList<Sphere> spheresToAdd)
        {
            foreach (Sphere sphere in spheresToAdd)
            {
                SphereSave sphereSave = SphereSave.FromSphere(sphere);
                SphereSaves.Add(sphereSave);
            }
        }

        public void AddPolygonList(PositionedObjectList<FlatRedBall.Math.Geometry.Polygon> polygonsToAdd)
        {
            foreach (FlatRedBall.Math.Geometry.Polygon polygon in polygonsToAdd)
            {
                PolygonSave polygonSave = PolygonSave.FromPolygon(polygon);
                PolygonSaves.Add(polygonSave);
            }
        }

        public void AddAxisAlignedCubeList(PositionedObjectList<FlatRedBall.Math.Geometry.AxisAlignedCube> axisAlignedCubesToAdd)
        {
            foreach (FlatRedBall.Math.Geometry.AxisAlignedCube cube in axisAlignedCubesToAdd)
            {
                AxisAlignedCubeSave cubeSave = AxisAlignedCubeSave.FromAxisAlignedCube(cube);
                AxisAlignedCubeSaves.Add(cubeSave);
            }
        }
        
        /// <summary>
        /// Deserializes a file into a new ShapeCollectionSave and returns it.
        /// </summary>
        /// <param name="fileName">The absolute or relative file name. If the file name is relative, then the FileManager's RelativeDirectory will be used.</param>
        /// <returns>The newly-created ShapeCollectionSave.</returns>
        public static ShapeCollectionSave FromFile(string fileName)
        {
            ShapeCollectionSave shapeSaveCollection =
                FileManager.XmlDeserialize<ShapeCollectionSave>(fileName);

            shapeSaveCollection.mFileName = fileName;

            return shapeSaveCollection;
        }

        public static ShapeCollectionSave FromShapeCollection(ShapeCollection shapeCollection)
        {
            ShapeCollectionSave toReturn = new ShapeCollectionSave();
            toReturn.AddAxisAlignedRectangleList(shapeCollection.AxisAlignedRectangles);
            toReturn.AddAxisAlignedCubeList(shapeCollection.AxisAlignedCubes);
            toReturn.AddCircleList(shapeCollection.Circles);
            toReturn.AddSphereList(shapeCollection.Spheres);
            toReturn.AddPolygonList(shapeCollection.Polygons);

            if (shapeCollection.Lines.Count != 0)
            {
                throw new NotImplementedException("Lines in ShapeCollectionSaves are not implemented yet.  Complain on the FRB forums");
            }

            if (shapeCollection.Capsule2Ds.Count != 0)
            {
                throw new NotImplementedException("Capsule2Ds in ShapeCollectionSaves are not implemented yet.  Complain on the FRB forums");
            }

            return toReturn;
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }

        public PositionedObjectList<Circle> ToCircleList()
        {
            PositionedObjectList<Circle> listToReturn = new PositionedObjectList<Circle>();

            foreach (CircleSave circleSave in CircleSaves)
            {
                Circle circle = circleSave.ToCircle();
                listToReturn.Add(circle);
            }

            return listToReturn;
        }

        public PositionedObjectList<Sphere> ToSphereList()
        {
            PositionedObjectList<Sphere> listToReturn = new PositionedObjectList<Sphere>();

            foreach (SphereSave sphereSave in SphereSaves)
            {
                Sphere sphere = sphereSave.ToSphere();
                listToReturn.Add(sphere);
            }

            return listToReturn;
        }

        public void SetValuesOn(ShapeCollection shapeCollection, PositionedObject newShapeParent, bool createMissingShapes)
        {
            foreach (var rectangle in this.AxisAlignedRectangleSaves)
            {
                AxisAlignedRectangle match = null;

                for (int j = 0; j < shapeCollection.AxisAlignedRectangles.Count; j++)
                {
                    var candidate = shapeCollection.AxisAlignedRectangles[j];
                    if (candidate.Name == rectangle.Name)
                    {
                        match = candidate;
                        break;
                    }
                }
                if (match == null && createMissingShapes)
                {
                    match = new AxisAlignedRectangle();
                    if(newShapeParent != null)
                    {
                        match.AttachTo(newShapeParent);
                    }
                    shapeCollection.AxisAlignedRectangles.Add(match);
                }

                rectangle.SetValuesOn(match);
                match.RelativeX = rectangle.X;
                match.RelativeY = rectangle.Y;
                match.RelativeZ = rectangle.Z;
            }

            foreach (var circle in this.CircleSaves)
            {
                Circle match = null;

                for (int j = 0; j < shapeCollection.Circles.Count; j++)
                {
                    var candidate = shapeCollection.Circles[j];
                    if (candidate.Name == circle.Name)
                    {
                        match = candidate;
                        break;
                    }
                }

                if (match == null && createMissingShapes)
                {
                    match = new Circle();
                    if (newShapeParent != null)
                    {
                        match.AttachTo(newShapeParent);
                    }
                    shapeCollection.Circles.Add(match);
                }

                circle.SetValuesOn(match);
                match.RelativeX = circle.X;
                match.RelativeY = circle.Y;
                match.RelativeZ = circle.Z;
            }

            foreach (var cube in this.AxisAlignedCubeSaves)
            {
                AxisAlignedCube match = null;

                for (int j = 0; j < shapeCollection.AxisAlignedCubes.Count; j++)
                {
                    var candidate = shapeCollection.AxisAlignedCubes[j];
                    if (candidate.Name == cube.Name)
                    {
                        match = candidate;
                        break;
                    }
                }

                if (match == null && createMissingShapes)
                {
                    match = new AxisAlignedCube();
                    if (newShapeParent != null)
                    {
                        match.AttachTo(newShapeParent);
                    }
                    shapeCollection.AxisAlignedCubes.Add(match);
                }

                cube.SetValuesOn(match);
                match.RelativeX = cube.X;
                match.RelativeY = cube.Y;
                match.RelativeZ = cube.Z;
            }

            foreach (var sphere in this.SphereSaves)
            {
                Sphere match = null;

                for (int j = 0; j < shapeCollection.Spheres.Count; j++)
                {
                    var candidate = shapeCollection.Spheres[j];
                    if (candidate.Name == sphere.Name)
                    {
                        match = candidate;
                        break;
                    }
                }

                if (match == null && createMissingShapes)
                {
                    match = new Sphere();
                    if (newShapeParent != null)
                    {
                        match.AttachTo(newShapeParent);
                    }
                    shapeCollection.Spheres.Add(match);
                }

                sphere.SetValuesOn(match);
                match.RelativeX = sphere.X;
                match.RelativeY = sphere.Y;
                match.RelativeZ = sphere.Z;
            }

            foreach (var polygon in this.PolygonSaves)
            {
                FlatRedBall.Math.Geometry.Polygon match = null;

                for (int j = 0; j < shapeCollection.Polygons.Count; j++)
                {
                    var candidate = shapeCollection.Polygons[j];
                    if (candidate.Name == polygon.Name)
                    {
                        match = candidate;
                        break;
                    }
                }

                if (match == null && createMissingShapes)
                {
                    match = new FlatRedBall.Math.Geometry.Polygon();
                    if (newShapeParent != null)
                    {
                        match.AttachTo(newShapeParent);
                    }
                    shapeCollection.Polygons.Add(match);
                }

                polygon.SetValuesOn(match);
                match.RelativeX = polygon.X;
                match.RelativeY = polygon.Y;
                match.RelativeZ = polygon.Z;
            }
        }

        public PositionedObjectList<FlatRedBall.Math.Geometry.AxisAlignedRectangle> ToAxisAlignedRectangleList()
        {
            PositionedObjectList<FlatRedBall.Math.Geometry.AxisAlignedRectangle> listToReturn = new PositionedObjectList<FlatRedBall.Math.Geometry.AxisAlignedRectangle>();

            foreach (AxisAlignedRectangleSave rectangleSave in AxisAlignedRectangleSaves)
            {
                FlatRedBall.Math.Geometry.AxisAlignedRectangle rectangle = rectangleSave.ToAxisAlignedRectangle();
                listToReturn.Add(rectangle);
            }

            return listToReturn;
        }

        public PositionedObjectList<FlatRedBall.Math.Geometry.AxisAlignedCube> ToAxisAlignedCubeList()
        {
            PositionedObjectList<FlatRedBall.Math.Geometry.AxisAlignedCube> listToReturn = new PositionedObjectList<FlatRedBall.Math.Geometry.AxisAlignedCube>();

            foreach (AxisAlignedCubeSave cubeSave in AxisAlignedCubeSaves)
            {
                FlatRedBall.Math.Geometry.AxisAlignedCube cube = cubeSave.ToAxisAlignedCube();
                listToReturn.Add(cube);
            }

            return listToReturn;
        }
  
        public PositionedObjectList<FlatRedBall.Math.Geometry.Polygon> ToPolygonList()
        {
            PositionedObjectList<FlatRedBall.Math.Geometry.Polygon> listToReturn = new PositionedObjectList<FlatRedBall.Math.Geometry.Polygon>();

            foreach (PolygonSave polygonSave in PolygonSaves)
            {
                FlatRedBall.Math.Geometry.Polygon polygon = polygonSave.ToPolygon();
                listToReturn.Add(polygon);
            }

            return listToReturn;
        }

        public ShapeCollection ToShapeCollection()
        {
            ShapeCollection newShapeCollection = new ShapeCollection();
            
            SetShapeCollection(newShapeCollection);

            return newShapeCollection;
        }

        public void SetShapeCollection(ShapeCollection newShapeCollection)
        {

            newShapeCollection.AxisAlignedRectangles.AddRange(ToAxisAlignedRectangleList());
            newShapeCollection.AxisAlignedCubes.AddRange(ToAxisAlignedCubeList());
            newShapeCollection.Circles.AddRange(ToCircleList());
            newShapeCollection.Spheres.AddRange(ToSphereList());
            newShapeCollection.Polygons.AddRange(ToPolygonList());

            // Handle lines when we add them to the save class


            if (!string.IsNullOrEmpty(mFileName) && FileManager.IsRelative(mFileName))
            {
                mFileName = FileManager.MakeAbsolute(mFileName);
            }
            newShapeCollection.Name = mFileName;
        }


        public void Shift(Vector3 shiftAmount)
        {
            foreach (AxisAlignedRectangleSave shape in AxisAlignedRectangleSaves)
            {
                shape.X += shiftAmount.X;
                shape.Y += shiftAmount.Y;
                shape.Z += shiftAmount.Z;
            }

            foreach (AxisAlignedCubeSave shape in AxisAlignedCubeSaves)
            {
                shape.X += shiftAmount.X;
                shape.Y += shiftAmount.Y;
                shape.Z += shiftAmount.Z;
            }

            foreach (PolygonSave shape in PolygonSaves)
            {
                shape.X += shiftAmount.X;
                shape.Y += shiftAmount.Y;
                shape.Z += shiftAmount.Z;
            }

            foreach (CircleSave shape in CircleSaves)
            {
                shape.X += shiftAmount.X;
                shape.Y += shiftAmount.Y;
                shape.Z += shiftAmount.Z;
            }
            foreach (SphereSave shape in SphereSaves)
            {
                shape.X += shiftAmount.X;
                shape.Y += shiftAmount.Y;
                shape.Z += shiftAmount.Z;
            }
        }

        #endregion
    }
}
