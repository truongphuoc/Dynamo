using System.IO;
using NUnit.Framework;
using Dynamo.Utilities;
using Dynamo.Nodes;
using Dynamo.Models;
using Microsoft.FSharp.Collections;

namespace Dynamo.Tests
{
    [TestFixture]
    internal class ListTests : DynamoUnitTest
    {
        [Test]
        public void TestExcel()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TestExcelGetDataWorksheet.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(5, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(6, model.CurrentWorkspace.Nodes.Count);

            //check input value
            var node1 = model.CurrentWorkspace.NodeFromWorkspace("32758a26-ef68-4f1e-9b7c-e6f9a580b86d");
            Assert.NotNull(node1);

            //get watch node
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("0be2e8a3-3eae-48c6-b789-79c3978f9417");
            var doubleWatchVal = watch.GetValue(0).GetListFromFSchemeValue();
            var firstLine = (doubleWatchVal[0]).GetListFromFSchemeValue();
            Assert.AreEqual(1.0, firstLine[0].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestCombineNode_ListNumberRange()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TestCombineNode_ListNumberRange.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(11, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(12, model.CurrentWorkspace.Nodes.Count);

            //check input value
            var node1 = model.CurrentWorkspace.NodeFromWorkspace("7a91fd07-2ff5-4438-a077-4a36f1cb1802");
            Assert.NotNull(node1);
            var node2 = model.CurrentWorkspace.NodeFromWorkspace("4970acd9-8f33-4aac-b7a5-382f8a96d5d3");
            Assert.NotNull(node2);
            var node3 = model.CurrentWorkspace.NodeFromWorkspace("b9399521-0680-4f78-99b1-2bf12300ef27");
            Assert.NotNull(node3);
            var node4 = model.CurrentWorkspace.NodeFromWorkspace("8fda00fc-14b5-4416-8be5-5e947858e3fd");
            Assert.NotNull(node4);
            var node5 = model.CurrentWorkspace.NodeFromWorkspace("6fb9e407-9a30-43b4-a5a8-0ec75f6b53fb");
            Assert.NotNull(node5);
            var node6 = model.CurrentWorkspace.NodeFromWorkspace("8a4ebd23-e223-469f-873b-a310f0274ee5");
            Assert.NotNull(node6);
            var node7 = model.CurrentWorkspace.NodeFromWorkspace("98474d52-4111-4b3a-86c1-33b0a6640df7");
            Assert.NotNull(node7);
            var node8 = model.CurrentWorkspace.NodeFromWorkspace("43ef0449-d695-4e9f-8488-8e61d7f87a18");
            Assert.NotNull(node8);
            var node9 = model.CurrentWorkspace.NodeFromWorkspace("7924ed34-c4e8-43ea-bb25-0e7f4d5825d1");
            Assert.NotNull(node9);
            var node10 = model.CurrentWorkspace.NodeFromWorkspace("5858ee20-5b48-487a-be6d-fff6bdb46fca");
            Assert.NotNull(node10);
            var node11 = model.CurrentWorkspace.NodeFromWorkspace("87da809e-e141-4d50-bbac-daf73a8fe844");
            Assert.NotNull(node11);
            var node12 = model.CurrentWorkspace.NodeFromWorkspace("36e44dd2-0439-40c2-aa05-c799d838355e");
            Assert.NotNull(node12);

            //get watch node
            //var watch = GetWatchNodeFromCurrentSpace(model, "0be2e8a3-3eae-48c6-b789-79c3978f9417");
            //var doubleWatchVal = GetDoubleFromFSchemeValue(watch.GetValue(0));
            //Assert.AreEqual(1.0, doubleWatchVal);
        }

        [Test]
        public void TestCombineNode_EdgeCase()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\CombineNode_EdgeCase.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(5, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(6, model.CurrentWorkspace.Nodes.Count);

            var node1 = model.CurrentWorkspace.NodeFromWorkspace("c645474c-1d23-4acb-8b47-1a19d5f2e3e2");
            Assert.NotNull(node1);
            var node2 = model.CurrentWorkspace.NodeFromWorkspace("43920f71-05fb-4fe5-ae41-a9975320e641");
            Assert.NotNull(node2);
            var node3 = model.CurrentWorkspace.NodeFromWorkspace("7305cc0c-d246-4e87-8715-a1a55cbb0205");
            Assert.NotNull(node3);
            var node4 = model.CurrentWorkspace.NodeFromWorkspace("a3bd8f70-810b-4ad4-a0b9-e6322cd19bd1");
            Assert.NotNull(node4);
            var node5 = model.CurrentWorkspace.NodeFromWorkspace("0a58611d-8b58-4840-9ffd-9f45e928ca76");
            Assert.NotNull(node5);
            var node6 = model.CurrentWorkspace.NodeFromWorkspace("093b94ea-3b09-4979-9897-708b8f82ba11");
            Assert.NotNull(node6);
        }

        [Test]
        public void TestTrueForAny()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TrueForAny.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(4, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(6, model.CurrentWorkspace.Nodes.Count);

            var node1 = model.CurrentWorkspace.NodeFromWorkspace("b9bc33d1-bd81-4b1b-bd11-65f817fbb3ca");
            Assert.NotNull(node1);
            var node2 = model.CurrentWorkspace.NodeFromWorkspace("5ae10a50-5909-493f-a2b4-0ca826a83258");
            Assert.NotNull(node2);
            var node3 = model.CurrentWorkspace.NodeFromWorkspace("ad9e1311-b371-468e-ac37-2cde3a1c3280");
            Assert.NotNull(node3);
        }

        [Test]
        public void TestTrueForAll()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TrueForAll.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(3, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(5, model.CurrentWorkspace.Nodes.Count);

            var node1 = model.CurrentWorkspace.NodeFromWorkspace("c09be09c-6f17-4ad4-9831-109475761db1");
            Assert.NotNull(node1);
            var node2 = model.CurrentWorkspace.NodeFromWorkspace("b9bc33d1-bd81-4b1b-bd11-65f817fbb3ca");
            Assert.NotNull(node2);
            var node3 = model.CurrentWorkspace.NodeFromWorkspace("21d2f43e-92c4-4dd1-8f61-d17d1805c74d");
            Assert.NotNull(node3);
        }

        [Test]
        public void TestSplitList()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\SplitList_NumberSequence.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(6, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(7, model.CurrentWorkspace.Nodes.Count);

            var node1 = model.CurrentWorkspace.NodeFromWorkspace("bdc0a740-19c6-4cbb-bf20-7a571f13d739");
            Assert.NotNull(node1);
            var node2 = model.CurrentWorkspace.NodeFromWorkspace("652deea7-09b2-48f6-8aa4-255f6276b71c");
            Assert.NotNull(node2);
            var node3 = model.CurrentWorkspace.NodeFromWorkspace("878ccd8d-2244-4fab-a35f-c8cae8858ee7");
            Assert.NotNull(node3);
            var node4 = model.CurrentWorkspace.NodeFromWorkspace("59632dd2-3bac-467d-b007-460333dd6012");
            Assert.NotNull(node4);
        }

        [Test]
        public void TestAddToList()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TestAddToList.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(6, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(7, model.CurrentWorkspace.Nodes.Count);

            var node1 = model.CurrentWorkspace.NodeFromWorkspace("b2fa6459-fa13-481e-96fc-9d4bc46d787e");
            Assert.NotNull(node1);
            var node2 = model.CurrentWorkspace.NodeFromWorkspace("e2a1f641-2935-4cac-a8b3-d8856c9e695a");
            Assert.NotNull(node2);
            var node3 = model.CurrentWorkspace.NodeFromWorkspace("7f3cae9a-6ea6-4708-a8a9-a1b9993117d4");
            Assert.NotNull(node3);
            var node4 = model.CurrentWorkspace.NodeFromWorkspace("4e26cd44-de84-44c0-a623-09e2499c3fe3");
            Assert.NotNull(node4);


            var node5 = model.CurrentWorkspace.NodeFromWorkspace("f6a1bac3-3374-40f4-81ab-aedb82eee515");
            Assert.NotNull(node5);
            var doubleWatchVal = node5.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(150.0, doubleWatchVal[0].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestTakeFromList()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TestTakeFromList.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(6, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(7, model.CurrentWorkspace.Nodes.Count);

            var node1 = model.CurrentWorkspace.NodeFromWorkspace("58de2589-5439-465d-8368-046eb87a4d51");
            Assert.NotNull(node1);
            var node2 = model.CurrentWorkspace.NodeFromWorkspace("8c535cc9-3dff-4715-a030-673db2bb8d4f");
            Assert.NotNull(node2);
            var node3 = model.CurrentWorkspace.NodeFromWorkspace("1fa87ad4-3cd8-4289-a09e-84ef0efe539d");
            Assert.NotNull(node3);
            var node4 = model.CurrentWorkspace.NodeFromWorkspace("133952fa-f225-425e-972a-c76fdd2c2577");
            Assert.NotNull(node4);


            var node5 = model.CurrentWorkspace.NodeFromWorkspace("8caa44c1-0a87-4902-993a-bb7e4883ed9f");
            Assert.NotNull(node5);
            var doubleWatchVal = node5.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(10000000.0, doubleWatchVal[0].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestDropFromList()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TestDropFromList.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(6, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(7, model.CurrentWorkspace.Nodes.Count);

            var node1 = model.CurrentWorkspace.NodeFromWorkspace("122ff3aa-0df1-4988-b7bd-3737768305bc");
            Assert.NotNull(node1);
            var node2 = model.CurrentWorkspace.NodeFromWorkspace("5ceed865-90aa-4811-881c-9df1157da5ef");
            Assert.NotNull(node2);
            var node3 = model.CurrentWorkspace.NodeFromWorkspace("79bacd0e-f39d-444d-8d1a-e393b4a90146");
            Assert.NotNull(node3);
            var node4 = model.CurrentWorkspace.NodeFromWorkspace("0292e6ca-1e02-48bb-9f4e-e434ceb28d05");
            Assert.NotNull(node4);


            var node5 = model.CurrentWorkspace.NodeFromWorkspace("4cc45fd3-8b4a-43dc-866b-1e0bd7b50308");
            Assert.NotNull(node5);
            var watchList = node5.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(1022350.0, watchList[0].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestShiftIndeces()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TestShiftIndeces.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(6, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(7, model.CurrentWorkspace.Nodes.Count);

            var node1 = model.CurrentWorkspace.NodeFromWorkspace("054884b8-393b-42d0-bf3a-af7178ba0c86");
            Assert.NotNull(node1);
            var node2 = model.CurrentWorkspace.NodeFromWorkspace("d29f90e3-1c9f-4ce9-a6a8-33fce81399ad");
            Assert.NotNull(node2);
            var node3 = model.CurrentWorkspace.NodeFromWorkspace("25ff103e-c78d-4277-9372-7c93cf53ee6c");
            Assert.NotNull(node3);
            var node4 = model.CurrentWorkspace.NodeFromWorkspace("24aead4d-73f6-40d5-a71b-e92f308714dd");
            Assert.NotNull(node4);


            var node5 = model.CurrentWorkspace.NodeFromWorkspace("705393f3-9eca-42c6-9e77-91bd0d781452");
            Assert.NotNull(node5);

            //var doubleWatchVal = GetListFromFSchemeValue(node5.GetValue(0));
            var doubleWatchVal = node5.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(285.0, doubleWatchVal[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(288.0, doubleWatchVal[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(291.0, doubleWatchVal[2].GetDoubleFromFSchemeValue());
            Assert.AreEqual(294.0, doubleWatchVal[3].GetDoubleFromFSchemeValue());
            Assert.AreEqual(297.0, doubleWatchVal[4].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestGetFromList()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TestGetFromList.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(6, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(7, model.CurrentWorkspace.Nodes.Count);

            var node1 = model.CurrentWorkspace.NodeFromWorkspace("2031df04-9fba-435c-b991-94df4f76f634");
            Assert.NotNull(node1);
            var node2 = model.CurrentWorkspace.NodeFromWorkspace("d223a746-757f-4212-b2bb-9406e6c1feb4");
            Assert.NotNull(node2);
            var node3 = model.CurrentWorkspace.NodeFromWorkspace("310dc80a-349f-4e30-9319-dd1edc71eb53");
            Assert.NotNull(node3);
            var node4 = model.CurrentWorkspace.NodeFromWorkspace("9605ee13-8dec-48d2-ba24-adaaf232ef0b");
            Assert.NotNull(node4);

            var node5 = model.CurrentWorkspace.NodeFromWorkspace("8fc7ce00-182e-4ad0-ae0a-9d0058d2f0f3");
            Assert.NotNull(node5);
            Assert.AreEqual(2000000099, node5.GetValue(0).GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestRemoveFromList()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TestRemoveFromList.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(6, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(7, model.CurrentWorkspace.Nodes.Count);

            var node1 = model.CurrentWorkspace.NodeFromWorkspace("9b5f59a8-43ad-4388-9f1d-c141cc7afc83");
            Assert.NotNull(node1);
            var node2 = model.CurrentWorkspace.NodeFromWorkspace("d223a746-757f-4212-b2bb-9406e6c1feb4");
            Assert.NotNull(node2);
            var node3 = model.CurrentWorkspace.NodeFromWorkspace("310dc80a-349f-4e30-9319-dd1edc71eb53");
            Assert.NotNull(node3);
            var node4 = model.CurrentWorkspace.NodeFromWorkspace("9605ee13-8dec-48d2-ba24-adaaf232ef0b");
            Assert.NotNull(node4);

            var node5 = model.CurrentWorkspace.NodeFromWorkspace("8fc7ce00-182e-4ad0-ae0a-9d0058d2f0f3");
            Assert.NotNull(node5);
            FSharpList<FScheme.Value> listWatchVal = node5.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(2000000100, listWatchVal[99].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestDropEveryNth()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TestDropEveryNth.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(7, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(8, model.CurrentWorkspace.Nodes.Count);

            var node1 = model.CurrentWorkspace.NodeFromWorkspace("d1dba071-81e0-4946-8711-833fb8c6a61c");
            Assert.NotNull(node1);
            var node2 = model.CurrentWorkspace.NodeFromWorkspace("d859cf13-d326-4947-bfad-395ead326160");
            Assert.NotNull(node2);
            var node3 = model.CurrentWorkspace.NodeFromWorkspace("4f51caa4-9dcc-4f89-a52e-4f1dfc644771");
            Assert.NotNull(node3);
            var node4 = model.CurrentWorkspace.NodeFromWorkspace("7a8dd3f4-ce34-485e-9fc9-0e1ec49892dc");
            Assert.NotNull(node4);

            var node5 = model.CurrentWorkspace.NodeFromWorkspace("7a33aa54-c891-4551-a3c8-98f8a1e95bb1");
            Assert.NotNull(node5);
            FSharpList<FScheme.Value> listWatchVal = node5.GetValue(0).GetListFromFSchemeValue();
            //FSharpList<FScheme.Value> listWatchVal = GetListFromFSchemeValue(node5.GetValue(0));
            //var doubleVar = GetDoubleFromFSchemeValue(node5.GetValue(0));
            Assert.AreEqual(120000, listWatchVal[0].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestTakeEveryNth()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TestTakeEveryNth.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(7, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(8, model.CurrentWorkspace.Nodes.Count);

            var node1 = model.CurrentWorkspace.NodeFromWorkspace("2ccbe80b-d0f9-4c5d-884b-260ea5c2336b");
            Assert.NotNull(node1);
            var node2 = model.CurrentWorkspace.NodeFromWorkspace("7ac8f293-30df-416a-ae27-9fa9a122b775");
            Assert.NotNull(node2);
            var node3 = model.CurrentWorkspace.NodeFromWorkspace("c9ac7caf-8dc4-4096-b6d4-a33d89fb41a2");
            Assert.NotNull(node3);
            var node4 = model.CurrentWorkspace.NodeFromWorkspace("ca1af682-d2fa-466e-9e4e-ef2b60e9bcdf");
            Assert.NotNull(node4);

            var node5 = model.CurrentWorkspace.NodeFromWorkspace<Watch>("a0cefe95-4fa6-44c4-b829-e39d228233f0");
            Assert.NotNull(node5);
            FSharpList<FScheme.Value> listWatchVal = node5.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(100150.0, listWatchVal[0].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestIsEmptyList()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TestEmpty.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(8, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(10, model.CurrentWorkspace.Nodes.Count);

            var node1 = model.CurrentWorkspace.NodeFromWorkspace("d1dba071-81e0-4946-8711-833fb8c6a61c");
            Assert.NotNull(node1);
            var node2 = model.CurrentWorkspace.NodeFromWorkspace("d859cf13-d326-4947-bfad-395ead326160");
            Assert.NotNull(node2);
            var node3 = model.CurrentWorkspace.NodeFromWorkspace("4f51caa4-9dcc-4f89-a52e-4f1dfc644771");
            Assert.NotNull(node3);
            var node4 = model.CurrentWorkspace.NodeFromWorkspace("7a8dd3f4-ce34-485e-9fc9-0e1ec49892dc");
            Assert.NotNull(node4);

            var node5 = model.CurrentWorkspace.NodeFromWorkspace<Watch>("fbaef2c4-380f-4c60-b2ed-c0c644ee0ce9");
            Assert.NotNull(node5);
            Assert.AreEqual(1.0, node5.GetValue(0).GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestSplitList_edge()
        {
            var model = dynSettings.Controller.DynamoModel;
            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TestSplitList.dyn");
            model.Open(openPath);

            dynSettings.Controller.RunExpression(null);

            Assert.AreEqual(4, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(5, model.CurrentWorkspace.Nodes.Count);

            var node1 = model.CurrentWorkspace.NodeFromWorkspace("67f11474-4143-4069-bd1a-90105e432ac8");
            Assert.NotNull(node1);
            var node2 = model.CurrentWorkspace.NodeFromWorkspace("30ef2258-2c62-43f8-a1f9-00d1ae340f34");
            Assert.NotNull(node2);
            var node3 = model.CurrentWorkspace.NodeFromWorkspace("e225d5a9-2216-470b-b5fb-fd2967ff2131");
            Assert.NotNull(node3);

            var node5 = model.CurrentWorkspace.NodeFromWorkspace<Watch>("5c320c9e-cc38-47b1-8cab-e6ce643c31db");
            Assert.NotNull(node5);
            Assert.AreEqual(0.0, node5.GetValue(0).GetDoubleFromFSchemeValue());
        }
    
        string listTestFolder { get { return Path.Combine(GetTestDirectory(), "core", "list"); } }

        [Test]
        public void TestBuildSublistsEmptyInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testBuildSubLists_emptyInput.dyn");
            model.Open(testFilePath);

            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");
            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(0, actual.Length);
        }

        [Test]
        public void TestBuildSublistsInvalidInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testBuildSubLists_invalidInput.dyn");
            model.Open(testFilePath);

            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void TestBuildSublistsNumberInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testBuildSubLists_numberInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild1 = actual[0].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild2 = actual[1].GetListFromFSchemeValue();

            Assert.AreEqual(2, actual.Length);
            Assert.AreEqual(1, actualChild1.Length);
            Assert.AreEqual(1, actualChild1[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(1, actualChild2.Length);
            Assert.AreEqual(3, actualChild2[0].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestBuildSublistsStringInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testBuildSubLists_stringInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild1 = actual[0].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild2 = actual[1].GetListFromFSchemeValue();

            Assert.AreEqual(2, actual.Length);
            Assert.AreEqual(1, actualChild1.Length);
            Assert.AreEqual("b", actualChild1[0].getStringFromFSchemeValue());
            Assert.AreEqual(1, actualChild2.Length);
            Assert.AreEqual("d", actualChild2[0].getStringFromFSchemeValue());
        }

        [Test]
        public void TestConcatenateListsEmptyInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testConcatenateLists_emptyInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();

            Assert.AreEqual(0, actual.Length);
        }

        [Test]
        public void TestConcatenateListsInvalidInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testConcatenateLists_invalidInput.dyn");
            model.Open(testFilePath);
            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void TestConcatenateListsNormalInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testConcatenateLists_normalInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();

            Assert.AreEqual(9, actual.Length);
            Assert.AreEqual(10, actual[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(20, actual[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(10, actual[2].GetDoubleFromFSchemeValue());
            Assert.AreEqual(20, actual[3].GetDoubleFromFSchemeValue());
            Assert.AreEqual(10, actual[4].GetDoubleFromFSchemeValue());
            Assert.AreEqual("a", actual[5].getStringFromFSchemeValue());
            Assert.AreEqual("b", actual[6].getStringFromFSchemeValue());
            Assert.AreEqual("a", actual[7].getStringFromFSchemeValue());
            Assert.AreEqual("b", actual[8].getStringFromFSchemeValue());
        }

        [Test]
        public void TestDiagonalLeftListEmptyInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testDiagonaLeftList_emptyInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();

            Assert.AreEqual(0, actual.Length);
        }

        [Test]
        public void TestDiagonalLeftListInvalidInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testDiagonaLeftList_invalidInput.dyn");
            model.Open(testFilePath);
            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void TestDiagonalLeftListNumberInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testDiagonaLeftList_numberInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild1 = actual[0].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild2 = actual[1].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild3 = actual[2].GetListFromFSchemeValue();

            Assert.AreEqual(3, actual.Length);

            Assert.AreEqual(1, actualChild1.Length);
            Assert.AreEqual(1, actualChild1[0].GetDoubleFromFSchemeValue());

            Assert.AreEqual(2, actualChild2.Length);
            Assert.AreEqual(2, actualChild2[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(3, actualChild2[1].GetDoubleFromFSchemeValue());

            Assert.AreEqual(2, actualChild2.Length);
            Assert.AreEqual(4, actualChild3[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(5, actualChild3[1].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestDiagonalLeftListStringInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testDiagonaLeftList_stringInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(3, actual.Length);
            FSharpList<FScheme.Value> actualChild1 = actual[0].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild2 = actual[1].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild3 = actual[2].GetListFromFSchemeValue();

            Assert.AreEqual(1, actualChild1.Length);
            Assert.AreEqual("a", actualChild1[0].getStringFromFSchemeValue());

            Assert.AreEqual(2, actualChild2.Length);
            Assert.AreEqual("b", actualChild2[0].getStringFromFSchemeValue());
            Assert.AreEqual("a", actualChild2[1].getStringFromFSchemeValue());

            Assert.AreEqual(1, actualChild3.Length);
            Assert.AreEqual("b", actualChild3[0].getStringFromFSchemeValue());
        }

        [Test]
        public void TestDiagonalRightListEmptyInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testDiagonaRightList_emptyInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(0, actual.Length);
        }

        [Test]
        public void TestDiagonalRightListInvalidInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testDiagonaRightList_invalidInput.dyn");
            model.Open(testFilePath);

            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void TestDiagonalRightListNumberInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testDiagonaRightList_numberInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(4, actual.Length);
            FSharpList<FScheme.Value> actualChild1 = actual[0].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild2 = actual[1].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild3 = actual[2].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild4 = actual[3].GetListFromFSchemeValue();

            Assert.AreEqual(1, actualChild1.Length);
            Assert.AreEqual(5, actualChild1[0].GetDoubleFromFSchemeValue());

            Assert.AreEqual(1, actualChild2.Length);
            Assert.AreEqual(3, actualChild2[0].GetDoubleFromFSchemeValue());

            Assert.AreEqual(2, actualChild3.Length);
            Assert.AreEqual(1, actualChild3[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(4, actualChild3[1].GetDoubleFromFSchemeValue());

            Assert.AreEqual(1, actualChild4.Length);
            Assert.AreEqual(2, actualChild4[0].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestFirstOfListEmptyInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testFirstOfList_emptyInput.dyn");
            model.Open(testFilePath);
            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void TestFirstOfListInvalidInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testFirstOfList_invalidInput.dyn");
            model.Open(testFilePath);
            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void TestFirstOfListNumberInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testFirstOfList_numberInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            double actual = watch.GetValue(0).GetDoubleFromFSchemeValue();
            double expected = 10;
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestFirstOfListStringInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testFirstOfList_stringInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            string actual = watch.GetValue(0).getStringFromFSchemeValue();
            string expected = "a";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestIsEmptyListEmptyInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testIsEmptyList_emptyInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            double actual = watch.GetValue(0).GetDoubleFromFSchemeValue();
            double expected = 1;
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestIsEmptyListInvalidInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testIsEmptyList_invalidInput.dyn");
            model.Open(testFilePath);
            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void TestIsEmptyListNumberInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testIsEmptyList_numberInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            double actual = watch.GetValue(0).GetDoubleFromFSchemeValue();
            double expected = 0;
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestIsEmptyListStringInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testIsEmptyList_stringInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            double actual = watch.GetValue(0).GetDoubleFromFSchemeValue();
            double expected = 0;
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestStringLengthEmptyInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testListLength_emptyInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            double actual = watch.GetValue(0).GetDoubleFromFSchemeValue();
            double expected = 0;
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestStringLengthInvalidInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testListLength_invalidInput.dyn");
            model.Open(testFilePath);
            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void TestStringLengthNumberInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testListLength_numberInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            double actual = watch.GetValue(0).GetDoubleFromFSchemeValue();
            double expected = 5;
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestStringLengthStringInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testListLength_stringInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            double actual = watch.GetValue(0).GetDoubleFromFSchemeValue();
            double expected = 4;
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestPartitionStringEmptyInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testPartitionList_emptyInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            double expected = 0;
            Assert.AreEqual(expected, actual.Length);
        }

        [Test]
        public void TestPartitionStringInvalidInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testPartitionList_invalidInput.dyn");
            model.Open(testFilePath);
            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void TestPartitionStringNumberInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testPartitionList_numberInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(2, actual.Length);

            FSharpList<FScheme.Value> childList1 = actual[0].GetListFromFSchemeValue();
            Assert.AreEqual(3, childList1.Length);
            Assert.AreEqual(1, childList1[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(2, childList1[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(3, childList1[2].GetDoubleFromFSchemeValue());

            FSharpList<FScheme.Value> childList2 = actual[1].GetListFromFSchemeValue();
            Assert.AreEqual(2, childList2.Length);
            Assert.AreEqual(4, childList2[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(5, childList2[1].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestPartitionStringStringInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testPartitionList_stringInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(2, actual.Length);

            FSharpList<FScheme.Value> childList1 = actual[0].GetListFromFSchemeValue();
            Assert.AreEqual(3, childList1.Length);
            Assert.AreEqual("a", childList1[0].getStringFromFSchemeValue());
            Assert.AreEqual("b", childList1[1].getStringFromFSchemeValue());
            Assert.AreEqual("a", childList1[2].getStringFromFSchemeValue());

            FSharpList<FScheme.Value> childList2 = actual[1].GetListFromFSchemeValue();
            Assert.AreEqual(1, childList2.Length);
            Assert.AreEqual("b", childList2[0].getStringFromFSchemeValue());
        }

        [Test]
        public void TestFlattenEmptyInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testPlatten_emptyInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            double expectedLength = 0;
            Assert.AreEqual(expectedLength, actual.Length);
        }

        [Test]
        public void TestFlattenInvalidInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testPlatten_invalidInput.dyn");
            model.Open(testFilePath);
            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void TestFlattenNormalInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testPlatten_normalInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(6, actual.Length);

            FSharpList<FScheme.Value> childList1 = actual[0].GetListFromFSchemeValue();
            Assert.AreEqual(5, childList1.Length);
            Assert.AreEqual(0, childList1[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(1, childList1[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(2, childList1[2].GetDoubleFromFSchemeValue());
            Assert.AreEqual(3, childList1[3].GetDoubleFromFSchemeValue());
            Assert.AreEqual(4, childList1[4].GetDoubleFromFSchemeValue());

            FSharpList<FScheme.Value> childList2 = actual[1].GetListFromFSchemeValue();
            Assert.AreEqual(4, childList2.Length);
            Assert.AreEqual("a", childList2[0].getStringFromFSchemeValue());
            Assert.AreEqual("b", childList2[1].getStringFromFSchemeValue());
            Assert.AreEqual("c", childList2[2].getStringFromFSchemeValue());
            Assert.AreEqual("d", childList2[3].getStringFromFSchemeValue());

            Assert.AreEqual("a", actual[2].getStringFromFSchemeValue());
            Assert.AreEqual("b", actual[3].getStringFromFSchemeValue());
            Assert.AreEqual("c", actual[4].getStringFromFSchemeValue());
            Assert.AreEqual("d", actual[5].getStringFromFSchemeValue());
        }

        [Test]
        public void TestFlattenCompletlyEmptyInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testPlattenCompletely_emptyInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            double expectedLength = 0;
            Assert.AreEqual(expectedLength, actual.Length);
        }

        [Test]
        public void TestFlattenCompletlyInvalidInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testPlattenCompletely_invalidInput.dyn");
            model.Open(testFilePath);
            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void TestFlattenCompletlyNormalInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testPlattenCompletely_normalInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(13, actual.Length);

            Assert.AreEqual(0, actual[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(1, actual[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(2, actual[2].GetDoubleFromFSchemeValue());
            Assert.AreEqual(3, actual[3].GetDoubleFromFSchemeValue());
            Assert.AreEqual(4, actual[4].GetDoubleFromFSchemeValue());
            Assert.AreEqual("a", actual[5].getStringFromFSchemeValue());
            Assert.AreEqual("b", actual[6].getStringFromFSchemeValue());
            Assert.AreEqual("c", actual[7].getStringFromFSchemeValue());
            Assert.AreEqual("d", actual[8].getStringFromFSchemeValue());
            Assert.AreEqual("a", actual[9].getStringFromFSchemeValue());
            Assert.AreEqual("b", actual[10].getStringFromFSchemeValue());
            Assert.AreEqual("c", actual[11].getStringFromFSchemeValue());
            Assert.AreEqual("d", actual[12].getStringFromFSchemeValue());
        }

        [Test]
        public void TestRepeatEmptyInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testRepeat_emptyInput.dyn");
            model.Open(testFilePath);
            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void TestRepeatNumberInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testRepeat_numberInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(5, actual.Length);

            FSharpList<FScheme.Value> childList1 = actual[0].GetListFromFSchemeValue();
            Assert.AreEqual(2, childList1.Length);
            Assert.AreEqual(0, childList1[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(0, childList1[1].GetDoubleFromFSchemeValue());

            FSharpList<FScheme.Value> childList2 = actual[1].GetListFromFSchemeValue();
            Assert.AreEqual(2, childList1.Length);
            Assert.AreEqual(1, childList2[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(1, childList2[1].GetDoubleFromFSchemeValue());

            FSharpList<FScheme.Value> childList3 = actual[2].GetListFromFSchemeValue();
            Assert.AreEqual(2, childList1.Length);
            Assert.AreEqual(2, childList3[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(2, childList3[1].GetDoubleFromFSchemeValue());

            FSharpList<FScheme.Value> childList4 = actual[3].GetListFromFSchemeValue();
            Assert.AreEqual(2, childList1.Length);
            Assert.AreEqual(3, childList4[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(3, childList4[1].GetDoubleFromFSchemeValue());

            FSharpList<FScheme.Value> childList5 = actual[4].GetListFromFSchemeValue();
            Assert.AreEqual(2, childList1.Length);
            Assert.AreEqual(4, childList5[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(4, childList5[1].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestRepeatStringInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testRepeat_stringInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(4, actual.Length);

            FSharpList<FScheme.Value> childList1 = actual[0].GetListFromFSchemeValue();
            Assert.AreEqual(2, childList1.Length);
            Assert.AreEqual("a", childList1[0].getStringFromFSchemeValue());
            Assert.AreEqual("a", childList1[1].getStringFromFSchemeValue());

            FSharpList<FScheme.Value> childList2 = actual[1].GetListFromFSchemeValue();
            Assert.AreEqual(2, childList1.Length);
            Assert.AreEqual("b", childList2[0].getStringFromFSchemeValue());
            Assert.AreEqual("b", childList2[1].getStringFromFSchemeValue());

            FSharpList<FScheme.Value> childList3 = actual[2].GetListFromFSchemeValue();
            Assert.AreEqual(2, childList1.Length);
            Assert.AreEqual("c", childList3[0].getStringFromFSchemeValue());
            Assert.AreEqual("c", childList3[1].getStringFromFSchemeValue());

            FSharpList<FScheme.Value> childList4 = actual[3].GetListFromFSchemeValue();
            Assert.AreEqual(2, childList1.Length);
            Assert.AreEqual("d", childList4[0].getStringFromFSchemeValue());
            Assert.AreEqual("d", childList4[1].getStringFromFSchemeValue());
        }

        [Test]
        public void TestRestOfListEmptyInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testRestOfList_emptyInput.dyn");
            model.Open(testFilePath);
            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void TestRestOfListInvalidInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testRestOfList_invalidInput.dyn");
            model.Open(testFilePath);
            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void TestRestOfListNumberInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testRestOfList_numberInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(4, actual.Length);
            Assert.AreEqual(20, actual[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(10, actual[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(20, actual[2].GetDoubleFromFSchemeValue());
            Assert.AreEqual(10, actual[3].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void TestRestOfListStringInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testRestOfList_stringInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(3, actual.Length);
            Assert.AreEqual("b", actual[0].getStringFromFSchemeValue());
            Assert.AreEqual("a", actual[1].getStringFromFSchemeValue());
            Assert.AreEqual("b", actual[2].getStringFromFSchemeValue());
        }

        [Test]
        public void TestTransposeEmptyInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testTransposeList_emptyInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(0, actual.Length);
        }

        [Test]
        public void TestTransposeInvalidInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testTransposeList_invalidInput.dyn");
            model.Open(testFilePath);
            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void TestTransposeNormalInput()
        {
            DynamoModel model = Controller.DynamoModel;
            string testFilePath = Path.Combine(listTestFolder, "testTransposeList_normalInput.dyn");
            model.Open(testFilePath);
            dynSettings.Controller.RunExpression(null);
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("789c1592-b64c-4a97-8f1a-8cef3d0cc2d0");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(5, actual.Length);

            FSharpList<FScheme.Value> childList1 = actual[0].GetListFromFSchemeValue();
            Assert.AreEqual(2, childList1.Length);
            Assert.AreEqual(1, childList1[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual("a", childList1[1].getStringFromFSchemeValue());

            FSharpList<FScheme.Value> childList2 = actual[1].GetListFromFSchemeValue();
            Assert.AreEqual(2, childList1.Length);
            Assert.AreEqual(2, childList2[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual("b", childList2[1].getStringFromFSchemeValue());

            FSharpList<FScheme.Value> childList3 = actual[2].GetListFromFSchemeValue();
            Assert.AreEqual(2, childList1.Length);
            Assert.AreEqual(3, childList3[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual("a", childList3[1].getStringFromFSchemeValue());

            FSharpList<FScheme.Value> childList4 = actual[3].GetListFromFSchemeValue();
            Assert.AreEqual(2, childList1.Length);
            Assert.AreEqual(4, childList4[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual("b", childList4[1].getStringFromFSchemeValue());

            FSharpList<FScheme.Value> childList5 = actual[4].GetListFromFSchemeValue();
            Assert.AreEqual(2, childList1.Length);
            Assert.AreEqual(5, childList5[0].GetDoubleFromFSchemeValue());
        }

        #region Sort Test Cases

        [Test]
        public void Sort_NumbersfFromDiffInput()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\Sort_NumbersfFromDiffInput.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(18, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(15, model.CurrentWorkspace.Nodes.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // fourth and last element in the list before sorting
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("de6bd134-55d1-4fb8-a605-1c486b5acb5f");
            FSharpList<FScheme.Value> listWatchVal = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(8, listWatchVal.Length);
            Assert.AreEqual(1, listWatchVal[4].GetDoubleFromFSchemeValue());
            Assert.AreEqual(0, listWatchVal[7].GetDoubleFromFSchemeValue());

            // First and last element in the list after sorting
            watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("25ee495f-2d8e-4fa5-8180-6d0e45eb4675");
            FSharpList<FScheme.Value> listWatchVal2 = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(8, listWatchVal2.Length);
            Assert.AreEqual(-3.76498800959146, listWatchVal2[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(1, listWatchVal2[7].GetDoubleFromFSchemeValue());
        }


        [Test]
        public void Sort_SimpleNumbers()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\Sort_SimpleNumbers.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(11, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(12, model.CurrentWorkspace.Nodes.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // First and last element in the list before sorting
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("de6bd134-55d1-4fb8-a605-1c486b5acb5f");
            FSharpList<FScheme.Value> listWatchVal = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(8, listWatchVal.Length);
            Assert.AreEqual(2, listWatchVal[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(1.7, listWatchVal[7].GetDoubleFromFSchemeValue());

            // First and last element in the list after sorting
            watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("25ee495f-2d8e-4fa5-8180-6d0e45eb4675");
            FSharpList<FScheme.Value> listWatchVal2 = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(8, listWatchVal2.Length);
            Assert.AreEqual(0, listWatchVal2[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(10, listWatchVal2[7].GetDoubleFromFSchemeValue());
        }


        [Test]
        public void Sort_StringsAndNumbers_NegativeTest()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\Sort_Strings&Numbers.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(7, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(8, model.CurrentWorkspace.Nodes.Count);

            // run the expression
            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });
        }

        [Test]
        public void Sort_Strings()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\Sort_Strings.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(8, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(9, model.CurrentWorkspace.Nodes.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // First and last element in the list before sorting
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("aa64651f-29cb-4008-b199-ec2f4ab3a1f7");
            FSharpList<FScheme.Value> listWatchVal = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(5, listWatchVal.Length);
            Assert.AreEqual("dddd", listWatchVal[0].getStringFromFSchemeValue());
            Assert.AreEqual("bbbbbbbbbbbbb", listWatchVal[4].getStringFromFSchemeValue());

            // First and last element in the list after sorting
            watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("d8ee9c7c-c456-4a38-a5d8-07eca624ebfe");
            FSharpList<FScheme.Value> listWatchVal2 = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(5, listWatchVal2.Length);
            Assert.AreEqual("a", listWatchVal2[0].getStringFromFSchemeValue());
            Assert.AreEqual("rrrrrrrrr", listWatchVal2[4].getStringFromFSchemeValue());
        }
        #endregion

        #region SortBy Test Cases
        [Test]
        public void SortBy_SimpleTest()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\SortBy_SimpleTest.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(10, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(10, model.CurrentWorkspace.Nodes.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // First and last element in the list before sorting
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("3cf42e26-c178-4cc4-81a5-38b1c7867f5e");
            FSharpList<FScheme.Value> listWatchVal = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(5, listWatchVal.Length);
            Assert.AreEqual(10.23, listWatchVal[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(8, listWatchVal[4].GetDoubleFromFSchemeValue());

            // First and last element in the list after sorting
            watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("c966ac1d-5caa-4cfe-bb0c-f6db9e5697c4");
            FSharpList<FScheme.Value> listWatchVal2 = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(5, listWatchVal2.Length);
            Assert.AreEqual(10.23, listWatchVal2[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(0.45, listWatchVal2[4].GetDoubleFromFSchemeValue());
        }
        #endregion

        #region Reverse Test Cases

        [Test]
        public void Reverse_ListWithOneNumber()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\Reverse_ListWithOneNumber.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(3, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(4, model.CurrentWorkspace.Nodes.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // First element in the list before Reversing
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("44505507-11d2-4792-b785-039304cadf89");
            FSharpList<FScheme.Value> listWatchVal = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(1, listWatchVal.Length);
            Assert.AreEqual(0, listWatchVal[0].GetDoubleFromFSchemeValue());

        }

        [Test]
        public void Reverse_MixedList()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\Reverse_MixedList.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(11, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(10, model.CurrentWorkspace.Nodes.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // First element in the list before Reversing
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("44505507-11d2-4792-b785-039304cadf89");
            FSharpList<FScheme.Value> listWatchVal = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(6, listWatchVal.Length);
            Assert.AreEqual(54.5, listWatchVal[0].GetDoubleFromFSchemeValue());

            // First element in the list after Reversing
            watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("6dc62b9d-6045-4b68-a34c-2d5da999958b");
            FSharpList<FScheme.Value> listWatchVal1 = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(6, listWatchVal1.Length);
            Assert.AreEqual("Dynamo", listWatchVal1[0].getStringFromFSchemeValue());

        }

        [Test]
        public void Reverse_NumberRange()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\Reverse_NumberRange.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(6, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(7, model.CurrentWorkspace.Nodes.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // First and last element in the list before Reversing
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("44505507-11d2-4792-b785-039304cadf89");
            FSharpList<FScheme.Value> listWatchVal = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(8, listWatchVal.Length);
            Assert.AreEqual(6, listWatchVal[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(-1, listWatchVal[7].GetDoubleFromFSchemeValue());

            // First and last element in the list after Reversing
            watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("6dc62b9d-6045-4b68-a34c-2d5da999958b");
            FSharpList<FScheme.Value> listWatchVal1 = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(8, listWatchVal1.Length);
            Assert.AreEqual(-1, listWatchVal1[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(6, listWatchVal1[7].GetDoubleFromFSchemeValue());

        }

        [Test]
        public void Reverse_UsingStringList()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\Reverse_UsingStringList.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(7, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(8, model.CurrentWorkspace.Nodes.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // First and last element in the list before Reversing
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("44505507-11d2-4792-b785-039304cadf89");
            FSharpList<FScheme.Value> listWatchVal = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(4, listWatchVal.Length);
            Assert.AreEqual("Script", listWatchVal[0].getStringFromFSchemeValue());
            Assert.AreEqual("Dynamo", listWatchVal[3].getStringFromFSchemeValue());

            // First and last element in the list after Reversing
            watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("6dc62b9d-6045-4b68-a34c-2d5da999958b");
            FSharpList<FScheme.Value> listWatchVal1 = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(4, listWatchVal1.Length);
            Assert.AreEqual("Dynamo", listWatchVal1[0].getStringFromFSchemeValue());
            Assert.AreEqual("Script", listWatchVal1[3].getStringFromFSchemeValue());

        }

        [Test]
        public void Reverse_WithArrayInput()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\Reverse_WithArrayInput.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(15, model.CurrentWorkspace.Connectors.Count);
            Assert.AreEqual(16, model.CurrentWorkspace.Nodes.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // First and last element in the list before Reversing
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("1c9d53b6-b5e0-4282-9768-a6c53115aba4");
            FSharpList<FScheme.Value> listWatchVal = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(3, listWatchVal.Length);
            //Assert.AreEqual(2, GetDoubleFromFSchemeValue(listWatchVal[0]));
            //Assert.AreEqual("Dynamo", GetDoubleFromFSchemeValue(listWatchVal[3]));

            // First and last element in the list after Reversing
            watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("2e8a3965-c908-4358-b7fc-331d0f3109ac");
            FSharpList<FScheme.Value> listWatchVal1 = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(3, listWatchVal1.Length);
            //Assert.AreEqual("Dynamo", getStringFromFSchemeValue(listWatchVal1[0]));
            //Assert.AreEqual("Script", getStringFromFSchemeValue(listWatchVal1[3]));

        }

        [Test]
        public void Reverse_WithSingletonInput()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\Reverse_WithSingletonInput.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(7, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(6, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // First and last element in the list before Reversing
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("1c9d53b6-b5e0-4282-9768-a6c53115aba4");
            FSharpList<FScheme.Value> listWatchVal = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(3, listWatchVal.Length);
            Assert.AreEqual(10, listWatchVal[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(2, listWatchVal[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(3, listWatchVal[2].GetDoubleFromFSchemeValue());

            // First and last element in the list after Reversing
            watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("2e8a3965-c908-4358-b7fc-331d0f3109ac");
            FSharpList<FScheme.Value> listWatchVal1 = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(3, listWatchVal1.Length);
            Assert.AreEqual(3, listWatchVal1[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(2, listWatchVal1[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(10, listWatchVal1[2].GetDoubleFromFSchemeValue());

        }

        #endregion

        #region Filter Tests

        [Test]
        public void Filter_SimpleTest()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\Filter_SimpleTest.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(9, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(8, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // First, Second and last element in the list before Filter
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("a54127b5-decb-4750-aaf3-1b895be73984");
            FSharpList<FScheme.Value> listWatchVal = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(11, listWatchVal.Length);
            Assert.AreEqual(0, listWatchVal[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(1, listWatchVal[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(10, listWatchVal[10].GetDoubleFromFSchemeValue());

            // First, Second and last element in the list after Filter
            watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("41279a88-2f0b-4bd3-bef1-1be693df5c7e");
            FSharpList<FScheme.Value> listWatchVal1 = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(5, listWatchVal1.Length);
            Assert.AreEqual(6, listWatchVal1[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(7, listWatchVal1[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(10, listWatchVal1[4].GetDoubleFromFSchemeValue());

        }

        [Test]
        public void Filter_NegativeTest()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\Filter_NegativeTest.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(9, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(8, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // First, second and last element in the list before Filter
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("1327061f-b25d-4e91-9df7-a79850cb59e0");
            FSharpList<FScheme.Value> listWatchVal = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(6, listWatchVal.Length);
            Assert.AreEqual(0, listWatchVal[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(1, listWatchVal[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(5, listWatchVal[5].GetDoubleFromFSchemeValue());

            // After filter there should not
            watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("41279a88-2f0b-4bd3-bef1-1be693df5c7e");
            FSharpList<FScheme.Value> listWatchVal1 = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(0, listWatchVal1.Length);

        }

        [Test]
        public void Filter_Complex()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\Filter_Complex.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(11, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(12, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // First, second and last element in the first list after Filter
            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("fce51e58-10e1-46b4-bc4c-756dfde00de7");
            FSharpList<FScheme.Value> listWatchVal = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(5, listWatchVal.Length);
            Assert.AreEqual(6, listWatchVal[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(7, listWatchVal[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(10, listWatchVal[4].GetDoubleFromFSchemeValue());

            // First, second and last element in the second list after Filter
            watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("412526ae-d86c-491c-a587-d43598fa9c93");
            FSharpList<FScheme.Value> listWatchVal1 = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(5, listWatchVal1.Length);
            Assert.AreEqual(0, listWatchVal1[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(1, listWatchVal1[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(4, listWatchVal1[4].GetDoubleFromFSchemeValue());

            // First, second and last elements in the list after combining above two filtered list
            watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("dc27f671-4cef-480f-9ddc-218d61db7e52");
            FSharpList<FScheme.Value> listWatchVal2 = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(5, listWatchVal2.Length);
            Assert.AreEqual(double.PositiveInfinity, listWatchVal2[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(7, listWatchVal2[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(2.5, listWatchVal2[4].GetDoubleFromFSchemeValue());


        }

        #endregion

        #region LaceShortest test cases

        [Test]
        public void LaceShortest_Simple()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\LaceShortest_Simple.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(13, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(12, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // Element from the Reverse list
            var reverse = model.CurrentWorkspace.NodeFromWorkspace<Reverse>("c3d629f7-76a0-40bc-bf39-da45d8b8ea7a");
            FSharpList<FScheme.Value> listReverseValue = reverse.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(2, listReverseValue.Length);
            Assert.AreEqual(4, listReverseValue[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(2, listReverseValue[1].GetDoubleFromFSchemeValue());

            // Elements from the Combine list
            var combine = model.CurrentWorkspace.NodeFromWorkspace<Combine>("cc23b43e-3709-4ed1-bedb-f903e4ea7d75");
            FSharpList<FScheme.Value> listCombineValue = combine.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(2, listCombineValue.Length);
            Assert.AreEqual(-0.5, listCombineValue[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(-1, listCombineValue[1].GetDoubleFromFSchemeValue());

            // Elements from first LaceShortest list
            var shortest = model.CurrentWorkspace.NodeFromWorkspace<LaceShortest>("10005d3c-3bbf-4690-b658-37b11c8402b1");
            FSharpList<FScheme.Value> listShotestValue = shortest.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(2, listShotestValue.Length);
            Assert.AreEqual(2, listShotestValue[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(4, listShotestValue[1].GetDoubleFromFSchemeValue());

            // Elements from second LaceShortest list
            var shortest1 = model.CurrentWorkspace.NodeFromWorkspace<LaceShortest>("ce7bf465-0f93-4e5a-8bc9-9960cd077f25");
            FSharpList<FScheme.Value> listShotestValue1 = shortest1.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(2, listShotestValue1.Length);
            Assert.AreEqual(-4, listShotestValue1[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(-4, listShotestValue1[1].GetDoubleFromFSchemeValue());

        }

        [Test]
        public void LaceShortest_NegativeInput()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\LaceShortest_NegativeInput.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(9, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(12, model.CurrentWorkspace.Connectors.Count);

            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });

        }

        [Test]
        public void LaceShortest_StringInput()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\LaceShortest_StringInput.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(13, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(15, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // Element from the Reverse list
            var reverse = model.CurrentWorkspace.NodeFromWorkspace<ConcatStrings>("1c4c75ff-735d-4431-9df3-2b187c469b3a");
            string actual = reverse.GetValue(0).getStringFromFSchemeValue();
            string expected = "1Design";
            Assert.AreEqual(expected, actual);

            // Elements from first LaceShortest list
            var shortest = model.CurrentWorkspace.NodeFromWorkspace<LaceShortest>("10005d3c-3bbf-4690-b658-37b11c8402b1");
            FSharpList<FScheme.Value> listShotestValue = shortest.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(3, listShotestValue.Length);
            Assert.AreEqual(1, listShotestValue[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(1, listShotestValue[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(1, listShotestValue[2].GetDoubleFromFSchemeValue());

            // Elements from second LaceShortest list
            var shortest1 = model.CurrentWorkspace.NodeFromWorkspace<LaceShortest>("c19f09a1-6132-4c9c-8f37-5f138e1a3067");
            FSharpList<FScheme.Value> listShotestValue1 = shortest1.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(3, listShotestValue1.Length);
            Assert.AreEqual("Dynamo", listShotestValue1[0].getStringFromFSchemeValue());
            Assert.AreEqual("Design", listShotestValue1[1].getStringFromFSchemeValue());
            Assert.AreEqual("Script", listShotestValue1[2].getStringFromFSchemeValue());

        }

        #endregion

        #region LaceLongest test cases

        [Test]
        public void LaceLongest_Simple()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\LaceLongest_Simple.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(8, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(7, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var watch = model.CurrentWorkspace.NodeFromWorkspace<Watch>("5da40769-ffc8-408b-94bb-8c5dff31132e");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild1 = actual[0].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild2 = actual[1].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild3 = actual[2].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild4 = actual[3].GetListFromFSchemeValue();

            Assert.AreEqual(4, actual.Length);

            Assert.AreEqual(1, actualChild1.Length);
            Assert.AreEqual(2, actualChild1[0].GetDoubleFromFSchemeValue());

            Assert.AreEqual(1, actualChild2.Length);
            Assert.AreEqual(8, actualChild2[0].GetDoubleFromFSchemeValue());

            Assert.AreEqual(1, actualChild3.Length);
            Assert.AreEqual(14, actualChild3[0].GetDoubleFromFSchemeValue());

            Assert.AreEqual(1, actualChild4.Length);
            Assert.AreEqual(19, actualChild4[0].GetDoubleFromFSchemeValue());

        }

        [Test]
        public void LaceLongest_Negative()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\LaceLongest_Negative.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(3, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(3, model.CurrentWorkspace.Connectors.Count);

            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });

        }

        [Test]
        public void LaceLongest_ListWith10000Element()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\LaceLongest_ListWith10000Element.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(4, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(3, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var watch = model.CurrentWorkspace.NodeFromWorkspace<LaceLongest>("25daa241-d8a4-4e74-aec1-6068358babf7");
            FSharpList<FScheme.Value> listWatchValue = watch.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(10000, listWatchValue.Length);
            Assert.AreEqual(2001, listWatchValue[1000].GetDoubleFromFSchemeValue());

        }

        #endregion

        #region FilterOut test cases

        [Test]
        public void FilterOut_SimpleTest()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\FilterOut_SimpleTest.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(8, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(8, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // Element from the Number node
            var numberRange = model.CurrentWorkspace.NodeFromWorkspace<DoubleInput>("b6571eb6-b1c2-4874-8749-b783176dc039");
            FSharpList<FScheme.Value> listAllNumbers = numberRange.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(10, listAllNumbers.Length);
            Assert.AreEqual(1, listAllNumbers[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(2, listAllNumbers[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(3, listAllNumbers[2].GetDoubleFromFSchemeValue());

            // Elements from first FilterOut list
            var filterOut = model.CurrentWorkspace.NodeFromWorkspace<FilterOut>("53ec97e2-d860-4fdc-8ea5-2288bf39bcfc");
            FSharpList<FScheme.Value> listFilteredValue = filterOut.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(8, listFilteredValue.Length);
            Assert.AreEqual(3, listFilteredValue[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(4, listFilteredValue[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(10, listFilteredValue[7].GetDoubleFromFSchemeValue());

            // Elements from second FilterOut list
            var filterOut1 = model.CurrentWorkspace.NodeFromWorkspace<FilterOut>("0af3f566-1b05-4578-9fb0-297ca98d6d8c");
            FSharpList<FScheme.Value> listFilteredValue1 = filterOut1.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(3, listFilteredValue1.Length);
            Assert.AreEqual(1, listFilteredValue1[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(2, listFilteredValue1[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(3, listFilteredValue1[2].GetDoubleFromFSchemeValue());

        }

        [Test]
        public void FilterOut_Complex()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\FilterOut_Complex.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(11, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(14, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            // Element from the Number node
            var numberRange = model.CurrentWorkspace.NodeFromWorkspace<DoubleInput>("b6571eb6-b1c2-4874-8749-b783176dc039");
            FSharpList<FScheme.Value> listAllNumbers = numberRange.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(10, listAllNumbers.Length);
            Assert.AreEqual(1, listAllNumbers[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(2, listAllNumbers[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(3, listAllNumbers[2].GetDoubleFromFSchemeValue());

            // Elements from FilterOut list
            var filterOut = model.CurrentWorkspace.NodeFromWorkspace<FilterOut>("53ec97e2-d860-4fdc-8ea5-2288bf39bcfc");
            FSharpList<FScheme.Value> listFilteredValue = filterOut.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(8, listFilteredValue.Length);
            Assert.AreEqual(3, listFilteredValue[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(4, listFilteredValue[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(10, listFilteredValue[7].GetDoubleFromFSchemeValue());

            // Elements from Take from List
            var takeFromList = model.CurrentWorkspace.NodeFromWorkspace<TakeList>("6921b2ef-fc5c-44b4-992f-9421c267d9ef");
            FSharpList<FScheme.Value> takenFromList = takeFromList.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(3, takenFromList.Length);
            Assert.AreEqual(3, takenFromList[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(4, takenFromList[1].GetDoubleFromFSchemeValue());
            Assert.AreEqual(5, takenFromList[2].GetDoubleFromFSchemeValue());

            // Elements from Drop from List 
            var dropFromList = model.CurrentWorkspace.NodeFromWorkspace<DropList>("57a41c41-fa71-41dd-aa25-ca2156f2ba0b");
            FSharpList<FScheme.Value> droppedFromList = dropFromList.GetValue(0).GetListFromFSchemeValue();
            Assert.AreEqual(0, droppedFromList.Length); // As there where only three element in the input list so after droppping the list should be empty.

        }

        [Test]
        public void FilterOut_NegativeTest()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\FilterOut_NegativeTest.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(4, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(3, model.CurrentWorkspace.Connectors.Count);

            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });

        }


        #endregion

        #region NumberRange test cases
        
        [Test]
        public void NumberRange_SimpleTest()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\NumberRange_SimpleTest.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(5, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(4, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var watch = model.CurrentWorkspace.NodeFromWorkspace<NumberRange>("4e781f03-5b48-4d58-a511-8c732665e961");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();

            Assert.AreEqual(51, actual.Length);
            Assert.AreEqual(0, actual[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(50, actual[50].GetDoubleFromFSchemeValue());

        }

        [Test]
        public void NumberRange_LargeNumber()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\NumberRange_LargeNumber.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(5, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(4, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var watch = model.CurrentWorkspace.NodeFromWorkspace<NumberRange>("4e781f03-5b48-4d58-a511-8c732665e961");

            FSharpList<FScheme.Value> actual = watch.GetValue(0).GetListFromFSchemeValue();

            Assert.AreEqual(1000001, actual.Length);

            Assert.AreEqual(500, actual[500].GetDoubleFromFSchemeValue());
            Assert.AreEqual(1000000, actual[1000000].GetDoubleFromFSchemeValue());

        }

        [Test]
        public void NumberRange_LacingShortest()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\NumberRange_LacingShortest.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(6, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(5, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var numberRange = model.CurrentWorkspace.NodeFromWorkspace<NumberRange>("4e781f03-5b48-4d58-a511-8c732665e961");

            FSharpList<FScheme.Value> actual = numberRange.GetValue(0).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild1 = actual[0].GetListFromFSchemeValue();

            Assert.AreEqual(1, actual.Length);

            Assert.AreEqual(10, actualChild1.Length);
            Assert.AreEqual(1, actualChild1[0].GetDoubleFromFSchemeValue());
            Assert.AreEqual(10, actualChild1[9].GetDoubleFromFSchemeValue());

        }

        [Test]
        public void NumberRange_LacingLongest()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\NumberRange_LacingLongest.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(6, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(5, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var numberRange = model.CurrentWorkspace.NodeFromWorkspace<NumberRange>("4e781f03-5b48-4d58-a511-8c732665e961");

            FSharpList<FScheme.Value> actual = numberRange.GetValue(0).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild1 = actual[0].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild2 = actual[1].GetListFromFSchemeValue();

            Assert.AreEqual(2, actual.Length);

            Assert.AreEqual(10, actualChild1.Length);
            Assert.AreEqual(10, actualChild1[9].GetDoubleFromFSchemeValue());

            Assert.AreEqual(5, actualChild2.Length);
            Assert.AreEqual(10, actualChild2[4].GetDoubleFromFSchemeValue());

        }

        [Test]
        public void NumberRange_LacingCrossProduct()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\NumberRange_LacingCrossProduct.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(6, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(5, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var numberRange = model.CurrentWorkspace.NodeFromWorkspace<NumberRange>("4e781f03-5b48-4d58-a511-8c732665e961");

            FSharpList<FScheme.Value> actual = numberRange.GetValue(0).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild1 = actual[0].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild2 = actual[1].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild3 = actual[2].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> actualChild4 = actual[3].GetListFromFSchemeValue();

            Assert.AreEqual(4, actual.Length);

            Assert.AreEqual(10, actualChild1.Length);
            Assert.AreEqual(10, actualChild1[9].GetDoubleFromFSchemeValue());

            Assert.AreEqual(5, actualChild2.Length);
            Assert.AreEqual(9, actualChild2[4].GetDoubleFromFSchemeValue());

            Assert.AreEqual(9, actualChild3.Length);
            Assert.AreEqual(10, actualChild3[8].GetDoubleFromFSchemeValue());

            Assert.AreEqual(5, actualChild4.Length);
            Assert.AreEqual(10, actualChild4[4].GetDoubleFromFSchemeValue());

        }
        
        #endregion

        #region ListMinimum test cases

        [Test]
        public void ListMinimum_NumberRange()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\ListMinimum_NumberRange.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(8, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(7, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var listMin = model.CurrentWorkspace.NodeFromWorkspace<ListMin>("aa8b8f1e-e8c4-4ced-bbb2-8ee43d7bb4f6");

            Assert.AreEqual(-1, listMin.GetValue(0).GetDoubleFromFSchemeValue());

        }

        [Test]
        public void ListMinimum_Complex()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\ListMinimum_Complex.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(11, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(11, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var listMin = model.CurrentWorkspace.NodeFromWorkspace<ListMin>("aa8b8f1e-e8c4-4ced-bbb2-8ee43d7bb4f6");

            Assert.AreEqual(5, listMin.GetValue(0).GetDoubleFromFSchemeValue());

        }

        #endregion

        #region AddToList test cases

        [Test]
        public void AddToList_SimpleTest()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\AddToList_SimpleTest.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(8, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(8, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var addToList = model.CurrentWorkspace.NodeFromWorkspace<Dynamo.Nodes.List>("31d0eb4e-8657-4eb1-a852-5e9b766eddd7");

            FSharpList<FScheme.Value> actual = addToList.GetValue(0).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> childList = actual[2].GetListFromFSchemeValue();

            Assert.AreEqual(6, actual.Length);
            Assert.AreEqual("Design", actual[0].getStringFromFSchemeValue());
            Assert.AreEqual(10, actual[5].GetDoubleFromFSchemeValue());

            Assert.AreEqual(4, childList.Length);
            Assert.AreEqual(-10, childList[0].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void AddToList_EmptyList()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\AddToList_EmptyList.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(6, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(5, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var addToList = model.CurrentWorkspace.NodeFromWorkspace<Dynamo.Nodes.List>("1976caa7-d45e-4a44-9faf-345d98337bbb");

            FSharpList<FScheme.Value> actual = addToList.GetValue(0).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> childList = actual[0].GetListFromFSchemeValue();

            Assert.AreEqual(1, actual.Length);

            Assert.AreEqual(2, childList.Length);
            Assert.IsEmpty(childList[0].getStringFromFSchemeValue());
            Assert.AreEqual(0, childList[1].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void AddToList_Complex()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\AddToList_Complex.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(11, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(11, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var addToList = model.CurrentWorkspace.NodeFromWorkspace<Dynamo.Nodes.List>("cfdfc020-05d0-4442-96df-8d97aad9c38c");

            FSharpList<FScheme.Value> actual = addToList.GetValue(0).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> childList1 = actual[0].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> childList2 = actual[1].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> childList3 = actual[2].GetListFromFSchemeValue();

            Assert.AreEqual(3, actual.Length);

            Assert.AreEqual(1, childList1.Length);
            Assert.AreEqual(3, childList1[0].GetDoubleFromFSchemeValue());

            Assert.AreEqual(1, childList2.Length);
            Assert.AreEqual(6, childList2[0].GetDoubleFromFSchemeValue());

            Assert.AreEqual(1, childList3.Length);
            Assert.AreEqual(9, childList3[0].GetDoubleFromFSchemeValue());

        }

        [Test]
        public void AddToList_GeometryToList()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\AddToList_GeometryToList.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(9, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(11, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var addToList = model.CurrentWorkspace.NodeFromWorkspace<Dynamo.Nodes.List>("31d0eb4e-8657-4eb1-a852-5e9b766eddd7");

            FSharpList<FScheme.Value> actual = addToList.GetValue(0).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> childList1 = actual[2].GetListFromFSchemeValue();

            Assert.AreEqual(6, actual.Length);

            Assert.AreEqual(4, childList1.Length);
            Assert.AreEqual(-10, childList1[0].GetDoubleFromFSchemeValue());

        }

        [Test]
        public void AddToList_Negative()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\AddToList_Negative.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(6, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(5, model.CurrentWorkspace.Connectors.Count);

            Assert.Throws<AssertionException>(() =>
            {
                dynSettings.Controller.RunExpression(null);
            });

        }

        #endregion

        #region SplitList test cases

        [Test]
        public void SplitList_SimpleTest()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\SplitList_SimpleTest.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(6, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(5, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var splitList = model.CurrentWorkspace.NodeFromWorkspace<Dynamo.Nodes.DeCons>("8226a43b-fd5e-45f6-a5f7-32815c12084a");

            Assert.AreEqual("Dynamo", splitList.GetValue(0).getStringFromFSchemeValue());

            FSharpList<FScheme.Value> secondOutput = splitList.GetValue(1).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> childList = secondOutput[0].GetListFromFSchemeValue();

            Assert.AreEqual(1, secondOutput.Length);

            Assert.AreEqual(2, childList.Length);
            Assert.AreEqual(0, childList[0].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void SplitList_FirstElementAsList()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\SplitList_FirstElementAsList.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(6, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(5, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var splitList = model.CurrentWorkspace.NodeFromWorkspace<Dynamo.Nodes.DeCons>("8226a43b-fd5e-45f6-a5f7-32815c12084a");

            FSharpList<FScheme.Value> firstOutput = splitList.GetValue(0).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> secondOutput = splitList.GetValue(1).GetListFromFSchemeValue();

            Assert.AreEqual(2, firstOutput.Length);
            Assert.AreEqual(0, firstOutput[0].GetDoubleFromFSchemeValue());

            Assert.AreEqual(1, secondOutput.Length);
            Assert.AreEqual("Dynamo", secondOutput[0].getStringFromFSchemeValue());

        }

        [Test]
        public void SplitList_Complex()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\SplitList_Complex.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(9, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(8, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var splitList = model.CurrentWorkspace.NodeFromWorkspace<Dynamo.Nodes.DeCons>("8226a43b-fd5e-45f6-a5f7-32815c12084a");

            FSharpList<FScheme.Value> firstOutput = splitList.GetValue(0).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> secondOutput = splitList.GetValue(1).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> child = secondOutput[0].GetListFromFSchemeValue();

            Assert.AreEqual(1, firstOutput.Length);
            Assert.AreEqual(3, firstOutput[0].GetDoubleFromFSchemeValue());

            Assert.AreEqual(2, secondOutput.Length);

            Assert.AreEqual(1, child.Length);
            Assert.AreEqual(6, child[0].GetDoubleFromFSchemeValue());

        }

        [Test]
        public void SplitList_ComplexAnotherExample()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\SplitList_ComplexAnotherExample.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(17, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(18, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var splitList = model.CurrentWorkspace.NodeFromWorkspace<Dynamo.Nodes.DeCons>("66e94123-deaf-4bc8-8c5f-b3bc0996a57e");

            FSharpList<FScheme.Value> firstOutput = splitList.GetValue(0).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> secondOutput = splitList.GetValue(1).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> child = secondOutput[0].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> child1 = secondOutput[1].GetListFromFSchemeValue();

            Assert.AreEqual(12, firstOutput.Length);
            Assert.AreEqual("x", firstOutput[0].getStringFromFSchemeValue());
            Assert.AreEqual("z", firstOutput[11].getStringFromFSchemeValue());

            Assert.AreEqual(2, secondOutput.Length);

            Assert.AreEqual(12, child.Length);
            Assert.AreEqual(19.35, child[0].GetDoubleFromFSchemeValue());

            Assert.AreEqual(12, child1.Length);
            Assert.AreEqual(32.85, child1[0].GetDoubleFromFSchemeValue());

        }
        #endregion

        #region TakeFromList test cases
        
        [Test]
        public void TakeFromList_SimpleTest()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TakeFromList_SimpleTest.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(9, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(8, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var takeFromList = model.CurrentWorkspace.NodeFromWorkspace<Dynamo.Nodes.TakeList>("14cb6593-24d8-4ffc-8ee5-9f4247449fc2");

            FSharpList<FScheme.Value> firstOutput = takeFromList.GetValue(0).GetListFromFSchemeValue();
            FSharpList<FScheme.Value> child = firstOutput[0].GetListFromFSchemeValue();
            FSharpList<FScheme.Value> child1 = firstOutput[4].GetListFromFSchemeValue();

            Assert.AreEqual(5, firstOutput.Length);

            Assert.AreEqual(1, child.Length);
            Assert.AreEqual(3, child[0].GetDoubleFromFSchemeValue());

            Assert.AreEqual(1, child1.Length);
            Assert.AreEqual(15, child1[0].GetDoubleFromFSchemeValue());

        }

        [Test]
        public void TakeFromList_WithStringList()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TakeFromList_WithStringList.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(8, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(7, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var takeFromList = model.CurrentWorkspace.NodeFromWorkspace<Dynamo.Nodes.TakeList>("14cb6593-24d8-4ffc-8ee5-9f4247449fc2");

            FSharpList<FScheme.Value> firstOutput = takeFromList.GetValue(0).GetListFromFSchemeValue();

            Assert.AreEqual(4, firstOutput.Length);

            Assert.AreEqual("Test", firstOutput[0].getStringFromFSchemeValue());
            Assert.AreEqual("Take", firstOutput[1].getStringFromFSchemeValue());
            Assert.AreEqual("From", firstOutput[2].getStringFromFSchemeValue());
            Assert.AreEqual("List", firstOutput[3].getStringFromFSchemeValue());

        }

        [Test]
        public void TakeFromList_NegativeIntValue()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TakeFromList_NegativeIntValue.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(8, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(7, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            dynSettings.Controller.RunExpression(null);

            var takeFromList = model.CurrentWorkspace.NodeFromWorkspace<Dynamo.Nodes.TakeList>("14cb6593-24d8-4ffc-8ee5-9f4247449fc2");

            FSharpList<FScheme.Value> firstOutput = takeFromList.GetValue(0).GetListFromFSchemeValue();

            Assert.AreEqual(1, firstOutput.Length);

            Assert.AreEqual("List", firstOutput[0].getStringFromFSchemeValue());

        }

        [Test]
        public void TakeFromList_InputEmptyList()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TakeFromList_InputEmptyList.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(4, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(3, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            Assert.Throws<AssertionException>(() =>
                {
                    dynSettings.Controller.RunExpression(null);
                });

        }

        [Test]
        public void TakeFromList_AmtAsRangeExpn()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\TakeFromList_AmtAsRangeExpn.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(9, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(8, model.CurrentWorkspace.Connectors.Count);

            // run the expression
            Assert.Throws<AssertionException>(() =>
                {
                    dynSettings.Controller.RunExpression(null);
                });
        }

        #endregion

        #region DropFromList test cases
        [Test]

        public void DropFromList_SimpleTest()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\DropFromList_SimpleTest.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(7, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(6, model.CurrentWorkspace.Connectors.Count);
            
            // run expression
            dynSettings.Controller.RunExpression(null);

            var dropFromList = model.CurrentWorkspace.NodeFromWorkspace<Dynamo.Nodes.DropList>("e2d27010-b8fc-4eb8-8703-63bab5ce6e85");
            FSharpList<FScheme.Value> output = dropFromList.GetValue(0).GetListFromFSchemeValue();

            var dropFromList1 = model.CurrentWorkspace.NodeFromWorkspace<DropList>("097e0b4b-4cbb-43b1-a21c-77a619ad1050");
            FSharpList<FScheme.Value> secondOutput = dropFromList1.GetValue(0).GetListFromFSchemeValue();

            Assert.AreEqual(6, output.Length);
            Assert.AreEqual(0, output[0].GetDoubleFromFSchemeValue());

            Assert.AreEqual(6, secondOutput.Length);
            Assert.AreEqual(5, secondOutput[0].GetDoubleFromFSchemeValue());
        }

        [Test]
        public void DropFromList_InputEmptyList()
        {

            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\list\DropFromList_InputEmptyList.dyn");
            model.Open(openPath);

            // check all the nodes and connectors are loaded
            Assert.AreEqual(3, model.CurrentWorkspace.Nodes.Count);
            Assert.AreEqual(2, model.CurrentWorkspace.Connectors.Count);

            // run expression
            Assert.Throws<AssertionException>(() =>
                {
                    dynSettings.Controller.RunExpression(null);
                });
        }

        #endregion
    }
}
