﻿using System;
using VVVV.PluginInterfaces.V1;

namespace VVVV.PluginInterfaces.V2.Output
{
	public class DoubleOutputPin : ValueOutputPin<double>
	{
		public DoubleOutputPin(IPluginHost host, OutputAttribute attribute)
			:base(host, attribute)
		{
		}
		
		public override double this[int index] 
		{
			get 
			{
				return FData[index % FSliceCount];
			}
			set 
			{
				FData[index % FSliceCount] = value;
			}
		}
	}
}
