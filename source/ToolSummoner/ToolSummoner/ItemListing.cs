using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ToolSummoner
{
    public struct ItemListing
    {
        // Will have the following properties:
        // - Unique ID (For logging and spreadsheet purposes)
        // - Image
        // - Name
        // - Description
        // - Stock Remaining

        public int uniqueID;
        public string imageFilePath;
        public string name;
        public string description;
        public int stockRemaining;
        public ItemLocation location;

        public ItemListing(int uniqueID, string imageFilePath, string name, string description, int stockRemaining, ItemLocation location)
        {
            this.uniqueID = uniqueID;
            this.imageFilePath = imageFilePath;
            this.name = name;
            this.description = description;
            this.stockRemaining = stockRemaining;
            this.location = location;
        }

        public bool HasValidImage()
        {
            return !string.IsNullOrEmpty(imageFilePath) && File.Exists(imageFilePath); // Just check if the file exists
        }

        public bool IsInStock()
        {
            return stockRemaining > 0;
        }
    }
}
