﻿/// The MIT License (MIT)
/// Copyright (c) 2016 Bazinga Technologies Inc

module FSharp.Data.GraphQL.Tests.ExecutionTests

open System
open Xunit
open FsCheck
open FSharp.Data.GraphQL
open FSharp.Data.GraphQL.Types
open FSharp.Data.GraphQL.Parser
open FSharp.Data.GraphQL.Execution

let sync = Async.RunSynchronously
let field name typedef (resolve: 'a -> 'b) = Schema.Field(name = name, schema = typedef, resolve = resolve)
let arg name typedef = Schema.Argument(name, typedef)
let objdef name fields = Schema.ObjectType(name, fields)

type TestSubject = {
    a: string
    b: string
    c: string
    d: string
    e: string
    f: string
    deep: DeepTestSubject
    pic: int option -> string
    promise: Async<TestSubject option>
}
and DeepTestSubject = {
    a: string
    b: string
    c: string list
}


[<Fact>]
let ``Execution executes arbitrary code`` () =
    let rec data = 
        {
            a = "Apple"
            b = "Banana"
            c = "Cookie"
            d = "Donut"
            e = "Egg"
            f = "Fish"
            pic = (fun size -> "Pic of size: " + (if size.IsSome then size.Value else 50).ToString())
            promise = (async { return Some data })
            deep = 
                {
                    a = "Already Been Done"
                    b = "Boring"
                    c = ["Contrived"; null; "Confusing"]
                }
        }

    let ast = parse """query Example($size: Int) {
          a,
          b,
          x: c
          ...c
          f
          ...on DataType {
            pic(size: $size)
            promise {
              a
            }
          }
          deep {
            a
            b
            c
            deeper {
              a
              b
            }
          }
        }

        fragment c on DataType {
          d
          e
        }"""

    let (expected: Map<string, obj>) = 
        Map.ofList [
          "a", upcast "Apple"
          "b", upcast "Banana"
          "x", upcast "Cookie"
          "d", upcast "Donut"
          "e", upcast "Egg"
          "f", upcast "Fish"
          "pic", upcast "Pic of size: 100"
          "promise", upcast Map.ofList [ "a", "Apple" ]
          "deep", upcast Map.ofList [
            "a", "Already Been Done" :> obj
            "b", "Boring" :> obj
            "c", [ "Contrived"; null; "Confusing" ] :> obj
          ]
        ]

    let DeepDataType = objdef "DeepDataType" [
        field "a" String (fun dt -> dt.a)
        field "b" String (fun dt -> dt.b)
        field "c" (ListOf String) (fun dt -> dt.c)
    ]
    let DataType = objdef "DataType" [
        field "a" String (fun (dt: TestSubject) -> dt.a)
        field "b" String (fun (dt: TestSubject) -> dt.b)
        field "c" String (fun (dt: TestSubject) -> dt.c)
        field "d" String (fun (dt: TestSubject) -> dt.d)
        field "e" String (fun (dt: TestSubject) -> dt.e)
        field "f" String (fun (dt: TestSubject) -> dt.f)
        //Schema.Field("pic", String, (fun (dt: TestSubject) args -> dt.pic(args.["pic"] :?> int)), "", [arg "size" Int])
        field "deep" DeepDataType (fun (dt: TestSubject) -> dt.deep)
    ]
    let schema = Schema(DataType)
    let result = sync <| schema.Execute(ast, data, variables = Map.ofList [ "size", upcast 100 ], operationName = "Example")
    equals result.Data.Value (upcast expected)


[<Fact>]
let ``Execution merges parallel fragments`` () =
    ()

[<Fact>]
let ``Execution uses root value context`` () =
    ()

[<Fact>]
let ``Execution uses arguments`` () =
    ()

[<Fact>]
let ``Execution uses inline operator if no operation name was provided`` () =
    ()

[<Fact>]
let ``Execution uses operation defined by name if provided`` () =
    ()

[<Fact>]
let ``Execution throws if no operation was provided`` () =
    ()
    
[<Fact>]
let ``Execution throws if no operation name was provided for document with multiple operations`` () =
    ()
    
[<Fact>]
let ``Execution throws if no operation with provided name was found`` () =
    ()
    
[<Fact>]
let ``Execution avoids recursion`` () =
    ()
    