// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class Issue1734
{
    [Fact]
    public void Test_XmlNode()
    {
        var xmlDoc = JsonXmlConvert.DeserializeXmlNode(JsonWithoutNamespace, "", true);

        XUnitAssert.AreEqualNormalized(
            """
            <Test_Service>
              <fname>mark</fname>
              <lname>joye</lname>
              <CarCompany>saab</CarCompany>
              <CarNumber>9741</CarNumber>
              <IsInsured>true</IsInsured>
              <safty>ABS</safty>
              <safty>AirBags</safty>
              <safty>childdoorlock</safty>
              <CarDescription>test Car</CarDescription>
              <collections json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                <XYZ>1</XYZ>
                <PQR>11</PQR>
                <contactdetails>
                  <contname>DOM</contname>
                  <contnumber>8787</contnumber>
                </contactdetails>
                <contactdetails>
                  <contname>COM</contname>
                  <contnumber>4564</contnumber>
                  <addtionaldetails json:Array="true">
                    <description>54657667</description>
                  </addtionaldetails>
                </contactdetails>
                <contactdetails>
                  <contname>gf</contname>
                  <contnumber>123</contnumber>
                  <addtionaldetails json:Array="true">
                    <description>123</description>
                  </addtionaldetails>
                </contactdetails>
              </collections>
            </Test_Service>
            """, IndentXml(xmlDoc.OuterXml));

        xmlDoc = JsonXmlConvert.DeserializeXmlNode(JsonWithNamespace, "", true);

        XUnitAssert.AreEqualNormalized(
            """
            <ns3:Test_Service xmlns:ns3="http://www.CCKS.org/XRT/Form">
              <ns3:fname>mark</ns3:fname>
              <ns3:lname>joye</ns3:lname>
              <ns3:CarCompany>saab</ns3:CarCompany>
              <ns3:CarNumber>9741</ns3:CarNumber>
              <ns3:IsInsured>true</ns3:IsInsured>
              <ns3:safty>ABS</ns3:safty>
              <ns3:safty>AirBags</ns3:safty>
              <ns3:safty>childdoorlock</ns3:safty>
              <ns3:CarDescription>test Car</ns3:CarDescription>
              <ns3:collections json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                <ns3:XYZ>1</ns3:XYZ>
                <ns3:PQR>11</ns3:PQR>
                <ns3:contactdetails>
                  <ns3:contname>DOM</ns3:contname>
                  <ns3:contnumber>8787</ns3:contnumber>
                </ns3:contactdetails>
                <ns3:contactdetails>
                  <ns3:contname>COM</ns3:contname>
                  <ns3:contnumber>4564</ns3:contnumber>
                  <ns3:addtionaldetails json:Array="true">
                    <ns3:description>54657667</ns3:description>
                  </ns3:addtionaldetails>
                </ns3:contactdetails>
                <ns3:contactdetails>
                  <ns3:contname>gf</ns3:contname>
                  <ns3:contnumber>123</ns3:contnumber>
                  <ns3:addtionaldetails json:Array="true">
                    <ns3:description>123</ns3:description>
                  </ns3:addtionaldetails>
                </ns3:contactdetails>
              </ns3:collections>
            </ns3:Test_Service>
            """, IndentXml(xmlDoc.OuterXml));
    }

    static string IndentXml(string xml)
    {
        var reader = XmlReader.Create(new StringReader(xml));

        var stringWriter = new StringWriter();
        var writer = XmlWriter.Create(stringWriter, new() {Indent = true, OmitXmlDeclaration = true});

        while (reader.Read())
        {
            writer.WriteNode(reader, false);
        }

        writer.Flush();

        return stringWriter.ToString();
    }

    [Fact]
    public void Test_XNode()
    {
        var xmlDoc = JsonXmlConvert.DeserializeXNode(JsonWithoutNamespace, "", true);

        var xml = xmlDoc.ToString();
        XUnitAssert.AreEqualNormalized(
            """
            <Test_Service>
              <fname>mark</fname>
              <lname>joye</lname>
              <CarCompany>saab</CarCompany>
              <CarNumber>9741</CarNumber>
              <IsInsured>true</IsInsured>
              <safty>ABS</safty>
              <safty>AirBags</safty>
              <safty>childdoorlock</safty>
              <CarDescription>test Car</CarDescription>
              <collections json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                <XYZ>1</XYZ>
                <PQR>11</PQR>
                <contactdetails>
                  <contname>DOM</contname>
                  <contnumber>8787</contnumber>
                </contactdetails>
                <contactdetails>
                  <contname>COM</contname>
                  <contnumber>4564</contnumber>
                  <addtionaldetails json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                    <description>54657667</description>
                  </addtionaldetails>
                </contactdetails>
                <contactdetails>
                  <contname>gf</contname>
                  <contnumber>123</contnumber>
                  <addtionaldetails json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                    <description>123</description>
                  </addtionaldetails>
                </contactdetails>
              </collections>
            </Test_Service>
            """, xml);

        xmlDoc = JsonXmlConvert.DeserializeXNode(JsonWithNamespace, "", true);

        xml = xmlDoc.ToString();
        XUnitAssert.AreEqualNormalized(
            """
            <ns3:Test_Service xmlns:ns3="http://www.CCKS.org/XRT/Form">
              <ns3:fname>mark</ns3:fname>
              <ns3:lname>joye</ns3:lname>
              <ns3:CarCompany>saab</ns3:CarCompany>
              <ns3:CarNumber>9741</ns3:CarNumber>
              <ns3:IsInsured>true</ns3:IsInsured>
              <ns3:safty>ABS</ns3:safty>
              <ns3:safty>AirBags</ns3:safty>
              <ns3:safty>childdoorlock</ns3:safty>
              <ns3:CarDescription>test Car</ns3:CarDescription>
              <ns3:collections json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                <ns3:XYZ>1</ns3:XYZ>
                <ns3:PQR>11</ns3:PQR>
                <ns3:contactdetails>
                  <ns3:contname>DOM</ns3:contname>
                  <ns3:contnumber>8787</ns3:contnumber>
                </ns3:contactdetails>
                <ns3:contactdetails>
                  <ns3:contname>COM</ns3:contname>
                  <ns3:contnumber>4564</ns3:contnumber>
                  <ns3:addtionaldetails json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                    <ns3:description>54657667</ns3:description>
                  </ns3:addtionaldetails>
                </ns3:contactdetails>
                <ns3:contactdetails>
                  <ns3:contname>gf</ns3:contname>
                  <ns3:contnumber>123</ns3:contnumber>
                  <ns3:addtionaldetails json:Array="true" xmlns:json="http://james.newtonking.com/projects/json">
                    <ns3:description>123</ns3:description>
                  </ns3:addtionaldetails>
                </ns3:contactdetails>
              </ns3:collections>
            </ns3:Test_Service>
            """,
            xml);
    }

    const string JsonWithoutNamespace = """
        {
          "Test_Service": {
            "fname": "mark",
            "lname": "joye",
            "CarCompany": "saab",
            "CarNumber": "9741",
            "IsInsured": "true",
            "safty": [
              "ABS",
              "AirBags",
              "childdoorlock"
            ],
            "CarDescription": "test Car",
            "collections": [
              {
                "XYZ": "1",
                "PQR": "11",
                "contactdetails": [
                  {
                    "contname": "DOM",
                    "contnumber": "8787"
                  },
                  {
                    "contname": "COM",
                    "contnumber": "4564",
                    "addtionaldetails": [
                      {
                        "description": "54657667"
                      }
                    ]
                  },
                  {
                    "contname": "gf",
                    "contnumber": "123",
                    "addtionaldetails": [
                      {
                        "description": "123"
                      }
                    ]
                  }
                ]
              }
            ]
          }
        }
        """;

    const string JsonWithNamespace = """
        {
          "ns3:Test_Service": {
            "@xmlns:ns3": "http://www.CCKS.org/XRT/Form",
            "ns3:fname": "mark",
            "ns3:lname": "joye",
            "ns3:CarCompany": "saab",
            "ns3:CarNumber": "9741",
            "ns3:IsInsured": "true",
            "ns3:safty": [
              "ABS",
              "AirBags",
              "childdoorlock"
            ],
            "ns3:CarDescription": "test Car",
            "ns3:collections": [
              {
                "ns3:XYZ": "1",
                "ns3:PQR": "11",
                "ns3:contactdetails": [
                  {
                    "ns3:contname": "DOM",
                    "ns3:contnumber": "8787"
                  },
                  {
                    "ns3:contname": "COM",
                    "ns3:contnumber": "4564",
                    "ns3:addtionaldetails": [
                      {
                        "ns3:description": "54657667"
                      }
                    ]
                  },
                  {
                    "ns3:contname": "gf",
                    "ns3:contnumber": "123",
                    "ns3:addtionaldetails": [
                      {
                        "ns3:description": "123"
                      }
                    ]
                  }
                ]
              }
            ]
          }
        }
        """;
}