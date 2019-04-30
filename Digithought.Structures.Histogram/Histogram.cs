using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Digithought.Structures
{
	public class Histogram
	{
		private const int MaxIterations = 20;

		public int Total { get; private set; }
		public int[] Data { get; private set; }

		public Histogram(int bins)
		{
			Data = new int[bins];
		}

		public Histogram(int[] data)
		{
			Data = data;
			Total = Data.Sum();
		}

		public void Reset()
		{
			Array.Clear(Data, 0, Data.Length);
			Total = 0;
		}

		public void AddData(double value)
		{
			++Data[GetAddress(value)];
			++Total;
		}

		/// <summary> Applies the given function to each member of data. </summary>
		public void Apply(Func<int, int> func)
		{
			for (var i = 0; i < Data.Length; ++i)
			{
				var delta = func(Data[i]) - Data[i];
				Total += delta;
				Data[i] += delta;
			}
		}

		/// <summary> Re-scale the histogram, smoothing for quantification errors. </summary>
		public void Scale(double value)
		{
			var error = 0.0;
			for (var i = 0; i < Data.Length; ++i)
			{
				var newValue = (double)Data[i] * value;
				Data[i] = Convert.ToInt32(newValue + error);
				error = newValue - Data[i];
			}
		}

		public int GetAddress(double value)
		{
			return Convert.ToInt32(Math.Min(1, Math.Max(0, value)) * (Data.Length - 1));
		}

		public double GetValue(int index)
		{
			return (double)index / (Data.Length - 1);
		}

		public double[] KMeans(int k)
		{
			return KMeans(Data, k);
		}

		public double Min(int threshold = 0)
		{
			for (var i = 1; i < Data.Length; ++i)
				if (Data[i] > threshold)
					return GetValue(i);
			return -1;
		}

		public double Max(int threshold = 0)
		{
			for (var i = Data.Length - 1; i >= 0; --i)
				if (Data[i] > threshold)
					return GetValue(i);
			return -1;
		}

		/// <summary> K-means of a 0..1 histogram. </summary>
		/// <remarks> 
		///		0. initialize groups to distinct actual values; 
		///		1. determine each value's closest group; 
		///		2. move the group to the average of it's members; 
		///		3. repeat until the groups don't move. 
		///	</remarks>
		private double[] KMeans(int[] histogram, int k)
		{
			var groupMeans = InitialGroupMeans(histogram, k);
			System.Diagnostics.Debug.Assert(groupMeans.All(m => m >= 0), "Cannot compute the KMeans with no data.");
			var groupIndexes = new int[histogram.Length];
			var iterations = 0;
			while (true)
			{
				var changed = false;
				UpdateGroups(groupMeans, groupIndexes);
				for (var groupIndex = 0; groupIndex < groupMeans.Length; ++groupIndex)
				{
					var newMean = MeanForGroup(histogram, groupIndexes, groupIndex, groupMeans[groupIndex]);
					changed |= !Approximately(newMean, groupMeans[groupIndex]);
					groupMeans[groupIndex] = newMean;
				}
				if (!changed || ++iterations >= MaxIterations)
					return groupMeans;
			}
		}

		private double[] InitialGroupMeans(int[] histogram, int k)
		{
			var interval = histogram.Length / k;
			return Enumerable.Range(0, k)
				.Select(r => GetValue(IndexOfNearestNonEmpty(histogram, r * interval + (interval / 2))))
				.ToArray();
		}

		private int IndexOfNearestNonEmpty(int[] histogram, int start)
		{
			for (var i = start; i >= 0 || start + (start - i) < histogram.Length; --i)
			{ 
				if (i >= 0 && histogram[i] > 0)
					return i;
				var j = start + (start - i);
				if (j < histogram.Length && histogram[j] > 0)
					return j;
			}
			return -1;
		}

		private double MeanForGroup(int[] histogram, int[] groupIndexes, int groupIndex, double defaultMean)
		{
			var count = 0;
			var sum = 0.0;
			for (var j = 0; j < groupIndexes.Length; ++j)
			{
				if (groupIndexes[j] == groupIndex)
				{
					count += histogram[j];
					sum += histogram[j] * GetValue(j);
				}
			}
			return count == 0 ? defaultMean : sum / count;
		}

		private void UpdateGroups(double[] groupMeans, int[] groupIndexes)
		{
			for (var i = 0; i < groupIndexes.Length; ++i)
			{
				groupIndexes[i] = GetNearestGroupIndex(GetValue(i), groupMeans);
			}
		}

		private int GetNearestGroupIndex(double value, double[] groupMeans)
		{
			var bestIndex = -1;
			var best = double.MaxValue;
			for (var i = 0; i < groupMeans.Length; ++i)
			{
				var distance = Math.Abs(groupMeans[i] - value);
				if (distance < best || (distance == best && i % 2 == 0))	// If equidistant, alternate winning
				{
					bestIndex = i;
					best = distance;
				}
			}
			return bestIndex;
		}

		public static bool Approximately(double a, double b)
		{
			return Math.Abs(b - a) < Math.Max(double.Epsilon * Math.Max(Math.Abs(a), Math.Abs(b)), double.Epsilon * 8);
		}
	}
}
