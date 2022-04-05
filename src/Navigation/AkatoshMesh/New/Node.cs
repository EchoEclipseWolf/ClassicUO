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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO;
using ClassicUO.AiEngine;
using ClassicUO.Game;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;


namespace AkatoshQuester.Helpers.Cartography
{
    public enum AkatoshNodeType {
        Ground,
        Runebook,
        PublicMoongate,
        Teleport,
        Interaction
    }

    /// <summary>
    /// Basically a node is defined with a geographical position in space.
    /// It is also characterized with both collections of outgoing arcs and incoming arcs.
    /// </summary>
    [Serializable]
	public class Node
	{
	    public int Id;
        public int NodeType;
        public int PositionHash;
        public Point3D Location;
        public Point3D EndLocation;

	    private AkatoshNodeType _type;

        [XmlIgnore]
        public AkatoshNodeType Type { get { return _type; } set { _type = value; } }

        private bool _passable;

        public HashSet<int> LinkedIds { get; set; }
        public HashSet<Point2D> LinkedFiles = new HashSet<Point2D>();

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="positionX">X coordinate.</param>
		/// <param name="positionY">Y coordinate.</param>
		/// <param name="positionZ">Z coordinate.</param>
        public Node(Point3D location, Point3D endLocation, AkatoshNodeType type)
		{
            Location = location;
		    EndLocation = endLocation;
			_passable = true;
            LinkedIds = new HashSet<int>();

            PositionHash = Location.ToString().GetHashCode();


            Type = type;
		}

        public Node()
        {
            Location = new Point3D(0, 0, 0);
            EndLocation = new Point3D(0, 0, 0);
            _passable = true;
            LinkedIds = new HashSet<int>();

            Type = AkatoshNodeType.Ground;

            PositionHash = Location.ToString().GetHashCode();
        }

        public Node(BinaryReader reader)
        {
            LinkedIds = new HashSet<int>();

            Id = reader.ReadInt32();
            PositionHash = reader.ReadInt32();
            Type = (AkatoshNodeType)reader.ReadInt32();
            _passable = reader.ReadBoolean();
            Location = new Point3D(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16());
            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++) {
                LinkedIds.Add(reader.ReadInt32());
            }
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++) {
                LinkedFiles.Add(new Point2D(reader.ReadDouble(), reader.ReadDouble()));
            }

            if(LinkedIds.Count > 0 && LinkedFiles.Count == 0) {
                int bob = 1;
            }

            PositionHash = Location.ToString().GetHashCode();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write((int)Type); // This is read before creating the node to know what type to create it as

            writer.Write(Id);
            writer.Write((int)0);
            writer.Write((int)Type);
            writer.Write(_passable);
            writer.Write((short)Location.X);
            writer.Write((short)Location.Y);
            writer.Write((short)Location.Z);
            writer.Write(LinkedIds.Count);
            foreach (var linkedId in LinkedIds)
            {
                writer.Write(linkedId);
            }
            if(LinkedFiles.Count == 0 && LinkedIds.Count != 0) {
                int bob = 1;
            }
            writer.Write(LinkedFiles.Count);
            foreach (var LinkedFile in LinkedFiles)
            {
                writer.Write(LinkedFile.X);
                writer.Write(LinkedFile.Y);
            }
        }

        public void UpdateLinked() {
            int count = 0;
            foreach (var id in LinkedIds) {
                if (!Navigation.CurrentMesh.NodesById.TryGetValue(id, out var node)) {
                    return;
                }

                var filePoint = Navigation.GetFilePointFromPoint(node.Location);
                if (LinkedFiles.Contains(filePoint)) {
                    return;
                }

                LinkedFiles.Add(filePoint);
                ++count;
                if (count > 1) {
                    int bob = 1;
                }
            }
        }


        public virtual async Task<bool> Run()
        {
            var direction = Navigation.DirectionForNextPosition(new Vector2((ushort)Location.X, (ushort)Location.Y),
               new Vector2(World.Player.X, World.Player.Y));

            if (!World.Player.Walk(direction, true)) {
                return false;
            }

            return true;
        }

        internal virtual void AddLink(Node node, bool oneWay = false) {
            if (LinkedIds.Contains(node.Id))
                return;

            LinkedIds.Add(node.Id);
            if (!oneWay) {
                node.AddLink(this);
            }

            UpdateLinked();
        }

        internal virtual void RemoveLink(Node node)
        {
            if (!LinkedIds.Contains(node.Id))
                return;

            LinkedIds.Remove(node.Id);
            node.RemoveLink(this);
        }

        public virtual bool Passable
		{
			set
			{
                _passable = value;
			}
		    get
		    {
                return _passable;
		    }		
		}

		/// <summary>
		/// Gets X coordinate.
		/// </summary>
		public double X { get { return Position.X; } }

		/// <summary>
		/// Gets Y coordinate.
		/// </summary>
		public double Y { get { return Position.Y; } }

		/// <summary>
		/// Gets Z coordinate.
		/// </summary>
		public double Z { get { return Position.Z; } }


		/// <summary>
		/// Gets/Sets the geographical position of the node.
		/// </summary>
		/// <exception cref="ArgumentNullException">Cannot set the Position to null.</exception>
        [XmlIgnore]
        public Point3D Position
		{
			set
			{
				if ( value==null ) throw new ArgumentNullException();
				//foreach (Arc A in _IncomingArcs) A.LengthUpdated = false;
				//foreach (Arc A in _OutgoingArcs) A.LengthUpdated = false;
                Location = value;
			}
            get { return Location; }
		}

		/*/// <summary>
		/// Gets the array of nodes that can be directly reached from this one.
		/// </summary>
        [XmlIgnore]
        public List<Node> AccessibleNodes
		{
			get
			{
			    var Tableau = new List<Node>();

				int i=0;
			    foreach (Node A in Linked)
			    {
			        Tableau.Add(A.EndNode);
			    }
			    return Tableau;
			}
		}

		/// <summary>
		/// Gets the array of nodes that can directly reach this one.
		/// </summary>
        [XmlIgnore]
        public List<Node> AccessingNodes
		{
			get
			{
                var Tableau = new List<Node>();
				int i=0;
				foreach (Arc A in IncomingArcs) 
                    Tableau.Add(A.StartNode);

				return Tableau;
			}
		}*/

        private static List<int> AddedIds = new List<int>();
        public List<Node> GetAllConnectedToThis()
        {
            var list = new List<Node>();
            AddedIds.Clear();

            GetAllConnectedToThis(list);

            return list;
        }

        public void GetAllConnectedToThis(List<Node> nodes)
        {
            if (AddedIds.Contains(Id)) {
                return;
            }

            AddedIds.Add(Id);
            nodes.Add(this);

            foreach (var t in LinkedIds) {
                if (!Navigation.CurrentMesh.NodesById.ContainsKey(t)) {
                    continue;
                }

                var node = Navigation.CurrentMesh.NodesById[t];
                node.GetAllConnectedToThis(nodes);
            }
        }

        internal void ConnectLinks(Dictionary<int, Node> dictionary, MeshGraph graph)
        {
           /* foreach (var linkedId in OutgoingArcIds)
            {
                int id = linkedId;
                var neighbornode = dictionary[id];

                if (neighbornode != null)
                {
                    graph.AddArc(this, neighbornode, 1);
                }
            }*/
        }

	    /// <summary>
	    /// object.ToString() override.
	    /// Returns the textual description of the node.
	    /// </summary>
	    /// <returns>String describing this node.</returns>
	    public override string ToString()
	    {
            return string.Format("< {0}, {1}, {2} > Distance : {3} Distance2D : {4}", Position.X, Position.Y, Position.Z, Position.Distance(new Point3D(World.Player.Position.X, World.Player.Position.Y, World.Player.Position.Z)), Position.Distance2D(new Point3D(World.Player.Position.X, World.Player.Position.Y, World.Player.Position.Z)));
	        return Position.ToString();
	    }

		/// <summary>
		/// Object.Equals override.
		/// Tells if two nodes are equal by comparing positions.
		/// </summary>
		/// <exception cref="ArgumentException">A Node cannot be compared with another type.</exception>
		/// <param name="O">The node to compare with.</param>
		/// <returns>'true' if both nodes are equal.</returns>
		public override bool Equals(object O)
		{
            try
		    {
                if (O is Node node) {
                    return Position.Equals(node.Position);
                }
            }
		    catch (Exception)
		    {
		        return false;
		    }

            return false;

            //return Position.Equals(N.Position);
        }

		/// <summary>
		/// Returns a copy of this node.
		/// </summary>
		/// <returns>The reference of the new object.</returns>
		public object Clone()
		{
			Node N = new Node(Location, EndLocation, Type);
			N._passable = _passable;
			return N;
		}

		/// <summary>
		/// Object.GetHashCode override.
		/// </summary>
		/// <returns>HashCode value.</returns>
		public override int GetHashCode() { return Position.GetHashCode(); }

		/// <summary>
		/// Returns the euclidian distance between two nodes : Sqrt(Dx²+Dy²+Dz²)
		/// </summary>
		/// <param name="N1">First node.</param>
		/// <param name="N2">Second node.</param>
		/// <returns>Distance value.</returns>
		public double EuclidianDistance(Node N2)
		{
			return Math.Sqrt( SquareEuclidianDistance(N2) );
		}

        public static double EuclidianDistance(Node n1, Node n2)
        {
            return Math.Sqrt(SquareEuclidianDistance(n1, n2));
        }

		/// <summary>
		/// Returns the square euclidian distance between two nodes : Dx²+Dy²+Dz²
		/// </summary>
		/// <exception cref="ArgumentNullException">Argument nodes must not be null.</exception>
		/// <param name="N1">First node.</param>
		/// <param name="N2">Second node.</param>
		/// <returns>Distance value.</returns>
		public double SquareEuclidianDistance(Node N2)
		{
			if ( N2==null ) 
                throw new ArgumentNullException();

			double DX = Position.X - N2.Position.X;
			double DY = Position.Y - N2.Position.Y;
			double DZ = Position.Z - N2.Position.Z;
			return DX*DX+DY*DY+DZ*DZ;
		}

        public static double SquareEuclidianDistance(Node N1, Node N2)
        {
            if (N2 == null)
                throw new ArgumentNullException();

            double DX = N1.Position.X - N2.Position.X;
            double DY = N1.Position.Y - N2.Position.Y;
            double DZ = N1.Position.Z - N2.Position.Z;
            return DX * DX + DY * DY + DZ * DZ;
        }

		/// <summary>
		/// Returns the manhattan distance between two nodes : |Dx|+|Dy|+|Dz|
		/// </summary>
		/// <exception cref="ArgumentNullException">Argument nodes must not be null.</exception>
		/// <param name="N1">First node.</param>
		/// <param name="N2">Second node.</param>
		/// <returns>Distance value.</returns>
		public double ManhattanDistance(Node N2)
		{
			if ( N2==null ) 
                throw new ArgumentNullException();
			double DX = Position.X - N2.Position.X;
			double DY = Position.Y - N2.Position.Y;
			double DZ = Position.Z - N2.Position.Z;
			return Math.Abs(DX)+Math.Abs(DY)+Math.Abs(DZ);
		}

        public static double ManhattanDistance(Node N1, Node N2)
        {
            if (N2 == null)
                throw new ArgumentNullException();
            double DX = N1.Position.X - N2.Position.X;
            double DY = N1.Position.Y - N2.Position.Y;
            double DZ = N1.Position.Z - N2.Position.Z;
            return Math.Abs(DX) + Math.Abs(DY) + Math.Abs(DZ);
        }

		/// <summary>
		/// Returns the maximum distance between two nodes : Max(|Dx|, |Dy|, |Dz|)
		/// </summary>
		/// <exception cref="ArgumentNullException">Argument nodes must not be null.</exception>
		/// <param name="N1">First node.</param>
		/// <param name="N2">Second node.</param>
		/// <returns>Distance value.</returns>
		public double MaxDistanceAlongAxis(Node N2)
		{
			if ( N2==null ) 
                throw new ArgumentNullException();

			double DX = Math.Abs(Position.X - N2.Position.X);
			double DY = Math.Abs(Position.Y - N2.Position.Y);
			double DZ = Math.Abs(Position.Z - N2.Position.Z);
			return Math.Max(DX, Math.Max(DY, DZ));
		}

        public static double MaxDistanceAlongAxis(Node N1, Node N2)
        {
            if (N2 == null)
                throw new ArgumentNullException();

            double DX = Math.Abs(N1.Position.X - N2.Position.X);
            double DY = Math.Abs(N1.Position.Y - N2.Position.Y);
            double DZ = Math.Abs(N1.Position.Z - N2.Position.Z);
            return Math.Max(DX, Math.Max(DY, DZ));
        }
		
		/// <summary>
		/// Returns the bounding box that wraps the specified list of nodes.
		/// </summary>
		/// <exception cref="ArgumentException">The list must only contain elements of type Node.</exception>
		/// <exception cref="ArgumentException">The list of nodes is empty.</exception>
		/// <param name="NodesGroup">The list of nodes to wrap.</param>
		/// <param name="MinPoint">The point of minimal coordinates for the box.</param>
		/// <param name="MaxPoint">The point of maximal coordinates for the box.</param>
		static public void BoundingBox(IList NodesGroup, out double[] MinPoint, out double[] MaxPoint)
		{
			Node N1 = NodesGroup[0] as Node;
			if ( N1==null ) throw new ArgumentException("The list must only contain elements of type Node.");
			if ( NodesGroup.Count==0 ) throw new ArgumentException("The list of nodes is empty.");
			int Dim = 3;
			MinPoint = new double[Dim];
			MaxPoint = new double[Dim];
			for (int i=0; i<Dim; i++) MinPoint[i]=MaxPoint[i]=N1.Position[i];
			foreach ( Node N in NodesGroup )
			{
				for ( int i=0; i<Dim; i++ )
				{
					if ( MinPoint[i]>N.Position[i] ) MinPoint[i]=N.Position[i];
					if ( MaxPoint[i]<N.Position[i] ) MaxPoint[i]=N.Position[i];
				}
			}
		}

        internal void ConnectLinks(Dictionary<int, Node> dictionary)
        {
            foreach (var linkedId in LinkedIds)
            {
                int id = linkedId;
                var neighbornode = dictionary[id];

                if (neighbornode != null)
                    LinkedIds.Add(neighbornode.Id);
            }
        }
    }
}