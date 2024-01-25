using System.Drawing;

namespace SCLCoreCLR;

public struct IntVec2
{
	public int X;

	public int Y;

	public IntVec2(int x, int y)
	{
		X = x;
		Y = y;
	}

	public IntVec2(Point pt)
	{
		X = pt.X;
		Y = pt.Y;
	}

	public static implicit operator Point(IntVec2 v)
	{
		return new Point(v.X, v.Y);
	}

	public static IntVec2 operator +(IntVec2 v0, IntVec2 v1)
	{
		return new IntVec2(v0.X + v1.X, v0.Y + v1.Y);
	}

	public static IntVec2 operator +(IntVec2 v, Size size)
	{
		return new IntVec2(v.X + size.Width, v.Y + size.Height);
	}

	public static IntVec2 operator -(IntVec2 v0, IntVec2 v1)
	{
		return new IntVec2(v0.X - v1.X, v0.Y - v1.Y);
	}

	public static IntVec2 operator *(IntVec2 v, float f)
	{
		return new IntVec2((int)((float)v.X * f), (int)((float)v.Y * f));
	}

	public static IntVec2 operator /(IntVec2 v, float f)
	{
		return new IntVec2((int)((float)v.X / f), (int)((float)v.Y / f));
	}

	public override string ToString()
	{
		return X + ", " + Y;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is IntVec2 intVec))
		{
			return false;
		}
		if (X == intVec.X)
		{
			return Y == intVec.Y;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return X.GetHashCode() ^ Y.GetHashCode();
	}

	public static bool operator ==(IntVec2 a, IntVec2 b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(IntVec2 a, IntVec2 b)
	{
		return !a.Equals(b);
	}
}
