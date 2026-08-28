// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class PreserveReferencesHandlingObject : TestFixtureBase
{
    #region PreserveReferencesHandlingObjectTypes

    public class Directory
    {
        public string Name { get; set; }
        public Directory Parent { get; set; }
        public IList<File> Files { get; set; }
    }

    public class File
    {
        public string Name { get; set; }
        public Directory Parent { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region PreserveReferencesHandlingObjectUsage

        var root = new Directory {Name = "Root"};
        var documents = new Directory {Name = "My Documents", Parent = root};

        var file = new File
        {
            Name = "ImportantLegalDocument.docx",
            Parent = documents
        };

        documents.Files = [file];

        try
        {
            JsonConvert.SerializeObject(documents, Formatting.Indented);
        }
        catch (JsonSerializationException)
        {
            // Self referencing loop detected for property 'Parent' with type
            // 'Argon.Tests.Documentation.Examples.ReferenceLoopHandlingObject+Directory'. Path 'Files[0]'.
        }

        var preserveReferencesAll = JsonConvert.SerializeObject(documents, Formatting.Indented, new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All
        });

        Console.WriteLine(preserveReferencesAll);
        // {
        //   "$id": "1",
        //   "Name": "My Documents",
        //   "Parent": {
        //     "$id": "2",
        //     "Name": "Root",
        //     "Parent": null,
        //     "Files": null
        //   },
        //   "Files": {
        //     "$id": "3",
        //     "$values": [
        //       {
        //         "$id": "4",
        //         "Name": "ImportantLegalDocument.docx",
        //         "Parent": {
        //           "$ref": "1"
        //         }
        //       }
        //     ]
        //   }
        // }

        var preserveReferenacesObjects = JsonConvert.SerializeObject(documents, Formatting.Indented, new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        });

        Console.WriteLine(preserveReferenacesObjects);
        // {
        //   "$id": "1",
        //   "Name": "My Documents",
        //   "Parent": {
        //     "$id": "2",
        //     "Name": "Root",
        //     "Parent": null,
        //     "Files": null
        //   },
        //   "Files": [
        //     {
        //       "$id": "3",
        //       "Name": "ImportantLegalDocument.docx",
        //       "Parent": {
        //         "$ref": "1"
        //       }
        //     }
        //   ]
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "$id": "1",
              "Name": "My Documents",
              "Parent": {
                "$id": "2",
                "Name": "Root",
                "Parent": null,
                "Files": null
              },
              "Files": [
                {
                  "$id": "3",
                  "Name": "ImportantLegalDocument.docx",
                  "Parent": {
                    "$ref": "1"
                  }
                }
              ]
            }
            """,
            preserveReferenacesObjects);
    }
}