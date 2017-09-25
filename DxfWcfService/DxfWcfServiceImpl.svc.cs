using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using WW.Cad.Drawing;
using WW.Cad.IO;
using WW.Cad.Model;
using WW.Cad.Model.Entities;
using WW.Cad.Model.Tables;
using WW.Math;
using Newtonsoft.Json;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Net;
using System.Web;
using WW.Cad.Drawing.GDI;
using System.Collections.Specialized;

namespace DxfWcfService
{
    //[ServiceBehavior(AddressFilterMode = AddressFilterMode.Any)]
    public class Booth
    {
        public string BOOTHNUMBER { get; set; }
        public string SIZEX { get; set; }
        public string SIZEY { get; set; }
        public string INSERTPOINT { get; set; }
        public string SHAPE { get; set; }
    }
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "DxfWcfServiceImpl" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select DxfWcfServiceImpl.svc or DxfWcfServiceImpl.svc.cs at the Solution Explorer and start debugging.
    public class DxfWcfServiceImpl : IDxfWcfServiceImpl
    {
        //Download and save file locally
        public Stream LoadFile(Stream input)
        {
            string body = new StreamReader(input).ReadToEnd();
            NameValueCollection _nvc = HttpUtility.ParseQueryString(body);
            string _fileName = _nvc["FileName"];
            string _client_id = _nvc["ClientId"];
            string _show_id = _nvc["ShowId"];
            string _accessKey = @_nvc["AccessKey"];
            string _secretKey = @_nvc["SecretKey"];
            string _bucketName = @_nvc["BucketName"];

            string s3DirectoryName = _client_id + @"/" + _show_id;
            string _localPath = "h://dxf_uploads//" + _client_id + " // " + _show_id + "//" + _fileName;
            string fileNameInS3 = _client_id + @"/" + _show_id + @"/dxf/" + _fileName;
            AmazonS3Transfer amazonS3Transfer = new AmazonS3Transfer();
            bool loadStatus = amazonS3Transfer.getMyFileFromS3(_bucketName, fileNameInS3, _localPath, _accessKey, _secretKey);

            Dictionary<string, string> status = new Dictionary<string, string>();
            if (loadStatus == true)
            {
                status.Add("status_code", "1");
            }
            else
            {
                status.Add("status_code", "0");
            }
            string result = JsonConvert.SerializeObject(status);
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
            return new MemoryStream(Encoding.UTF8.GetBytes(result));
        }
        //Read DWG or DXF
        public static DxfModel ReadDxf(string _filename)
        {
            DxfModel _model = null;
            string extension = Path.GetExtension(_filename);
            if (string.Compare(extension, ".dwg", true) == 0)
            {
                _model = DwgReader.Read(_filename);
            }
            else
            {
                _model = DxfReader.Read(_filename);
            }
            return _model;
        }

        //Returns All layers
        public Stream GetLayers(Stream input)
        {
            string body = new StreamReader(input).ReadToEnd();
            NameValueCollection _nvc = HttpUtility.ParseQueryString(body);
            string _fileName = _nvc["FileName"];
            string _client_id = _nvc["ClientId"];
            string _show_id = _nvc["ShowId"];
            DxfModel _model = ReadDxf("h://dxf_uploads//" + _client_id + " // " + _show_id + "//" + _fileName);
            Dictionary<string, string> layers = new Dictionary<string, string>();
            List<string> variableNames = new List<string>();
            for (int i = 0; i < _model.Layers.Count; i++)
            {
                variableNames.Add(String.Format("layer{0}", i.ToString()));
            }
            int j = 0;
            foreach (var layerName in _model.Layers)
            {
                layers[variableNames[j]] = layerName.ToString();
                j++;
            }
             string layerInfo = JsonConvert.SerializeObject(layers);

            WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
            return new MemoryStream(Encoding.UTF8.GetBytes(layerInfo));

            //return Json of all layers
            //return JsonConvert.SerializeObject(layers);
        }

        //gets the origin of the screen
        public static string GetScreenOrigin(String boundaries)
        {
            String value = boundaries.Replace(" ", string.Empty);
            List<string> myList = value.Split(',').ToList();
            string firstItem = myList.ElementAt(0).ToString();
            string x = firstItem.Split(':').Last();
            string nextItem = myList.ElementAt(4);
            string y = nextItem.ToString();
            string _point = x + "," + y;
            return _point;
        }

        //Remove spaces
        public static string RemoveSpaces(string sToRemoveSpaces)
        {
            return sToRemoveSpaces.Replace(" ", string.Empty);
        }

        //Transform Point
        public static string TransformPoint(string point, string origin)
        {
            string[] actualPoint = point.Split(',');
            string[] originPoint = origin.Split(',');
            var deltaX = ((int)double.Parse(actualPoint[0]) - (int)double.Parse(originPoint[0])) / 12;
            var deltaY = ((int)double.Parse(originPoint[1]) - (int)double.Parse(actualPoint[1])) / 12;
            String _point = deltaX.ToString() + "," + deltaY.ToString();
            return _point;
        }

        //Untransform point from string of point
        public static string UntransfromPoint(string point, string origin)
        {
            string[] actualPoint = point.Split(',');
            string[] originPoint = origin.Split(',');
            var deltaX = ((int)double.Parse(actualPoint[0])*12) + (int)double.Parse(originPoint[0]);
            var deltaY = ((int)double.Parse(actualPoint[1])*-12) + (int)double.Parse(originPoint[1]);
            String _point = deltaX.ToString() + "," + deltaY.ToString();
            return _point;
        }

        //untransfrom point from windows point
        public static Point UntransfromPoint(Point point, string origin)
        {
            string[] originPoint = origin.Split(',');
            var deltaX = (point.X * 12) + (int)double.Parse(originPoint[0]);
            var deltaY = (point.Y * -12) + (int)double.Parse(originPoint[1]);
            Point _untransformedPoint = new Point(deltaX, deltaY);
            return _untransformedPoint;
        }

        //Get Vertices From Vertex, Width and Height
        public static List<Point> GetVerticesFromVertex(string vertex, int _deltaX, int _deltaY)
        {
            string[] _vertex = vertex.Split(',');
            Point _v1 = new Point(int.Parse(_vertex[0]), int.Parse(_vertex[1]));
            Point _v2 = new Point(_v1.X + _deltaX, _v1.Y);
            Point _v3 = new Point(_v1.X + _deltaX, _v1.Y - _deltaY);
            Point _v4 = new Point(_v1.X , _v1.Y - _deltaY);

            List<Point> _vertexList = new List<Point> {_v1,_v2,_v3,_v4 };

            return _vertexList;
        }

        //Calculate Height
        public static int GetHeight(string s1, string s2)
        {
            string[] _s1 = s1.Split(',');
            string[] _s2 = s2.Split(',');
            int _height = Math.Abs(int.Parse(_s1[1]) - int.Parse(_s2[1]));
            return _height;
        }

        //calculate Width
        public static int GetWidth(string s1, string s2)
        {
            string[] _s1 = s1.Split(',');
            string[] _s2 = s2.Split(',');
            int _width = Math.Abs(int.Parse(_s1[0]) - int.Parse(_s2[0]));
            return _width;
        }

        //Convert text to point
        public static Point StringToPoint(string _s)
        {
            string[] coords = _s.Split(',');

            Point _point = new Point(int.Parse(coords[0]), (int.Parse(coords[1])) * -1);

            return _point;
        }

        //Get minimum point
        public static string FindMinimum(Point[] _points)
        {
            Point minimumPoint = _points.First(p => p.X == _points.Min(po => po.X) &&
                                                p.Y == _points.Max(po => po.Y));
            string _point = minimumPoint.X + "," + minimumPoint.Y;
            return _point;
        }

        //Get Max X and Max Y from list of points
        public static string FindGridLine(Point[] _points)
        {
            var maxX = _points.Max(Point => Point.X);
            var maxY = _points.Min(point => point.Y);
            string _s = maxX + "," + maxY;
            return _s;
        }

        //convert List of vertices to point List
        public static List<Point> VerticesToList(List<string> _vertices)
        {
            List<Point> verticesPointList = new List<Point>();
            foreach (string item in _vertices)
            {
                verticesPointList.Add(StringToPoint(item));
            }
            return verticesPointList;
        }

        //To determine Text point in polygon or not
        public static bool PointInPolygon(Point[] _vertices, Point point)
        {
            var j = _vertices.Length - 1;
            var oddNodes = false;

            for (var i = 0; i < _vertices.Length; i++)
            {
                if (_vertices[i].Y < point.Y && _vertices[j].Y >= point.Y ||
                    _vertices[j].Y < point.Y && _vertices[i].Y >= point.Y)
                {
                    if (_vertices[i].X +
                        (point.Y - _vertices[i].Y) / (_vertices[j].Y - _vertices[i].Y) * (_vertices[j].X - _vertices[i].X) < point.X)
                    {
                        oddNodes = !oddNodes;
                    }
                }
                j = i;
            }

            return oddNodes;
        }

        //LWPOLYLINE
        public static List<string> LwPolyline(DxfEntity _entity, string _origin)
        {
            DxfLwPolyline lwPolyline = _entity as DxfLwPolyline;

            List<string> newVertices = new List<string>();

            //loop over all vertices in entity
            foreach (DxfLwPolyline.Vertex vertex in lwPolyline.Vertices)
            {
                newVertices.Add(RemoveSpaces(vertex.Position.ToString()));
            }

            //Transformed points list
            List<string> verticesTransformed = new List<string>();
            foreach (string item in newVertices)
            {
                verticesTransformed.Add(TransformPoint(item, _origin));
            }
            return verticesTransformed;
        }

        //For a given Polygon Find related booth number and pass the booth data
        public static Booth PassBooth(DxfModel _model, string _boothNumberLayer, List<string> _vertices, string _screenOrigin)
        {
            int _sizeX = 0;
            int _sizeY = 0;
            string _insertPoint;
            string _boothNumber;
            string _shape;
            var booth = new Booth();

            List<string> _verticesList = new List<string>();
            foreach (var item in _vertices)
            {
                _verticesList.Add(item);
            }
            Point[] verticesArray = VerticesToList(_vertices).ToArray();
            foreach (DxfEntity _textEntity in _model.Entities)
            {
                DxfText textTo = _textEntity as DxfText;

                //Logic to relate booth and booth number    
                if (_textEntity.EntityType.ToString() == "TEXT" && (_textEntity.Layer.ToString().Equals(_boothNumberLayer, StringComparison.InvariantCultureIgnoreCase)))
                //_textEntity.Layer.ToString() == _boothNumberLayer
                {
                    var textString = RemoveSpaces(textTo.TextBounds.Center.ToString());
                    var textPoint = TransformPoint(textTo.Transform.Transform(textTo.TextBounds.Center).ToString(), _screenOrigin);
                    var textPoint1 = StringToPoint(textPoint);
                    if (PointInPolygon(verticesArray, textPoint1) == true)
                    {
                        _insertPoint = RemoveSpaces(string.Join(",", _verticesList));
                        _boothNumber = textTo.Text;
                        _shape = "Polygon";

                        //If the polygon has 4 sides
                        if (verticesArray.Length == 4)
                        {
                            _sizeX = GetWidth(_vertices[0].ToString(), _vertices[2].ToString());
                            _sizeY = GetHeight(_vertices[1].ToString(), _vertices[3].ToString());
                            _insertPoint = FindMinimum(verticesArray).ToString();
                            _shape = "Box";
                        }
                        booth.BOOTHNUMBER = _boothNumber;
                        booth.INSERTPOINT = _insertPoint;
                        booth.SIZEX = _sizeX.ToString();
                        booth.SIZEY = _sizeY.ToString();
                        booth.SHAPE = _shape;
                    }
                }
            }
            return booth;
        }

        //Get All Booth Info for a given model
        public Stream GetBoothsInfo(Stream input)
        {
            string body = new StreamReader(input).ReadToEnd();
            NameValueCollection _nvc = HttpUtility.ParseQueryString(body);
            string _fileName = _nvc["FileName"];
            string _client_id = _nvc["ClientId"];
            string _show_id = _nvc["ShowId"];
            string _boothOutlineLayer = _nvc["BoothOutline"];
            string _boothNumberLayer = _nvc["BoothNumber"];
            DxfModel _model = ReadDxf("h://dxf_uploads//" + _client_id + " // " + _show_id + "//" + _fileName);
            BoundsCalculator _mBounds = new BoundsCalculator();
            _mBounds.GetBounds(_model);
            Bounds3D _cBounds = _mBounds.Bounds;
            //get screen origin
            string _screenOrigin = GetScreenOrigin(_cBounds.ToString());

            //Empty List to attach all booth information
            List<Booth> _boothInfo = new List<Booth>();

            //Empty List to hold vertices of all Booths
            List<Point> _allVertices = new List<Point>();

            //loop over all Entites in a model
            foreach (DxfEntity _entity in _model.Entities)
            {
                if (_entity.Layer.ToString().Equals(_boothOutlineLayer,StringComparison.InvariantCultureIgnoreCase))
                //(_entity.Layer.ToString() == _boothOutlineLayer)
                {
                    if (_entity.EntityType.ToString() == "LWPOLYLINE" || _entity.EntityType.ToString() == "POLYLINE")
                    {
                        var verticesTransformed = LwPolyline(_entity, _screenOrigin);
                        Booth _booth = PassBooth(_model, _boothNumberLayer, verticesTransformed, _screenOrigin);
                        Point[] verticesArray = VerticesToList(verticesTransformed).ToArray();
                        foreach (Point _vertex in verticesArray)
                        {
                            _allVertices.Add(_vertex);
                        }
                        _boothInfo.Add(_booth);
                    }
                    else if (_entity.EntityType.ToString() == "INSERT")
                    {
                        DxfInsert Insertentity = _entity as DxfInsert;
                        Matrix4D transform = Insertentity.BlockInsertionTransformations[0, 0] * Insertentity.Block.BaseTransformation;

                        DxfBlock block = Insertentity.Block;
                        foreach (DxfEntity lwEntity in block.Entities)
                        {
                            if (lwEntity.EntityType == "LWPOLYLINE")
                            {
                                DxfLwPolyline lwPolyline = lwEntity as DxfLwPolyline;
                                Matrix4D _insertedPolylineTransform = transform * lwPolyline.Transform;
                                List<string> _newVertices = new List<string>();

                                //loop over all vertices in entity
                                foreach (DxfLwPolyline.Vertex vertex in lwPolyline.Vertices)
                                {
                                    _newVertices.Add(RemoveSpaces(_insertedPolylineTransform.Transform(vertex.Position).ToString()));
                                }
                                List<string> verticesTransformed = new List<string>();
                                foreach (string item in _newVertices)
                                {
                                    verticesTransformed.Add(TransformPoint(item, _screenOrigin));
                                }
                                Booth _booth = PassBooth(_model, _boothNumberLayer, verticesTransformed, _screenOrigin);
                                Point[] verticesArray = VerticesToList(verticesTransformed).ToArray();
                                foreach (Point _vertex in verticesArray)
                                {
                                    _allVertices.Add(_vertex);
                                }
                                _boothInfo.Add(_booth);
                            }
                        }
                    }
                }
            }
            Booth _boundaries = new Booth()
            {
                BOOTHNUMBER = "",
                INSERTPOINT = _screenOrigin,
                SHAPE = "origin",
                SIZEX = "",
                SIZEY = ""
            };
            _boothInfo.Add(_boundaries);
            string gridline = FindGridLine(_allVertices.ToArray());
            string[] gridLine = gridline.Split(',');
            Booth _gridLine = new Booth()
            {
                BOOTHNUMBER = "",
                INSERTPOINT = "",
                SHAPE = "Grid Line",
                SIZEX = gridLine[0],
                SIZEY = gridLine[1]
            };
            _boothInfo.Add(_gridLine);

            string boothInformation = JsonConvert.SerializeObject(_boothInfo);

            WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
            return new MemoryStream(Encoding.UTF8.GetBytes(boothInformation));

            //return JsonConvert.SerializeObject(_boothInfo);
        }

        //Get Jpeg from selected Layers
        public Stream GetPictures(Stream input)
        {
            string body = new StreamReader(input).ReadToEnd();
            NameValueCollection _nvc = HttpUtility.ParseQueryString(body);
            string _boothOutlineLayer = _nvc["BoothOutline"];
            string _boothNumberLayer = _nvc["BoothNumber"];
            string _fileName = _nvc["FileName"];
            string _client_id = _nvc["ClientId"];
            string _show_id = _nvc["ShowId"];
            string _accessKey = @_nvc["AccessKey"];
            string _secretKey = @_nvc["SecretKey"];
            string _bucketName = @_nvc["BucketName"];
            DxfModel _model = ReadDxf("h://dxf_uploads//" + _client_id + " // " + _show_id + "//" + _fileName);
            foreach (DxfLayer _layer in _model.Layers)
            {
                if (_layer.Name != _boothOutlineLayer && _layer.Name != _boothNumberLayer)
                {
                    _layer.Enabled = false;
                }
            }
            string outfile = Path.GetFileNameWithoutExtension(Path.GetFullPath(_fileName));
            Stream stream;

            GDIGraphics3D graphics = new GDIGraphics3D(GraphicsConfig.BlackBackgroundCorrectForBackColor);
            Size maxSize = new Size(750, 750);
            Bitmap bitmap =
                ImageExporter.CreateAutoSizedBitmap(
                    _model,
                    graphics,
                    Matrix4D.Identity,
                    System.Drawing.Color.Black,
                    maxSize
                );
            string fileNameS3 = outfile + ".jpg";
            string filePath = "h://dxf_uploads//" + _client_id + " // " + _show_id + "//" + fileNameS3;
            stream = File.Create(filePath);
            ImageExporter.EncodeImageToJpeg(bitmap, stream);
            
            string _subDirectory = _client_id + @"/" + _show_id + @"/dxf";
            AmazonS3Transfer amazonS3Transfer = new AmazonS3Transfer();
            bool uploadStatus = amazonS3Transfer.sendMyFileToS3(stream, _bucketName, _subDirectory, fileNameS3, _accessKey, _secretKey);
            Dictionary<string, string> status = new Dictionary<string, string>();
            if (uploadStatus == true)
            { 
                status.Add("status_code", "1");
            }
            else
            {
                 status.Add("status_code", "0");
            }
            string result = JsonConvert.SerializeObject(status);
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
            return new MemoryStream(Encoding.UTF8.GetBytes(result));
}

        //Dxf/dwg Export with eshow layers
        public Stream ExportLayout(Stream input)
        {
            string body = new StreamReader(input).ReadToEnd();
            NameValueCollection _nvc = HttpUtility.ParseQueryString(body);
            dynamic _data = @_nvc["Data"];
            string _fileName = _nvc["FileName"];
            string _client_id = _nvc["ClientId"];
            string _show_id = _nvc["ShowId"];
            string _accessKey = @_nvc["AccessKey"];
            string _secretKey = @_nvc["SecretKey"];
            string _bucketName = @_nvc["BucketName"];

            DxfModel _model = ReadDxf("h://dxf_uploads//" + _client_id + " // " + _show_id + "//" + _fileName);
            DxfLayer _numberLayer = new DxfLayer("ESHOW_NUMBERS");
            DxfLayer _outlineLayer = new DxfLayer("ESHOW_BOOTHS");
            _model.Layers.Add(_numberLayer);
            _model.Layers.Add(_outlineLayer);

            //parsing Json Array
            List<Booth> _boothList = JsonConvert.DeserializeObject<List<Booth>>(_data);
            string _origin = null;
            foreach (var _originInfo in _boothList)
            {
                if (_originInfo.SHAPE.Equals("ORIGIN", StringComparison.InvariantCultureIgnoreCase))
                {
                    _origin = _originInfo.INSERTPOINT;
                }

            }

            if (_origin != null)
            { 
                foreach (var _booth in _boothList)
                {
                    if ((!_booth.SHAPE.Equals("ORIGIN", StringComparison.InvariantCultureIgnoreCase)) || (!_booth.SHAPE.Equals("GRIDLINE", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        switch (_booth.SHAPE)
                        {
                            case "Box":
                                string insertPoint = _booth.INSERTPOINT;
                                int deltaX = int.Parse(_booth.SIZEX)*12;
                                int deltaY = int.Parse(_booth.SIZEY)*12;
                                string boothNumber = _booth.BOOTHNUMBER;
                                string vertexOriginal = UntransfromPoint(insertPoint, _origin);
                                List<Point> VertexList = GetVerticesFromVertex(vertexOriginal, deltaX, deltaY);
                                //converting Windows points to Cad Points
                                Point2D[] vertexPoints = new Point2D[4];
                                for (int i = 0; i < VertexList.Count; i++)
                                {
                                    vertexPoints[i].X = VertexList[i].X;
                                    vertexPoints[i].Y = VertexList[i].Y;
                                }
                                //Add Booth to Dxf
                                DxfPolyline2D booth = new DxfPolyline2D();
                                booth.Vertices.AddRange(vertexPoints);
                                booth.Layer = _outlineLayer;
                                booth.Closed = true;
                                booth.Color = EntityColor.ByLayer;
                                _model.Entities.Add(booth);

                                //Add text to DXF
                                Point textPoint = new Point(VertexList[0].X + 10, VertexList[0].Y - 50);
                                DxfText boothText = new DxfText(boothNumber, new Point3D(textPoint.X, textPoint.Y, 0d), 10d)
                                {
                                    Layer = _numberLayer,
                                    Color = EntityColor.ByLayer
                                };
                                _model.Entities.Add(boothText);
                                break;

                            case "Polygon":
                                insertPoint = _booth.INSERTPOINT;
                                int[] ins = insertPoint.Split(',').Select(n => Convert.ToInt32(n)).ToArray();
                                List<int> insPoints = ins.OfType<int>().ToList();
                                insPoints.RemoveAt(insPoints.Count - 1);
                                insPoints.RemoveAt(insPoints.Count - 1);
                                List<Point> vertexList = new List<Point>();
                                Point tempPoint;
                                //converting to vertices
                                for (int i = 0; i <= insPoints.Count; i+=2)
                                {
                                    tempPoint = new Point(insPoints[i], y: insPoints[i + 1]);
                                    vertexList.Add(tempPoint);
                                }
                                boothNumber = _booth.BOOTHNUMBER;
                                //untransfroming points
                                for (int i = 0; i < vertexList.Count; i++)
                                {
                                    vertexList[i] = UntransfromPoint(vertexList[i], _origin);
                                }
                                //converting Windows points to Cad Points
                                vertexPoints = new Point2D[vertexList.Count];
                                for (int i = 0; i < vertexList.Count; i++)
                                {
                                    vertexPoints[i].X = vertexList[i].X;
                                    vertexPoints[i].Y = vertexList[i].Y;
                                }
                                booth = new DxfPolyline2D();
                                booth.Vertices.AddRange(vertexPoints);
                                booth.Layer = _outlineLayer;
                                booth.Closed = true;
                                booth.Color = EntityColor.ByLayer;
                                _model.Entities.Add(booth);

                                //Add text to DXF
                                textPoint = new Point(vertexList[0].X + 10, vertexList[0].Y - 50);
                                boothText = new DxfText(boothNumber, new Point3D(textPoint.X, textPoint.Y, 0d), 10d)
                                {
                                    Layer = _numberLayer,
                                    Color = EntityColor.ByLayer
                                };
                                _model.Entities.Add(boothText);
                                break;
                        }
                    }                   
                }
            }
            // write new DWG
            Stream dwgStream;
            string outfile = Path.GetFileNameWithoutExtension(Path.GetFullPath(_fileName));
            string fileNameS3 = outfile + "_export.dwg";
            string filePath = "h://dxf_uploads//" + _client_id + " // " + _show_id + "//" + fileNameS3;
            using (dwgStream = new FileStream(@filePath, FileMode.Create, FileAccess.Write))
            {
                DwgWriter.Write(dwgStream, _model);
            }
            //Upload to s3
            //string _subDirectory = _client_id + @"/" + _show_id + @"/dxf";
            //AmazonS3Transfer amazonS3Transfer = new AmazonS3Transfer();
            //bool uploadStatus = amazonS3Transfer.sendMyFileToS3(dwgStream, _bucketName, _subDirectory, fileNameS3, _accessKey, _secretKey);
            bool uploadStatus = true;
            Dictionary<string, string> status = new Dictionary<string, string>();
            if (uploadStatus == true)
            {
                status.Add("status_code", "1");
            }
            else
            {
                status.Add("status_code", "0");
            }
            string result = JsonConvert.SerializeObject(status);
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
            return new MemoryStream(Encoding.UTF8.GetBytes(result));
        }
    }
}
