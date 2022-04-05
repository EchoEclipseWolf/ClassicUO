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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using AkatoshQuester.Helpers.LightGeometry;


namespace AkatoshQuester.Helpers.Cartography
{
	/// <summary>
	/// An arc is defined with its two extremity nodes StartNode and EndNode therefore it is oriented.
	/// It is also characterized by a crossing factor named 'Weight'.
	/// This value represents the difficulty to reach the ending node from the starting one.
	/// </summary>
	[Serializable]
	public class Arc
	{
		Node _StartNode, _EndNode;
		double _Weight;
		bool _Passable;
		double _Length;
		bool _LengthUpdated;

		/// <summary>
		/// Arc constructor.
		/// </summary>
		/// <exception cref="ArgumentNullException">Extremity nodes cannot be null.</exception>
		/// <exception cref="ArgumentException">StartNode and EndNode must be different.</exception>
		/// <param name="Start">The node from which the arc starts.</param>
		/// <param name="End">The node to which the arc ends.</param>
		public Arc(Node Start, Node End)
		{
			StartNode = Start;
			EndNode = End;
			Weight = 1;
			LengthUpdated = false;
			Passable = true;

		    StartNodeId = Start.Id;
		    EndNodeId = End.Id;
		}

        public Arc()
        {
            StartNode = null;
            EndNode = null;
            Weight = 1;
            LengthUpdated = false;
            Passable = true;

            StartNodeId = 0;
            EndNodeId = 0;
        }

		/// <summary>
		/// Gets/Sets the node from which the arc starts.
		/// </summary>
		/// <exception cref="ArgumentNullException">StartNode cannot be set to null.</exception>
		/// <exception cref="ArgumentException">StartNode cannot be set to EndNode.</exception>

        public int StartNodeId { get; set; }
        public Node StartNode
		{
			set
			{
			    if (value == null)
			    {
			        _StartNode = null;
			        return;
			    }
			    //throw new ArgumentNullException("StartNode");
				//if ( EndNode!=null && value.Equals(EndNode) ) 
                //    throw new ArgumentException("StartNode and EndNode must be different");

				//if ( _StartNode !=null  ) 
               //     _StartNode.OutgoingArcs.Remove(this);

				//_StartNode = value;
				//_StartNode.OutgoingArcs.Add(this);
			}
			get { return _StartNode; }
		}

		/// <summary>
		/// Gets/Sets the node to which the arc ends.
		/// </summary>
		/// <exception cref="ArgumentNullException">EndNode cannot be set to null.</exception>
		/// <exception cref="ArgumentException">EndNode cannot be set to StartNode.</exception>
        public int EndNodeId { get; set; }
        public Node EndNode
		{
			set
			{
                if (value == null)
                {
                    _EndNode = null;
                    return;
                }
				//if ( StartNode!=null && value.Equals(StartNode) ) throw new ArgumentException("StartNode and EndNode must be different");
				//if ( _EndNode!=null ) _EndNode.IncomingArcs.Remove(this);

				//_EndNode = value;
				//_EndNode.IncomingArcs.Add(this);
			}
			get { return _EndNode; }
		}

		/// <summary>
		/// Sets/Gets the weight of the arc.
		/// This value is used to determine the cost of moving through the arc.
		/// </summary>
		public double Weight
		{
			set { _Weight = value; }
			get { return _Weight; }
		}

		/// <summary>
		/// Gets/Sets the functional state of the arc.
		/// 'true' means that the arc is in its normal state.
		/// 'false' means that the arc will not be taken into account (as if it did not exist or if its cost were infinite).
		/// </summary>
		public bool Passable
		{
			set { _Passable = value; }
			get { return _Passable; }		
		}

		public bool LengthUpdated
		{
			set { _LengthUpdated = value; }
			get { return _LengthUpdated; }
		}

		/// <summary>
		/// Gets arc's length.
		/// </summary>
		public double Length
		{
			get
			{
				if ( LengthUpdated==false )
				{
					_Length = CalculateLength();
					LengthUpdated = true;
				}
				return _Length;
			}
		}

		/// <summary>
		/// Performs the calculous that returns the arc's length
		/// Can be overriden for derived types of arcs that are not linear.
		/// </summary>
		/// <returns></returns>
		virtual protected double CalculateLength()
		{
            return _StartNode.Position.Distance(_EndNode.Position);
		}

		/// <summary>
		/// Gets the cost of moving through the arc.
		/// Can be overriden when not simply equals to Weight*Length.
		/// </summary>
		virtual public double Cost
		{
			get { return Weight*Length; }
		}

		/// <summary>
		/// Returns the textual description of the arc.
		/// object.ToString() override.
		/// </summary>
		/// <returns>String describing this arc.</returns>
		public override string ToString()
		{
			return _StartNode.ToString()+"-->"+_EndNode.ToString();
		}

		/// <summary>
		/// Object.Equals override.
		/// Tells if two arcs are equal by comparing StartNode and EndNode.
		/// </summary>
		/// <exception cref="ArgumentException">Cannot compare an arc with another type.</exception>
		/// <param name="O">The arc to compare with.</param>
		/// <returns>'true' if both arcs are equal.</returns>
		public override bool Equals(object O)
		{
			Arc A = (Arc) O;
			if ( A==null ) throw new ArgumentException("Cannot compare type "+GetType()+" with type "+O.GetType()+" !");
			return _StartNode.Equals(A._StartNode) && _EndNode.Equals(A._EndNode);
		}

		/// <summary>
		/// Object.GetHashCode override.
		/// </summary>
		/// <returns>HashCode value.</returns>
		public override int GetHashCode() { return (int)Length; }

        public virtual void SaveToStream(Stream stream)
        {
            var xml = new XDocument(new XElement("Arc",
                new XAttribute("SId", StartNodeId),
                new XAttribute("EId", EndNodeId),
                new XAttribute("W", Weight),
                new XAttribute("P", Passable)));

            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            var sw = new StringWriter();
            using (XmlWriter xw = XmlWriter.Create(sw, settings))
            {
                xml.Save(xw);
            }

            string s = sw.ToString();

            byte[] info = new UTF8Encoding(true).GetBytes(s);
            stream.Write(info, 0, info.Length);

            byte[] newline = Encoding.ASCII.GetBytes(Environment.NewLine);
            stream.Write(newline, 0, newline.Length);
        }

        public virtual void LoadFromElement(XElement element, Dictionary<int, Node>[] nodeLists)
        {
            XAttribute xmlStartNodeId = element.Attributes().FirstOrDefault(attribute => "SId" == attribute.Name.ToString());
            int startNodeId = 0;
            if (!int.TryParse(xmlStartNodeId.Value, out startNodeId))
                startNodeId = 0;

            StartNodeId = startNodeId;

            XAttribute xmlEndNodeId = element.Attributes().FirstOrDefault(attribute => "EId" == attribute.Name.ToString());
            int endNodeId = 0;
            if (!int.TryParse(xmlEndNodeId.Value, out endNodeId))
                endNodeId = 0;

            EndNodeId = endNodeId;

            XAttribute xmlWeight = element.Attributes().FirstOrDefault(attribute => "W" == attribute.Name.ToString());
            double weight = 0;
            if (xmlWeight == null || !double.TryParse(xmlWeight.Value, out weight))
                weight = 0;

            Weight = weight;


            XAttribute xmlPassable = element.Attributes().FirstOrDefault(attribute => "P" == attribute.Name.ToString());
            bool passable = true;
            if (xmlPassable == null || !bool.TryParse(xmlPassable.Value, out passable))
                passable = true;

            Passable = passable;

            int listId = StartNodeId / 100;
            StartNode = nodeLists[listId][StartNodeId];

            int endListId = EndNodeId / 100;
            EndNode = nodeLists[endListId][EndNodeId];

           // StartNode = nodes[StartNodeId];
            //EndNode = nodes[EndNodeId];
        }
	}
}

