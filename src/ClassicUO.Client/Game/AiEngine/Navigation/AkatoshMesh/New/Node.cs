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

    public class NodeLink {
        public long Id;
        public Point2D FilePoint;
        public int MapIndex;
        public long XYHash;

        public NodeLink(long id, Point2D filePoint, int mapIndex, long xYHash)
        {
            Id = id;
            FilePoint = filePoint;
            MapIndex = mapIndex;
            XYHash = xYHash;
        }

        public NodeLink(BinaryReader reader) {
            Id = reader.ReadInt64();
            FilePoint = new Point2D(reader.ReadInt16(), reader.ReadInt16());
            MapIndex = reader.ReadInt16();
            XYHash = reader.ReadInt64();
        }

        public void Save(BinaryWriter writer) {
            writer.Write(Id);
            writer.Write((short)FilePoint.X);
            writer.Write((short)FilePoint.Y);
            writer.Write((short)MapIndex);
            writer.Write(XYHash);
        }
    }

    /// <summary>
    /// Basically a node is defined with a geographical position in space.
    /// It is also characterized with both collections of outgoing arcs and incoming arcs.
    /// </summary>
    [Serializable]
	public class Node
	{
	    public long Id;
        public long XyHash;
        public int NodeType;
        public Point3D Location;
        public Point3D EndLocation;
        public int MapIndex;
        public int EndMapIndex;

        private AkatoshNodeType _type;

        [XmlIgnore]
        public AkatoshNodeType Type { get { return _type; } set { _type = value; } }

        private bool _passable;

        public List<NodeLink> Linked { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="positionX">X coordinate.</param>
		/// <param name="positionY">Y coordinate.</param>
		/// <param name="positionZ">Z coordinate.</param>
        public Node(Point3D location, int startMapId, Point3D endLocation, int endMapId, AkatoshNodeType type)
		{
            Location = location;
		    EndLocation = endLocation;
			_passable = true;
            Linked = new List<NodeLink>();
            MapIndex = startMapId;
            EndMapIndex = endMapId;
            Id = (int)GetNodeHash(Location);
            XyHash = MeshGrid.GetXYPositionHash(Location, startMapId);

            Type = type;
		}

        public Node()
        {
            Location = new Point3D(0, 0, 0);
            EndLocation = new Point3D(0, 0, 0);
            _passable = true;
            Linked = new List<NodeLink>();

            Type = AkatoshNodeType.Ground;
        }

        public Node(BinaryReader reader)
        {
            Id = reader.ReadInt64();
            XyHash = reader.ReadInt64();
            _passable = reader.ReadBoolean();
            Location = new Point3D(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16());
            EndLocation = new Point3D(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16());
            MapIndex = reader.ReadInt32();
            EndMapIndex = reader.ReadInt32();
            

            var count = reader.ReadInt32();
            Linked = new List<NodeLink>();
            for (int i = 0; i < count; i++) {
                Linked.Add(new NodeLink(reader));
            }

            if (XyHash == 0) {
                XyHash = MeshGrid.GetXYPositionHash(Location, MapIndex);
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write((int)Type); // This is read before creating the node to know what type to create it as

            writer.Write(Id);
            writer.Write(XyHash);
            writer.Write(_passable);
            writer.Write((short)Location.X);
            writer.Write((short)Location.Y);
            writer.Write((short)Location.Z);
            writer.Write((short)EndLocation.X);
            writer.Write((short)EndLocation.Y);
            writer.Write((short)EndLocation.Z);
            writer.Write(MapIndex);
            writer.Write(EndMapIndex);

            writer.Write(Linked.Count);
            foreach (var linkedId in Linked) {
                linkedId.Save(writer);
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
            if (Linked.FirstOrDefault(n => n.Id == Id || n.Id == node.Id) != null)
                return;

            Linked.Add(new NodeLink(node.Id, Navigation.GetFilePointFromPoint(node.Position), node.MapIndex, node.XyHash));

            if (Linked.Count > 10) {
                int bob = 1;
            }
            if (!oneWay) {
                node.AddLink(this, true);
            }
        }

        internal virtual void RemoveLink(Node node)
        {
            if (Linked.FirstOrDefault(n => n.Id == Id) == null)
                return;

            var newLinked = new List<NodeLink>();

            foreach (var nodeLink in Linked) {
                if (nodeLink.Id == Id) {
                    continue;
                }
                newLinked.Add(nodeLink);
            }
            Linked = newLinked;
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

        public double Distance => Position.Distance(new Point3D(World.Player.Position.X, World.Player.Position.Y, World.Player.Position.Z));

        public double Distance2D => Position.Distance2D(new Point3D(World.Player.Position.X, World.Player.Position.Y, World.Player.Position.Z));

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
			Node N = new Node(Location, MapIndex, EndLocation, EndMapIndex, Type);
			N._passable = _passable;
			return N;
		}

		/// <summary>
		/// Object.GetHashCode override.
		/// </summary>
		/// <returns>HashCode value.</returns>
		public override int GetHashCode() { return Position.GetHashCode(); }

        public static long GetNodeHash(Point3D position)
        {
            var hashCode = (long)position.X;
            hashCode = (hashCode * 92821) ^ (long)position.Y;
            hashCode = (hashCode * 92821) ^ (long)position.Z;

            return hashCode;
        }

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
    }
}