﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;

using MyEngine;
using MyEngine.Components;

namespace MyGame
{
    public class PlanetaryBody : ComponentWithShortcuts
    {
        public int chunkNumberOfVerticesOnEdge = 10;
        public float radius;
        public int subdivisionMaxRecurisonDepth = 10;
        public volatile Material planetMaterial;
        public float radiusVariation = 20;
        public float subdivisionSphereRadiusModifier = 0.5f;
        public float startingRadiusSubdivisionModifier = 1;

        const bool debugSameHeightEverywhere = false; // DEBUG

        Perlin perlin;
        Worley worley;

        List<PlanetaryBodyChunk> rootChunks = new List<PlanetaryBodyChunk>();



        public PlanetaryBody(Entity entity) : base(entity)
        {
            perlin = new Perlin(5646);
            worley = new Worley(894984, Worley.DistanceFunction.Euclidian);

            entity.EventSystem.Register((MyEngine.Events.GraphicsUpdate evt) => OnGraphicsUpdate(evt.DeltaTime));
        }



        // http://stackoverflow.com/questions/1185408/converting-from-longitude-latitude-to-cartesian-coordinates
        public SpehricalCoord CalestialToSpherical(Vector3 c)
        {
            var r = c.Length;
            if (r == 0) return new SpehricalCoord(0, 0, 0);
            return new SpehricalCoord(
                (float)Math.Atan2(c.Z, c.X),
                (float)Math.Asin(c.Y / r),
                r
            );
        }

        // x = left right = longitude
        // y = top down = latitude
        // z = radius
        public Vector3 SphericalToCalestial(SpehricalCoord s)
        {
            var r = s.altitude;
            //s.latitude = s.latitude / 180.0f * M_PI;
            //s.longitude = s.longitude / 180.0f * M_PI;
            //if (r == 0) r = radius;
            return new Vector3(
                (float)Math.Cos(s.latitude) * (float)Math.Cos(s.longitude) * r,
                (float)Math.Sin(s.latitude) * r,
                (float)Math.Cos(s.latitude) * (float)Math.Sin(s.longitude) * r
            );
        }



        public Vector3 GetFinalPos(Vector3 calestialPos, int detailDensity = 1)
        {
                var s = CalestialToSpherical(calestialPos);
                s.altitude = GetHeight(calestialPos, detailDensity);
                return SphericalToCalestial(s);
        }


        public float GetHeight(Vector3 calestialPos, int detailDensity = 1)
        {
            if(debugSameHeightEverywhere)
            {
                return radius;
            }

            var initialPos = calestialPos.Normalized();
            var pos = initialPos;

            int octaves = 2;
            float freq = 10;
            float ampModifier = .05f;
            float freqModifier = 15;
            float result = 0.0f;
            float amp = radiusVariation;
            pos *= freq;
            for (int i = 0; i < octaves; i++)
            {
                result += perlin.Get3D(pos) * amp;
                pos *= freqModifier;
                amp *= ampModifier;
            }

            {
                // hill tops
                var p = perlin.Get3D(initialPos * 10.0f);
                if (p > 0) result -= p * radiusVariation * 2;
            }

            {
                // craters
                var p = worley.GetAt(initialPos * 2.0f, 1);
                result += MyMath.SmoothStep(0.0f, 0.1f, p[0]) * radiusVariation * 2;
            }

            result += radius;
            return result;

            /*
            int octaves = 4;
            float sum = 0.5;
            float freq = 1.0, amp = 1.0;
            vec2 dsum = vec2(0);
            for (int i=0; i < octaves; i++)
            {
                Vector3 n = perlin.Get3D(calestialPos*freq);
                dsum += vec2(n.y, n.z);
                sum += amp * n.x / (1 + dot(dsum, dsum));
                freq *= lacunarity;
                amp *= gain;
            }
            return sum;
            */
        }


        List<Vector3> vertices;
        void FACE(int A, int B, int C)
        {

            var child = new PlanetaryBodyChunk(this, null);
            child.noElevationRange.a = vertices[A];
            child.noElevationRange.b = vertices[B];
            child.noElevationRange.c = vertices[C];
            child.realVisibleRange.a = GetFinalPos(child.noElevationRange.a);
            child.realVisibleRange.b = GetFinalPos(child.noElevationRange.b);
            child.realVisibleRange.c = GetFinalPos(child.noElevationRange.c);
            this.rootChunks.Add(child);
        }



        public void Start()
        {
            //detailLevel = (int)ceil(planetInfo.rootChunks[0].range.ToBoundingSphere().radius / 100);

            vertices = new List<Vector3>();
            var indicies = new List<uint>();


            var r = this.radius / 2.0f;

            var t = (1 + MyMath.Sqrt(5.0f)) / 2.0f * r;
            var d = r;


            vertices.Add(new Vector3(-d, t, 0));
            vertices.Add(new Vector3(d, t, 0));
            vertices.Add(new Vector3(-d, -t, 0));
            vertices.Add(new Vector3(d, -t, 0));

            vertices.Add(new Vector3(0, -d, t));
            vertices.Add(new Vector3(0, d, t));
            vertices.Add(new Vector3(0, -d, -t));
            vertices.Add(new Vector3(0, d, -t));

            vertices.Add(new Vector3(t, 0, -d));
            vertices.Add(new Vector3(t, 0, d));
            vertices.Add(new Vector3(-t, 0, -d));
            vertices.Add(new Vector3(-t, 0, d));



            // 5 faces around point 0
            FACE(0, 11, 5);
            FACE(0, 5, 1);
            FACE(0, 1, 7);
            FACE(0, 7, 10);
            FACE(0, 10, 11);

            // 5 adjacent faces
            FACE(1, 5, 9);
            FACE(5, 11, 4);
            FACE(11, 10, 2);
            FACE(10, 7, 6);
            FACE(7, 1, 8);

            // 5 faces around point 3
            FACE(3, 9, 4);
            FACE(3, 4, 2);
            FACE(3, 2, 6);
            FACE(3, 6, 8);
            FACE(3, 8, 9);

            // 5 adjacent faces
            FACE(4, 9, 5);
            FACE(2, 4, 11);
            FACE(6, 2, 10);
            FACE(8, 6, 7);
            FACE(9, 8, 1);



        }




        void OnGraphicsUpdate(double deltaTime)
        {


        }


        void StopMeshGenerationInChilds(PlanetaryBodyChunk chunk)
        {
            foreach (var child in chunk.childs)
            {
                child.StopMeshGeneration();
                StopMeshGenerationInChilds(child);
            }
        }

        void TrySubdivideToLevel_Generation(PlanetaryBodyChunk chunk, Sphere sphere, int recursionDepth)
        {
            if (recursionDepth > 0 && GeometryUtility.Intersects(chunk.realVisibleRange, sphere))
            {
                chunk.SubDivide();
                chunk.StopMeshGeneration();
                sphere.radius *= subdivisionSphereRadiusModifier * 1.1f;
                foreach (var child in chunk.childs)
                {
                    TrySubdivideToLevel_Generation(child, sphere, recursionDepth - 1);
                }
            }
            else
            {
                if (chunk.renderer == null)
                {
                    chunk.RequestMeshGeneration();
                }
                StopMeshGenerationInChilds(chunk);
            }

        }

        void HideInChilds(PlanetaryBodyChunk chunk)
        {
            foreach (var child in chunk.childs)
            {
                if (child.renderer != null && child.renderer.RenderingMode != RenderingMode.DontRender) child.renderer.RenderingMode = RenderingMode.DontRender;
                HideInChilds(child);
            }
        }


        // return true if all childs are visible
        bool TrySubdivideToLevel_Visibility(PlanetaryBodyChunk chunk, Sphere sphere, int recursionDepth)
        {
            if (recursionDepth > 0 && GeometryUtility.Intersects(chunk.realVisibleRange, sphere))
            {
                var areChildrenFullyVisible = true;
                chunk.SubDivide();
                sphere.radius *= subdivisionSphereRadiusModifier;
                foreach (var child in chunk.childs)
                {
                    areChildrenFullyVisible &= TrySubdivideToLevel_Visibility(child, sphere, recursionDepth - 1);
                }

                // hide only if all our childs are visible, they mighht still be generating
                if (areChildrenFullyVisible) if (chunk.renderer != null) chunk.renderer.RenderingMode = RenderingMode.DontRender;

                return areChildrenFullyVisible;
            }
            else
            {
                if (chunk.renderer == null) return false;

                // end node, we must show this one and hide all childs or parents, parents should already be hidden
                if (chunk.renderer.RenderingMode != RenderingMode.RenderGeometryAndCastShadows)
                {
                    // is not visible
                    // show it
                    chunk.renderer.RenderingMode = RenderingMode.RenderGeometryAndCastShadows;
                }

                // if visible, update final positions weight according to distance
                if (chunk.renderer.RenderingMode == RenderingMode.RenderGeometryAndCastShadows)
                {
                    float d = chunk.renderer.bounds.Center.Distance(Scene.mainCamera.Transform.Position);
                    float e0 = sphere.radius / subdivisionSphereRadiusModifier;
                    float e1 = e0 * subdivisionSphereRadiusModifier;
                    float w = MyMath.SmoothStep(e0, e1, d);
                    chunk.renderer.Material.Uniforms.Set("param_finalPosWeight", w);
                    //chunk.renderer.Material.Uniforms.Set("param_finalPosWeight", DebugKeys.keyOL);
                }

                HideInChilds(chunk);

                return true;
            }

        }


        public void TrySubdivideOver(Vector3 pos)
        {
            var sphere = new Sphere(pos - Transform.Position, this.radius * startingRadiusSubdivisionModifier);
            foreach (PlanetaryBodyChunk rootChunk in this.rootChunks)
            {
                TrySubdivideToLevel_Generation(rootChunk, sphere, this.subdivisionMaxRecurisonDepth);
                TrySubdivideToLevel_Visibility(rootChunk, sphere, this.subdivisionMaxRecurisonDepth);
            }
        }


    }
}