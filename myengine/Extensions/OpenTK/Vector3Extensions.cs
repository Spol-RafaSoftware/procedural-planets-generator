﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

using OpenTK;

namespace MyEngine
{
    public static partial class Vector3Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 RotateBy(this Vector3 vector, Quaternion rotation)
        {
            Matrix4 rot = Matrix4.CreateFromQuaternion(rotation);
            Vector3 newDirection;
            Vector3.TransformVector(ref vector, ref rot, out newDirection);
            return newDirection;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 CompomentWiseMult(this Vector3 a, Vector3 b)
        {
            return new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(this Vector3 a, Vector3 b)
        {
            return (a - b).Length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSqr(this Vector3 a, Vector3 b)
        {
            return (a - b).LengthSquared;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Cross(this Vector3 a, Vector3 b)
        {
            return Vector3.Cross(a, b);

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(this Vector3 a, Vector3 b)
        {
            float result;
            Vector3.Dot(ref a, ref b, out result);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Multiply(this Vector3 a, float scale)
        {
            return Vector3.Multiply(a, scale);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Divide(this Vector3 a, float scale)
        {
            Vector3.Divide(ref a, scale, out a);
            //return Vector3.Divide(a, scale);
            return a;
        }
        /// <summary>
        /// returns -1.0 if x is less than 0.0, 0.0 if x is equal to 0.0, and +1.0 if x is greater than 0.0.
        /// https://www.opengl.org/sdk/docs/man/html/sign.xhtml
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Sign(this Vector3 a)
        {
            var ret = new Vector3(0, 0, 0);

            if (a.X > 0) ret.X = 1;
            else if (a.X < 0) ret.X = -1;

            if (a.Y > 0) ret.Y = 1;
            else if (a.Y < 0) ret.Y = -1;

            if (a.Z > 0) ret.Z = 1;
            else if (a.Z < 0) ret.Z = -1;

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Abs(this Vector3 a)
        {
            if (a.X < 0) a.X *= -1;
            if (a.Y < 0) a.Y *= -1;
            if (a.Z < 0) a.Z *= -1;
            return a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Towards(this Vector3 from, Vector3 to)
        {
            return to-from;
        }

        // http://stackoverflow.com/questions/12435671/quaternion-lookat-function
        // http://gamedev.stackexchange.com/questions/15070/orienting-a-model-to-face-a-target
        public static Quaternion LookRot(this Vector3 dir)
        {
            var up = Constants.Vector3Up;
            var fwd = Constants.Vector3Forward;
            dir.Normalize();
            float dot = Vector3.Dot(fwd, dir);

            if (Math.Abs(dot - (-1.0f)) < 0.000001f)
            {
                return new Quaternion(up.X, up.Y, up.Z, 3.1415926535897932f);
            }
            if (Math.Abs(dot - (1.0f)) < 0.000001f)
            {
                return Quaternion.Identity;
            }

            float rotAngle = (float)Math.Acos(dot);
            Vector3 rotAxis = Vector3.Cross(fwd, dir);
            rotAxis = Vector3.Normalize(rotAxis);
            return Quaternion.FromAxisAngle(rotAxis, rotAngle);
        }


    }
}
