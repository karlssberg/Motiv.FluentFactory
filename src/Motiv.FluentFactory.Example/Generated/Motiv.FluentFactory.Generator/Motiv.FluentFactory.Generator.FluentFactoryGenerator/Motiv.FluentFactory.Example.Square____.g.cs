namespace Motiv.FluentFactory.Example
{
    internal partial record Square<T>
        where T : System.Numerics.INumber<T>
    {
        /// <summary>
        ///     <seealso cref="Motiv.FluentFactory.Example.Square{T}"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Step_0__Motiv_FluentFactory_Example_Square____<T> WithWidth(in T width)
        {
            return new Step_0__Motiv_FluentFactory_Example_Square____<T>(width);
        }
    }

    /// <summary>
    ///     <seealso cref="Motiv.FluentFactory.Example.Square{T}"/>
    /// </summary>
    internal struct Step_0__Motiv_FluentFactory_Example_Square____<T> where T : System.Numerics.INumber<T>
    {
        private readonly T _width__parameter;
        internal Step_0__Motiv_FluentFactory_Example_Square____(in T width)
        {
            this._width__parameter = width;
        }

        /// <summary>
        /// Creates a new instance using constructor Motiv.FluentFactory.Example.Square&lt;T&gt;.Square(T Width).
        ///
        ///     <seealso cref="Motiv.FluentFactory.Example.Square{T}"/>
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public Square<T> Create()
        {
            return new Square<T>(this._width__parameter);
        }
    }
}