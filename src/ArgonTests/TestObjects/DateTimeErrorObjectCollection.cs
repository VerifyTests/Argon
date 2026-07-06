// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace TestObjects;

public class DateTimeErrorObjectCollection :
    Collection<DateTime>,
    IJsonOnDeserializeError
{
    public void OnDeserializeError(object originalObject, string path, object member, Exception exception, Action markAsHandled) =>
        markAsHandled();
}