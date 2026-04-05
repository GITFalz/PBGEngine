using System.Runtime.InteropServices;
using PBG.Core;
using PBG.Parse;

namespace PBG.MathLibrary
{
    public struct Matrix4 : ISceneSerializable, IVector<float>
    {
        public float M11, M21, M31, M41;

        public float M12, M22, M32, M42;

        public float M13, M23, M33, M43;

        public float M14, M24, M34, M44;

        public static readonly uint ByteSize = (uint)Marshal.SizeOf<Matrix4>();

        public static readonly Matrix4 Identity = new Matrix4(
            1,0,0,0,
            0,1,0,0,
            0,0,1,0,
            0,0,0,1
        );

        public static readonly Matrix4 Zero = new Matrix4(
            0,0,0,0,
            0,0,0,0,
            0,0,0,0,
            0,0,0,0
        );

        public readonly int Count => 16; 
        public readonly IVector<float> Default => Identity;

        public Matrix4(
            float m11, float m21, float m31, float m41,
            float m12, float m22, float m32, float m42,
            float m13, float m23, float m33, float m43,
            float m14, float m24, float m34, float m44)
        {
            M11=m11; M21=m21; M31=m31; M41=m41;
            M12=m12; M22=m22; M32=m32; M42=m42;
            M13=m13; M23=m23; M33=m33; M43=m43;
            M14=m14; M24=m24; M34=m34; M44=m44;
        }

        public float this[int index]
        {
            readonly get 
            {
                return index switch
                {
                    0  => M11, 1  => M21, 2  => M31, 3  => M41,
                    4  => M12, 5  => M22, 6  => M32, 7  => M42,
                    8  => M13, 9  => M23, 10 => M33, 11 => M43,
                    12 => M14, 13 => M24, 14 => M34, 15 => M44,
                    _ => throw new IndexOutOfRangeException($"[Error:({GetType().Name})] : Unknown index '{index}'")
                };
            }
            set
            {
                switch (index)
                {
                    case 0:  M11 = value; break; case 1:  M21 = value; break; case 2:  M31 = value; break; case 3:  M41 = value; break;
                    case 4:  M12 = value; break; case 5:  M22 = value; break; case 6:  M32 = value; break; case 7:  M42 = value; break;
                    case 8:  M13 = value; break; case 9:  M23 = value; break; case 10: M33 = value; break; case 11: M43 = value; break;
                    case 12: M14 = value; break; case 13: M24 = value; break; case 14: M34 = value; break; case 15: M44 = value; break;
                    default: throw new IndexOutOfRangeException($"[Error:({GetType().Name})] : Unknown index '{index}'");
                }
            }
        }

        public static Matrix4 CreateTranslation(float x, float y, float z) =>
            new Matrix4(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                x, y, z, 1
            );

        public static Matrix4 CreateTranslation(Vector3 v) =>
            CreateTranslation(v.X, v.Y, v.Z);

        public static Matrix4 CreateScale(float x, float y, float z) =>
            new Matrix4(
                x, 0, 0, 0,
                0, y, 0, 0,
                0, 0, z, 0,
                0, 0, 0, 1
            );

        public static Matrix4 CreateScale(Vector3 s) =>
            CreateScale(s.X, s.Y, s.Z);

        public static Matrix4 CreateScale(float uniform) =>
            CreateScale(uniform, uniform, uniform);

        public static Matrix4 CreateRotationX(float angleRad)
        {
            float c = MathF.Cos(angleRad), s = MathF.Sin(angleRad);
            return new Matrix4(
                1,  0, 0, 0,
                0,  c,-s, 0,
                0,  s, c, 0,
                0,  0, 0, 1
            );
        }

        public static Matrix4 CreateRotationY(float angleRad)
        {
            float c = MathF.Cos(angleRad), s = MathF.Sin(angleRad);
            return new Matrix4(
                c, 0, s, 0,
                0, 1, 0, 0,
                -s, 0, c, 0,
                0, 0, 0, 1
            );
        }

        public static Matrix4 CreateRotationZ(float angleRad)
        {
            float c = MathF.Cos(angleRad), s = MathF.Sin(angleRad);
            return new Matrix4(
                c, -s, 0, 0,
                s,  c, 0, 0,
                0,  0, 1, 0,
                0,  0, 0, 1
            );
        }

        public static Matrix4 CreateFromQuaternion(Quaternion q)
        {
            q = q.Normalized();
            float x=q.X, y=q.Y, z=q.Z, w=q.W;

            return new Matrix4(
                1-2*(y*y+z*z),   2*(x*y+w*z),   2*(x*z-w*y), 0,
                  2*(x*y-w*z), 1-2*(x*x+z*z),   2*(y*z+w*x), 0,
                  2*(x*z+w*y),   2*(y*z-w*x), 1-2*(x*x+y*y), 0,
                0,             0,             0,             1
            );
        }

        public static Matrix4 CreateTRS(Vector3 translation, Quaternion rotation, Vector3 scale) =>
            CreateScale(scale) * CreateFromQuaternion(rotation) * CreateTranslation(translation);

        public static Matrix4 CreatePerspective(float fovYRad, float aspect, float near, float far)
        {
            float tanHalfFov = MathF.Tan(fovYRad * 0.5f);
            float m00 = 1f / (aspect * tanHalfFov);
            float m11 = -1f / tanHalfFov; // flip Y for Vulkan
            float m22 = far / (near - far);
            float m23 = -1f;
            float m32 = -(far * near) / (far - near);

            return new Matrix4(
                m00,  0,   0,   0,
                  0,m11,   0,   0,
                  0,  0, m22, m23,
                  0,  0, m32,   0
            );
        }

        public static Matrix4 CreateOrthographic(float width, float height, float near, float far)
        {
            return CreateOrthographicOffCenter(-width/2f, width/2f, -height/2f, height/2f, near, far);
        }

        public static Matrix4 CreateOrthographicOffCenter(float left, float right, float bottom, float top, float near, float far)
        {
            float rml = right - left;
            float tmb = top   - bottom;
            float fmn = far   - near;

            return new Matrix4(
                2f/rml,              0,              0, 0,
                    0,        2f/tmb,              0, 0,
                    0,              0,        -1f/fmn, 0,
                -(right+left)/rml, -(top+bottom)/tmb, -near/fmn, 1
            );
        }

        public Quaternion ExtractRotation()
        {
            Vector3 col0 = Vector3.Normalize(new Vector3(M11, M21, M31));
            Vector3 col1 = Vector3.Normalize(new Vector3(M12, M22, M32));
            Vector3 col2 = Vector3.Normalize(new Vector3(M13, M23, M33));

            float trace = col0.X + col1.Y + col2.Z;

            if (trace > 0f)
            {
                float s = 0.5f / MathF.Sqrt(trace + 1f);
                return new Quaternion(
                    (col2.Y - col1.Z) * s,
                    (col0.Z - col2.X) * s,
                    (col1.X - col0.Y) * s,
                    0.25f / s
                );
            }
            else if (col0.X > col1.Y && col0.X > col2.Z)
            {
                float s = 2f * MathF.Sqrt(1f + col0.X - col1.Y - col2.Z);
                return new Quaternion(
                    0.25f * s,
                    (col0.Y + col1.X) / s,
                    (col0.Z + col2.X) / s,
                    (col2.Y - col1.Z) / s
                );
            }
            else if (col1.Y > col2.Z)
            {
                float s = 2f * MathF.Sqrt(1f + col1.Y - col0.X - col2.Z);
                return new Quaternion(
                    (col0.Y + col1.X) / s,
                    0.25f * s,
                    (col1.Z + col2.Y) / s,
                    (col0.Z - col2.X) / s
                );
            }
            else
            {
                float s = 2f * MathF.Sqrt(1f + col2.Z - col0.X - col1.Y);
                return new Quaternion(
                    (col0.Z + col2.X) / s,
                    (col1.Z + col2.Y) / s,
                    0.25f * s,
                    (col1.X - col0.Y) / s
                );
            }
        }

        public static Matrix4 CreateLookAt(Vector3 eye, Vector3 target, Vector3 up)
        {
            Vector3 f = Vector3.Normalize(target - eye);
            Vector3 r = Vector3.Normalize(Vector3.Cross(f, up));
            Vector3 u = Vector3.Cross(r, f);

            return new Matrix4(
                     r.X,      u.X,     -f.X, 0,
                     r.Y,      u.Y,     -f.Y, 0,
                     r.Z,      u.Z,     -f.Z, 0,
                -Vector3.Dot(r,eye), -Vector3.Dot(u,eye), Vector3.Dot(f,eye), 1
            );
        }

        public Matrix4 Transposed() =>
            new Matrix4(
                M11, M12, M13, M14,
                M21, M22, M23, M24,
                M31, M32, M33, M34,
                M41, M42, M43, M44
            );

        public Matrix4 Inverted()
        {
            if (Invert(out var m))
                return m;
            return this;
        }

        public bool Invert(out Matrix4 result)
        {
            float c11 =  (M22 * (M33 * M44 - M34 * M43) - M23 * (M32 * M44 - M34 * M42) + M24 * (M32 * M43 - M33 * M42));
            float c12 = -(M21 * (M33 * M44 - M34 * M43) - M23 * (M31 * M44 - M34 * M41) + M24 * (M31 * M43 - M33 * M41));
            float c13 =  (M21 * (M32 * M44 - M34 * M42) - M22 * (M31 * M44 - M34 * M41) + M24 * (M31 * M42 - M32 * M41));
            float c14 = -(M21 * (M32 * M43 - M33 * M42) - M22 * (M31 * M43 - M33 * M41) + M23 * (M31 * M42 - M32 * M41));

            float det = M11 * c11 + M12 * c12 + M13 * c13 + M14 * c14;

            if (MathF.Abs(det) < 1e-6f)
            {
                result = default;
                return false; // Matrix is singular, cannot be inverted
            }

            float invDet = 1.0f / det;

            result = new Matrix4
            {
                M11 =  c11 * invDet,
                M12 =  c12 * invDet,
                M13 =  c13 * invDet,
                M14 =  c14 * invDet,

                M21 = -(M12 * (M33 * M44 - M34 * M43) - M13 * (M32 * M44 - M34 * M42) + M14 * (M32 * M43 - M33 * M42)) * invDet,
                M22 =  (M11 * (M33 * M44 - M34 * M43) - M13 * (M31 * M44 - M34 * M41) + M14 * (M31 * M43 - M33 * M41)) * invDet,
                M23 = -(M11 * (M32 * M44 - M34 * M42) - M12 * (M31 * M44 - M34 * M41) + M14 * (M31 * M42 - M32 * M41)) * invDet,
                M24 =  (M11 * (M32 * M43 - M33 * M42) - M12 * (M31 * M43 - M33 * M41) + M13 * (M31 * M42 - M32 * M41)) * invDet,

                M31 =  (M12 * (M23 * M44 - M24 * M43) - M13 * (M22 * M44 - M24 * M42) + M14 * (M22 * M43 - M23 * M42)) * invDet,
                M32 = -(M11 * (M23 * M44 - M24 * M43) - M13 * (M21 * M44 - M24 * M41) + M14 * (M21 * M43 - M23 * M41)) * invDet,
                M33 =  (M11 * (M22 * M44 - M24 * M42) - M12 * (M21 * M44 - M24 * M41) + M14 * (M21 * M42 - M22 * M41)) * invDet,
                M34 = -(M11 * (M22 * M43 - M23 * M42) - M12 * (M21 * M43 - M23 * M41) + M13 * (M21 * M42 - M22 * M41)) * invDet,

                M41 = -(M12 * (M23 * M34 - M24 * M33) - M13 * (M22 * M34 - M24 * M32) + M14 * (M22 * M33 - M23 * M32)) * invDet,
                M42 =  (M11 * (M23 * M34 - M24 * M33) - M13 * (M21 * M34 - M24 * M31) + M14 * (M21 * M33 - M23 * M31)) * invDet,
                M43 = -(M11 * (M22 * M34 - M24 * M32) - M12 * (M21 * M34 - M24 * M31) + M14 * (M21 * M32 - M22 * M31)) * invDet,
                M44 =  (M11 * (M22 * M33 - M23 * M32) - M12 * (M21 * M33 - M23 * M31) + M13 * (M21 * M32 - M22 * M31)) * invDet,
            };

            return true;
        }

        public static Matrix4 operator *(Matrix4 a, Matrix4 b)
        {
            return new Matrix4(
                a.M11*b.M11 + a.M12*b.M21 + a.M13*b.M31 + a.M14*b.M41,
                a.M21*b.M11 + a.M22*b.M21 + a.M23*b.M31 + a.M24*b.M41,
                a.M31*b.M11 + a.M32*b.M21 + a.M33*b.M31 + a.M34*b.M41,
                a.M41*b.M11 + a.M42*b.M21 + a.M43*b.M31 + a.M44*b.M41,

                a.M11*b.M12 + a.M12*b.M22 + a.M13*b.M32 + a.M14*b.M42,
                a.M21*b.M12 + a.M22*b.M22 + a.M23*b.M32 + a.M24*b.M42,
                a.M31*b.M12 + a.M32*b.M22 + a.M33*b.M32 + a.M34*b.M42,
                a.M41*b.M12 + a.M42*b.M22 + a.M43*b.M32 + a.M44*b.M42,

                a.M11*b.M13 + a.M12*b.M23 + a.M13*b.M33 + a.M14*b.M43,
                a.M21*b.M13 + a.M22*b.M23 + a.M23*b.M33 + a.M24*b.M43,
                a.M31*b.M13 + a.M32*b.M23 + a.M33*b.M33 + a.M34*b.M43,
                a.M41*b.M13 + a.M42*b.M23 + a.M43*b.M33 + a.M44*b.M43,

                a.M11*b.M14 + a.M12*b.M24 + a.M13*b.M34 + a.M14*b.M44,
                a.M21*b.M14 + a.M22*b.M24 + a.M23*b.M34 + a.M24*b.M44,
                a.M31*b.M14 + a.M32*b.M24 + a.M33*b.M34 + a.M34*b.M44,
                a.M41*b.M14 + a.M42*b.M24 + a.M43*b.M34 + a.M44*b.M44
            );
        }

        public static Vector4 operator *(Matrix4 m, Vector4 v) =>
            new Vector4(
                m.M11 * v.X + m.M12 * v.Y + m.M13 * v.Z + m.M14 * v.W,
                m.M21 * v.X + m.M22 * v.Y + m.M23 * v.Z + m.M24 * v.W,
                m.M31 * v.X + m.M32 * v.Y + m.M33 * v.Z + m.M34 * v.W,
                m.M41 * v.X + m.M42 * v.Y + m.M43 * v.Z + m.M44 * v.W
            );

        public static Vector4 operator *(Vector4 v, Matrix4 m) =>
            new Vector4(
                v.X * m.M11 + v.Y * m.M21 + v.Z * m.M31 + v.W * m.M41,
                v.X * m.M12 + v.Y * m.M22 + v.Z * m.M32 + v.W * m.M42,
                v.X * m.M13 + v.Y * m.M23 + v.Z * m.M33 + v.W * m.M43,
                v.X * m.M14 + v.Y * m.M24 + v.Z * m.M34 + v.W * m.M44
            );

        public Vector3 TransformPoint(Vector3 v)
        {
            float x = M11*v.X + M12*v.Y + M13*v.Z + M14;
            float y = M21*v.X + M22*v.Y + M23*v.Z + M24;
            float z = M31*v.X + M32*v.Y + M33*v.Z + M34;
            float w = M41*v.X + M42*v.Y + M43*v.Z + M44;
            return new Vector3(x/w, y/w, z/w);
        }

        public Vector3 TransformDirection(Vector3 v)
        {
            return new Vector3(
                M11*v.X + M12*v.Y + M13*v.Z,
                M21*v.X + M22*v.Y + M23*v.Z,
                M31*v.X + M32*v.Y + M33*v.Z
            );
        }

        public Vector4 Transform(Vector4 v) =>
            new Vector4(
                M11*v.X + M12*v.Y + M13*v.Z + M14*v.W,
                M21*v.X + M22*v.Y + M23*v.Z + M24*v.W,
                M31*v.X + M32*v.Y + M33*v.Z + M34*v.W,
                M41*v.X + M42*v.Y + M43*v.Z + M44*v.W
            );

        public float[] ToArray() =>
        [
            M11, M21, M31, M41,
            M12, M22, M32, M42,
            M13, M23, M33, M43,
            M14, M24, M34, M44
        ];

        public void CopyTo(Span<float> span)
        {
            span[0]=M11; span[1]=M21; span[2]=M31; span[3]=M41;
            span[4]=M12; span[5]=M22; span[6]=M32; span[7]=M42;
            span[8]=M13; span[9]=M23; span[10]=M33; span[11]=M43;
            span[12]=M14; span[13]=M24; span[14]=M34; span[15]=M44;
        }

        public override string ToString() =>
            $"[{M11:F3} {M21:F3} {M31:F3} {M41:F3}]\n" +
            $"[{M12:F3} {M22:F3} {M32:F3} {M42:F3}]\n" +
            $"[{M13:F3} {M23:F3} {M33:F3} {M43:F3}]\n" +
            $"[{M14:F3} {M24:F3} {M34:F3} {M44:F3}]";

        public List<string> ToStringList() => 
        [
            M11+"", M21+"", M31+"", M41+"",
            M12+"", M22+"", M32+"", M42+"",
            M13+"", M23+"", M33+"", M43+"",
            M14+"", M24+"", M34+"", M44+""
        ];

        public Matrix4 Set(List<string> list)
        {
            try
            {
                int i = 0;
                float P() => Float.Parse(list[i++]);
                M11 = P(); M21 = P(); M31 = P(); M41 = P();
                M12 = P(); M22 = P(); M32 = P(); M42 = P();
                M13 = P(); M23 = P(); M33 = P(); M43 = P();
                M14 = P(); M24 = P(); M34 = P(); M44 = P();
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("Matrix4 string list was the wrong size, " + list.Count + " instead of 16");
            }
            return this;
        }
        
        public void Deserialize(List<SceneFieldJson> data)
        {
            Set(data, 0, ref M11); Set(data, 1, ref M21); Set(data, 2, ref M31); Set(data, 3, ref M41);
            Set(data, 4, ref M12); Set(data, 5, ref M22); Set(data, 6, ref M32); Set(data, 7, ref M42);
            Set(data, 8, ref M13); Set(data, 9, ref M23); Set(data,10, ref M33); Set(data,11, ref M43);
            Set(data,12, ref M14); Set(data,13, ref M24); Set(data,14, ref M34); Set(data,15, ref M44);
        }
        
        private void Set(List<SceneFieldJson> data, int index, ref float value)
        {
            if (data.Count > index && data[index].TryParse(out float v)) value = v;
        }

        public List<SceneFieldJson> Serialize() => [ 
            new(M11), new(M21), new(M31), new(M41),
            new(M12), new(M22), new(M32), new(M42),
            new(M13), new(M23), new(M33), new(M43),
            new(M14), new(M24), new(M34), new(M44) 
        ];
    }
}