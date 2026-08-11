# Dates in JSON

In the [JSON spec](http://www.ietf.org/rfc/rfc4627.txt) there is no literal syntax for dates in JSON. The spec has objects, arrays, strings, integers, and floats, but it defines no standard for what a date looks like.


## Dates and Json.NET

The default format used by Json.NET is the [ISO 8601 standard](http://en.wikipedia.org/wiki/ISO_8601)


## DateOnly and TimeOnly

`DateOnly` and `TimeOnly` are supported out of the box on net6.0 and above. No converter is required.

Both are written as, and parsed from, JSON strings using a fixed invariant-culture format:

| Type       | Format                 | Example              |
|------------|------------------------|----------------------|
| `DateOnly` | `yyyy'-'MM'-'dd`       | `"2000-12-29"`       |
| `TimeOnly` | `HH':'mm':'ss.FFFFFFF` | `"13:45:30.1234567"` |

```c#
var json = JsonConvert.SerializeObject(new DateOnly(2000, 12, 29));
// "2000-12-29"

var date = JsonConvert.DeserializeObject<DateOnly>("\"2000-12-29\"");
```

Nullable (`DateOnly?` / `TimeOnly?`) and collection members behave the same way, with `null` written as `null`.

Note that these formats are fixed. `DateFormatHandling`, `DateFormatString` and `DateTimeZoneHandling` apply to `DateTime` and `DateTimeOffset` only, and have no effect on `DateOnly` or `TimeOnly`. To use a different format, add a custom `JsonConverter`.

On net462, net472 and net48 the types do not exist, so this support is not compiled in.


## Related Topics

 * `Argon.DateFormatHandling`
 * `Argon.DateTimeZoneHandling`
 * `Argon.JavaScriptDateTimeConverter`
 * `Argon.IsoDateTimeConverter`
