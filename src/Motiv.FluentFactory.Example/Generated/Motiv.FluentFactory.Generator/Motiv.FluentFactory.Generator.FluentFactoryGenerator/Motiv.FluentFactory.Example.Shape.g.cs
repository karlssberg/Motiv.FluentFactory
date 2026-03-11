namespace Motiv.FluentFactory.Example
{
    [global::System.CodeDom.Compiler.GeneratedCode("Motiv.FluentFactory", "1.0.0.0")]
    internal partial class Shape
    {
        /// <summary>
        ///     <seealso cref="Motiv.FluentFactory.Example.Cuboid{T}"/>
        ///     <seealso cref="Motiv.FluentFactory.Example.Diamond{T}"/>
        ///     <seealso cref="Motiv.FluentFactory.Example.Rectangle{T}"/>
        ///     <seealso cref="Motiv.FluentFactory.Example.Square{T}"/>
        /// </summary>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static global::Motiv.FluentFactory.Example.Step_0__Motiv_FluentFactory_Example_Shape<T> WithWidth<T>(in T width)
            where T : global::System.Numerics.INumber<T>
        {
            return new global::Motiv.FluentFactory.Example.Step_0__Motiv_FluentFactory_Example_Shape<T>(width);
        }

        /// <summary>
        ///     <seealso cref="Motiv.FluentFactory.Example.Circle{T}"/>
        /// </summary>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static global::Motiv.FluentFactory.Example.Step_3__Motiv_FluentFactory_Example_Shape<T> WithRadius<T>(in T radius)
            where T : global::System.Numerics.INumber<T>
        {
            return new global::Motiv.FluentFactory.Example.Step_3__Motiv_FluentFactory_Example_Shape<T>(radius);
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCode("Motiv.FluentFactory", "1.0.0.0")]
    /// <summary>
    ///     <seealso cref="Motiv.FluentFactory.Example.Cuboid{T}"/>
    ///     <seealso cref="Motiv.FluentFactory.Example.Diamond{T}"/>
    ///     <seealso cref="Motiv.FluentFactory.Example.Rectangle{T}"/>
    ///     <seealso cref="Motiv.FluentFactory.Example.Square{T}"/>
    /// </summary>
    internal struct Step_0__Motiv_FluentFactory_Example_Shape<T> where T : global::System.Numerics.INumber<T>
    {
        private readonly T _width__parameter;
        internal Step_0__Motiv_FluentFactory_Example_Shape(in T width)
        {
            this._width__parameter = width;
        }

        /// <summary>
        ///     <seealso cref="Motiv.FluentFactory.Example.Cuboid{T}"/>
        ///     <seealso cref="Motiv.FluentFactory.Example.Diamond{T}"/>
        ///     <seealso cref="Motiv.FluentFactory.Example.Rectangle{T}"/>
        /// </summary>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public global::Motiv.FluentFactory.Example.Step_1__Motiv_FluentFactory_Example_Shape<T> WithHeight(in T height)
        {
            return new global::Motiv.FluentFactory.Example.Step_1__Motiv_FluentFactory_Example_Shape<T>(this._width__parameter, height);
        }

        /// <summary>
        /// Creates a new instance using constructor Motiv.FluentFactory.Example.Square&lt;T&gt;.Square(T Width).
        ///
        ///     <seealso cref="Motiv.FluentFactory.Example.Square{T}"/>
        /// </summary>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public global::Motiv.FluentFactory.Example.Square<T> CreateSquare()
        {
            return new global::Motiv.FluentFactory.Example.Square<T>(this._width__parameter);
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCode("Motiv.FluentFactory", "1.0.0.0")]
    /// <summary>
    ///     <seealso cref="Motiv.FluentFactory.Example.Cuboid{T}"/>
    ///     <seealso cref="Motiv.FluentFactory.Example.Diamond{T}"/>
    ///     <seealso cref="Motiv.FluentFactory.Example.Rectangle{T}"/>
    /// </summary>
    internal struct Step_1__Motiv_FluentFactory_Example_Shape<T> where T : global::System.Numerics.INumber<T>
    {
        private readonly T _width__parameter;
        private readonly T _height__parameter;
        internal Step_1__Motiv_FluentFactory_Example_Shape(in T width, in T height)
        {
            this._width__parameter = width;
            this._height__parameter = height;
        }

        /// <summary>
        ///     <seealso cref="Motiv.FluentFactory.Example.Cuboid{T}"/>
        /// </summary>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public global::Motiv.FluentFactory.Example.Step_2__Motiv_FluentFactory_Example_Shape<T> WithDepth(in T depth)
        {
            return new global::Motiv.FluentFactory.Example.Step_2__Motiv_FluentFactory_Example_Shape<T>(this._width__parameter, this._height__parameter, depth);
        }

        /// <summary>
        /// Creates a new instance using constructor Motiv.FluentFactory.Example.Rectangle&lt;T&gt;.Rectangle(T Width, T Height).
        ///
        ///     <seealso cref="Motiv.FluentFactory.Example.Rectangle{T}"/>
        /// </summary>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public global::Motiv.FluentFactory.Example.Rectangle<T> CreateRectangle()
        {
            return new global::Motiv.FluentFactory.Example.Rectangle<T>(this._width__parameter, this._height__parameter);
        }

        /// <summary>
        /// Creates a new instance using constructor Motiv.FluentFactory.Example.Diamond&lt;T&gt;.Diamond(T Width, T Height).
        ///
        ///     <seealso cref="Motiv.FluentFactory.Example.Diamond{T}"/>
        /// </summary>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public global::Motiv.FluentFactory.Example.Diamond<T> CreateDiamond()
        {
            return new global::Motiv.FluentFactory.Example.Diamond<T>(this._width__parameter, this._height__parameter);
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCode("Motiv.FluentFactory", "1.0.0.0")]
    /// <summary>
    ///     <seealso cref="Motiv.FluentFactory.Example.Cuboid{T}"/>
    /// </summary>
    internal struct Step_2__Motiv_FluentFactory_Example_Shape<T> where T : global::System.Numerics.INumber<T>
    {
        private readonly T _width__parameter;
        private readonly T _height__parameter;
        private readonly T _depth__parameter;
        internal Step_2__Motiv_FluentFactory_Example_Shape(in T width, in T height, in T depth)
        {
            this._width__parameter = width;
            this._height__parameter = height;
            this._depth__parameter = depth;
        }

        /// <summary>
        /// Creates a new instance using constructor Motiv.FluentFactory.Example.Cuboid&lt;T&gt;.Cuboid(T Width, T Height, T Depth).
        ///
        ///     <seealso cref="Motiv.FluentFactory.Example.Cuboid{T}"/>
        /// </summary>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public global::Motiv.FluentFactory.Example.Cuboid<T> CreateCuboid()
        {
            return new global::Motiv.FluentFactory.Example.Cuboid<T>(this._width__parameter, this._height__parameter, this._depth__parameter);
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCode("Motiv.FluentFactory", "1.0.0.0")]
    /// <summary>
    ///     <seealso cref="Motiv.FluentFactory.Example.Circle{T}"/>
    /// </summary>
    internal struct Step_3__Motiv_FluentFactory_Example_Shape<T> where T : global::System.Numerics.INumber<T>
    {
        private readonly T _radius__parameter;
        internal Step_3__Motiv_FluentFactory_Example_Shape(in T radius)
        {
            this._radius__parameter = radius;
        }

        /// <summary>
        /// Creates a new instance using constructor Motiv.FluentFactory.Example.Circle&lt;T&gt;.Circle(T Radius).
        ///
        ///     <seealso cref="Motiv.FluentFactory.Example.Circle{T}"/>
        /// </summary>
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public global::Motiv.FluentFactory.Example.Circle<T> CreateCircle()
        {
            return new global::Motiv.FluentFactory.Example.Circle<T>(this._radius__parameter);
        }
    }
}// <auto-generated/>
#nullable enable
