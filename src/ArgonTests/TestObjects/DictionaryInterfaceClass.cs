// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace TestObjects;

public class DictionaryInterfaceClass
{
    public string Name { get; set; }

    public IDictionary<string, int> Dictionary { get; set; } =
        new Dictionary<string, int>
        {
            {
                "existing", 1
            }
        };

    public ICollection<int> Collection { get; set; } =
        [
            1,
            2,
            3
        ];

    public EmployeeReference Employee { get; set; } =
        new()
        {
            Name = "EmployeeName!"
        };

    public object Random { get; set; }
}