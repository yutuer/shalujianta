using System.Linq;
using Godot;

namespace FishEatFish.Battle.HexMap
{
    public readonly struct HexCoord : System.IEquatable<HexCoord>
    {
        public readonly int Q;
        public readonly int R;

        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int S => -Q - R;

        public static readonly HexCoord North = new HexCoord(0, -1);
        public static readonly HexCoord South = new HexCoord(0, 1);
        public static readonly HexCoord NorthEast = new HexCoord(1, -1);
        public static readonly HexCoord NorthWest = new HexCoord(-1, 0);
        public static readonly HexCoord SouthEast = new HexCoord(1, 0);
        public static readonly HexCoord SouthWest = new HexCoord(-1, 1);

        public static readonly HexCoord[] Directions = new[]
        {
            North, NorthEast, SouthEast,
            South, SouthWest, NorthWest
        };

        public HexCoord[] GetNeighbors()
        {
            return new HexCoord[]
            {
                this + North,
                this + NorthEast,
                this + SouthEast,
                this + South,
                this + SouthWest,
                this + NorthWest
            };
        }

        public HexCoord GetNeighbor(int direction)
        {
            return this + Directions[direction];
        }

        public static HexCoord operator +(HexCoord a, HexCoord b)
        {
            return new HexCoord(a.Q + b.Q, a.R + b.R);
        }

        public static HexCoord operator -(HexCoord a, HexCoord b)
        {
            return new HexCoord(a.Q - b.Q, a.R - b.R);
        }

        public static HexCoord operator *(HexCoord a, int multiplier)
        {
            return new HexCoord(a.Q * multiplier, a.R * multiplier);
        }

        public static bool operator ==(HexCoord left, HexCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HexCoord left, HexCoord right)
        {
            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            if (obj is HexCoord other)
            {
                return Equals(other);
            }
            return false;
        }

        public bool Equals(HexCoord other)
        {
            return Q == other.Q && R == other.R;
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(Q, R);
        }

        public override string ToString()
        {
            return $"({Q}, {R})";
        }

        public int DistanceTo(HexCoord other)
        {
            return (System.Math.Abs(Q - other.Q) +
                    System.Math.Abs(R - other.R) +
                    System.Math.Abs(S - other.S)) / 2;
        }

        public bool IsWithinRadius(int radius)
        {
            return DistanceTo(new HexCoord(0, 0)) <= radius;
        }

        public static HexCoord FromAxial(int q, int r)
        {
            return new HexCoord(q, r);
        }

        public (int q, int r) ToAxial()
        {
            return (Q, R);
        }

        public static HexCoord Round(float q, float r)
        {
            float s = -q - r;

            int rq = (int)System.Math.Round(q);
            int rr = (int)System.Math.Round(r);
            int rs = (int)System.Math.Round(s);

            float qDiff = System.Math.Abs(rq - q);
            float rDiff = System.Math.Abs(rr - r);
            float sDiff = System.Math.Abs(rs - s);

            if (qDiff > rDiff && qDiff > sDiff)
            {
                rq = -rr - rs;
            }
            else if (rDiff > sDiff)
            {
                rr = -rq - rs;
            }

            return new HexCoord(rq, rr);
        }

        public bool ContainsInArray(HexCoord[] array)
        {
            return array.Contains(this);
        }

        public HexCoord GetDirectionTo(HexCoord target)
        {
            int dq = target.Q - Q;
            int dr = target.R - R;

            if (dq == 0 && dr == 0)
                return new HexCoord(0, 0);

            float length = System.Math.Max(System.Math.Abs(dq), System.Math.Max(System.Math.Abs(dr), System.Math.Abs(-dq - dr)));
            if (length == 0)
                return new HexCoord(0, 0);

            dq = (int)System.Math.Round(dq / length);
            dr = (int)System.Math.Round(dr / length);

            return new HexCoord(dq, dr);
        }

        public int GetDirectionIndex(HexCoord target)
        {
            var direction = GetDirectionTo(target);

            for (int i = 0; i < Directions.Length; i++)
            {
                if (Directions[i] == direction)
                    return i;
            }

            return -1;
        }

        public (int col, int row) ToOffset()
        {
            int col = Q + (R + (R & 1)) / 2;
            int row = R;
            return (col, row);
        }

        public static HexCoord FromOffset(int col, int row)
        {
            int q = col - (row - (row & 1)) / 2;
            int r = row;
            return new HexCoord(q, r);
        }
    }
}
