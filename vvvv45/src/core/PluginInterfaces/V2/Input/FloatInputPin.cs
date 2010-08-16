﻿using System;
using VVVV.PluginInterfaces.V1;

namespace VVVV.PluginInterfaces.V2.Input
{
	public class FloatInputPin : ValueInputPin<float>
	{
		public FloatInputPin(IPluginHost host, InputAttribute attribute)
			:base(host, attribute)
		{
		}
		
		public override float this[int index] 
		{
			get 
			{
				return (float) FData[index % FSliceCount];
			}
			set 
			{
				if (!FValueFastIn.IsConnected)
					FData[index % FSliceCount] = (double) value;
			}
		}
	}
	
	public class ObservableFloatInputPin : ObservableValueInputPin<float>
	{
		public ObservableFloatInputPin(IPluginHost host, InputAttribute attribute)
			:base(host, attribute)
		{
		}
		
		public override float this[int index] 
		{
			get 
			{
				return (float) FData[index % FSliceCount];
			}
			set 
			{
				if (!FValueIn.IsConnected)
					FData[index % FSliceCount] = (double) value;
			}
		}
	}
}
