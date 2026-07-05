module NullTokenTests

open Argon
open Xunit

type ListHolder() =
    member val List: int list = [] with get, set
    member val After: int = 0 with get, set

type MapHolder() =
    member val Map: Map<string, int> = Map.empty with get, set
    member val After: int = 0 with get, set

// Bug: FSharpListConverter.ReadJson had no Null handling, so a JSON null crashed and
// desynced the reader instead of yielding a null list.
[<Fact>]
let ``Deserialize null into FSharp list property`` () =
    let result =
        JsonConvert.DeserializeObject<ListHolder>("""{"List": null, "After": 7}""", FSharpListConverter())

    Assert.Null(box result.List)
    Assert.Equal(7, result.After)

// Bug: FSharpMapConverter.ReadJson had the same missing Null check.
[<Fact>]
let ``Deserialize null into FSharp map property`` () =
    let result =
        JsonConvert.DeserializeObject<MapHolder>("""{"Map": null, "After": 7}""", FSharpMapConverter())

    Assert.Null(box result.Map)
    Assert.Equal(7, result.After)
