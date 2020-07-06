using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Ngn.LogAction
{
	public class LogActionInfo
	{
		public ActionType Type { get; set; }

		public DateTime Date { get; set; }

		public string UserName { get; set; }

		public string Comment { get; set; }

		public string EntityType { get; set; }

		public int EntityId { get; set; }

		public string EntityDisplayName { get; set; }

		public XElement CurrentEntity { get; set; }

		public XElement PreviousEntity { get; set; }

		public IEnumerable<DifferenceInfo> Differences { get; set; }
	}
}
