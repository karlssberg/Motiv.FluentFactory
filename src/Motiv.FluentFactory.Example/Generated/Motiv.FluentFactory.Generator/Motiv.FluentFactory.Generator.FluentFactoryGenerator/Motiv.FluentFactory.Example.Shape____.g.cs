﻿namespace Motiv.FluentFactory.Example
{
    internal partial class Shape<T>
        where T : System.Numerics.INumber<T>
    {
        /// <summary>
        ///     <seealso cref="Motiv.FluentFactory.Example.Cuboid{T}"/>
        ///     <seealso cref="Motiv.FluentFactory.Example.Diamond{T}"/>
        ///     <seealso cref="Motiv.FluentFactory.Example.Rectangle{T}"/>
        ///     <seealso cref="Motiv.FluentFactory.Example.Square{T}"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Step_0__Motiv_FluentFactory_Example_Shape____<T> WithWidth(in T width)
        {
            return new Step_0__Motiv_FluentFactory_Example_Shape____<T>(width);
        }

        /// <summary>
        ///     <seealso cref="Motiv.FluentFactory.Example.Circle{T}"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Step_3__Motiv_FluentFactory_Example_Shape____<T> WithRadius(in T radius)
        {
            return new Step_3__Motiv_FluentFactory_Example_Shape____<T>(radius);
        }
    }

    /// <summary>
    ///     <seealso cref="Motiv.FluentFactory.Example.Cuboid{T}"/>
    ///     <seealso cref="Motiv.FluentFactory.Example.Diamond{T}"/>
    ///     <seealso cref="Motiv.FluentFactory.Example.Rectangle{T}"/>
    ///     <seealso cref="Motiv.FluentFactory.Example.Square{T}"/>
    /// </summary>
    internal struct Step_0__Motiv_FluentFactory_Example_Shape____<T> where T : System.Numerics.INumber<T>
    {
        private readonly T _width__parameter;
        internal Step_0__Motiv_FluentFactory_Example_Shape____(in T width)
        {
            this._width__parameter = width;
        }

        /// <summary>
        ///     <seealso cref="Motiv.FluentFactory.Example.Cuboid{T}"/>
        ///     <seealso cref="Motiv.FluentFactory.Example.Diamond{T}"/>
        ///     <seealso cref="Motiv.FluentFactory.Example.Rectangle{T}"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Step_1__Motiv_FluentFactory_Example_Shape____<T> WithHeight(in T height)
        {
            return new Step_1__Motiv_FluentFactory_Example_Shape____<T>(this._width__parameter, height);
        }

        /// <summary>
        /// Creates a new instance using constructor Motiv.FluentFactory.Example.Square&lt;T&gt;.Square(T Width).
        ///
        ///     <seealso cref="Motiv.FluentFactory.Example.Square{T}"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Square<T> CreateSquare()
        {
            return new Square<T>(this._width__parameter);
        }
    }

    /// <summary>
    ///     <seealso cref="Motiv.FluentFactory.Example.Cuboid{T}"/>
    ///     <seealso cref="Motiv.FluentFactory.Example.Diamond{T}"/>
    ///     <seealso cref="Motiv.FluentFactory.Example.Rectangle{T}"/>
    /// </summary>
    internal struct Step_1__Motiv_FluentFactory_Example_Shape____<T> where T : System.Numerics.INumber<T>
    {
        private readonly T _width__parameter;
        private readonly T _height__parameter;
        internal Step_1__Motiv_FluentFactory_Example_Shape____(in T width, in T height)
        {
            this._width__parameter = width;
            this._height__parameter = height;
        }

        /// <summary>
        ///     <seealso cref="Motiv.FluentFactory.Example.Cuboid{T}"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Step_2__Motiv_FluentFactory_Example_Shape____<T> WithDepth(in T depth)
        {
            return new Step_2__Motiv_FluentFactory_Example_Shape____<T>(this._width__parameter, this._height__parameter, depth);
        }

        /// <summary>
        /// Creates a new instance using constructor Motiv.FluentFactory.Example.Rectangle&lt;T&gt;.Rectangle(T Width, T Height).
        ///
        ///     <seealso cref="Motiv.FluentFactory.Example.Rectangle{T}"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Rectangle<T> CreateRectangle()
        {
            return new Rectangle<T>(this._width__parameter, this._height__parameter);
        }

        /// <summary>
        /// Creates a new instance using constructor Motiv.FluentFactory.Example.Diamond&lt;T&gt;.Diamond(T Width, T Height).
        ///
        ///     <seealso cref="Motiv.FluentFactory.Example.Diamond{T}"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Diamond<T> CreateDiamond()
        {
            return new Diamond<T>(this._width__parameter, this._height__parameter);
        }
    }

    /// <summary>
    ///     <seealso cref="Motiv.FluentFactory.Example.Cuboid{T}"/>
    /// </summary>
    internal struct Step_2__Motiv_FluentFactory_Example_Shape____<T> where T : System.Numerics.INumber<T>
    {
        private readonly T _width__parameter;
        private readonly T _height__parameter;
        private readonly T _depth__parameter;
        internal Step_2__Motiv_FluentFactory_Example_Shape____(in T width, in T height, in T depth)
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Cuboid<T> CreateCuboid()
        {
            return new Cuboid<T>(this._width__parameter, this._height__parameter, this._depth__parameter);
        }
    }

    /// <summary>
    ///     <seealso cref="Motiv.FluentFactory.Example.Circle{T}"/>
    /// </summary>
    internal struct Step_3__Motiv_FluentFactory_Example_Shape____<T> where T : System.Numerics.INumber<T>
    {
        private readonly T _radius__parameter;
        internal Step_3__Motiv_FluentFactory_Example_Shape____(in T radius)
        {
            this._radius__parameter = radius;
        }

        /// <summary>
        /// Creates a new instance using constructor Motiv.FluentFactory.Example.Circle&lt;T&gt;.Circle(T Radius).
        ///
        ///     <seealso cref="Motiv.FluentFactory.Example.Circle{T}"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Circle<T> CreateCircle()
        {
            return new Circle<T>(this._radius__parameter);
        }
    }
}