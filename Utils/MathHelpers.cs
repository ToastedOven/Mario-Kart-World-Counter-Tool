using Godot;
using OpenCvSharp;

namespace CounterTool.Utils;

public static class MathHelpers
{
    extension(Vec2f vector)
    {
        public float Magnitude() => Mathf.Sqrt(Mathf.Pow(vector.Item0, 2) + Mathf.Pow(vector.Item1, 2));

        public Vec2f Normalized()
        {
            var magnitude = vector.Magnitude();
            return new Vec2f(vector.Item0 / magnitude, vector.Item1 / magnitude);
        }

        public float DotProduct(Vec2f other) => vector.Item0 * other.Item0 + vector.Item1 * other.Item1;

        public Point ToPoint() => new Point(vector.Item0, vector.Item1);
    }

    extension(Rect rect)
    {
        public bool IntersectsWith(Point rayOrigin, Vec2f rayDirection)
        {
            var dist = new Point(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f).DistanceTo(rayOrigin);
            var hit = rayOrigin + (rayDirection * dist).ToPoint();
            
            var min = rect.TopLeft;
            var max = rect.BottomRight;

            return min.X <= hit.X && hit.X <= max.X && min.Y <= hit.Y && hit.Y <= max.Y;
        }
    }
}