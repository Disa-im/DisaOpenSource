using Raksha.Asn1;
using Raksha.Asn1.Tsp;

namespace Raksha.Tsp
{
	public class GenTimeAccuracy
	{
		private Accuracy accuracy;

		public GenTimeAccuracy(
			Accuracy accuracy)
		{
			this.accuracy = accuracy;
		}

		public int Seconds { get { return GetTimeComponent(accuracy.Seconds); } }

		public int Millis { get { return GetTimeComponent(accuracy.Millis); } }

		public int Micros { get { return GetTimeComponent(accuracy.Micros); } }

		private int GetTimeComponent(
			DerInteger time)
		{
			return time == null ? 0 : time.Value.IntValue;
		}

		public override string ToString()
		{
			return Seconds + "." + Millis.ToString("000") + Micros.ToString("000");
		}
	}
}
