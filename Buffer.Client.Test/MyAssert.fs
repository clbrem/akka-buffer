namespace MyAssert

type Assert<'T>() =
    inherit Xunit.Assert()
    static member FailWith fmt (actual:'T) =
        Assert.Fail(sprintf fmt actual)

