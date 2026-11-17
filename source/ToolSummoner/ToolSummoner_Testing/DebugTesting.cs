using ToolSummoner;

namespace ToolSummoner_Testing
{
    [TestClass]
    public sealed class DebugTesting
    {
        [TestMethod]
        /// <summary>
        /// 
        /// </summary>
        public void TestGetRobotArmPort()
        {
            // todo: Make some kind of test cases here.
            SoftwareController software = new SoftwareController();

            software.GetRobotArmPort();
        }


        [TestMethod]
        /// <summary>
        /// 
        /// </summary>
        public void TestItemListingsCanBeSavedAndLoaded()
        {
            string filePath = @"..\..\..\..\..\Assets\TestItems.xml";
            List<ItemListing> expected = new()
            {
                new ItemListing(
                    1,
                    "Assets/Tools/Bolt.png",
                    "Bolt",
                    "Steel bolt for fastening components together.",
                    25,
                    new ItemLocation(1, 1)),
                new ItemListing(
                    2,
                    "Assets/Tools/Box_cutter.png",
                    "Box Cutter",
                    "Retractable utility knife for cutting cardboard and packaging.",
                    10,
                    new ItemLocation(2, 1)),
                new ItemListing(
                    3,
                    "Assets/Tools/Brush.png",
                    "Brush",
                    "General-purpose brush for cleaning and applying materials.",
                    12,
                    new ItemLocation(3, 1)),
                new ItemListing(
                    4,
                    "Assets/Tools/Hammer.png",
                    "Hammer",
                    "Hand tool for driving nails and light demolition work.",
                    8,
                    new ItemLocation(1, 2)),
                new ItemListing(
                    5,
                    "Assets/Tools/Nut.png",
                    "Nut",
                    "Threaded fastener designed to secure a matching bolt.",
                    25,
                    new ItemLocation(2, 2)),
                new ItemListing(
                    6,
                    "Assets/Tools/Screwdriver.png",
                    "Screwdriver",
                    "Hand tool for driving and removing screws.",
                    9,
                    new ItemLocation(3, 2)),
                new ItemListing(
                    7,
                    "Assets/Tools/Wrench.png",
                    "Wrench",
                    "Hand tool for tightening and loosening nuts and bolts.",
                    6,
                    new ItemLocation(1, 3))
            };

            try
            {
                SoftwareController writer = new SoftwareController();
                writer.SaveItemsToFile(filePath, expected);

                SoftwareController reader = new SoftwareController();
                SoftwareController.ParseErrorCode error = reader.GetItemsFromFiles(filePath);

                Assert.AreEqual(SoftwareController.ParseErrorCode.None, error);
                Assert.AreEqual(7, reader.Items.Count);
                Assert.AreEqual(expected[0].uniqueID, reader.Items[0].uniqueID);
                Assert.AreEqual(expected[0].imageFilePath, reader.Items[0].imageFilePath);
                Assert.AreEqual(expected[0].name, reader.Items[0].name);
                Assert.AreEqual(expected[0].description, reader.Items[0].description);
                Assert.AreEqual(expected[0].stockRemaining, reader.Items[0].stockRemaining);
                Assert.AreEqual(expected[0].location.xCoordinate, reader.Items[0].location.xCoordinate);
                Assert.AreEqual(expected[0].location.yCoordinate, reader.Items[0].location.yCoordinate);

                reader.Items[0] = new ItemListing(
                    99,
                    "images/updated-drill.png",
                    "Updated drill",
                    "Updated description",
                    3,
                    new ItemLocation(1.5, 2.75));
                reader.SaveItemsToFile(filePath);

                SoftwareController updatedReader = new SoftwareController();
                Assert.AreEqual(
                    SoftwareController.ParseErrorCode.None,
                    updatedReader.GetItemsFromFiles(filePath));
                Assert.AreEqual(99, updatedReader.Items[0].uniqueID);
                Assert.AreEqual("Updated drill", updatedReader.Items[0].name);
                Assert.AreEqual(3, updatedReader.Items[0].stockRemaining);
                Assert.AreEqual(1.5, updatedReader.Items[0].location.xCoordinate);
                Assert.AreEqual(2.75, updatedReader.Items[0].location.yCoordinate);
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }
    }
}
