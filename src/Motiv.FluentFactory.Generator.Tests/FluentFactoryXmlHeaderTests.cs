using VerifyCS =
    Motiv.FluentFactory.Generator.Tests.CSharpSourceGeneratorVerifier<Motiv.FluentFactory.Generator.FluentFactoryGenerator>;

namespace Motiv.FluentFactory.Generator.Tests;

public class FluentFactoryXmlHeaderTests
{
    private const string SourceFile = "Source.cs";

    [Fact]
    internal async Task Should_generate_properly_formatted_xml_documentation_without_line_breaks()
    {
        const string code =
            """
            using Motiv.FluentFactory.Generator;

            namespace Test;

            [FluentFactory]
            public static partial class Factory;

            public class MyBuildTarget
            {
                [FluentConstructor(typeof(Factory))]
                public MyBuildTarget(string value)
                {
                    Value = value;
                }

                public string Value { get; set; }
            }
            """;

        const string expected =
            """
            using System;

            namespace Test
            {
                public static partial class Factory
                {
                    /// <summary>
                    ///     <seealso cref="Test.MyBuildTarget"/>
                    /// </summary>
                    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    public static Step_0__Test_Factory WithValue(in string value)
                    {
                        return new Step_0__Test_Factory(value);
                    }
                }

                /// <summary>
                ///     <seealso cref="Test.MyBuildTarget"/>
                /// </summary>
                public struct Step_0__Test_Factory
                {
                    private readonly string _value__parameter;
                    internal Step_0__Test_Factory(in string value)
                    {
                        this._value__parameter = value;
                    }

                    /// <summary>
                    /// Creates a new instance using constructor Test.MyBuildTarget.MyBuildTarget(string value).
                    ///
                    ///     <seealso cref="Test.MyBuildTarget"/>
                    /// </summary>
                    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                    public MyBuildTarget Create()
                    {
                        return new MyBuildTarget(this._value__parameter);
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { (SourceFile, code) },
                GeneratedSources =
                {
                    (typeof(FluentFactoryGenerator), "Test.Factory.g.cs", expected)
                }
            }
        }.RunAsync();
    }
}