// Copyright 2003 Eric Marchesin - <eric.marchesin@laposte.net>
//
// This source file(s) may be redistributed by any means PROVIDING they
// are not sold for profit without the authors expressed written consent,
// and providing that this notice and the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
//-----------------------------------------------------------------------
using System;
using AkatoshQuester.Helpers.Cartography;


namespace AkatoshQuester.Helpers.LightGeometry
{
	/// <summary>
	/// Basic geometry class : easy to replace
	/// Written so as to be generalized
	/// </summary>
	[Serializable]
	public class Point2D
	{
		double[] _Coordinates = new double[2];

		/// <summary>
		/// Point2D constructor.
		/// </summary>
		/// <exception cref="ArgumentNullException">Argument array must not be null.</exception>
		/// <exception cref="ArgumentException">The Coordinates' array must contain exactly 3 elements.</exception>
		/// <param name="Coordinates">An array containing the three coordinates' values.</param>
		public Point2D(double[] Coordinates)
		{
			if ( Coordinates == null ) throw new ArgumentNullException();
			if ( Coordinates.Length!=3 ) throw new ArgumentException("The Coordinates' array must contain exactly 3 elements.");
			X = Coordinates[0]; Y = Coordinates[1];
		}

        public static Point2D Empty => new Point2D(0, 0);

        public Point2D()
        {
            X = 0; 
            Y = 0; 
        }

		/// <summary>
		/// Point2D constructor.
		/// </summary>
		/// <param name="CoordinateX">X coordinate.</param>
		/// <param name="CoordinateY">Y coordinate.</param>
		/// <param name="CoordinateZ">Z coordinate.</param>
		public Point2D(double CoordinateX, double CoordinateY)
		{
			X = CoordinateX; Y = CoordinateY;
		}

		/// <summary>
		/// Accede to coordinates by indexes.
		/// </summary>
		/// <exception cref="IndexOutOfRangeException">Index must belong to [0;2].</exception>
		public double this[int CoordinateIndex]
		{
			get { return _Coordinates[CoordinateIndex]; }
			set	{ _Coordinates[CoordinateIndex] = value; }
		}

		/// <summary>
		/// Gets/Set X coordinate.
		/// </summary>
		public double X { set { _Coordinates[0] = value; } get { return _Coordinates[0]; } }

		/// <summary>
		/// Gets/Set Y coordinate.
		/// </summary>
		public double Y { set { _Coordinates[1] = value; } get { return _Coordinates[1]; } }

        public double Distance(Point2D p2)
        {
            return (int)Math.Sqrt((X - p2.X) * (X - p2.X) + (Y - p2.Y) * (Y - p2.Y));
        }

        
		/// <summary>
		/// Object.Equals override.
		/// Tells if two points are equal by comparing coordinates.
		/// </summary>
		/// <exception cref="ArgumentException">Cannot compare Point2D with another type.</exception>
		/// <param name="Point">The other 3DPoint to compare with.</param>
		/// <returns>'true' if points are equal.</returns>
		public override bool Equals(object Point)
		{
			Point2D P = (Point2D)Point;
			if ( P==null ) throw new ArgumentException("Object must be of type "+GetType());
			bool Resultat = true;
			for (int i=0; i<2; i++) Resultat &= P[i].Equals(this[i]);
			return Resultat;
		}

        /// <summary>
        /// Object.GetHashCode override.
        /// </summary>
        /// <returns>HashCode value.</returns>
        public override int GetHashCode() {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Object.GetHashCode override.
        /// Returns a textual description of the point.
        /// </summary>
        /// <returns>String describing this point.</returns>
        public override string ToString() {
            return $"{X}, {Y}";
        }
	}
}
