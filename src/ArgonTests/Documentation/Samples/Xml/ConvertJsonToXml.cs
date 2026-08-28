// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class ConvertJsonToXml : TestFixtureBase
{
    [Fact]
    public void Example()
    {
        #region ConvertJsonToXml

        var json = """
            {
              '@Id': 1,
              'Email': 'james@example.com',
              'Active': true,
              'CreatedDate': '2013-01-20T00:00:00Z',
              'Roles': [
                'User',
                'Admin'
              ],
              'Team': {
                '@Id': 2,
                'Name': 'Software Developers',
                'Description': 'Creators of fine software products and services.'
              }
            }
            """;

        XNode node = JsonXmlConvert.DeserializeXNode(json, "Root");

        Console.WriteLine(node.ToString());
        // <Root Id="1">
        //   <Email>james@example.com</Email>
        //   <Active>true</Active>
        //   <CreatedDate>2013-01-20T00:00:00Z</CreatedDate>
        //   <Roles>User</Roles>
        //   <Roles>Admin</Roles>
        //   <Team Id="2">
        //     <Name>Software Developers</Name>
        //     <Description>Creators of fine software products and services.</Description>
        //   </Team>
        // </Root>

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            <Root Id="1">
              <Email>james@example.com</Email>
              <Active>true</Active>
              <CreatedDate>2013-01-20T00:00:00Z</CreatedDate>
              <Roles>User</Roles>
              <Roles>Admin</Roles>
              <Team Id="2">
                <Name>Software Developers</Name>
                <Description>Creators of fine software products and services.</Description>
              </Team>
            </Root>
            """,
            node.ToString());
    }
}