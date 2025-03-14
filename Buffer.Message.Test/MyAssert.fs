namespace Buffer.Message.MyAssert

type Assert<'T>() =
    inherit Xunit.Assert()
    static member FailWith fmt (actual:'T) =
            Assert.Fail(sprintf fmt actual)
    static member EqualTo (expected:'T) (actual:'T) = Assert.Equal(expected, actual)
    static member Some (assertion: 'T -> unit)(actual:'T option) =
        match actual with
        | Some item -> assertion item
        | None -> Assert.Fail("Expected Some but got None")
    static member None (actual:'T option) =
        match actual with
        | Some _-> Assert.Fail("Expected None but got Some")
        | None -> ()
    static member Pass() = ()
    
