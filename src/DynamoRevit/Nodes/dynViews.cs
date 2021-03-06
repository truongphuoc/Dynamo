﻿//Copyright 2013 Ian Keough

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Dynamo.Controls;
using Dynamo.Models;
using Dynamo.Utilities;
using Microsoft.FSharp.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Data;
using System.ComponentModel;

using Value = Dynamo.FScheme.Value;
using Dynamo.Revit;

namespace Dynamo.Nodes
{
    [NodeName("Drafting View")]
    [NodeCategory(BuiltinNodeCategories.REVIT_VIEW)]
    [NodeDescription("Creates a drafting view.")]
    public class DraftingView: RevitTransactionNodeWithOneOutput
    {
        public DraftingView()
        {
            InPortData.Add(new PortData("name", "Name", typeof(Value.String)));
            OutPortData.Add(new PortData("v", "Drafting View", typeof(Value.Container)));

            RegisterAllPorts();

            ArgumentLacing = LacingStrategy.Longest;
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            ViewDrafting vd = null;
            string viewName = ((Value.String)args[0]).Item;

            if (this.Elements.Any())
            {
                Element e;
                if (dynUtils.TryGetElement(this.Elements[0], typeof(ViewDrafting), out e))
                {
                    vd = (ViewDrafting)e;
                }
                else
                {
                    vd = dynRevitSettings.Doc.Document.Create.NewViewDrafting();
                    this.Elements[0] = vd.Id;
                }
            }
            else
            {
                vd = dynRevitSettings.Doc.Document.Create.NewViewDrafting();
                this.Elements.Add(vd.Id);
            }

            //rename the view
            if(!vd.Name.Equals(viewName))
                 vd.Name = ViewBase.CreateUniqueViewName(viewName);

            return Value.NewContainer(vd);
        }
    }

    public delegate View3D View3DCreationDelegate(ViewOrientation3D orient, string name, bool isPerspective);

    public abstract class ViewBase:RevitTransactionNodeWithOneOutput
    {
        protected bool isPerspective = false;

        protected ViewBase()
        {
            InPortData.Add(new PortData("eye", "The eye position point.", typeof(Value.Container)));
            InPortData.Add(new PortData("target", "The location where the view is pointing.", typeof(Value.Container)));
            InPortData.Add(new PortData("name", "The name of the view.", typeof(Value.String)));
            InPortData.Add(new PortData("extents", "Pass in a bounding box or an element to define the 3D crop of the view.", typeof(Value.String)));
            InPortData.Add(new PortData("isolate", "If an element is supplied in 'extents', it will be isolated in the view.", typeof(Value.String)));

            OutPortData.Add(new PortData("view", "The newly created 3D view.", typeof(Value.Container)));

            RegisterAllPorts();
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            View3D view = null;
            var eye = (XYZ)((Value.Container)args[0]).Item;
            var target = (XYZ)((Value.Container)args[1]).Item;
            var name = ((Value.String)args[2]).Item;
            var extents = ((Value.Container)args[3]).Item;
            var isolate = Convert.ToBoolean(((Value.Number)args[4]).Item);

            var globalUp = XYZ.BasisZ;
            var direction = target.Subtract(eye);
            var up = direction.CrossProduct(globalUp).CrossProduct(direction);
            var orient = new ViewOrientation3D(eye, up, direction);

            if (this.Elements.Any())
            {
                Element e;
                if (dynUtils.TryGetElement(this.Elements[0], typeof(View3D), out e))
                {
                    view = (View3D)e;
                    if (!view.ViewDirection.IsAlmostEqualTo(direction) || !view.Origin.IsAlmostEqualTo(eye))
                    {
                        view.Unlock();
                        view.SetOrientation(orient);
                        view.SaveOrientationAndLock();
                    }

                    if (!view.Name.Equals(name))
                        view.Name = ViewBase.CreateUniqueViewName(name);
                }
                else
                {
                    //create a new view
                    view = ViewBase.Create3DView(orient, name, isPerspective);
                    Elements[0] = view.Id;
                }
            }
            else
            {
                view = Create3DView(orient, name, isPerspective);
                Elements.Add(view.Id);
            }

            var fec = dynRevitUtils.SetupFilters(dynRevitSettings.Doc.Document);

            if (isolate)
            {
                view.CropBoxActive = true;

                var element = extents as Element;
                if (element != null)
                {
                    var e = element;

                    var all = fec.ToElements();
                    var toHide =
                        fec.ToElements().Where(x => !x.IsHidden(view) && x.CanBeHidden(view) && x.Id != e.Id).Select(x => x.Id).ToList();
                    
                    if (toHide.Count > 0)
                        view.HideElements(toHide);

                    dynRevitSettings.Doc.Document.Regenerate();

                    Debug.WriteLine(string.Format("Eye:{0},Origin{1}, BBox_Origin{2}, Element{3}",
                        eye.ToString(), view.Origin.ToString(), view.CropBox.Transform.Origin.ToString(), (element.Location as LocationPoint).Point.ToString()));

                    //http://wikihelp.autodesk.com/Revit/fra/2013/Help/0000-API_Deve0/0039-Basic_In39/0067-Views67/0069-The_View69
                    if (isPerspective)
                    {
                        var farClip = view.get_Parameter("Far Clip Active");
                        farClip.Set(0);
                    }
                    else
                    {
                        //http://adndevblog.typepad.com/aec/2012/05/set-crop-box-of-3d-view-that-exactly-fits-an-element.html
                        var pts = new List<XYZ>();
                        foreach (GeometryObject gObj in element.get_Geometry(dynRevitSettings.GeometryOptions))
                        {
                            if (gObj is Solid)
                            {
                                //get all the edges in it
                                var solid = gObj as Solid;
                                foreach (Edge gEdge in solid.Edges)
                                {
                                    IList<XYZ> xyzArray = gEdge.Tessellate();
                                    pts.AddRange(xyzArray);
                                }
                            }
                        }

                        var bounding = view.CropBox;
                        var transInverse = bounding.Transform.Inverse;
                        var transPts = pts.Select(transInverse.OfPoint).ToList();

                        //ingore the Z coordindates and find
                        //the max X ,Y and Min X, Y in 3d view.
                        double dMaxX = 0, dMaxY = 0, dMinX = 0, dMinY = 0;

                        //geom.XYZ ptMaxX, ptMaxY, ptMinX,ptMInY; 
                        //coorresponding point.
                        bool bFirstPt = true;
                        foreach (var pt1 in transPts)
                        {
                            if (true == bFirstPt)
                            {
                                dMaxX = pt1.X;
                                dMaxY = pt1.Y;
                                dMinX = pt1.X;
                                dMinY = pt1.Y;
                                bFirstPt = false;
                            }
                            else
                            {
                                if (dMaxX < pt1.X)
                                    dMaxX = pt1.X;
                                if (dMaxY < pt1.Y)
                                    dMaxY = pt1.Y;
                                if (dMinX > pt1.X)
                                    dMinX = pt1.X;
                                if (dMinY > pt1.Y)
                                    dMinY = pt1.Y;
                            }
                        }

                        bounding.Max = new XYZ(dMaxX, dMaxY, bounding.Max.Z);
                        bounding.Min = new XYZ(dMinX, dMinY, bounding.Min.Z);
                        view.CropBox = bounding;
                    }
                }
                else
                {
                    var xyz = extents as BoundingBoxXYZ;
                    if (xyz != null)
                    {
                        view.CropBox = xyz;
                    }
                }
            }
            else
            {
                view.UnhideElements(fec.ToElementIds());
                view.CropBoxActive = false;
            }

            return Value.NewContainer(view);
        }

        public static View3D Create3DView(ViewOrientation3D orient, string name, bool isPerspective)
        {
            //http://adndevblog.typepad.com/aec/2012/05/viewplancreate-method.html

            IEnumerable<ViewFamilyType> viewFamilyTypes = from elem in new
              FilteredElementCollector(dynRevitSettings.Doc.Document).OfClass(typeof(ViewFamilyType))
                                                          let type = elem as ViewFamilyType
                                                          where type.ViewFamily == ViewFamily.ThreeDimensional
                                                          select type;

            //create a new view
            View3D view = isPerspective ?
                              View3D.CreatePerspective(dynRevitSettings.Doc.Document, viewFamilyTypes.First().Id) :
                              View3D.CreateIsometric(dynRevitSettings.Doc.Document, viewFamilyTypes.First().Id);

            view.SetOrientation(orient);
            view.SaveOrientationAndLock();
            view.Name = CreateUniqueViewName(name);

            return view;
        }
    
        /// <summary>
        /// Determines whether a view with the provided name already exists.
        /// If a view exists with the provided name, and new view is created with
        /// an incremented name. Otherwise, the original view name is returned.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string CreateUniqueViewName(string name)
        {
            string viewName = name;
            bool found = false;

            var collector = new FilteredElementCollector(dynRevitSettings.Doc.Document);
            collector.OfClass(typeof(View));

            if (collector.ToElements().Count(x=>x.Name == name) == 0)
                return name;

            int count = 0;
            while (!found)
            {
                string[] nameChunks = viewName.Split('_');

                viewName = string.Format("{0}_{1}", nameChunks[0], count.ToString(CultureInfo.InvariantCulture));

                if (collector.ToElements().ToList().Any(x => x.Name == viewName))
                    count++;
                else
                    found = true;
            }

            return viewName;
        }
    }

    [NodeName("Axonometric View")]
    [NodeCategory(BuiltinNodeCategories.REVIT_VIEW)]
    [NodeDescription("Creates an axonometric view.")]
    public class IsometricView : ViewBase
    {
        public IsometricView ()
        {
            isPerspective = false;
        }
    }

    [NodeName("Perspective View")]
    [NodeCategory(BuiltinNodeCategories.REVIT_VIEW)]
    [NodeDescription("Creates a perspective view.")]
    public class PerspectiveView : ViewBase
    {
        public PerspectiveView()
        {
            isPerspective = true;
        }
    }

    [NodeName("Bounding Box XYZ")]
    [NodeCategory(BuiltinNodeCategories.MODIFYGEOMETRY_TRANSFORM)]
    [NodeDescription("Create a bounding box.")]
    public class BoundingBoxXyz : NodeWithOneOutput
    {
        public BoundingBoxXyz()
        {
            InPortData.Add(new PortData("trans", "The coordinate system of the box.", typeof(Value.Container)));
            InPortData.Add(new PortData("x size", "The size of the bounding box in the x direction of the local coordinate system.", typeof(Value.Number)));
            InPortData.Add(new PortData("y size", "The size of the bounding box in the y direction of the local coordinate system.", typeof(Value.Number)));
            InPortData.Add(new PortData("z size", "The size of the bounding box in the z direction of the local coordinate system.", typeof(Value.Number)));
            OutPortData.Add(new PortData("bbox", "The bounding box.", typeof(Value.Container)));

            RegisterAllPorts();

            ArgumentLacing = LacingStrategy.Longest;
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            BoundingBoxXYZ bbox = new BoundingBoxXYZ();
            
            Transform t = (Transform)((Value.Container)args[0]).Item;
            double x = (double)((Value.Number)args[1]).Item;
            double y = (double)((Value.Number)args[2]).Item;
            double z = (double)((Value.Number)args[3]).Item;

            bbox.Transform = t;
            bbox.Min = new XYZ(0, 0, 0);
            bbox.Max = new XYZ(x, y, z);
            return Value.NewContainer(bbox);
        }

    }

    [NodeName("Section View")]
    [NodeCategory(BuiltinNodeCategories.REVIT_VIEW)]
    [NodeDescription("Creates a section view.")]
    public class SectionView : RevitTransactionNodeWithOneOutput
    {
        public SectionView()
        {
            InPortData.Add(new PortData("bbox", "The bounding box of the view.", typeof(Value.Container)));
            OutPortData.Add(new PortData("v", "The newly created section view.", typeof(Value.Container)));

            RegisterAllPorts();
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            ViewSection view = null;
            BoundingBoxXYZ bbox = (BoundingBoxXYZ)((Value.Container)args[0]).Item;

            //recreate the view. it does not seem possible to update a section view's orientation
            if (this.Elements.Any())
            {
                //create a new view
                view = CreateSectionView(bbox);
                Elements[0] = view.Id;
            }
            else
            {
                view = CreateSectionView(bbox);
                Elements.Add(view.Id);
            }

            return Value.NewContainer(view);
        }

        private static ViewSection CreateSectionView(BoundingBoxXYZ bbox)
        {
            //http://adndevblog.typepad.com/aec/2012/05/viewplancreate-method.html

            IEnumerable<ViewFamilyType> viewFamilyTypes = from elem in new
              FilteredElementCollector(dynRevitSettings.Doc.Document).OfClass(typeof(ViewFamilyType))
                                                          let type = elem as ViewFamilyType
                                                          where type.ViewFamily == ViewFamily.Section
                                                          select type;

            //create a new view
            ViewSection view = ViewSection.CreateSection(dynRevitSettings.Doc.Document, viewFamilyTypes.First().Id, bbox);
            return view;
        }
    }

    [NodeName("Get Active View")]
    [NodeCategory(BuiltinNodeCategories.REVIT_VIEW)]
    [NodeDescription("Gets the active Revit view.")]
    public class ActiveRevitView : RevitTransactionNodeWithOneOutput
    {
        public ActiveRevitView()
        {
            OutPortData.Add(new PortData("v", "The active revit view.", typeof(Value.Container)));

            RegisterAllPorts();
        }

        public override Value Evaluate(FSharpList<Value> args)
        {

            return Value.NewContainer(dynRevitSettings.Doc.Document.ActiveView);
        }

    }

    [NodeName("Save Image Of View")]
    [NodeCategory(BuiltinNodeCategories.REVIT_VIEW)]
    [NodeDescription("Saves an image of a Revit view.")]
    public class SaveImageFromRevitView : RevitTransactionNodeWithOneOutput
    {
        public SaveImageFromRevitView()
        {
            InPortData.Add(new PortData("view", "The view to save an image of.", typeof(Value.Container)));
            InPortData.Add(new PortData("filename", "The file to save the image as.", typeof(Value.String)));
            OutPortData.Add(new PortData("image", "An image of the revit view.", typeof(Value.Container)));

            RegisterAllPorts();
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            var view = (View)((Value.Container)args[0]).Item;
            string pathName = ((Value.String)args[1]).Item;

            //string name = view.ViewName;
            //string pathName = path; +"\\" + name;

            var options = new ImageExportOptions
            {
                ExportRange = ExportRange.SetOfViews,
                FilePath = pathName,
                HLRandWFViewsFileType = ImageFileType.PNG,
                ImageResolution = ImageResolution.DPI_72,
                ZoomType = ZoomFitType.Zoom,
                ShadowViewsFileType = ImageFileType.PNG
            };

            options.SetViewsAndSheets(new List<ElementId> { view.Id });

            dynRevitSettings.Doc.Document.ExportImage(options);//revit only has a method to save image to disk.
            //hack - make sure to change the read image below if other file types are supported
            Image image = Image.FromFile(pathName + ".png");

            return Value.NewContainer(image);
        }

    }
    
    [NodeName("Watch Image")]
    [NodeDescription("Previews an image")]
    [NodeCategory(BuiltinNodeCategories.CORE_EVALUATE)]
    public class WatchImage : NodeWithOneOutput
    {

        ResultImageUI resultImageUI = new ResultImageUI();

        System.Windows.Controls.Image image1 = null;
        public WatchImage()
        {
            InPortData.Add(new PortData("image", "image", typeof(object)));
            OutPortData.Add(new PortData("", "Success?", typeof(bool)));

            RegisterAllPorts();
        }

        public override void SetupCustomUIElements(object ui)
        {

            var NodeUI = ui as dynNodeView;

            image1 = new System.Windows.Controls.Image();
            image1.Width = 320;
            image1.Height = 240;
            //image1.Margin = new Thickness(5);
            image1.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            image1.Name = "image1";
            image1.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            //image1.DataContext = resultImageUI;

            var bindingVal = new System.Windows.Data.Binding("ResultImage")
            {
                Mode = BindingMode.OneWay,
                Converter = new ImageConverter(),
                NotifyOnValidationError = false,
                Source = resultImageUI,
                //Path = ResultImageUI,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            image1.SetBinding(System.Windows.Controls.Image.SourceProperty, bindingVal);

            NodeUI.inputGrid.Children.Add(image1);

        }

        public override Value Evaluate(FSharpList<Value> args)
        {

            resultImageUI.ResultImage = (Image)((Value.Container)args[0]).Item;

            //DispatchOnUIThread(delegate
            //{

            //   image1.Source = resultImage;
            //});

            return Value.NewNumber(1);
        }

        /// <summary>
        /// One-way converter from System.Drawing.Image to System.Windows.Media.ImageSource
        /// from http://www.stevecooper.org/index.php/2010/08/06/databinding-a-system-drawing-image-into-a-wpf-system-windows-image/
        /// </summary>
        [ValueConversion(typeof(System.Drawing.Image), typeof(System.Windows.Media.ImageSource))]
        public class ImageConverter : IValueConverter
        {
            public object Convert(object value, Type targetType,
                object parameter, CultureInfo culture)
            {
                // empty images are empty...
                if (value == null) { return null; }

                try
                {
                    var image = (System.Drawing.Image)value;

                    // Winforms Image we want to get the WPF Image from...
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    MemoryStream memoryStream = new MemoryStream();
                    // Save to a memory stream...
                    //image.Save("C:\\falconOut\\Falcon.bmp");

                    image.Save(memoryStream, ImageFormat.Bmp);
                    // Rewind the stream...
                    //memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                    bitmap.StreamSource = memoryStream;
                    bitmap.EndInit();
                    return bitmap;
                }
                catch (Exception ex)
                {
                    DynamoLogger.Instance.Log(ex.Message);
                    DynamoLogger.Instance.Log(ex.StackTrace);
                    return null;
                }
            }

            public object ConvertBack(object value, Type targetType,
                object parameter, CultureInfo culture)
            {
                return null;
            }
        }

        public class ResultImageUI : INotifyPropertyChanged
        {
            private Image resultImage;

            public Image ResultImage
            {
                get
                {
                    return resultImage;
                }

                set
                {
                    resultImage = value;
                    Notify("ResultImage");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                    handler(this, e);
            }

            protected void OnPropertyChanged(string propertyName)
            {
                OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
            }

            protected void Notify(string propertyName)
            {

                if (this.PropertyChanged != null)
                {
                    try
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch (Exception ex)
                    {
                        DynamoLogger.Instance.Log(ex.Message);
                        DynamoLogger.Instance.Log(ex.StackTrace);
                    }
                }
            }

        }
      
    }

    [NodeName("View Sheet")]
    [NodeCategory(BuiltinNodeCategories.REVIT_VIEW)]
    [NodeDescription("Create a view sheet.")]
    public class ViewSheet : RevitTransactionNodeWithOneOutput
    {
        public ViewSheet()
        {
            InPortData.Add(new PortData("name", "The name of the sheet.", typeof(Value.String)));
            InPortData.Add(new PortData("number", "The number of the sheet.", typeof(Value.String)));
            InPortData.Add(new PortData("title block", "The title block to use.", typeof(Value.Container)));
            InPortData.Add(new PortData("view(s)", "The view(s) to add to the sheet.", typeof(Value.List)));

            OutPortData.Add(new PortData("sheet", "The view sheet.", typeof(Value.Container)));

            RegisterAllPorts();
        }

        public override Value Evaluate(FSharpList<Value> args)
        {
            var name = ((Value.String)args[0]).Item;
            var number = ((Value.String)args[1]).Item;
            var tb = (FamilySymbol)((Value.Container)args[2]).Item;
            var views = ((Value.List)args[3]).Item;

            Autodesk.Revit.DB.ViewSheet sheet = null;
 
            if (this.Elements.Any())
            {
                Element e;
                if (dynUtils.TryGetElement(this.Elements[0], typeof(Autodesk.Revit.DB.ViewSheet), out e))
                {
                    sheet = (Autodesk.Revit.DB.ViewSheet)e;

                    if(sheet.Name != null && sheet.Name != name)
                        sheet.Name = name;
                    if(number != null && sheet.SheetNumber != number)
                        sheet.SheetNumber = number;
                }
                else
                {
                    //create a new view sheet
                    sheet = Autodesk.Revit.DB.ViewSheet.Create(dynRevitSettings.Doc.Document, tb.Id);
                    sheet.Name = name;
                    sheet.SheetNumber = number;
                    Elements[0] = sheet.Id;
                }
            }
            else
            {
                sheet = Autodesk.Revit.DB.ViewSheet.Create(dynRevitSettings.Doc.Document, tb.Id);
                sheet.Name = name;
                sheet.SheetNumber = number;
                Elements.Add(sheet.Id);
            }

            //rearrange views on sheets

            return Value.NewContainer(sheet);
        }
    }

    //[NodeName("Override Element Color in View")]
    //[NodeDescription("Override an element's surface color in the active view.")]
    //[NodeCategory(BuiltinNodeCategories.REVIT_VIEW)]
    //public class dynOverrideColorInView : dynRevitTransactionNodeWithOneOutput
    //{
    //    private FillPattern _solidPattern;

    //    public dynOverrideColorInView()
    //    {
    //        InPortData.Add(new PortData("color", "The color to use as an override.", typeof(Value.Container)));
    //        InPortData.Add(new PortData("element", "The element(s) to receive the new color.", typeof(Value.Container)));
    //        OutPortData.Add(new PortData("", "Success?", typeof(bool)));

    //        RegisterAllPorts();
            
    //    }

    //    public override Value Evaluate(FSharpList<Value> args)
    //    {
    //        var color = (System.Drawing.Color)((Value.Container) args[0]).Item;
    //        var elem = (Element) ((Value.Container) args[1]).Item;

    //        var view = dynRevitSettings.Doc.ActiveView;
    //        var ogs = new OverrideGraphicSettings();

    //        ogs.SetProjectionFillColor(new Autodesk.Revit.DB.Color(color.R, color.G, color.B));

    //        view.SetElementOverrides(elem.Id, ogs);
    //    }
    //}
}
