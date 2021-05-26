using System;
using UnityEngine;

public class Utility : MonoBehaviour
{
    public static float Lerp(float from, float to, float by)
    {
        return from + ((to - from) * by);
    }

    public static IComparable Bound(IComparable value, IComparable low, IComparable high)
    {
        if (value.CompareTo(low) < 0)
        {
            return low;
        }
        if (value.CompareTo(high) > 0)
        {
            return high;
        }
        return value;
    }

    public static bool IsAbout(float target, float value, float threshold)
    {
        return (Math.Abs(target - value) < Math.Abs(threshold));
    }

    public static float SnapValue(float value, float snap)
    {
        if (value > 0)
        {
            return Mathf.RoundToInt(value * snap) / snap;
        }
        else
        {
            return -Mathf.RoundToInt(Math.Abs(value) * snap) / snap;
        }
    }
    
    // find radian angle
    public static float AngleInRad(Vector3 vec1, Vector3 vec2)
    {
        return Mathf.Atan2(vec2.y - vec1.y, vec2.x - vec1.x);
    }

    // find degree angle
    public static float AngleInDeg(Vector3 vec1, Vector3 vec2)
    {
        return Mathf.Atan2(vec2.y - vec1.y, vec2.x - vec1.x) * 180 / Mathf.PI;
    }
}