using System;
using System.Collections.Generic;

namespace SCLCoreCLR;

public class NativeToCLRMap<T>
{
	private Dictionary<long, WeakReference> m_Map = new Dictionary<long, WeakReference>();

	public void Add(long native_object, T clr_object)
	{
		m_Map[native_object] = new WeakReference(clr_object);
	}

	public void Remove(long native_object)
	{
		m_Map.Remove(native_object);
	}

	public T Get(long native_object)
	{
		return (T)m_Map[native_object].Target;
	}
}
